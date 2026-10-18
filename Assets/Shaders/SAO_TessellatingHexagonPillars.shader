Shader "BattleSummoner/SAO_TessellatingHexagonPillars"
{
    Properties
    {
        _CoreColor ("Core White Glow", Color) = (1.00, 1.00, 1.00, 1.0)
        _EdgeColor ("Edge Silver Outline", Color) = (0.85, 0.95, 1.00, 1.0)
        _AccentColor ("Prismatic Accent", Color) = (0.95, 0.98, 1.00, 1.0)
        _EdgeIntensity ("Edge Glow Intensity", Float) = 1.8
        _PulseIntensity ("Energy Pulse Intensity", Float) = 2.0
        _PulseFrequency ("Pulse Longitudinal Freq", Float) = 4.5
        _PulseSpeed ("Pulse Speed Along Pillar", Float) = 26.0
        _EndCapIntensity ("End Cap White Intensity", Float) = 2.2
        _NearFadeMin ("Camera Near Fade Min", Float) = 1.0
        _NearFadeMax ("Camera Near Fade Max", Float) = 2.8
        _FarFadeMin ("Far Distance Fade Min", Float) = 22.0
        _FarFadeMax ("Far Distance Fade Max", Float) = 65.0
        _FlashIntensity ("Dive Arrival Flash", Range(0, 5)) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "IgnoreProjector"="True"
        }
        LOD 100
        Cull Off
        ZWrite Off
        Blend One One // Pure additive blending on dark void

        Pass
        {
            Name "HexagonPillarUnlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float2 uv           : TEXCOORD0;
                float3 worldPos     : TEXCOORD1;
                float3 normalWS     : NORMAL;
                float3 viewDirWS    : TEXCOORD3;
                float4 vertexColor  : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
                float4 _EdgeColor;
                float4 _AccentColor;
                float _EdgeIntensity;
                float _PulseIntensity;
                float _PulseFrequency;
                float _PulseSpeed;
                float _EndCapIntensity;
                float _NearFadeMin;
                float _NearFadeMax;
                float _FarFadeMin;
                float _FarFadeMax;
                float _FlashIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.worldPos = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                output.uv = input.uv;
                output.vertexColor = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float3 viewDir = normalize(input.viewDirWS);
                float3 normal = normalize(input.normalWS);

                // 1. Distance Attenuation (Smoothly vanish near camera + deep abyss fade)
                float camDist = length(input.worldPos - _WorldSpaceCameraPos);
                float nearFade = smoothstep(_NearFadeMin, _NearFadeMax, camDist);
                float farFade = 1.0 - smoothstep(_FarFadeMin, _FarFadeMax, camDist);
                float distFade = nearFade * farFade;

                // 2. Hexagonal Facet (6 vertical facets)
                float facet = frac(uv.x * 6.0);
                float edgeFactor = abs(facet - 0.5) * 2.0; // 0 at facet center, 1 at edge

                // Inside of the hexagon is glowing white
                float insideWhite = smoothstep(0.92, 0.25, edgeFactor) * 1.4;
                
                // Crisp high-contrast wireframe edge boundary
                float wireframe = smoothstep(0.80, 0.98, edgeFactor) * _EdgeIntensity;

                // 3. Glowing End Caps / Rings (Pure white front and back faces)
                float endCap = (smoothstep(0.06, 0.0, uv.y) + smoothstep(0.94, 1.0, uv.y)) * _EndCapIntensity;

                // 4. Longitudinal Laser White Pulse surging down the pillar
                float pulseTime = _Time.y * _PulseSpeed;
                float pulseWave = sin(uv.y * _PulseFrequency - pulseTime + input.vertexColor.r * 6.28);
                float pulse = pow(saturate(pulseWave * 1.5 - 0.5), 3.0) * _PulseIntensity;

                // Pure white core emission for inside of hexagon
                float3 insideEmission = float3(1.0, 1.0, 1.0) * (insideWhite + endCap + pulse * 1.2);

                // Edge outline in crisp silver / subtle electric rim
                float3 edgeEmission = _EdgeColor.rgb * wireframe;

                float3 emission = (insideEmission + edgeEmission) * distFade;

                // 5. Dive Arrival Flash (Smooth transition to blinding pure white)
                emission += float3(1.0, 1.0, 1.0) * _FlashIntensity;

                return half4(emission, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
