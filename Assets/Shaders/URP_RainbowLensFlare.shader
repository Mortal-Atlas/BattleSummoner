Shader "BattleSummoner/VFX/RainbowLensFlare"
{
    Properties
    {
        [Header(Rainbow Flare Appearance)]
        _MainTex ("Flare Mask", 2D) = "white" {}
        _Intensity ("Overall Intensity", Range(0.0, 5.0)) = 1.8
        _RainbowSpread ("Chromatic Rainbow Spread", Range(0.1, 2.0)) = 1.0
        _StreakLength ("Anamorphic Streak Length", Range(0.5, 10.0)) = 4.5
        _HaloRadius ("Prismatic Halo Radius", Range(0.1, 1.0)) = 0.45
        _HaloWidth ("Halo Ring Width", Range(0.01, 0.2)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay+100"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend One One // Additive

        Pass
        {
            Name "RainbowLensFlarePass"

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
                float3 screenPos    : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
                float _RainbowSpread;
                float _StreakLength;
                float _HaloRadius;
                float _HaloWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // Procedural spectral rainbow gradient (Wavelength to RGB)
            half3 SpectralRainbow(float t)
            {
                t = frac(t);
                half r = saturate(sin(t * 6.2831853 + 0.0) * 0.5 + 0.5);
                half g = saturate(sin(t * 6.2831853 + 2.094) * 0.5 + 0.5);
                half b = saturate(sin(t * 6.2831853 + 4.188) * 0.5 + 0.5);
                return half3(r, g, b);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 centeredUV = input.uv * 2.0 - 1.0;
                float dist = length(centeredUV);

                // 1. Central Core Glow
                float core = saturate(1.0 - dist * 2.5);
                core = pow(core, 3.0) * 1.5;

                // 2. Horizontal Anamorphic Rainbow Streak
                float streakX = abs(centeredUV.x * _StreakLength);
                float streakY = abs(centeredUV.y * 35.0);
                float streak = saturate(1.0 - length(float2(streakX, streakY)));
                streak = pow(streak, 4.0);
                half3 streakCol = SpectralRainbow(centeredUV.x * _RainbowSpread * 0.5 + 0.5) * streak * 2.5;

                // 3. Prismatic Rainbow Halo Ring
                float haloDist = abs(dist - _HaloRadius);
                float halo = smoothstep(_HaloWidth, 0.0, haloDist);
                float angle = atan2(centeredUV.y, centeredUV.x) / 6.2831853 + 0.5;
                half3 haloCol = SpectralRainbow(angle * _RainbowSpread) * halo * 1.2;

                // Combine elements
                half3 finalColor = half3(1.0, 0.95, 0.8) * core + streakCol + haloCol;
                finalColor *= _Intensity;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
