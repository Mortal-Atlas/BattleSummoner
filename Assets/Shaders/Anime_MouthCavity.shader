Shader "BattleSummoner/Anime_MouthCavity"
{
    Properties
    {
        _LipLineColor ("Lip Line Color", Color) = (0.24, 0.08, 0.10, 1.0)
        _CavityColor ("Oral Cavity Color", Color) = (0.26, 0.05, 0.08, 1.0)
        _TeethColor ("Teeth Color", Color) = (0.98, 0.98, 0.98, 1.0)
        _TongueColor ("Tongue Color", Color) = (0.94, 0.44, 0.52, 1.0)
        
        [Header(Morph Controls)]
        _Open ("Mouth Open", Range(0, 1)) = 0.0
        _Smile ("Smile", Range(-1, 1)) = 0.0
        _VowelA ("Vowel A (Aa)", Range(0, 1)) = 0.0
        _VowelI ("Vowel I (Ee)", Range(0, 1)) = 0.0
        _VowelU ("Vowel U (Oo)", Range(0, 1)) = 0.0
        _VowelE ("Vowel E (Eh)", Range(0, 1)) = 0.0
        _VowelO ("Vowel O (Oh)", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent+50" 
            "RenderPipeline"="UniversalPipeline"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

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
                float4 _LipLineColor;
                float4 _CavityColor;
                float4 _TeethColor;
                float4 _TongueColor;

                float _Open;
                float _Smile;
                float _VowelA;
                float _VowelI;
                float _VowelU;
                float _VowelE;
                float _VowelO;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Local coordinate normalized to center: [-1, 1]
                float2 p = (input.uv - 0.5) * 2.0;

                // Total mouth opening (0 = closed slit, 1 = wide open)
                float totalOpen = saturate(_Open + (_VowelA * 0.95) + (_VowelE * 0.75) + (_VowelO * 0.95) + (_VowelI * 0.50) + (_VowelU * 0.40));

                // Smile / Frown vertical curvature
                float smileCurve = (p.x * p.x) * (_Smile * 0.28);
                p.y -= smileCurve;

                // Closed mouth line
                if (totalOpen < 0.04)
                {
                    float widthLimit = 0.75 + (_Smile * 0.15);
                    if (abs(p.x) < widthLimit)
                    {
                        float lineDist = abs(p.y);
                        float lineAlpha = 1.0 - smoothstep(0.02, 0.08, lineDist);
                        float endTaper = 1.0 - smoothstep(widthLimit - 0.15, widthLimit, abs(p.x));
                        return float4(_LipLineColor.rgb, lineAlpha * endTaper * _LipLineColor.a);
                    }
                    return float4(0, 0, 0, 0);
                }

                // Morphing shape factors for vowels
                float widthMod = 0.65 + (_VowelI * 0.35) + (max(0.0, _Smile) * 0.15) - (_VowelU * 0.28) - (_VowelO * 0.18);
                float heightMod = 0.08 + (_VowelA * 0.72) + (_VowelO * 0.82) + (_VowelE * 0.55) + (_VowelI * 0.28) + (_VowelU * 0.35) + (_Open * 0.50);

                // Scaled coordinate
                float2 sp = float2(p.x / max(0.15, widthMod), p.y / max(0.08, heightMod));
                float dist = length(sp);

                // Outside mouth
                if (dist > 1.0)
                {
                    // Smooth lip border outline
                    if (dist < 1.12)
                    {
                        float borderAlpha = 1.0 - smoothstep(1.0, 1.12, dist);
                        return float4(_LipLineColor.rgb, borderAlpha);
                    }
                    return float4(0, 0, 0, 0);
                }

                // Inside the mouth:
                float4 col = _CavityColor;

                // Upper Teeth (rendered in upper 32% of mouth)
                if (sp.y > 0.28 && abs(sp.x) < 0.78)
                {
                    float teethAlpha = smoothstep(0.28, 0.40, sp.y);
                    col = lerp(col, _TeethColor, teethAlpha);
                }

                // Tongue (rendered in lower curve)
                float2 tonguePos = float2(0.0, -0.55);
                float tongueDist = length(sp - tonguePos);
                if (tongueDist < 0.75)
                {
                    float tongueAlpha = 1.0 - smoothstep(0.50, 0.75, tongueDist);
                    col = lerp(col, _TongueColor, tongueAlpha * 0.95);
                }

                // Outer border blending
                float edgeAlpha = smoothstep(0.90, 1.0, dist);
                col = lerp(col, _LipLineColor, edgeAlpha * 0.90);

                return col;
            }
            ENDHLSL
        }
    }
}
