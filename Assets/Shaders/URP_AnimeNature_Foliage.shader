Shader "BattleSummoner/AnimeNature/Foliage"
{
    Properties
    {
        [Header(Anime Foliage Colors)]
        _BaseColor ("Sunlit Color", Color) = (0.35, 0.78, 0.28, 1.0)
        _ShadowColor ("Shadow Color", Color) = (0.12, 0.38, 0.22, 1.0)
        _TranslucencyColor ("Translucency Backlight", Color) = (0.65, 0.88, 0.25, 1.0)
        _TranslucencyPower ("Translucency Strength", Range(0.0, 2.0)) = 0.85
        _CelRampThreshold ("Cel Threshold", Range(0.0, 1.0)) = 0.45
        _CelRampSoftness ("Cel Softness", Range(0.01, 0.5)) = 0.15

        [Header(Spherical Normal Blending)]
        _SphericalNormalBlend ("Spherical Normal Factor", Range(0.0, 1.0)) = 0.75

        [Header(Wind Animation)]
        _WindSpeed ("Wind Speed", Range(0.0, 5.0)) = 1.6
        _WindStrength ("Wind Strength", Range(0.0, 1.0)) = 0.12
        _WindGust ("Flutter Strength", Range(0.0, 1.0)) = 0.08

        [Header(Anime Sun Rim)]
        _RimColor ("Rim Highlight", Color) = (0.9, 1.0, 0.7, 1.0)
        _RimPower ("Rim Power", Range(1.0, 8.0)) = 3.5

        [Header(Surface Options)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull [_Cull]

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float4 color        : COLOR;
                float3 radialNormWS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float4 _TranslucencyColor;
                float _TranslucencyPower;
                float _CelRampThreshold;
                float _CelRampSoftness;
                float _SphericalNormalBlend;
                float _WindSpeed;
                float _WindStrength;
                float _WindGust;
                float4 _RimColor;
                float _RimPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 posOS = input.positionOS.xyz;

                // Spherical radial direction from local canopy center
                float3 radialOS = normalize(posOS + float3(0, -0.5, 0));

                // Procedural Wind Displacement
                float time = _Time.y * _WindSpeed;
                float heightFactor = saturate(posOS.y * 0.25);
                float sway = sin(time + posOS.x * 0.5 + posOS.z * 0.5) * _WindStrength * heightFactor;
                float flutter = cos(time * 2.8 + posOS.y * 3.0) * _WindGust * heightFactor;
                posOS.x += sway + flutter;
                posOS.z += sway * 0.7 - flutter * 0.5;

                float3 posWS = TransformObjectToWorld(posOS);
                output.positionCS = TransformWorldToHClip(posWS);
                output.positionWS = posWS;

                float3 normWS = TransformObjectToWorldNormal(input.normalOS);
                float3 radWS = TransformObjectToWorldNormal(radialOS);

                output.normalWS = normWS;
                output.radialNormWS = radWS;
                output.color = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 normal = normalize(lerp(input.normalWS, input.radialNormWS, _SphericalNormalBlend));
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);

                // Main directional light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightDir = normalize(mainLight.direction);

                // Cel Shaded Lighting Ramp
                float NdotL = dot(normal, lightDir);
                float halfLambert = NdotL * 0.5 + 0.5;
                float shadowAtten = mainLight.shadowAttenuation;
                float litFactor = smoothstep(_CelRampThreshold - _CelRampSoftness, _CelRampThreshold + _CelRampSoftness, halfLambert * shadowAtten);

                // Subsurface translucency (backlight through leaves)
                float backLight = saturate(dot(-lightDir, viewDir)) * _TranslucencyPower;
                half3 foliageColor = lerp(_ShadowColor.rgb, _BaseColor.rgb, litFactor);
                foliageColor += _TranslucencyColor.rgb * backLight * (1.0 - litFactor * 0.5);

                // Anime Sun Rim Highlight
                float rim = 1.0 - saturate(dot(normal, viewDir));
                rim = pow(rim, _RimPower) * saturate(dot(lightDir, normal) * 0.5 + 0.5);
                foliageColor += _RimColor.rgb * rim * 0.5;

                // Ambient contribution
                half3 ambient = SampleSH(normal) * 0.35;
                foliageColor += ambient * _ShadowColor.rgb;

                return half4(foliageColor, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    FallBack "Universal Render Pipeline/Lit"
}
