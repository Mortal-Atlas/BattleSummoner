Shader "Custom/BioluminescentFirefly"
{
    Properties
    {
        [HDR] _GlowColor ("Glow Color", Color) = (0.4, 0.95, 1.0, 1.0)
        _CoreSize ("Core Size", Range(0.01, 0.5)) = 0.15
        _SoftFalloff ("Soft Falloff", Range(0.5, 4.0)) = 1.8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One // Additive HDR Bloom glow
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            fixed4 _GlowColor;
            float _CoreSize;
            float _SoftFalloff;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 d = i.uv - 0.5;
                float dist = length(d) * 2.0; // 0 at center, 1 at edge
                if (dist > 1.0) discard;

                float halo = pow(saturate(1.0 - dist), _SoftFalloff);
                float core = smoothstep(_CoreSize, 0.0, dist) * 2.0;

                float intensity = halo + core;
                return i.color * _GlowColor * intensity;
            }
            ENDCG
        }
    }
    Fallback Off
}
