Shader "BattleSummoner/Environment/URP_CircularContinent_MacroTerrain"
{
    Properties
    {
        [Header(Macro Continental Textures)]
        _MacroAlbedoTex ("Macro Continental Albedo", 2D) = "white" {}
        _MacroNormalTex ("Macro Normal Map", 2D) = "bump" {}
        _MacroSplatTex  ("Macro Biome Splat Mask", 2D) = "white" {}
        _MacroBlend     ("Macro Color Blend Weight", Range(0.0, 1.0)) = 0.92
        _MacroNormalStrength ("Macro Normal Strength", Range(0.0, 2.0)) = 1.0

        [Header(Painterly Anime Normals)]
        _BrushScale      ("Painterly Brush Scale", Range(1.0, 50.0)) = 18.0
        _BrushDepth      ("Painterly Impasto Depth", Range(0.0, 1.0)) = 0.25
        _CelBands        ("Watercolor Cel Bands", Range(2.0, 6.0)) = 4.0

        [Header(Biome Base Colors)]
        _ColorGrass ("Forest Emerald Grass", Color) = (0.26, 0.54, 0.17, 1.0)
        _ColorRock ("Basalt & Mountain Crag", Color) = (0.28, 0.27, 0.30, 1.0)
        _ColorSand ("Desert Sand Gold", Color) = (0.88, 0.72, 0.42, 1.0)
        _ColorSnow ("Glacial Alpine Snow", Color) = (0.94, 0.97, 1.00, 1.0)
        _ColorCobble ("City Cobblestone", Color) = (0.78, 0.76, 0.72, 1.0)

        [Header(Micro Detail Textures)]
        _GrassTex ("Grass Albedo", 2D) = "white" {}
        _GrassTiling ("Grass Tiling", Float) = 0.08
        _RockTex ("Rock Albedo", 2D) = "white" {}
        _RockTiling ("Rock Tiling", Float) = 0.04
        _CobbleTex ("Cobblestone Albedo", 2D) = "white" {}
        _CobbleTiling ("Cobblestone Tiling", Float) = 0.12

        [Header(Slope Cliffs)]
        _CliffSlopeThreshold ("Cliff Slope Threshold (Y Normal)", Range(0.2, 0.9)) = 0.55
        _CliffBlendSmoothness ("Cliff Blend Smoothness", Range(0.01, 0.4)) = 0.15
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.08
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

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
                float4 color        : COLOR;
                float fogFactor     : TEXCOORD3;
            };

            TEXTURE2D(_MacroAlbedoTex); SAMPLER(sampler_MacroAlbedoTex);
            TEXTURE2D(_MacroNormalTex); SAMPLER(sampler_MacroNormalTex);
            TEXTURE2D(_MacroSplatTex);  SAMPLER(sampler_MacroSplatTex);

            TEXTURE2D(_GrassTex);       SAMPLER(sampler_GrassTex);
            TEXTURE2D(_RockTex);        SAMPLER(sampler_RockTex);
            TEXTURE2D(_CobbleTex);      SAMPLER(sampler_CobbleTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorGrass;
                half4 _ColorRock;
                half4 _ColorSand;
                half4 _ColorSnow;
                half4 _ColorCobble;

                float _MacroBlend;
                float _MacroNormalStrength;
                float _BrushScale;
                float _BrushDepth;
                float _CelBands;

                float _GrassTiling;
                float _RockTiling;
                float _CobbleTiling;
                float _CliffSlopeThreshold;
                float _CliffBlendSmoothness;
                float _Smoothness;
            CBUFFER_END

            // Procedural Voronoi Brushstroke Noise (Self-contained, fast, zero external dependencies)
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

                VertexPositionInputs vertexInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInputs.positionCS;
                output.positionWS = vertexInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 geomNormalWS = normalize(input.normalWS);
                float2 worldUV = input.positionWS.xz;

                // 1. Sample Macro Continental Maps
                float2 macroUV = input.uv;
                half4 macroAlbedo = SAMPLE_TEXTURE2D(_MacroAlbedoTex, sampler_MacroAlbedoTex, macroUV);
                half4 macroNormRaw = SAMPLE_TEXTURE2D(_MacroNormalTex, sampler_MacroNormalTex, macroUV);
                half3 macroNormTS = UnpackNormalScale(macroNormRaw, _MacroNormalStrength);

                // World normal reconstruction from macro normal
                float3 macroNormalWS = normalize(float3(macroNormTS.x, macroNormTS.z, macroNormTS.y));
                float3 baseNormalWS = normalize(lerp(geomNormalWS, macroNormalWS, _MacroNormalStrength * 0.7));

                // 2. Micro PBR detail sampling
                half4 texGrass  = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, worldUV * _GrassTiling);
                half4 texRock   = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, worldUV * _RockTiling);
                half4 texCobble = SAMPLE_TEXTURE2D(_CobbleTex, sampler_CobbleTex, worldUV * _CobbleTiling);

                half microDetail = (texGrass.r * 0.6 + texRock.r * 0.4);
                half3 naturalAlbedo = macroAlbedo.rgb * (0.85 + 0.30 * microDetail);

                // 3. Slope-based cliff rock on steep faces
                float slope = baseNormalWS.y;
                float cliffFactor = saturate((_CliffSlopeThreshold - slope) / max(0.001, _CliffBlendSmoothness));
                cliffFactor = cliffFactor * cliffFactor * (3.0 - 2.0 * cliffFactor);
                half3 finalAlbedo = lerp(naturalAlbedo, _ColorRock.rgb * texRock.rgb * 1.4, cliffFactor);

                // 4. Procedural Painterly Impasto perturbation
                float brushScale = (_BrushScale > 0.0) ? _BrushScale : 18.0;
                float brushDepth = (_BrushDepth > 0.0) ? _BrushDepth : 0.25;
                float2 brushUV = input.positionWS.xz * (brushScale * 0.01);
                float b0 = VoronoiBrush(brushUV);
                float bX = VoronoiBrush(brushUV + float2(0.02, 0));
                float bY = VoronoiBrush(brushUV + float2(0, 0.02));
                float3 brushOffset = float3((bX - b0) * brushDepth, 0, (bY - b0) * brushDepth);
                float3 finalNormalWS = normalize(baseNormalWS + brushOffset);

                // 5. Lighting with anime Half-Lambert wrap & quantized watercolor bands
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half rawNdotL = dot(finalNormalWS, normalize(mainLight.direction));
                half halfLambert = saturate(rawNdotL * 0.65 + 0.35);

                float celBands = (_CelBands >= 2.0) ? _CelBands : 4.0;
                float steppedTone = floor(halfLambert * celBands) / (celBands - 1.0);
                half shadowAtten = lerp(0.5, 1.0, mainLight.shadowAttenuation);
                half3 directLight = mainLight.color * (steppedTone * 0.6 + halfLambert * 0.4) * shadowAtten;

                half3 skyAmbientFloor = half3(0.28, 0.30, 0.36);
                half3 ambientLight = max(SampleSH(finalNormalWS), skyAmbientFloor);
                half3 litColor = finalAlbedo * (directLight * 1.05 + ambientLight * 0.70);

                // Fog
                litColor = MixFog(litColor, input.fogFactor);

                return half4(litColor, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    FallBack "Universal Render Pipeline/Lit"
}
