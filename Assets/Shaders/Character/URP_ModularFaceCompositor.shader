Shader "BattleSummoner/Character/URP_ModularFaceCompositor"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Featureless Skin Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Base Skin Tint", Color) = (1, 1, 1, 1)

        _UpperFaceTex("Upper Face Atlas (Eyes + Eyebrows)", 2D) = "black" {}
        _UpperFaceRect("Upper Face Head UV Rect (MinX, MinY, Width, Height)", Vector) = (0.25, 0.45, 0.5, 0.22)
        _UpperFaceST("Upper Face Atlas Scale/Offset", Vector) = (1, 1, 0, 0)

        _LowerFaceTex("Lower Face Atlas (Mouth Visemes)", 2D) = "black" {}
        _LowerFaceRect("Lower Face Head UV Rect (MinX, MinY, Width, Height)", Vector) = (0.32, 0.20, 0.36, 0.16)
        _LowerFaceST("Lower Face Atlas Scale/Offset", Vector) = (1, 1, 0, 0)

        [Header(Player Eye Color Override)]
        [Toggle] _EnableIrisTint("Enable Player Eye Color Override", Float) = 0
        _IrisTargetColor("Player Chosen Eye Color", Color) = (0.13, 0.65, 0.35, 1)
        _IrisHueMin("Iris Green Hue Min (Degrees)", Range(100, 160)) = 135
        _IrisHueMax("Iris Green Hue Max (Degrees)", Range(170, 220)) = 195
        _IrisMinSat("Iris Min Saturation", Range(0.0, 0.5)) = 0.20
        _IrisMinVal("Iris Min Luminance/Value", Range(0.0, 0.3)) = 0.10
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_UpperFaceTex); SAMPLER(sampler_UpperFaceTex);
            TEXTURE2D(_LowerFaceTex); SAMPLER(sampler_LowerFaceTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _UpperFaceRect;
                float4 _UpperFaceST;
                float4 _LowerFaceRect;
                float4 _LowerFaceST;
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
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 SampleDecalRegion(Texture2D decalTex, SamplerState decalSampler, float2 headUV, float4 regionRect, float4 atlasST)
            {
                float2 localUV = (headUV - regionRect.xy) / max(regionRect.zw, float2(0.0001, 0.0001));
                if (localUV.x < 0.0 || localUV.x > 1.0 || localUV.y < 0.0 || localUV.y > 1.0)
                    return half4(0, 0, 0, 0);

                float2 finalAtlasUV = localUV * atlasST.xy + atlasST.zw;
                return SAMPLE_TEXTURE2D(decalTex, decalSampler, finalAtlasUV);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 baseSkin = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // Composite Channel 1: Upper Face (Eyebrows + Eyes)
                half4 upperSample = SampleDecalRegion(_UpperFaceTex, sampler_UpperFaceTex, IN.uv, _UpperFaceRect, _UpperFaceST);

                // Real-time GPU green iris tinting
                if (_EnableIrisTint > 0.5 && upperSample.a > 0.05)
                {
                    float3 hsv = RGBtoHSV(upperSample.rgb);
                    float hueDeg = hsv.x * 360.0;

                    if (hueDeg >= _IrisHueMin && hueDeg <= _IrisHueMax && hsv.y >= _IrisMinSat && hsv.z >= _IrisMinVal)
                    {
                        float3 targetHSV = RGBtoHSV(_IrisTargetColor.rgb);
                        float newH = targetHSV.x;
                        float newS = saturate(hsv.y * (targetHSV.y / max(0.01, 0.75)));
                        float newV = hsv.z * (targetHSV.z / max(0.01, 0.85));
                        upperSample.rgb = HSVtoRGB(float3(newH, newS, newV));
                    }
                }

                half3 compositeRGB = lerp(baseSkin.rgb, upperSample.rgb, upperSample.a);

                // Composite Channel 2: Lower Face (Mouth)
                half4 lowerSample = SampleDecalRegion(_LowerFaceTex, sampler_LowerFaceTex, IN.uv, _LowerFaceRect, _LowerFaceST);
                compositeRGB = lerp(compositeRGB, lowerSample.rgb, lowerSample.a);

                // Cel-shaded directional light step
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalize(IN.normalWS), mainLight.direction));
                half celShade = smoothstep(0.35, 0.42, NdotL) * 0.35 + 0.65;

                return half4(compositeRGB * mainLight.color * celShade, baseSkin.a);
            }
            ENDHLSL
        }
    }
}
