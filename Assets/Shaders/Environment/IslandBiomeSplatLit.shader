Shader "BattleSummoner/Environment/IslandBiomeSplatLit"
{
    Properties
    {
        [Header(Global Island Maps)]
        _MasterColorMap ("Master Satellite Map (UV0)", 2D) = "white" {}
        _BiomeSplatMap ("Biome Splat Map (UV0)", 2D) = "black" {}

        [Header(Detail Biome Textures (Tiled))]
        _GrassTex ("Lush Grass Texture", 2D) = "white" {}
        _DesertTex ("Desert Sand Texture", 2D) = "white" {}
        _VolcanicTex ("Volcanic Basalt Texture", 2D) = "white" {}
        _BeachTex ("Beach Sand Texture", 2D) = "white" {}
        _CliffTex ("Rock Cliff Strata Texture", 2D) = "white" {}
        _DetailTiling ("Detail Texture Tiling", Float) = 32.0

        [Header(Slope Cliff Blending)]
        _CliffThreshold ("Cliff Slope Threshold", Range(0.2, 0.9)) = 0.58
        _CliffBlend ("Cliff Blend Softness", Range(0.01, 0.35)) = 0.12

        [Header(Surface PBR)]
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.08
        _SatelliteTintStrength ("Satellite Map Tint Factor", Range(0.0, 1.0)) = 0.40
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
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv0          : TEXCOORD0;
                float2 uv1          : TEXCOORD1;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv0          : TEXCOORD2;
                float2 uv1          : TEXCOORD3;
                float4 color        : COLOR;
                float fogFactor     : TEXCOORD4;
            };

            TEXTURE2D(_MasterColorMap); SAMPLER(sampler_MasterColorMap);
            TEXTURE2D(_BiomeSplatMap);  SAMPLER(sampler_BiomeSplatMap);
            TEXTURE2D(_GrassTex);       SAMPLER(sampler_GrassTex);
            TEXTURE2D(_DesertTex);      SAMPLER(sampler_DesertTex);
            TEXTURE2D(_VolcanicTex);    SAMPLER(sampler_VolcanicTex);
            TEXTURE2D(_BeachTex);       SAMPLER(sampler_BeachTex);
            TEXTURE2D(_CliffTex);       SAMPLER(sampler_CliffTex);

            CBUFFER_START(UnityPerMaterial)
                float _DetailTiling;
                float _CliffThreshold;
                float _CliffBlend;
                float _Smoothness;
                float _SatelliteTintStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInputs.positionCS;
                output.positionWS = vertexInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv0 = input.uv0;
                output.uv1 = input.uv0 * _DetailTiling;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);

                // Sample detail textures
                half4 colGrass = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, input.uv1);
                half4 colDesert = SAMPLE_TEXTURE2D(_DesertTex, sampler_DesertTex, input.uv1);
                half4 colVolc = SAMPLE_TEXTURE2D(_VolcanicTex, sampler_VolcanicTex, input.uv1);
                half4 colBeach = SAMPLE_TEXTURE2D(_BeachTex, sampler_BeachTex, input.uv1);
                half4 colCliff = SAMPLE_TEXTURE2D(_CliffTex, sampler_CliffTex, input.uv1);

                // Sample Biome Splat Map (blend with vertex colors for extra robustness)
                half4 splatTex = SAMPLE_TEXTURE2D(_BiomeSplatMap, sampler_BiomeSplatMap, input.uv0);
                half4 splatWeights = lerp(input.color, splatTex, 0.5);

                half totalWeight = splatWeights.r + splatWeights.g + splatWeights.b + splatWeights.a + 1e-4;
                half4 normWeights = splatWeights / totalWeight;

                // Blend ground biomes
                half3 groundAlbedo = normWeights.r * colGrass.rgb +
                                     normWeights.g * colDesert.rgb +
                                     normWeights.b * colVolc.rgb +
                                     normWeights.a * colBeach.rgb;

                // Slope-based Cliff Blending (steep slopes show cliff strata rock)
                float slope = normalWS.y;
                float cliffFactor = saturate((_CliffThreshold - slope) / max(0.001, _CliffBlend));
                cliffFactor = cliffFactor * cliffFactor * (3.0 - 2.0 * cliffFactor);

                half3 baseAlbedo = lerp(groundAlbedo, colCliff.rgb, cliffFactor);

                // Multiply subtle satellite map coloration
                half4 satelliteMap = SAMPLE_TEXTURE2D(_MasterColorMap, sampler_MasterColorMap, input.uv0);
                half3 finalAlbedo = baseAlbedo * lerp(half3(1.0, 1.0, 1.0), satelliteMap.rgb * 1.35, _SatelliteTintStrength);

                // Direct Lighting
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 directLighting = mainLight.color * (NdotL * mainLight.shadowAttenuation);

                // Ambient Spherical Harmonics
                half3 ambientLighting = SampleSH(normalWS);

                half3 finalColor = finalAlbedo * (directLighting + ambientLighting);

                // Apply Fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    FallBack "Universal Render Pipeline/Lit"
}
