Shader "BattleSummoner/Painterly/GenshinPainterlyEnvironment"
{
    Properties
    {
        [MainTexture] _BaseMap("Painterly Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Sunlit Warm Tint", Color) = (1.0, 0.98, 0.94, 1.0)
        [Header(Cel Shading Controls)]
        _RampTex("1D Cel Ramp (Optional)", 2D) = "white" {}
        _UseRampTex("Use 1D Ramp Texture", Range(0, 1)) = 0.0
        _ShadowColor("Cool Shadow Tint (Lavender/Slate)", Color) = (0.58, 0.62, 0.76, 1.0)
        _CelThreshold("Shadow Terminator Threshold", Range(0.0, 1.0)) = 0.46
        _CelSoftness("Brushstroke Terminator Feather", Range(0.01, 0.5)) = 0.15
        _AmbientBoost("Ambient Fill Lighting Multiplier", Range(0.2, 2.5)) = 1.15

        [Header(Emissive Glow)]
        _EmissionMap("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0.0, 0.0, 0.0, 1.0)

        [Header(Sunlit Rim Highlight)]
        _RimColor("Golden Rim Highlight", Color) = (1.0, 0.93, 0.78, 1.0)
        _RimPower("Rim Falloff", Range(1.0, 8.0)) = 4.0
        _RimStrength("Rim Intensity", Range(0.0, 1.0)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 250

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                half4  vertexCol  : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _EmissionMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                half _UseRampTex;
                half4 _ShadowColor;
                half _CelThreshold;
                half _CelSoftness;
                half _AmbientBoost;
                half4 _RimColor;
                half _RimPower;
                half _RimStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.vertexCol  = IN.color;
                OUT.fogFactor  = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                float3 N = normalize(IN.normalWS);
                float3 L = normalize(mainLight.direction);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // Spherical Harmonics Ambient Term ensures soft anime shading with NO pitch-black silhouettes
                half3 shAmbient = SampleSH(N) * _AmbientBoost;
                half3 safeAmbient = max(shAmbient, half3(0.24, 0.28, 0.36));

                half halfLambert = dot(N, L) * 0.5 + 0.5;
                half lightTerm = saturate(halfLambert * mainLight.shadowAttenuation);

                half rampFactor = smoothstep(_CelThreshold - _CelSoftness, _CelThreshold + _CelSoftness, lightTerm);
                half3 rampSample = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(lightTerm, 0.5)).rgb;

                half3 shadowLighting = _ShadowColor.rgb * safeAmbient;
                half3 directLighting = mainLight.color + safeAmbient;
                half3 defaultRamp = lerp(shadowLighting, directLighting, rampFactor);

                half3 celLighting = lerp(defaultRamp, rampSample * (mainLight.color + safeAmbient), _UseRampTex);

                // Point & Spot Light contributions (town lanterns, torches, hearth fires)
                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < pixelLightCount; ++i)
                {
                    Light addLight = GetAdditionalLight(i, IN.positionWS);
                    half addNdotL = saturate(dot(N, addLight.direction));
                    celLighting += addLight.color * (addNdotL * addLight.distanceAttenuation * addLight.shadowAttenuation);
                }
                #endif

                half3 color = baseTex.rgb * celLighting;

                // Vertical Grounding + Cavity AO from Vertex Color Channel R (baked via Blender 5.1 Pipeline)
                half vertexAO = saturate(IN.vertexCol.r);
                color *= lerp(1.0, vertexAO, step(0.001, IN.vertexCol.r));

                // Emissive glow (glowing runes, lantern glass, forge embers)
                half4 emissiveTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv);
                color += emissiveTex.rgb * _EmissionColor.rgb;

                // Sunlit golden rim
                half rim = pow(1.0 - saturate(dot(N, V)), _RimPower) * rampFactor;
                color += _RimColor.rgb * (rim * _RimStrength);

                color = MixFog(color, IN.fogFactor);
                return half4(color, baseTex.a);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
