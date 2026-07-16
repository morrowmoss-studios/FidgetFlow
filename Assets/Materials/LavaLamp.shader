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

            float smin(float a, float b, float k)
            {
                float h = saturate(0.5 + 0.5 * (b - a) / k);
                return lerp(b, a, h) - k * h * (1.0 - h);
            }

            float hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            float scene(float3 p, float t, float blobSize, float flowSpeed, float blendSmooth)
            {
                float d = 100.0;
                int count = (int)_BlobCount;

                for (int i = 0; i < count; i++)
                {
                    float fi = float(i);
                    float speed = (hash(fi * 3.7) - 0.5) * 2.0 * flowSpeed;

                    float3 offset = float3(
                        sin(t * speed       + fi * 52.5) * 1.8,
                        cos(t * speed * 0.8 + fi * 23.1) * 1.8,
                        sin(t * speed * 0.6 + fi * 17.9) * 1.8
                    );

                    float radius = blobSize * (0.7 + hash(fi * 7.3) * 0.6);
                    float sphere = length(p - offset) - radius;
                    d = smin(d, sphere, blendSmooth);
                }

                return d;
            }

            float3 getNormal(float3 p, float t, float blobSize, float flowSpeed, float blendSmooth)
            {
                float2 e = float2(0.002, 0.0);
                return normalize(float3(
                    scene(p + e.xyy, t, blobSize, flowSpeed, blendSmooth) - scene(p - e.xyy, t, blobSize, flowSpeed, blendSmooth),
                    scene(p + e.yxy, t, blobSize, flowSpeed, blendSmooth) - scene(p - e.yxy, t, blobSize, flowSpeed, blendSmooth),
                    scene(p + e.yyx, t, blobSize, flowSpeed, blendSmooth) - scene(p - e.yyx, t, blobSize, flowSpeed, blendSmooth)
                ));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv * 2.0 - 1.0;
                uv.x *= _AspectRatio;

                float t = _Time.y;

                // bass swells blob size on beat
                float dynamicBlobSize = _BlobSize * (1.0 + _AudioBass * 0.8);
                // mid speeds up flow
                float dynamicFlow = _FlowSpeed * (1.0 + _AudioMid * 2.0);
                // high adds jitter by tightening blend
                float dynamicBlend = _BlendSmoothness * (1.0 - _AudioHigh * 0.4);
                dynamicBlend = max(dynamicBlend, 0.1);

                float3 camPos = float3(0.0, 0.0, _CameraZ);
                float3 rayDir = normalize(float3(uv.x, uv.y, -_FOV));

                float depth = 0.0;
                float3 p = camPos;
                bool hit = false;

                for (int i = 0; i < 80; i++)
                {
                    p = camPos + rayDir * depth;
                    float dist = scene(p, t, dynamicBlobSize, dynamicFlow, dynamicBlend);
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

                float3 normal = getNormal(p, t, dynamicBlobSize, dynamicFlow, dynamicBlend);
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
                // energy drives brightness
                finalColor *= _Brightness * (1.0 + _AudioEnergy * 1.5);
                finalColor *= exp(-depth * 0.04);

                return half4(saturate(finalColor), 1);
            }
            ENDHLSL
        }
    }
}