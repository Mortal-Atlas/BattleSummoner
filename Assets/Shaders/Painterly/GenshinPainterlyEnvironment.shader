Shader "BattleSummoner/Painterly/GenshinPainterlyEnvironment"
{
    Properties
    {
        [MainTexture] _BaseMap("Nano Banana Painterly Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Sunlit Warm Tint", Color) = (1.0, 0.98, 0.94, 1.0)
        _RampTex("Nano Banana 1D Cel Ramp (Optional)", 2D) = "white" {}
        _UseRampTex("Use 1D Ramp Texture", Range(0, 1)) = 0.0
        _ShadowColor("Cool Shadow Tint (Periwinkle/Slate)", Color) = (0.56, 0.62, 0.78, 1.0)

        [Header(Cel Shading Controls)]
        _CelThreshold("Shadow Terminator Threshold", Range(0.0, 1.0)) = 0.46
        _CelSoftness("Brushstroke Terminator Feather", Range(0.01, 0.5)) = 0.15

        [Header(World Cohesion and Grounding)]
        _BottomGroundingColor("Ground Contact Tint", Color) = (0.72, 0.68, 0.65, 1.0)
        _GroundingHeight("Grounding Gradient Height (m)", Float) = 3.5
        _GroundingStrength("Grounding Blend Strength", Range(0.0, 1.0)) = 0.35

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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _UseRampTex;
                half4 _ShadowColor;
                half _CelThreshold;
                half _CelSoftness;
                half4 _BottomGroundingColor;
                float _GroundingHeight;
                half _GroundingStrength;
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

                half halfLambert = dot(N, L) * 0.5 + 0.5;
                half lightTerm = saturate(halfLambert * mainLight.shadowAttenuation);

                half rampFactor = smoothstep(_CelThreshold - _CelSoftness, _CelThreshold + _CelSoftness, lightTerm);
                half3 rampSample = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(lightTerm, 0.5)).rgb;
                half3 defaultRamp = lerp(_ShadowColor.rgb, mainLight.color, rampFactor);
                half3 finalLighting = lerp(defaultRamp, rampSample * mainLight.color, _UseRampTex);

                half3 color = baseTex.rgb * finalLighting;

                half heightGrad = saturate(IN.positionWS.y / max(0.1, _GroundingHeight));
                half vertexAO = (IN.vertexCol.r > 0.01) ? lerp(1.0, IN.vertexCol.r, 0.45) : 1.0;
                color = lerp(color * _BottomGroundingColor.rgb, color, lerp(1.0, heightGrad, _GroundingStrength)) * vertexAO;

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
