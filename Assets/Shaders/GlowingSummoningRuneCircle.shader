Shader "Custom/GlowingSummoningRuneCircle"
{
    Properties
    {
        _MainTex ("Rune Texture", 2D) = "white" {}
        [HDR] _GlowColor ("Glow Color", Color) = (0.2, 0.75, 1.0, 1.0)
        _GlowIntensity ("Glow Intensity", Range(0.0, 5.0)) = 1.5
        _PulseSpeed ("Pulse Frequency", Range(0.0, 10.0)) = 2.0
        _PulseAmount ("Pulse Depth", Range(0.0, 1.0)) = 0.25
        _RotationSpeed ("Rotation Speed", Range(-20.0, 20.0)) = 1.5
        _EdgeSoftness ("Edge Softness", Range(0.01, 1.0)) = 0.15
        _MasterAlpha ("Master Alpha", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One // Additive HDR Glow
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _PulseSpeed;
            float _PulseAmount;
            float _RotationSpeed;
            float _EdgeSoftness;
            float _MasterAlpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                // Center UV around (0.5, 0.5) and rotate
                float2 centeredUV = v.uv - 0.5;
                float angle = _Time.y * _RotationSpeed * 0.1;
                float s = sin(angle);
                float c = cos(angle);
                float2 rotatedUV = float2(
                    centeredUV.x * c - centeredUV.y * s,
                    centeredUV.x * s + centeredUV.y * c
                );
                o.uv = rotatedUV + 0.5;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Circular mask from center
                float2 centerVec = i.uv - 0.5;
                float dist = length(centerVec) * 2.0; // 0 at center, 1 at unit radius
                if (dist > 1.0) discard;

                float edgeMask = smoothstep(1.0, 1.0 - _EdgeSoftness, dist);

                fixed4 tex = tex2D(_MainTex, i.uv);
                float lum = max(tex.r, max(tex.g, tex.b));
                if (tex.a > 0.01 && tex.a < 0.99) lum = tex.a; // Use alpha if present

                // Living magical pulse
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                fixed4 col = _GlowColor * (lum * _GlowIntensity * pulse);
                col.a = lum * edgeMask * _MasterAlpha;

                return col;
            }
            ENDCG
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}
