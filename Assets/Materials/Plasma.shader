Shader "FidgetFlow/Plasma"
{
    Properties
    {
        _Segments ("Kaleidoscope Segments", Range(2, 24)) = 8
        _Speed ("Flow Speed", Range(0, 5)) = 0.6
        _NoiseScale ("Noise Scale", Range(0.5, 10)) = 3.0
        _WarpStrength ("Warp Strength", Range(0, 2)) = 0.6
        _StreakPower ("Streak Sharpness", Range(0.5, 8)) = 3.0
        _Saturation ("Color Saturation", Range(0, 1)) = 0.9
        _ColorShift ("Color Shift", Range(0, 2)) = 0.8
        _Threshold ("Background Threshold", Range(0, 1)) = 0.35
        _CoreGlowIntensity ("Core Glow Intensity", Range(0, 5)) = 1.5
        _CoreGlowFalloff ("Core Glow Falloff", Range(1, 20)) = 6.0
        _CoreGlowColor ("Core Glow Color", Color) = (1,1,1,1)
        _Brightness ("Overall Brightness", Range(0, 3)) = 1.3
        _AudioBass ("Audio Bass", Float) = 0
        _AudioMid ("Audio Mid", Float) = 0
        _AudioHigh ("Audio High", Float) = 0
        _AudioEnergy ("Audio Energy", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _Segments;
            float _Speed;
            float _NoiseScale;
            float _WarpStrength;
            float _StreakPower;
            float _Saturation;
            float _ColorShift;
            float _Threshold;
            float _CoreGlowIntensity;
            float _CoreGlowFalloff;
            float4 _CoreGlowColor;
            float _Brightness;
            float _AudioBass;
            float _AudioMid;
            float _AudioHigh;
            float _AudioEnergy;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 5; i++)
                {
                    v += amp * valueNoise(p);
                    p *= 2.02;
                    amp *= 0.5;
                }
                return v;
            }

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv * 2.0 - 1.0;
                // aspect correct AFTER getting angle/radius so fold is symmetric
                float radius = length(float2(uv.x * 0.462, uv.y));
                float angle = atan2(uv.y, uv.x * 0.462);

                // bass pulses the segment count slightly
                float dynamicSegments = _Segments + _AudioBass * 2.0;
                float segAngle = TWO_PI / max(dynamicSegments, 1.0);
                float foldedAngle = abs(fmod(angle, segAngle) - segAngle * 0.5);

                // energy speeds up flow
                float dynamicSpeed = _Speed * (1.0 + _AudioEnergy * 1.5);
                float t = _Time.y * dynamicSpeed;

                float2 p = float2(foldedAngle * 4.0, radius * 4.0);

                // bass surges warp on beat — streaks flare outward
                float dynamicWarp = _WarpStrength * (1.0 + _AudioBass * 2.5);
                float n1 = fbm(p * _NoiseScale * 0.3 + float2(0.0, t));
                float2 warped = p + n1 * dynamicWarp * 3.0;
                float n2 = fbm(warped * _NoiseScale * 0.3 - float2(0.0, t * 0.5));

                // high frequencies sharpen streaks
                float dynamicStreak = _StreakPower * (1.0 + _AudioHigh * 0.5);
                float streaks = pow(saturate(n2), dynamicStreak);
                float falloff = saturate(1.0 - radius * 0.6);
                streaks *= lerp(0.6, 1.4, falloff);
                streaks = saturate(streaks);

                // mid shifts color faster
                float hue = frac(foldedAngle / segAngle + n2 * _ColorShift + t * 0.05 + _AudioMid * 0.3);
                float3 color = hsv2rgb(float3(hue, _Saturation, streaks));

                float mask = smoothstep(_Threshold - 0.1, _Threshold + 0.3, streaks);
                color *= mask;

                // core glow pulses with bass
                float dynamicCore = _CoreGlowIntensity * (1.0 + _AudioBass * 2.0);
                float core = exp(-radius * _CoreGlowFalloff);
                color += core * dynamicCore * _CoreGlowColor.rgb;

                color *= _Brightness * (1.0 + _AudioEnergy * 0.5);

                return half4(saturate(color), 1.0);
            }
            ENDHLSL
        }
    }
}