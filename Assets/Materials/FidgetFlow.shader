Shader "FidgetFlow/Kaleidoscope"
{
    Properties
    {
        _FoldCount ("Fold Count", Float) = 6
        _NoiseScale ("Noise Scale", Float) = 7
        _FlowSpeed ("Flow Speed", Float) = 0.3
        _WarpStrength ("Warp Strength", Float) = 1.5
        _ColorRamp ("Color Ramp", 2D) = "white" {}
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

            float2 kaleido(float2 uv, float folds)
            {
                float2 centered = uv - 0.5;
                float angle = atan2(centered.y, centered.x);
                float radius = length(centered);

                float segment = TWO_PI / folds;
                angle = fmod(angle, segment);
                angle = abs(angle - segment * 0.5);

                return float2(cos(angle), sin(angle)) * radius;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = kaleido(IN.uv, _FoldCount);

                float2 warpOffset = float2(
                    noise(uv * _NoiseScale + _Time.y * _FlowSpeed),
                    noise(uv * _NoiseScale - _Time.y * _FlowSpeed)
                );

                float n = noise((uv + warpOffset * _WarpStrength) * _NoiseScale);
                n = n * 0.5 + 0.5;

                half4 rampColor = SAMPLE_TEXTURE2D(_ColorRamp, sampler_ColorRamp, float2(n, 0.5));
                return rampColor;
            }
            ENDHLSL
        }
    }
}