Shader "BattleSummoner/Painterly/GenshinPainterlyFoliage"
{
    Properties
    {
        [MainTexture] _BaseMap("Leaf Alpha Cutout Texture", 2D) = "white" {}
        _TopCanopyColor("Sunlit Canopy Crown Color", Color) = (1.0, 0.78, 0.88, 1.0)
        _BottomCanopyColor("Shaded Canopy Under-Color", Color) = (0.78, 0.45, 0.65, 1.0)
        _SubsurfaceColor("Sun Backlight Subsurface Glow", Color) = (1.0, 0.88, 0.72, 1.0)
        _Cutoff("Alpha Cutoff", Range(0.05, 0.9)) = 0.35
        _WindSpeed("Breeze Speed", Float) = 1.6
        _WindAmplitude("Breeze Sway Amplitude", Float) = 0.08
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
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
                float3 positionWS : TEXCOORD0;
                float3 sphereNormalWS : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  heightOS   : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _TopCanopyColor;
                half4 _BottomCanopyColor;
                half4 _SubsurfaceColor;
                half _Cutoff;
                float _WindSpeed;
                float _WindAmplitude;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 posOS = IN.positionOS.xyz;
                float sway = sin(_Time.y * _WindSpeed + posOS.x * 2.0 + posOS.z * 2.0) * _WindAmplitude * saturate(posOS.y);
                posOS.x += sway;

                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;

                float3 canopyCenterOS = float3(0.0, 2.5, 0.0);
                float3 sphericalDir = normalize(posOS - canopyCenterOS);
                OUT.sphereNormalWS = TransformObjectToWorldNormal(sphericalDir);

                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.heightOS = saturate(posOS.y / 5.0);
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                clip(tex.a - _Cutoff);

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 N = normalize(IN.sphereNormalWS);
                float3 L = normalize(mainLight.direction);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                half halfLambert = saturate(dot(N, L) * 0.5 + 0.5);
                half celStep = smoothstep(0.32, 0.58, halfLambert * mainLight.shadowAttenuation);

                half3 shAmbient = SampleSH(N);
                half3 safeAmbient = max(shAmbient, half3(0.24, 0.28, 0.36));

                half3 canopyGrad = lerp(_BottomCanopyColor.rgb, _TopCanopyColor.rgb, saturate(celStep * 0.7 + IN.heightOS * 0.3));
                half sss = pow(saturate(dot(V, -L)), 3.0) * 0.45;

                // Modulate canopy color with texture albedo if present
                half3 baseAlbedo = (tex.r > 0.02 || tex.g > 0.02 || tex.b > 0.02) ? tex.rgb : half3(1, 1, 1);
                half3 finalLighting = (mainLight.color * (mainLight.shadowAttenuation * 0.7 + 0.3)) + safeAmbient;
                half3 finalRGB = (canopyGrad * baseAlbedo + _SubsurfaceColor.rgb * sss) * finalLighting;

                finalRGB = MixFog(finalRGB, IN.fogFactor);
                return half4(finalRGB, 1.0);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
