Shader "FidgetFlow/LavaLamp"
{
    Properties
    {
        _BlobCount ("Blob Count", Range(3, 12)) = 6
        _BlobSize ("Blob Size", Range(0.2, 1.5)) = 0.7
        _FlowSpeed ("Flow Speed", Float) = 0.4
        _BlendSmoothness ("Blend Smoothness", Float) = 0.6
        _ColorA ("Color A", Color) = (1.0, 0.1, 0.4, 1)
        _ColorB ("Color B", Color) = (0.1, 0.4, 1.0, 1)
        _ColorC ("Color C", Color) = (0.8, 0.1, 1.0, 1)
        _Brightness ("Brightness", Float) = 1.2
        _CameraZ ("Camera Distance", Float) = 6.0
        _AspectRatio ("Aspect Ratio", Float) = 0.462
        _FOV ("Field of View", Float) = 1.2
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

            float _BlobCount;
            float _BlobSize;
            float _FlowSpeed;
            float _BlendSmoothness;
            float4 _ColorA;
            float4 _ColorB;
            float4 _ColorC;
            float _Brightness;
            float _CameraZ;
            float _AspectRatio;
            float _FOV;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float smin(float a, float b, float k)
            {
                float h = saturate(0.5 + 0.5 * (b - a) / k);
                return lerp(b, a, h) - k * h * (1.0 - h);
            }

            float hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            float scene(float3 p, float t)
            {
                float d = 100.0;
                int count = (int)_BlobCount;

                for (int i = 0; i < count; i++)
                {
                    float fi = float(i);
                    float speed = (hash(fi * 3.7) - 0.5) * 2.0 * _FlowSpeed;

                    // equal spread in all three axes
                    float3 offset = float3(
                        sin(t * speed       + fi * 52.5) * 2.0,
                        cos(t * speed * 0.8 + fi * 23.1) * 2.0,
                        sin(t * speed * 0.6 + fi * 17.9) * 2.0
                    );

                    float radius = _BlobSize * (0.7 + hash(fi * 7.3) * 0.6);
                    float sphere = length(p - offset) - radius;
                    d = smin(d, sphere, _BlendSmoothness);
                }

                return d;
            }

            float3 getNormal(float3 p, float t)
            {
                float2 e = float2(0.002, 0.0);
                return normalize(float3(
                    scene(p + e.xyy, t) - scene(p - e.xyy, t),
                    scene(p + e.yxy, t) - scene(p - e.yxy, t),
                    scene(p + e.yyx, t) - scene(p - e.yyx, t)
                ));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv * 2.0 - 1.0;
                // aspect and FOV both exposed as properties now
                uv.x *= _AspectRatio;

                float t = _Time.y;

                float3 camPos = float3(0.0, 0.0, _CameraZ);
                float3 rayDir = normalize(float3(uv.x, uv.y, -_FOV));

                float depth = 0.0;
                float3 p = camPos;
                bool hit = false;

                for (int i = 0; i < 80; i++)
                {
                    p = camPos + rayDir * depth;
                    float dist = scene(p, t);
                    depth += dist;
                    if (dist < 0.001)
                    {
                        hit = true;
                        break;
                    }
                    if (depth > 25.0) break;
                }

                if (!hit)
                {
                    float3 bg = float3(0.02, 0.01, 0.04);
                    return half4(bg, 1);
                }

                float3 normal = getNormal(p, t);
                float3 lightDir = normalize(float3(0.5, 1.0, 1.0));
                float3 viewDir = normalize(-rayDir);

                float diff = saturate(dot(normal, lightDir));
                float3 halfVec = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(normal, halfVec)), 48.0);
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), 3.0);

                float t1 = sin(p.x * 0.5 + t * 0.3) * 0.5 + 0.5;
                float t2 = cos(p.y * 0.4 + t * 0.2) * 0.5 + 0.5;
                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, t1);
                col = lerp(col, _ColorC.rgb, t2 * 0.6);
                col += fresnel * 0.4;

                float3 finalColor = col * (diff * 0.8 + 0.25) + spec * 0.6;
                finalColor *= _Brightness;
                finalColor *= exp(-depth * 0.04);

                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}