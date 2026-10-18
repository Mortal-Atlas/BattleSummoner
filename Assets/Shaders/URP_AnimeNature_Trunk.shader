Shader "BattleSummoner/AnimeNature/Trunk"
{
    Properties
    {
        [Header(Stylized Bark Colors)]
        _BaseColor ("Bark Base Color", Color) = (0.42, 0.28, 0.18, 1.0)
        _ShadowColor ("Bark Shadow Color", Color) = (0.22, 0.14, 0.08, 1.0)
        _MossBaseColor ("Root Moss Color", Color) = (0.28, 0.42, 0.18, 1.0)
        _MossHeight ("Moss Blend Height", Range(0.0, 5.0)) = 1.2
        _CelThreshold ("Cel Threshold", Range(0.0, 1.0)) = 0.4
        _CelSoftness ("Cel Softness", Range(0.01, 0.5)) = 0.15

        [Header(Wind Sway)]
        _WindSpeed ("Wind Speed", Range(0.0, 5.0)) = 1.6
        _WindStrength ("Wind Strength", Range(0.0, 0.5)) = 0.04
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

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
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float heightOS      : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float4 _MossBaseColor;
                float _MossHeight;
                float _CelThreshold;
                float _CelSoftness;
                float _WindSpeed;
                float _WindStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 posOS = input.positionOS.xyz;
                output.heightOS = posOS.y;

                // Subtle trunk wind sway anchored at ground
                float time = _Time.y * _WindSpeed;
                float sway = sin(time + posOS.y * 0.2) * _WindStrength * saturate(posOS.y * 0.15);
                posOS.x += sway;

                float3 posWS = TransformObjectToWorld(posOS);
                output.positionCS = TransformWorldToHClip(posWS);
                output.positionWS = posWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 normal = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightDir = normalize(mainLight.direction);

                float NdotL = dot(normal, lightDir);
                float halfLambert = NdotL * 0.5 + 0.5;
                float litFactor = smoothstep(_CelThreshold - _CelSoftness, _CelThreshold + _CelSoftness, halfLambert * mainLight.shadowAttenuation);

                // Vertical moss gradient near ground
                float mossFactor = 1.0 - saturate(input.heightOS / max(_MossHeight, 0.01));
                half3 baseBark = lerp(_ShadowColor.rgb, _BaseColor.rgb, litFactor);
                half3 trunkColor = lerp(baseBark, _MossBaseColor.rgb * (litFactor * 0.5 + 0.5), mossFactor * 0.65);

                half3 ambient = SampleSH(normal) * 0.3;
                trunkColor += ambient * _ShadowColor.rgb;

                return half4(trunkColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vertShadow(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _MainLightPosition.xyz));

                return output;
            }

            half4 fragShadow(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
