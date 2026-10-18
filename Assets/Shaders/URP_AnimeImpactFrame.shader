Shader "BattleSummoner/VFX/AnimeImpactFrame"
{
    Properties
    {
        _MainTex ("Screen Source", 2D) = "white" {}
        _ActiveStrength ("Effect Strength", Range(0.0, 1.0)) = 0.0
        _Mode ("Style Mode (0: InvertBW, 1: HighContrastSketch, 2: ChromaticShock)", Float) = 0.0
        _Center ("Impact Point (Viewport)", Vector) = (0.5, 0.5, 0, 0)
        _SpeedlinesDensity ("Speedlines Density", Range(10.0, 100.0)) = 45.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "AnimeImpactFramePass"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _ActiveStrength;
                float _Mode;
                float4 _Center;
                float _SpeedlinesDensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 original = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                if (_ActiveStrength < 0.01) return original;

                // Grayscale luminance
                half luma = dot(original.rgb, half3(0.299, 0.587, 0.114));

                // Speedline radial effect
                float2 dir = input.uv - _Center.xy;
                float angle = atan2(dir.y, dir.x);
                float dist = length(dir);
                float speedlines = sin(angle * _SpeedlinesDensity) * 0.5 + 0.5;
                speedlines = step(0.65, speedlines) * saturate(dist * 2.0);

                half3 impactCol = original.rgb;

                if (_Mode < 0.5) // Mode 0: Inverted B&W Manga Sketch
                {
                    half inverted = 1.0 - luma;
                    half sketched = step(0.4, inverted);
                    impactCol = half3(sketched, sketched, sketched);
                    impactCol = lerp(impactCol, half3(0, 0, 0), speedlines * 0.7);
                }
                else if (_Mode < 1.5) // Mode 1: High Contrast Manga Monochrome
                {
                    half threshold = step(0.52, luma);
                    impactCol = half3(threshold, threshold, threshold);
                    impactCol += half3(speedlines, speedlines, speedlines) * 0.5;
                }
                else // Mode 2: Chromatic Shock Inversion
                {
                    impactCol = half3(1.0 - original.r, original.g * 0.2, 1.0 - original.b);
                }

                return half4(lerp(original.rgb, impactCol, _ActiveStrength), 1.0);
            }
            ENDHLSL
        }
    }
}
