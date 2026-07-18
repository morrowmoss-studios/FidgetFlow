Shader "FidgetFlow/LavaLamp"
{
    Properties
    {
        _BlobCount ("Blob Count", Range(3, 15)) = 5
        _BlobSize ("Blob Size", Range(0.2, 1.5)) = 0.7
        _FlowSpeed ("Flow Speed", Float) = 0.15
        _BlendSmoothness ("Blend Smoothness", Float) = 0.4
        _Brightness ("Brightness", Float) = 1.2
        _CameraZ ("Camera Distance", Float) = 6.0
        _AspectRatio ("Aspect Ratio", Float) = 1.0
        _FOV ("Field of View", Float) = 1.2
        _ColorShuffleSpeed ("Color Shuffle Speed", Float) = 0.3
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
            float _Brightness;
            float _CameraZ;
            float _AspectRatio;
            float _FOV;
            float _ColorShuffleSpeed;
            float _AudioBass;
            float _AudioMid;
            float _AudioHigh;
            float _AudioEnergy;

            float _BlobStates[15];
            float _BlobProgress[15];

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

            float3 blobCenter(int i, float t)
            {
                float fi = float(i);
                float speed = (hash(fi * 3.7) - 0.5) * 2.0 * _FlowSpeed;

                // each blob pulses outward from its natural position on beat
                // different blobs pulse at slightly different phases so it looks organic
                float beatPulse = _AudioEnergy * 0.4 * sin(_Time.y * 8.0 + fi * 2.1);

                float3 naturalPos = float3(
                    sin(t * speed       + fi * 52.5) * 1.8,
                    cos(t * speed * 0.8 + fi * 23.1) * 1.8,
                    sin(t * speed * 0.6 + fi * 17.9) * 1.8
                );

                // push blob outward from center on beat
                float3 dir = normalize(naturalPos + float3(0.001, 0.001, 0.001));
                return naturalPos + dir * beatPulse;
            }

            float3 explosionDir(int j)
            {
                float angle = float(j) / 8.0 * TWO_PI;
                float vert = (float(j % 3) - 1.0) * 0.8;
                return normalize(float3(cos(angle), sin(vert), sin(angle)));
            }

            float scene(float3 p, float t)
            {
                float d = 100.0;
                int count = (int)_BlobCount;

                for (int i = 0; i < count; i++)
                {
                    float fi = float(i);
                    float3 center = blobCenter(i, t);

                    // blob size pulses with energy too
                    float sizeBoost = 1.0 + _AudioEnergy * 0.3;
                    float radius = _BlobSize * sizeBoost * (0.7 + hash(fi * 7.3) * 0.6);

                    float blobState = _BlobStates[i];
                    float progress = _BlobProgress[i];

                    if (blobState > 0.5 && progress > 0.0)
                    {
                        float miniRadius = radius * 0.28;
                        float spread = progress * 1.8;

                        float audioJitter = (blobState > 1.5)
                            ? _AudioEnergy * 0.25 * sin(_Time.y * 6.0 + fi * 1.7)
                            : 0.0;

                        for (int j = 0; j < 8; j++)
                        {
                            float3 dir = explosionDir(j);
                            float jitterScale = 0.8 + hash(fi * 3.3 + float(j) * 7.1) * 0.4;
                            float3 miniCenter = center + dir * (spread * jitterScale + audioJitter);
                            float miniSphere = length(p - miniCenter) - miniRadius;
                            d = smin(d, miniSphere, 0.08);
                        }
                    }
                    else
                    {
                        float sphere = length(p - center) - radius;
                        d = smin(d, sphere, _BlendSmoothness);
                    }
                }

                return d;
            }

            float3 getNormal(float3 p, float t)
            {
                float2 e = float2(0.01, 0.0);
                return normalize(float3(
                    scene(p + e.xyy, t) - scene(p - e.xyy, t),
                    scene(p + e.yxy, t) - scene(p - e.yxy, t),
                    scene(p + e.yyx, t) - scene(p - e.yyx, t)
                ));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv * 2.0 - 1.0;
                uv.x *= _AspectRatio;

                float t = _Time.y;

                float3 camPos = float3(0.0, 0.0, _CameraZ);
                float3 rayDir = normalize(float3(uv.x, uv.y, -_FOV));

                float depth = 0.0;
                float3 p = camPos;
                bool hit = false;

                for (int i = 0; i < 64; i++)
                {
                    p = camPos + rayDir * depth;
                    float dist = scene(p, t);
                    depth += dist * 0.7;
                    if (dist < 0.002)
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

                // color shift speed toned way down — subtle audio influence not frantic
                float colorShuffleSpeed = _ColorShuffleSpeed + _AudioEnergy * 0.4;
                float rainbowT = t * colorShuffleSpeed + p.x * 0.15 + p.y * 0.1;
                float3 col = 0.5 + 0.5 * cos(TWO_PI * (rainbowT + float3(0.0, 0.333, 0.667)));
                col += fresnel * 0.4;

                float3 finalColor = col * (diff * 0.8 + 0.25) + spec * 0.6;
                // brightness boost also toned down
                finalColor *= _Brightness * (1.0 + _AudioEnergy * 0.5);
                finalColor *= exp(-depth * 0.04);

                return half4(saturate(finalColor), 1);
            }
            ENDHLSL
        }
    }
}