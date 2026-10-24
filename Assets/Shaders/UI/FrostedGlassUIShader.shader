Shader "BattleSummoner/UI/FrostedGlassAppCard"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlassTint ("Obsidian Glass Tint", Color) = (0.055, 0.071, 0.11, 0.86)
        _BorderColor ("Inner Specular Border Color", Color) = (0.898, 0.757, 0.471, 0.65)
        _BorderWidth ("Border UV Width", Range(0.0, 0.08)) = 0.015
        _SheenIntensity ("Diagonal Glass Sheen", Range(0.0, 0.35)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _GlassTint;
                half4 _BorderColor;
                half  _BorderWidth;
                half  _SheenIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 col = _GlassTint * IN.color;

                // Subtle diagonal app-card glass sheen
                half diagonal = saturate(1.0 - abs((IN.uv.x + IN.uv.y) - 1.0) * 2.2);
                col.rgb += diagonal * _SheenIntensity;

                // Crisp 1px-style specular border highlight
                float2 distToEdge = min(IN.uv, 1.0 - IN.uv);
                float edgeFactor = min(distToEdge.x, distToEdge.y);
                half isBorder = step(edgeFactor, _BorderWidth);
                col = lerp(col, _BorderColor, isBorder * _BorderColor.a);

                col.a *= texSample.a;
                return col;
            }
            ENDHLSL
        }
    }
}
