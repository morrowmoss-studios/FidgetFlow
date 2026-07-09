Shader "FidgetFlow/FractalCrystal"
{
    Properties
    {
        _Iterations ("Iterations", Range(2, 16)) = 10
        _Scale ("Scale", Range(1.0, 4.0)) = 2.5
        _Offset ("Offset", Float) = 1.0
        _RotationSpeed ("Rotation Speed", Float) = 0.05
        _CameraDistance ("Camera Distance", Float) = 5.0
        _MaxSteps ("Max Steps", Range(30, 120)) = 100
        _Brightness ("Brightness", Float) = 2.0
        _GlowStrength ("Glow Strength", Float) = 0.8
        _ColorA ("Color A", Color) = (0.6, 0.1, 1.0, 1)
        _ColorB ("Color B", Color) = (0.0, 0.8, 1.0, 1)
        _ColorC ("Color C", Color) = (1.0, 0.3, 0.1, 1)
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

            float _Iterations;
            float _Scale;
            float _Offset;
            float _RotationSpeed;
            float _CameraDistance;
            float _MaxSteps;
            float _Brightness;
            float _GlowStrength;
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

            // Kaleidoscopic IFS - produces clean faceted crystal geometry
            float2 kifsSDF(float3 p, int iters)
            {
                float3 z = p;
                float scale = _Scale;
                float offset = _Offset;
                float trap = 1000.0;

                for (int i = 0; i < iters; i++)
                {
                    // fold across all planes - creates kaleidoscopic symmetry
                    z = abs(z);

                    // fold across diagonal planes - creates facets
                    if (z.x < z.y) z.xy = z.yx;
                    if (z.x < z.z) z.xz = z.zx;
                    if (z.y < z.z) z.yz = z.zy;

                    // scale and pull toward corner
                    z = z * scale - float3(offset, offset, offset) * (scale - 1.0);

                    // fold back negative z
                    if (z.z < -offset * 0.5 * (scale - 1.0))
                        z.z += offset * (scale - 1.0);

                    // track orbit for coloring
                    trap = min(trap, dot(z, z));
                }

                // distance to sphere at origin
                return float2(length(z) * pow(scale, -(float)iters), sqrt(trap));
            }

            float3 getNormal(float3 p, int iters)
            {
                float2 e = float2(0.002, 0.0);
                return normalize(float3(
                    kifsSDF(p + e.xyy, iters).x - kifsSDF(p - e.xyy, iters).x,
                    kifsSDF(p + e.yxy, iters).x - kifsSDF(p - e.yxy, iters).x,
                    kifsSDF(p + e.yyx, iters).x - kifsSDF(p - e.yyx, iters).x
                ));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // portrait UV
                float2 uv = IN.uv * 2.0 - 1.0;
                uv.x *= 0.462;

                // camera orbits with vertical sweep
                float t = _Time.y * _RotationSpeed;
                float3 camPos = float3(
                    sin(t) * _CameraDistance,
                    cos(t * 0.5) * _CameraDistance * 0.5,
                    cos(t) * _CameraDistance
                );

                float3 target = float3(0, 0, 0);
                float3 forward = normalize(target - camPos);
                float3 right = normalize(cross(float3(0, 1, 0), forward));
                float3 up = cross(forward, right);
                float3 rayDir = normalize(uv.x * right + uv.y * up + 1.5 * forward);

                // raymarch
                float3 rayPos = camPos;
                float totalDist = 0.0;
                float trap = 0.0;
                float minDist = 999.0;
                bool hit = false;
                int iters = (int)_Iterations;
                int steps = (int)_MaxSteps;

                for (int i = 0; i < steps; i++)
                {
                    float2 result = kifsSDF(rayPos, iters);
                    float dist = result.x;
                    trap = result.y;
                    minDist = min(minDist, dist);

                    if (dist < 0.0005)
                    {
                        hit = true;
                        break;
                    }

                    if (totalDist > 25.0) break;

                    totalDist += dist * 0.5;
                    rayPos = camPos + rayDir * totalDist;
                }

                // background + glow from near misses
                if (!hit)
                {
                    float glow = exp(-minDist * 4.0) * _GlowStrength;
                    float3 bg = float3(0.01, 0.0, 0.03);
                    bg += _ColorA.rgb * glow * 0.5;
                    bg += _ColorB.rgb * glow * 0.3;
                    return half4(bg, 1);
                }

                // surface
                float3 normal = getNormal(rayPos, iters);
                float3 viewDir = normalize(camPos - rayPos);

                float3 lightDir1 = normalize(float3(1.0, 1.5, -1.0));
                float3 lightDir2 = normalize(float3(-1.0, -0.5, 0.5));

                float diff1 = saturate(dot(normal, lightDir1));
                float diff2 = saturate(dot(normal, lightDir2)) * 0.3;
                float amb = 0.15;

                float3 halfVec = normalize(lightDir1 + viewDir);
                float spec = pow(saturate(dot(normal, halfVec)), 80.0);

                // fresnel rim
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), 4.0);

                // orbit trap drives color
                float ot = saturate(trap * 0.15);
                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, ot);
                col = lerp(col, _ColorC.rgb, ot * ot);
                col += fresnel * _ColorB.rgb * 0.6;

                float3 finalColor = col * (diff1 + diff2 + amb) + spec * 0.9;
                finalColor *= _Brightness;
                finalColor *= exp(-totalDist * 0.02);

                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}