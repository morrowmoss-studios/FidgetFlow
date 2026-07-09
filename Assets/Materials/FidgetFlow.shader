Shader "FidgetFlow/Kaleidoscope"
{
    Properties
    {
        _FoldCount ("Fold Count", Float) = 6
        _NoiseScale ("Noise Scale", Float) = 7
        _FlowSpeed ("Flow Speed", Float) = 0.3
        _WarpStrength ("Warp Strength", Float) = 1.5
        _RotationSpeed ("Rotation Speed", Float) = 0.1
        _Octaves ("Noise Octaves", Range(1,6)) = 4
        _ColorRamp ("Color Ramp", 2D) = "white" {}
        _RampTiling ("Color Line Tightness", Float) = 1.0
        _RampOffset ("Color Scroll Speed", Float) = 0.0
        _RampContrast ("Color Smoothness", Range(0.1, 3)) = 1.0
        _AngleColorShift ("Angle Color Shift", Float) = 0.3
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

            float _FoldCount;
            float _NoiseScale;
            float _FlowSpeed;
            float _WarpStrength;
            float _RotationSpeed;
            float _Octaves;
            float _RampTiling;
            float _RampOffset;
            float _RampContrast;
            float _AngleColorShift;

            TEXTURE2D(_ColorRamp);
            SAMPLER(sampler_ColorRamp);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float n00 = dot(hash2(i + float2(0,0)), f - float2(0,0));
                float n10 = dot(hash2(i + float2(1,0)), f - float2(1,0));
                float n01 = dot(hash2(i + float2(0,1)), f - float2(0,1));
                float n11 = dot(hash2(i + float2(1,1)), f - float2(1,1));

                return lerp(lerp(n00, n10, u.x), lerp(n01, n11, u.x), u.y);
            }

            // FBM: layered noise octaves for rich complex detail
            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            float2 kaleido(float2 uv, float folds, float rotation)
            {
                float2 centered = uv - 0.5;

                // rotate the whole pattern over time
                float s = sin(rotation);
                float c = cos(rotation);
                centered = float2(c * centered.x - s * centered.y,
                                  s * centered.x + c * centered.y);

                float angle = atan2(centered.y, centered.x);
                float radius = length(centered);

                angle = fmod(angle + TWO_PI, TWO_PI);

                float segment = TWO_PI / folds;
                angle = fmod(angle, segment);
                angle = abs(angle - segment * 0.5);

                return float2(cos(angle), sin(angle)) * radius;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float rotation = _Time.y * _RotationSpeed;
                float2 uv = kaleido(IN.uv, _FoldCount, rotation);

                // angle for color shifting
                float angle = atan2(uv.y, uv.x);

                // domain warp using fbm for richer distortion
                float2 warpOffset = float2(
                    fbm(uv * _NoiseScale + _Time.y * _FlowSpeed, (int)_Octaves),
                    fbm(uv * _NoiseScale - _Time.y * _FlowSpeed, (int)_Octaves)
                );

                float n = fbm((uv + warpOffset * _WarpStrength) * _NoiseScale, (int)_Octaves);
                n = n * 0.5 + 0.5;

                n = saturate((n - 0.5) * _RampContrast + 0.5);

                // angle shifts color phase so wedges have subtle color variety
                float angleShift = (angle / TWO_PI) * _AngleColorShift;
                float rampCoord = frac(n * _RampTiling + _RampOffset + angleShift + _Time.y * 0.02);

                half4 rampColor = SAMPLE_TEXTURE2D(_ColorRamp, sampler_ColorRamp, float2(rampCoord, 0.5));
                return rampColor;
            }
            ENDHLSL
        }
    }
}