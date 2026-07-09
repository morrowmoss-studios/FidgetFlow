Shader "FidgetFlow/FractalCrystal"
{
    Properties
    {
        _FoldIterations ("Fold Iterations", Range(2, 12)) = 8
        _Scale ("Scale", Range(1.5, 3.0)) = 2.0
        _RotationSpeed ("Rotation Speed", Float) = 0.1
        _CameraDistance ("Camera Distance", Float) = 3.0
        _MaxSteps ("Max Steps", Range(30, 120)) = 80
        _Brightness ("Brightness", Float) = 1.5
        _ColorA ("Color A", Color) = (1, 0.3, 0.8, 1)
        _ColorB ("Color B", Color) = (0.1, 0.8, 1.0, 1)
        _ColorC ("Color C", Color) = (0.9, 0.6, 0.1, 1)
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

            float _FoldIterations;
            float _Scale;
            float _RotationSpeed;
            float _CameraDistance;
            float _MaxSteps;
            float _Brightness;
            float4 _ColorA;
            float4 _ColorB;
            float4 _ColorC;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // Mandelbox SDF with orbit trap for coloring
            float2 mandelboxSDF(float3 p, int iters)
            {
                float3 z = p;
                float dr = 1.0;
                float r = 0.0;
                float orbit = 0.0;
                float minR = 0.25;
                float fixedR = 1.0;

                for (int i = 0; i < iters; i++)
                {
                    // box fold
                    z = clamp(z, -1.0, 1.0) * 2.0 - z;

                    // sphere fold
                    r = dot(z, z);
                    if (r < minR)
                    {
                        float temp = fixedR / minR;
                        z *= temp;
                        dr *= temp;
                    }
                    else if (r < fixedR)
                    {
                        float temp = fixedR / r;
                        z *= temp;
                        dr *= temp;
                    }

                    // scale and offset back to origin
                    z = z * _Scale + p;
                    dr = dr * abs(_Scale) + 1.0;

                    // orbit trap: track how close z gets to origin
                    orbit = max(orbit, length(z) * 0.1);
                }

                return float2(length(z) / abs(dr), orbit);
            }

            // normal via SDF gradient sampling
            float3 getNormal(float3 p, int iters)
            {
                float2 e = float2(0.001, 0.0);
                return normalize(float3(
                    mandelboxSDF(p + e.xyy, iters).x - mandelboxSDF(p - e.xyy, iters).x,
                    mandelboxSDF(p + e.yxy, iters).x - mandelboxSDF(p - e.yxy, iters).x,
                    mandelboxSDF(p + e.yyx, iters).x - mandelboxSDF(p - e.yyx, iters).x
                ));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // screen coords centered at 0 with aspect correction
                float2 uv = IN.uv * 2.0 - 1.0;
                uv.x *= 0.462; // 1170/2532 portrait aspect

                // orbiting camera
                float t = _Time.y * _RotationSpeed;
                float3 camPos = float3(
                    sin(t) * _CameraDistance,
                    sin(t * 0.4) * 1.5,
                    cos(t) * _CameraDistance
                );
                float3 target = float3(0, 0, 0);

                // camera matrix
                float3 forward = normalize(target - camPos);
                float3 right = normalize(cross(float3(0, 1, 0), forward));
                float3 up = cross(forward, right);

                // ray direction
                float3 rayDir = normalize(uv.x * right + uv.y * up + 1.5 * forward);

                // raymarch
                float3 rayPos = camPos;
                float totalDist = 0.0;
                float minDist = 999.0;
                float orbit = 0.0;
                bool hit = false;
                int iters = (int)_FoldIterations;
                int maxSteps = (int)_MaxSteps;

                for (int i = 0; i < maxSteps; i++)
                {
                    float2 result = mandelboxSDF(rayPos, iters);
                    float dist = result.x;
                    orbit = result.y;
                    minDist = min(minDist, dist);

                    if (dist < 0.001)
                    {
                        hit = true;
                        break;
                    }

                    if (totalDist > 20.0) break;

                    // conservative step multiplier keeps us from overshooting
                    totalDist += dist * 0.5;
                    rayPos = camPos + rayDir * totalDist;
                }

                // background: dark with subtle glow from near misses
                if (!hit)
                {
                    float glow = exp(-minDist * 5.0) * 0.3;
                    return half4(_ColorA.rgb * glow, 1);
                }

                // surface normal
                float3 normal = getNormal(rayPos, iters);

                // lighting
                float3 lightDir = normalize(float3(1, 1, -1));
                float diff = saturate(dot(normal, lightDir));
                float3 viewDir = normalize(camPos - rayPos);
                float3 halfVec = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(normal, halfVec)), 32.0);

                // orbit trap drives color blend across 3 user colors
                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, saturate(orbit * 2.0));
                col = lerp(col, _ColorC.rgb, saturate(orbit * orbit * 3.0));

                // combine lighting and color
                float3 finalColor = col * (diff * 0.8 + 0.2) + spec * 0.5;
                finalColor *= _Brightness;
                finalColor *= exp(-totalDist * 0.05); // depth fog

                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}