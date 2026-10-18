Shader "Custom/StylizedAnimeSkybox"
{
    Properties
    {
        _ZenithColor ("Zenith Color", Color) = (0.15, 0.35, 0.65, 1.0)
        _HorizonColor ("Horizon Color", Color) = (0.75, 0.85, 0.95, 1.0)
        _GroundColor ("Ground Color", Color) = (0.10, 0.12, 0.15, 1.0)
        _SunColor ("Sun Color", Color) = (1.0, 0.95, 0.85, 1.0)
        _SunSize ("Sun Size", Range(0.001, 0.2)) = 0.03
        _SunBloom ("Sun Bloom Edge", Range(0.01, 1.0)) = 0.15
        _MoonColor ("Moon Color", Color) = (0.85, 0.92, 1.0, 1.0)
        _MoonSize ("Moon Size", Range(0.001, 0.2)) = 0.04
        _MoonCrescent ("Moon Crescent Offset", Range(-1.0, 1.0)) = 0.35
        _StarColor ("Star Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _StarIntensity ("Star Intensity", Range(0.0, 5.0)) = 1.0
        _StarDensity ("Star Density", Range(10.0, 300.0)) = 120.0
        _HorizonExponent ("Horizon Gradient Spread", Range(0.5, 10.0)) = 2.5
        _EclipseMode ("Eclipse Mode", Range(0.0, 1.0)) = 0.0
        _CoronaColor ("Corona Color", Color) = (1.0, 0.2, 0.4, 1.0)
        _Exposure ("Exposure", Range(0.1, 4.0)) = 1.0
        
        [Header(Shooting Stars)]
        _ShootingStarIntensity ("Shooting Star Intensity", Range(0.0, 5.0)) = 2.5
        _ShootingStarSpeed ("Shooting Star Speed", Range(0.1, 5.0)) = 1.5
        _ShootingStarColor ("Shooting Star Color", Color) = (0.92, 0.97, 1.0, 1.0)
        _ShootingStarTrail ("Shooting Star Trail Length", Range(0.05, 0.8)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

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
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 viewDir : TEXCOORD0;
            };

            fixed4 _ZenithColor;
            fixed4 _HorizonColor;
            fixed4 _GroundColor;
            fixed4 _SunColor;
            float _SunSize;
            float _SunBloom;
            fixed4 _MoonColor;
            float _MoonSize;
            float _MoonCrescent;
            fixed4 _StarColor;
            float _StarIntensity;
            float _StarDensity;
            float _HorizonExponent;
            float _EclipseMode;
            fixed4 _CoronaColor;
            float _Exposure;
            float _ShootingStarIntensity;
            float _ShootingStarSpeed;
            fixed4 _ShootingStarColor;
            float _ShootingStarTrail;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.viewDir = v.vertex.xyz;
                return o;
            }

            float Hash31(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float ShootingStar(float3 dir, float timeVal, float seed, float trailLen)
            {
                float cycle = 4.2;
                float t = frac((timeVal + seed * 3.17) / cycle);
                if (t > 0.32) return 0.0;
                float progress = t / 0.32;
                
                float phi = seed * 6.28318;
                float theta = 0.35 + frac(seed * 7.13) * 0.45;
                float3 startPos = float3(cos(phi) * sin(theta), cos(theta), sin(phi) * sin(theta));
                float3 streakDir = normalize(float3(-sin(phi) * 0.7 - 0.4, -0.65, cos(phi) * 0.4 - 0.3));
                
                float travelDist = progress * 1.10;
                float3 headPos = normalize(startPos + streakDir * travelDist);
                float3 tailPos = normalize(startPos + streakDir * max(0.0, travelDist - trailLen));
                
                float3 v = headPos - tailPos;
                float vLen = length(v);
                if (vLen < 0.001) return 0.0;
                float3 vNorm = v / vLen;
                
                float3 toDir = dir - tailPos;
                float proj = clamp(dot(toDir, vNorm), 0.0, vLen);
                float3 closest = tailPos + vNorm * proj;
                float dist = length(dir - closest);
                
                float tailFade = proj / vLen;
                float intensity = exp(-dist * dist * 3200.0) * pow(saturate(tailFade), 1.8);
                float headDist = length(dir - headPos);
                float headBloom = exp(-headDist * headDist * 1600.0) * 2.5;
                float envelope = sin(progress * 3.14159);
                return (intensity * 1.8 + headBloom) * envelope;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.viewDir);
                float y = dir.y;

                // 3-band sky gradient
                fixed4 sky;
                if (y >= 0.0)
                {
                    float t = pow(1.0 - y, _HorizonExponent);
                    sky = lerp(_ZenithColor, _HorizonColor, saturate(t));
                }
                else
                {
                    float t = pow(saturate(-y), 0.8);
                    sky = lerp(_HorizonColor, _GroundColor, t);
                }

                // Sun calculation using _WorldSpaceLightPos0
                float3 sunDir = normalize(_WorldSpaceLightPos0.xyz);
                if (length(sunDir) < 0.1) sunDir = normalize(float3(0.5, 0.7, 0.4));
                float sunDot = dot(dir, sunDir);
                float sunDist = 1.0 - sunDot;

                if (_EclipseMode > 0.5)
                {
                    // Eclipse Corona
                    float ring = smoothstep(_SunSize * 2.2, _SunSize * 1.0, sunDist) *
                                 smoothstep(_SunSize * 0.85, _SunSize * 1.05, sunDist);
                    float coronaGlow = pow(saturate(sunDot), 24.0) * 1.8;
                    sky += _CoronaColor * (ring * 4.0 + coronaGlow);
                    if (sunDist < _SunSize * 0.95)
                    {
                        sky = lerp(sky, fixed4(0.01, 0.01, 0.02, 1.0), smoothstep(_SunSize * 0.95, _SunSize * 0.85, sunDist));
                    }
                }
                else
                {
                    // Sun Disc
                    float sunCore = step(sunDist, _SunSize);
                    float sunBloom = pow(saturate(sunDot), 1.0 / max(0.001, _SunBloom)) * 0.65;
                    sky += _SunColor * (sunCore * 2.2 + sunBloom);
                }

                // Moon (opposite to Sun)
                if (_EclipseMode < 0.5)
                {
                    float3 moonDir = -sunDir;
                    float moonDot = dot(dir, moonDir);
                    float moonDist = 1.0 - moonDot;
                    if (moonDist < _MoonSize * 1.5)
                    {
                        float moonCore = smoothstep(_MoonSize, _MoonSize * 0.85, moonDist);
                        float3 crescentOffsetDir = normalize(moonDir + float3(0.025 * _MoonCrescent, 0.02 * _MoonCrescent, 0));
                        float crescentDist = 1.0 - dot(dir, crescentOffsetDir);
                        float crescentMask = smoothstep(_MoonSize * 0.65, _MoonSize * 0.90, crescentDist);
                        sky += _MoonColor * (moonCore * crescentMask * 1.6);
                    }
                }

                // Starfield (Soft pinpoint circular stars with organic twinkle)
                if (_StarIntensity > 0.01 && y > -0.05)
                {
                    float3 starCoord = dir * _StarDensity;
                    float3 starCell = floor(starCoord);
                    float3 cellUV = frac(starCoord) - 0.5;
                    float h = Hash31(starCell);
                    if (h > 0.965)
                    {
                        float d = length(cellUV);
                        float starGlow = exp(-d * d * 36.0);
                        float twinkle = sin(_Time.y * 3.0 + h * 6.28) * 0.40 + 0.60;
                        float horizonFade = saturate(y * 4.0);
                        sky += _StarColor * (_StarIntensity * starGlow * twinkle * horizonFade * 2.2);
                    }
                }

                // Dynamic Anime Shooting Stars
                if (_ShootingStarIntensity > 0.01 && y > 0.05)
                {
                    float ssTime = _Time.y * _ShootingStarSpeed;
                    float ss1 = ShootingStar(dir, ssTime, 0.18, _ShootingStarTrail);
                    float ss2 = ShootingStar(dir, ssTime, 0.54, _ShootingStarTrail * 0.85);
                    float ss3 = ShootingStar(dir, ssTime, 0.89, _ShootingStarTrail * 1.15);
                    float ssTotal = (ss1 + ss2 + ss3) * _ShootingStarIntensity;
                    sky += _ShootingStarColor * ssTotal;
                }

                return sky * _Exposure;
            }
            ENDCG
        }
    }
    Fallback "Skybox/Procedural"
}
