Shader "FidgetFlow/MandelbrotJulia"
{
    Properties
    {
        _Mode ("Mode (0=Mandelbrot 1=Julia)", Range(0,1)) = 0
        _JuliaX ("Julia X", Float) = -0.7
        _JuliaY ("Julia Y", Float) = 0.27
        _JuliaAnimSpeed ("Julia Animate Speed", Float) = 0.2
        _ZoomSpeed ("Zoom Speed", Float) = 0.0475
        _ZoomDepth ("Zoom Depth", Float) = 10.0
        _TargetX ("Target X", Float) = -0.7269
        _TargetY ("Target Y", Float) = 0.1889
        _Iterations ("Iterations", Range(64, 512)) = 200
        _ColorSpeed ("Color Speed", Float) = 0.3
        _ColorSpread ("Color Spread", Float) = 4.0
        _Brightness ("Brightness", Float) = 1.2
        _InnerBrightness ("Inner Brightness", Float) = 0.5
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

            float _Mode;
            float _JuliaX;
            float _JuliaY;
            float _JuliaAnimSpeed;
            float _ZoomSpeed;
            float _ZoomDepth;
            float _TargetX;
            float _TargetY;
            float _Iterations;
            float _ColorSpeed;
            float _ColorSpread;
            float _Brightness;
            float _InnerBrightness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float3 rainbow(float t)
            {
                t = frac(t);
                return 0.5 + 0.5 * cos(TWO_PI * (t + float3(0.0, 0.333, 0.667)));
            }

            float3 fractalColor(float2 c, float2 juliaC, float iTime)
            {
                float2 z, seed;
                if (_Mode < 0.5)
                {
                    z = float2(0, 0);
                    seed = c;
                }
                else
                {
                    z = c;
                    seed = juliaC;
                }

                float iter = 0.0;
                int maxIter = (int)_Iterations;
                float smoothIter = 0.0;
                float minLen = 1e10;

                for (int i = 0; i < maxIter; i++)
                {
                    z = float2(
                        z.x * z.x - z.y * z.y + seed.x,
                        2.0 * z.x * z.y + seed.y
                    );

                    minLen = min(minLen, dot(z, z));

                    if (dot(z, z) > 256.0)
                    {
                        smoothIter = float(i) - log2(log2(dot(z, z))) + 4.0;
                        iter = 1.0;
                        break;
                    }
                }

                if (iter == 0.0)
                {
                    float trap = sqrt(minLen);
                    float colorT1 = trap * _ColorSpread + iTime * _ColorSpeed;
                    float colorT2 = trap * _ColorSpread * 2.3 - iTime * _ColorSpeed * 0.5;
                    float3 col = rainbow(colorT1) * 0.7 + rainbow(colorT2) * 0.3;
                    return col * _InnerBrightness;
                }

                float colorT1 = smoothIter * _ColorSpread * 0.08 + iTime * _ColorSpeed;
                float colorT2 = smoothIter * _ColorSpread * 0.03 - iTime * _ColorSpeed * 0.4;
                float3 col = rainbow(colorT1) * 0.7 + rainbow(colorT2) * 0.5;
                return col * _Brightness;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float iTime = _Time.y;

                float2 uv = IN.uv * 2.0 - 1.0;
                uv.x *= 0.462;

                float2 target = float2(_TargetX, _TargetY);

                float2 juliaC = float2(
                    _JuliaX + sin(iTime * _JuliaAnimSpeed) * 0.3,
                    _JuliaY + cos(iTime * _JuliaAnimSpeed * 0.7) * 0.3
                );

                float phase = frac(iTime * _ZoomSpeed);

                // layer A
                float tA = phase * _ZoomDepth;
                float zoomA = exp(tA);
                float2 cA = uv / zoomA + target;
                float3 colA = fractalColor(cA, juliaC, iTime);

                // layer B offset by half period
                float tB = frac(phase + 0.5) * _ZoomDepth;
                float zoomB = exp(tB);
                float2 cB = uv / zoomB + target;
                float3 colB = fractalColor(cB, juliaC, iTime);

                // cosine weights - always sum to 1, perfectly smooth, no visible seam
                float wA = 0.5 - 0.5 * cos(phase * TWO_PI);
                float wB = 1.0 - wA;

                float3 col = (colA * wA + colB * wB) / (wA + wB);

                return half4(saturate(col), 1);
            }
            ENDHLSL
        }
    }
}