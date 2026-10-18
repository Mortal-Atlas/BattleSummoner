Shader "BattleSummoner/SAO_WarpTunnel"
{
    Properties
    {
        _CoreColor ("Speed Streak White", Color) = (0.95, 0.98, 1.0, 1.0)
        _AccentColor ("Streak Silver Ice", Color) = (0.85, 0.90, 0.98, 1.0)
        _Speed ("Warp Velocity", Float) = 8.0
        _StreakDensity ("Light Streak Density", Float) = 45.0
        _RingFrequency ("Concentric Ring Freq", Float) = 14.0
        _FlashIntensity ("Dive Flash Intensity", Range(0, 5)) = 0.0
        _HexGridStrength ("Hex Data Grid Strength", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend One One // Additive blend for radiant light streaks

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
                float3 worldPos     : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
                float4 _AccentColor;
                float _Speed;
                float _StreakDensity;
                float _RingFrequency;
                float _FlashIntensity;
                float _HexGridStrength;
            CBUFFER_END

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y * _Speed;

                // 1. High-Speed Longitudinal Light Streaks (SAO Link Start speedlines)
                float angle = uv.x * 6.28318;
                float streakLane = floor(uv.x * _StreakDensity);
                float laneRand = Hash11(streakLane * 17.13);
                
                // Streaks flying longitudinally along Y (Z in cylinder)
                float streakPos = uv.y * 12.0 - time * (1.2 + laneRand * 1.5) + laneRand * 10.0;
                float streak = saturate(sin(streakPos) * 1.5 - 0.7);
                streak = pow(streak, 4.0) * (0.5 + laneRand * 0.8);

                // 2. Concentric Energy Rings
                float ring = sin(uv.y * _RingFrequency - time * 2.5);
                ring = pow(saturate(ring), 6.0) * 1.5;

                // 3. Digital Data Grid / Tech lines
                float gridX = abs(frac(uv.x * 60.0) - 0.5);
                float gridY = abs(frac(uv.y * 30.0 - time * 0.3) - 0.5);
                float techGrid = smoothstep(0.04, 0.0, min(gridX, gridY)) * _HexGridStrength;

                // 4. Color Synthesis
                float colorMix = sin(angle * 2.0 + time * 0.8) * 0.5 + 0.5;
                float3 baseNeon = lerp(_CoreColor.rgb, _AccentColor.rgb, colorMix);

                float3 finalRGB = (streak * 2.5 + ring * 1.2 + techGrid * 0.8) * baseNeon;

                // Add ultra-white brilliant core to streaks
                finalRGB += float3(1, 1, 1) * pow(streak, 8.0) * 2.0;

                // 5. Dive Flash (Blinding white transition into the world)
                finalRGB += float3(1, 1, 1) * _FlashIntensity;

                return half4(finalRGB, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
