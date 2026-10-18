Shader "BattleSummoner/VFX/RadialZoomBlur"
{
    Properties
    {
        _MainTex ("Source Texture", 2D) = "white" {}
        _BlurStrength ("Blur Strength", Range(0.0, 1.0)) = 0.0
        _Center ("Blur Center (Viewport)", Vector) = (0.5, 0.5, 0, 0)
        _ChromaticAberration ("Chromatic Separation", Range(0.0, 0.5)) = 0.05
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
            Name "RadialZoomBlurPass"

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
                float _BlurStrength;
                float4 _Center;
                float _ChromaticAberration;
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
                if (_BlurStrength < 0.001)
                {
                    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                }

                float2 dir = input.uv - _Center.xy;
                float dist = length(dir);
                float2 stepDir = (dir / max(dist, 0.0001)) * (_BlurStrength * 0.045);

                const int SAMPLES = 10;
                half4 sum = half4(0, 0, 0, 0);

                for (int i = 0; i < SAMPLES; i++)
                {
                    float factor = (float)i / (SAMPLES - 1);
                    float2 offset = stepDir * (float)i;

                    // Chromatic split on R and B channels
                    float2 uvR = input.uv - offset * (1.0 + _ChromaticAberration);
                    float2 uvG = input.uv - offset;
                    float2 uvB = input.uv - offset * (1.0 - _ChromaticAberration);

                    half r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvR).r;
                    half g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvG).g;
                    half b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvB).b;

                    sum += half4(r, g, b, 1.0);
                }

                return sum / (float)SAMPLES;
            }
            ENDHLSL
        }
    }
}
