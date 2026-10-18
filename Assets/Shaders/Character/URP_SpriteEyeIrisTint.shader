Shader "BattleSummoner/Character/URP_SpriteEyeIrisTint"
{
    Properties
    {
        [MainTexture] _BaseMap("Sprite Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Sprite Tint", Color) = (1, 1, 1, 1)

        [Header(Player Eye Iris Tinting)]
        [Toggle] _EnableIrisTint("Enable Player Eye Color Override", Float) = 1
        _IrisTargetColor("Player Chosen Eye Color", Color) = (0.13, 0.65, 0.35, 1)
        _IrisHueMin("Iris Green Hue Min (Degrees)", Range(100, 160)) = 135
        _IrisHueMax("Iris Green Hue Max (Degrees)", Range(170, 220)) = 195
        _IrisMinSat("Iris Min Saturation", Range(0.0, 0.5)) = 0.20
        _IrisMinVal("Iris Min Luminance/Value", Range(0.0, 0.3)) = 0.10
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "IgnoreProjector"="True" 
            "PreviewType"="Plane" 
            "CanUseSpriteAtlas"="True" 
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _IrisTargetColor;
                float _EnableIrisTint;
                float _IrisHueMin;
                float _IrisHueMax;
                float _IrisMinSat;
                float _IrisMinVal;
            CBUFFER_END

            float3 RGBtoHSV(float3 rgb)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(rgb.bg, K.wz), float4(rgb.gb, K.xy), step(rgb.b, rgb.g));
                float4 q = lerp(float4(p.xyw, rgb.r), float4(rgb.r, p.yzx), step(p.x, rgb.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HSVtoRGB(float3 hsv)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
                return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * IN.color * _BaseColor;

                // Real-time GPU green iris replacement
                if (_EnableIrisTint > 0.5 && col.a > 0.05)
                {
                    float3 hsv = RGBtoHSV(col.rgb);
                    float hueDeg = hsv.x * 360.0;

                    if (hueDeg >= _IrisHueMin && hueDeg <= _IrisHueMax && hsv.y >= _IrisMinSat && hsv.z >= _IrisMinVal)
                    {
                        float3 targetHSV = RGBtoHSV(_IrisTargetColor.rgb);
                        float newH = targetHSV.x;
                        float newS = saturate(hsv.y * (targetHSV.y / max(0.01, 0.75)));
                        float newV = hsv.z * (targetHSV.z / max(0.01, 0.85));
                        float3 newRGB = HSVtoRGB(float3(newH, newS, newV));
                        col.rgb = newRGB;
                    }
                }

                // Premultiply alpha for blend mode One OneMinusSrcAlpha
                col.rgb *= col.a;
                return col;
            }
            ENDHLSL
        }
    }
}
