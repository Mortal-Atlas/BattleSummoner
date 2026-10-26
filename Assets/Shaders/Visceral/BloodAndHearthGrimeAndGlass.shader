Shader "BattleSummoner/Visceral/BloodAndHearthGrimeAndGlass"
{
    Properties
    {
        _MainTex ("Base Albedo", 2D) = "white" {}
        _BaseColor ("Base Tint", Color) = (1,1,1,1)
        _GrimeNoiseTex ("Blood & Ash Noise Mask", 2D) = "black" {}
        _BloodColor ("Arterial Crimson Color", Color) = (0.42, 0.02, 0.02, 1)
        _AshColor ("Dungeon Ash & Soot Color", Color) = (0.12, 0.11, 0.10, 1)
        _BloodAndAshGrimeMask ("Armor Blood & Grime Accumulation (0-1)", Range(0, 1)) = 0.0
        _BloodWetnessGloss ("Fresh Blood Specular Gloss", Range(0, 1)) = 0.85
        _ViewportBloodIntensity ("9:16 Screen Glass Spatter Alpha", Range(0, 1)) = 0.0
        _ViewportDripOffset ("Vertical Drip UV Offset", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLitGrime"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_GrimeNoiseTex);
            SAMPLER(sampler_GrimeNoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _BloodColor;
                float4 _AshColor;
                float _BloodAndAshGrimeMask;
                float _BloodWetnessGloss;
                float _ViewportBloodIntensity;
                float _ViewportDripOffset;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _BaseColor;
                float2 dripUV = IN.uv + float2(0.0, _ViewportDripOffset);
                half4 noiseSample = SAMPLE_TEXTURE2D(_GrimeNoiseTex, sampler_GrimeNoiseTex, dripUV * 2.5);

                // Calculate visceral blood & ash grime coverage
                float grimeThreshold = saturate(_BloodAndAshGrimeMask * 1.35 - (1.0 - noiseSample.r) * 0.5);
                half4 grimeBlend = lerp(_AshColor, _BloodColor, noiseSample.g);

                // Specular rim highlight for wet arterial blood
                float3 viewDir = normalize(float3(0, 0, 1));
                float fresnel = pow(1.0 - saturate(dot(normalize(IN.normalWS), viewDir)), 3.0);
                half3 wetHighlight = _BloodColor.rgb * fresnel * _BloodWetnessGloss * grimeThreshold;

                half3 finalRGB = lerp(baseCol.rgb, grimeBlend.rgb, grimeThreshold) + wetHighlight;
                return half4(finalRGB, baseCol.a);
            }
            ENDHLSL
        }
    }
}
