Shader "BattleSummoner/Painterly/GenshinPainterlyCharacter"
{
    Properties
    {
        [MainTexture] _BaseMap("Character Albedo Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Base Tint", Color) = (1.0, 0.98, 0.96, 1.0)
        _ShadowTint("Anime Shadow Tint (Warm Lavender-Slate)", Color) = (0.68, 0.64, 0.78, 1.0)
        _CelStep("Cel Terminator Step", Range(0.1, 0.9)) = 0.42
        _CelFeather("Cel Terminator Softness", Range(0.005, 0.25)) = 0.045
        _MinBrightnessFloor("Dark Armor Minimum Value Floor", Range(0.05, 0.35)) = 0.16
        _RimColor("Character Silhouette Rim Color", Color) = (1.0, 0.94, 0.82, 1.0)
        _RimPower("Rim Sharpness", Range(1.5, 8.0)) = 3.8
        _RimIntensity("Rim Strength", Range(0.0, 1.0)) = 0.42
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                half4  vertexCol  : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowTint;
                half _CelStep;
                half _CelFeather;
                half _MinBrightnessFloor;
                half4 _RimColor;
                half _RimPower;
                half _RimIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.vertexCol  = (dot(IN.color.rgb, IN.color.rgb) > 0.001) ? IN.color : half4(1, 1, 1, 1);
                OUT.fogFactor  = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor * IN.vertexCol;

                // Lift pitch-black armor values so silhouette and armor folds remain readable
                half3 liftedAlbedo = max(texCol.rgb, half3(_MinBrightnessFloor, _MinBrightnessFloor, _MinBrightnessFloor * 1.15));

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 N = normalize(IN.normalWS);
                float3 L = normalize(mainLight.direction);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                half halfLambert = dot(N, L) * 0.5 + 0.5;
                half cel = smoothstep(_CelStep - _CelFeather, _CelStep + _CelFeather, halfLambert * lerp(0.35, 1.0, mainLight.shadowAttenuation));

                half3 shaded = lerp(liftedAlbedo * _ShadowTint.rgb, liftedAlbedo * mainLight.color, cel);

                half rim = pow(1.0 - saturate(dot(N, V)), _RimPower) * (0.35 + 0.65 * cel);
                shaded += _RimColor.rgb * (rim * _RimIntensity);

                shaded = MixFog(shaded, IN.fogFactor);
                return half4(shaded, texCol.a);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
