Shader "Bitgem/URP/StylizedWater_ProtoSeries"
{
    Properties
    {
        [Header(Water Colors and Transparency)]
        _ShallowColor ("Shallow Color", Color) = (0.28, 0.78, 0.85, 0.70)
        _DeepColor ("Deep Color", Color) = (0.05, 0.22, 0.48, 0.95)
        _DepthDistance ("Depth Fade Distance", Range(0.1, 20.0)) = 4.0
        _Transparency ("Overall Opacity", Range(0.0, 1.0)) = 0.88

        [Header(Shoreline and Intersection Foam)]
        _FoamColor ("Foam Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _FoamDistance ("Foam Distance", Range(0.01, 5.0)) = 0.95
        _FoamCutoff ("Foam Edge Sharpness", Range(0.01, 1.0)) = 0.35
        _FoamNoiseScale ("Foam Noise Scale", Float) = 1.8

        [Header(Surface Ripples and Caustics)]
        _SurfaceTexture ("Surface Caustics / Foam Texture", 2D) = "white" {}
        _SurfaceTiling ("Surface Tiling", Vector) = (0.12, 0.12, 0, 0)
        _SurfaceSpeed1 ("Surface Speed 1 (XY)", Vector) = (0.04, 0.03, 0, 0)
        _SurfaceSpeed2 ("Surface Speed 2 (XY)", Vector) = (-0.03, 0.04, 0, 0)
        _SurfaceDistortion ("Normal Ripple Strength", Range(0.0, 2.0)) = 0.45

        [Header(Vertex Wave Animation)]
        _WaveSpeed ("Wave Speed", Float) = 1.25
        _WaveHeight ("Wave Height", Float) = 0.08
        _WaveFrequency ("Wave Frequency", Float) = 0.35

        [Header(Lighting and Highlights)]
        _SunSparkleColor ("Sun Sparkle Color", Color) = (1.0, 0.98, 0.85, 1.0)
        _SunSparkleExponent ("Sun Sparkle Exponent", Range(8.0, 256.0)) = 64.0
        _FresnelColor ("Fresnel Rim Color", Color) = (0.75, 0.92, 1.0, 0.6)
        _FresnelPower ("Fresnel Power", Range(0.5, 8.0)) = 3.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float4 screenPos    : TEXCOORD3;
                float2 uv           : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float _DepthDistance;
                float _Transparency;

                float4 _FoamColor;
                float _FoamDistance;
                float _FoamCutoff;
                float _FoamNoiseScale;

                float4 _SurfaceTexture_ST;
                float4 _SurfaceTiling;
                float4 _SurfaceSpeed1;
                float4 _SurfaceSpeed2;
                float _SurfaceDistortion;

                float _WaveSpeed;
                float _WaveHeight;
                float _WaveFrequency;

                float4 _SunSparkleColor;
                float _SunSparkleExponent;
                float4 _FresnelColor;
                float _FresnelPower;
            CBUFFER_END

            TEXTURE2D(_SurfaceTexture);
            SAMPLER(sampler_SurfaceTexture);

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Vertex Wave displacement
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float wave = sin(posWS.x * _WaveFrequency + _Time.y * _WaveSpeed) *
                             cos(posWS.z * _WaveFrequency + _Time.y * (_WaveSpeed * 0.85));
                posWS.y += wave * _WaveHeight;

                output.positionWS = posWS;
                output.positionCS = TransformWorldToHClip(posWS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.uv = input.uv;

                // Approximate normal with wave tilt
                float3 normWS = TransformObjectToWorldNormal(input.normalOS);
                float dWdx = cos(posWS.x * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveFrequency * _WaveHeight;
                float dWdz = -sin(posWS.z * _WaveFrequency + _Time.y * (_WaveSpeed * 0.85)) * _WaveFrequency * _WaveHeight;
                normWS = normalize(normWS + float3(-dWdx, 0, -dWdz));
                output.normalWS = normWS;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Screen coordinates and scene depth calculation
                float2 screenUV = input.screenPos.xy / max(input.screenPos.w, 0.0001);
                #if UNITY_UV_STARTS_AT_TOP
                if (_ProjectionParams.x > 0)
                    screenUV.y = 1.0 - screenUV.y;
                #endif

                float rawDepth = SampleSceneDepth(screenUV);
                float sceneLinearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceLinearDepth = input.screenPos.w;
                float depthDiff = max(0.0, sceneLinearDepth - surfaceLinearDepth);

                // 2. Dual-scrolling surface caustics / voronoi texture
                float2 uv1 = input.positionWS.xz * _SurfaceTiling.xy + _Time.y * _SurfaceSpeed1.xy;
                float2 uv2 = input.positionWS.xz * _SurfaceTiling.xy * 1.25 + _Time.y * _SurfaceSpeed2.xy;

                half4 texSample1 = SAMPLE_TEXTURE2D(_SurfaceTexture, sampler_SurfaceTexture, uv1);
                half4 texSample2 = SAMPLE_TEXTURE2D(_SurfaceTexture, sampler_SurfaceTexture, uv2);
                half caustics = saturate((texSample1.r + texSample2.r) * 0.65);

                // 3. Depth-based color gradient (Shallow -> Deep)
                float depthFactor = saturate(depthDiff / max(_DepthDistance, 0.01));
                half4 waterColor = lerp(_ShallowColor, _DeepColor, depthFactor);

                // Overlay animated caustics
                waterColor.rgb += caustics * 0.22 * _ShallowColor.rgb;

                // 4. Shoreline & Object Intersection Foam
                float foamNoise = (texSample1.g + texSample2.b) * 0.5;
                float foamFactor = saturate(1.0 - (depthDiff / max(_FoamDistance, 0.001)));
                foamFactor = smoothstep(_FoamCutoff, 1.0, foamFactor * (0.6 + foamNoise * 0.5));
                waterColor.rgb = lerp(waterColor.rgb, _FoamColor.rgb, foamFactor * _FoamColor.a);

                // 5. Directional Light Specular Highlight (Sun Sparkles)
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float3 halfDir = normalize(lightDir + viewDir);

                float NdotH = saturate(dot(input.normalWS, halfDir));
                float sparkle = pow(NdotH, _SunSparkleExponent);
                float3 specularHighlight = sparkle * _SunSparkleColor.rgb * mainLight.color * (1.0 - foamFactor);
                waterColor.rgb += specularHighlight;

                // 6. Fresnel Rim Glow
                float NdotV = saturate(dot(input.normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                waterColor.rgb = lerp(waterColor.rgb, _FresnelColor.rgb, fresnel * _FresnelColor.a * 0.5);

                // 7. Alpha Opacity
                float alpha = saturate(waterColor.a * _Transparency * (0.45 + depthFactor * 0.55 + foamFactor * 0.5));

                return half4(waterColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
