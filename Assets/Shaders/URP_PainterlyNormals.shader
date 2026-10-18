Shader "BattleSummoner/Shaders/PainterlyNormals"
{
    Properties
    {
        [Header(Painterly Appearance)]
        _BaseColor ("Base Color", Color) = (0.9, 0.85, 0.78, 1.0)
        _ShadowColor ("Shadow Wash Color", Color) = (0.5, 0.45, 0.55, 1.0)
        _BrushScale ("Brushstroke Frequency", Range(1.0, 50.0)) = 18.0
        _BrushDepth ("Impasto Normal Strength", Range(0.0, 1.0)) = 0.35
        _CelBands ("Watercolor Tone Bands", Range(2.0, 6.0)) = 3.0
        _OutlineColor ("Painterly Ink Outline", Color) = (0.2, 0.15, 0.18, 1.0)
        _OutlineWidth ("Outline Thickness", Range(0.0, 0.05)) = 0.012
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        // Outline Pass (Inverted Hull)
        Pass
        {
            Name "PainterlyOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vertOutline
            #pragma fragment fragOutline

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float _BrushScale;
                float _BrushDepth;
                float _CelBands;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            Varyings vertOutline(Attributes input)
            {
                Varyings output;
                float3 pos = input.positionOS.xyz + input.normalOS * _OutlineWidth;
                output.positionCS = TransformObjectToHClip(pos);
                return output;
            }

            half4 fragOutline(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // Main Painterly Surface Pass
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float _BrushScale;
                float _BrushDepth;
                float _CelBands;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            // Procedural Voronoi / Brushstroke Noise
            float2 Hash2(float2 p)
            {
                return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
            }

            float VoronoiBrush(float2 uv)
            {
                float2 g = floor(uv);
                float2 f = frac(uv);
                float minDist = 1.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 lattice = float2(x, y);
                        float2 offset = Hash2(g + lattice);
                        float dist = length(lattice + offset - f);
                        minDist = min(minDist, dist);
                    }
                }
                return minDist;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(posWS);
                output.positionWS = posWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Perturb normal with procedural painterly impasto brush strokes
                float2 brushUV = input.uv * _BrushScale;
                float b0 = VoronoiBrush(brushUV);
                float bX = VoronoiBrush(brushUV + float2(0.02, 0));
                float bY = VoronoiBrush(brushUV + float2(0, 0.02));

                float3 brushOffset = float3((bX - b0) * _BrushDepth, (bY - b0) * _BrushDepth, 0);
                float3 normal = normalize(input.normalWS + brushOffset);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float NdotL = dot(normal, normalize(mainLight.direction));
                float halfLambert = NdotL * 0.5 + 0.5;

                // Quantize into watercolor wash bands
                float steppedTone = floor(halfLambert * _CelBands) / (_CelBands - 1.0);
                steppedTone = saturate(steppedTone * mainLight.shadowAttenuation);

                half3 surfaceColor = lerp(_ShadowColor.rgb, _BaseColor.rgb, steppedTone);
                surfaceColor += SampleSH(normal) * 0.25; // Soft ambient bounce

                return half4(surfaceColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
