Shader "BattleSummoner/Persona4_MidnightChannelCRT"
{
    Properties
    {
        _PhosphorColor ("Phosphor Color", Color) = (0.92, 0.84, 0.12, 1.0)
        _DarkColor ("Dark Fog Color", Color) = (0.08, 0.07, 0.01, 1.0)
        _ScanlineDensity ("Scanline Density", Float) = 160.0
        _ScanlineSpeed ("Scanline Speed", Float) = 1.8
        _StaticStrength ("Static Strength", Range(0, 1)) = 0.22
        _RainSpeed ("Rain Streak Speed", Float) = 2.4
        _SilhouetteStrength ("Silhouette Visibility", Range(0, 1)) = 0.85
        _RippleProgress ("Ripple Progress", Range(0, 2)) = 0.0
        _RippleCenter ("Ripple Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _VignetteStrength ("Vignette Strength", Range(0, 3)) = 1.6
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
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

            CBUFFER_START(UnityPerMaterial)
                float4 _PhosphorColor;
                float4 _DarkColor;
                float _ScanlineDensity;
                float _ScanlineSpeed;
                float _StaticStrength;
                float _RainSpeed;
                float _SilhouetteStrength;
                float _RippleProgress;
                float4 _RippleCenter;
                float _VignetteStrength;
                float _GlowIntensity;
            CBUFFER_END

            // Procedural pseudo-random hash
            float Hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;

                // 1. Interactive Liquid Ripple Distortion (Touch into TV Portal)
                if (_RippleProgress > 0.001)
                {
                    float2 d = uv - _RippleCenter.xy;
                    float dist = length(d);
                    float wave = sin((dist - _RippleProgress * 0.7) * 45.0);
                    float falloff = exp(-pow(dist - _RippleProgress * 0.7, 2.0) * 120.0);
                    uv += normalize(d + 0.0001) * wave * falloff * 0.08 * (1.0 - saturate(_RippleProgress * 0.5));
                }

                // CRT Barrel Distortion (Curved Glass Screen)
                float2 centeredUV = uv - 0.5;
                float r2 = dot(centeredUV, centeredUV);
                uv = 0.5 + centeredUV * (1.0 + 0.12 * r2);

                // 2. Persona 4 Television Rain / Drips
                float rain1 = sin(uv.x * 50.0 + time * 3.0) * 0.5 + 0.5;
                float rain2 = frac(uv.y * 3.0 - time * _RainSpeed + Hash21(floor(float2(uv.x * 35.0, 0))));
                float rainDrips = smoothstep(0.85, 0.98, rain2) * rain1;

                // 3. Midnight Channel Shadowy Silhouette (Flickering human form in fog)
                float2 silhUV = uv - float2(0.5, 0.45);
                silhUV.x *= 1.35;
                // Torso & head oval distance approximation
                float dHead = length(silhUV - float2(sin(time * 1.5) * 0.015, 0.12));
                float dBody = length(silhUV * float2(1.0, 0.6) - float2(0, -0.05));
                float silhouette = smoothstep(0.18, 0.08, dHead) + smoothstep(0.24, 0.10, dBody);
                silhouette = saturate(silhouette) * _SilhouetteStrength;
                // Add eerie broadcast flicker to silhouette
                silhouette *= (0.8 + 0.2 * sin(time * 28.0 + uv.y * 100.0));

                // 4. Analog Static Noise
                float staticNoise = Hash21(uv * 400.0 + float2(time * 95.0, time * 130.0));

                // 5. CRT Scanlines & Roll Bar
                float scanline = sin((uv.y + time * _ScanlineSpeed * 0.08) * _ScanlineDensity * 3.14159);
                scanline = scanline * 0.5 + 0.5;
                float rollBar = sin(uv.y * 4.0 - time * 1.2) * 0.15;

                // 6. Color Composition (Iconic Persona 4 Yellow Phosphor vs Dark Fog)
                float fog = smoothstep(0.2, 0.8, uv.y) * 0.35 + 0.65;
                float3 baseTone = lerp(_DarkColor.rgb, _PhosphorColor.rgb, fog);

                // Blend in shadow silhouette
                baseTone = lerp(baseTone, _DarkColor.rgb * 0.4, silhouette);

                // Blend static & scanlines
                baseTone += (staticNoise - 0.5) * _StaticStrength;
                baseTone *= (0.75 + 0.25 * scanline + rollBar);
                baseTone += rainDrips * 0.25 * _PhosphorColor.rgb;

                // 7. CRT Vignette & Screen Edge Darkening
                float vignette = uv.x * (1.0 - uv.x) * uv.y * (1.0 - uv.y) * 16.0;
                vignette = saturate(pow(vignette, _VignetteStrength * 0.5));
                baseTone *= vignette;

                // 8. Portal Glow Flash when Ripple expands
                if (_RippleProgress > 0.0)
                {
                    float flash = saturate(_RippleProgress * 1.5);
                    baseTone += float3(1.0, 0.95, 0.6) * flash * 2.0;
                }

                return half4(baseTone * _GlowIntensity, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
