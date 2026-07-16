Shader "FidgetFlow/FibonacciTunnel"
{
    Properties
    {
        _ZoomSpeed ("Zoom Speed", Float) = 0.15
        _RotationSpeed ("Rotation Speed", Float) = 0.05
        _ColorSpeed ("Color Speed", Float) = 0.2
        _Brightness ("Brightness", Float) = 2.0
        _Divisions ("Divisions", Float) = 8.0
        _LineWidth ("Line Width", Float) = 0.15
        _GoldenSpiral ("Golden Spiral Mix", Range(0,1)) = 0.5
        _Layers ("Layers", Float) = 4.0
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

            float _ZoomSpeed;
            float _RotationSpeed;
            float _ColorSpeed;
            float _Brightness;
            float _Divisions;
            float _LineWidth;
            float _GoldenSpiral;
            float _Layers;

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

            float4 fibLayer(float2 uv, float zoom, float iTime)
            {
                float r = length(uv);
                if (r < 0.0001) return float4(0,0,0,0);

                float phi = 1.6180339887;
                float logR = log(r + 0.001) + zoom;

                // camera counter-spin applied per layer
                float angle = atan2(uv.y, uv.x) + iTime * _RotationSpeed;

                // RADIAL RINGS: fibonacci-spaced concentric rings
                // logR in phi-space gives rings at golden ratio intervals
                float ringPhase = frac(logR / log(phi));
                float ring = exp(-pow((ringPhase - 0.5) * 2.0, 2.0) / (_LineWidth * _LineWidth));

                // ANGULAR SPOKES: divide circle by fibonacci number
                // no atan2 seam because we use the full angle range
                float spokePhase = frac(angle * _Divisions / TWO_PI);
                float spoke = exp(-pow((spokePhase - 0.5) * 2.0, 2.0) / (_LineWidth * _LineWidth));

                // GOLDEN SPIRAL: mix in a logarithmic spiral on top
                // this is where the fibonacci nature really shows
                float spiralAngle = angle / TWO_PI;
                float spiralPhase = frac(logR * 1.618 - spiralAngle * _Divisions);
                float spiral = exp(-pow((spiralPhase - 0.5) * 2.0, 2.0) / (_LineWidth * _LineWidth));

                // combine: rings + spokes = grid, mix with spiral for golden ratio feel
                float grid = max(ring, spoke);
                float finalGlow = lerp(grid, max(grid, spiral), _GoldenSpiral);

                float radialFade = smoothstep(0.0, 0.05, r) * smoothstep(1.6, 0.2, r);
                finalGlow *= radialFade;

                // color: rings by radius, spokes by angle, combined
                float colorR = logR * 0.3 + iTime * _ColorSpeed;
                float colorA = angle / TWO_PI + iTime * _ColorSpeed * 0.7;
                float3 col = rainbow(colorR) * ring + rainbow(colorA) * spoke * 0.8 + rainbow(colorR + colorA) * spiral * 0.6;
                col /= max(ring + spoke * 0.8 + spiral * 0.6, 0.001);

                return float4(col * finalGlow, finalGlow);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float iTime = _Time.y;

                float2 uv = IN.uv * 2.0 - 1.0;
                uv.x *= 0.462;

                float phi = 1.6180339887;
                float logPhi = log(phi);
                float phase = frac(iTime * _ZoomSpeed / logPhi);
                int layers = (int)_Layers;

                float3 totalColor = float3(0,0,0);

                for (int i = 0; i < layers; i++)
                {
                    float fi = float(i);
                    float layerPhase = frac(phase + fi / _Layers);
                    float zoom = layerPhase * logPhi;

                    float w = sin(layerPhase * PI);
                    w = w * w;

                    float4 layer = fibLayer(uv, zoom, iTime);
                    totalColor += layer.rgb * w;
                }

                totalColor *= _Brightness;

                // glowing center core
                float r = length(uv);
                totalColor += rainbow(iTime * _ColorSpeed * 0.5) * exp(-r * 10.0) * 0.5;

                return half4(saturate(totalColor), 1);
            }
            ENDHLSL
        }
    }
}