Shader "FidgetFlow/UrchinZoom"
{
    Properties
    {
        _Speed ("Zoom Speed", Float) = 0.5
        _RotationSpeed ("Rotation Speed", Float) = 1.0
        _ColorShift ("Color Shift", Float) = 0.0
        _Brightness ("Brightness", Float) = 1.0
        _StreakAmount ("Streak Amount", Float) = 3.0
        _TwistAmount ("Twist Amount", Float) = 1.0
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

            float _Speed;
            float _RotationSpeed;
            float _ColorShift;
            float _Brightness;
            float _StreakAmount;
            float _TwistAmount;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float3 H(float a)
            {
                return cos(float3(PI, HALF_PI, 0) + (a + _ColorShift) * TWO_PI) * 0.5 + 0.5;
            }

            // bug-style rotation matrix from angle
            float2x2 rot2(float a)
            {
                float s = sin(a), c = cos(a);
                return float2x2(c, -s, s, c);
            }

            float urchin_map(float3 u, float v, float T, float depth)
            {
                float l = 5.0, f = 1e10, y, z;

                // bug twist: rotate UV space based on depth and time
                float twist = depth * 0.001 * _TwistAmount;
                u.xy = mul(rot2(twist + T * _RotationSpeed), u.xy);

                u.xy = float2(atan2(u.x, u.y), length(u.xy));
                u.x += T * v * PI * 0.7;

                [unroll]
                for (int ii = 1; ii <= 5; ii++)
                {
                    float i = float(ii);
                    float3 p = u;
                    y = round((p.y - i) / l) * l + i;
                    p.x *= y;
                    p.x -= y * y * T * PI;
                    p.x -= round(p.x / TWO_PI) * TWO_PI;
                    p.y -= y;
                    z = cos(y * T * TWO_PI) * 0.5 + 0.5;

                    // streak: as zoom depth increases, tubes elongate on x axis
                    float streak = 1.0 + depth * 0.002 * _StreakAmount;
                    float2 tubeP = float2(p.x / streak, p.y);
                    float tubeDist = length(tubeP);

                    f = min(f, max(tubeDist, -p.z - z * 9.0) - 0.1 - z * 0.2 - p.z / 100.0);
                }
                return f;
            }

            float3 urchin_color(float2 uv, float T, float iTime)
            {
                float v = 43.333;
                float2 m = float2(0.0, 0.5);

                float3 ray = normalize(float3(uv, 1.0));

                // infinite zoom: camera flies forward continuously
                float3 cam = float3(0, 0, -130.0 + sin(iTime * _Speed) * 80.0);

                float3 c = float3(0, 0, 0);
                float3 p = float3(0, 0, 0);
                float3 k = float3(0, 0, 0);
                float d = 0, s = 0, f = 0, z = 0, r = 0;
                bool b = false;

                [loop]
                for (int i = 0; i < 60; i++)
                {
                    p = ray * d + cam;
                    p.xy /= v;
                    r = length(p.xy);
                    z = abs(1.0 - r * r);
                    b = r < 1.0;
                    if (b) z = sqrt(z);
                    p.xy /= (z + 1.0);
                    p.xy -= m;
                    p.xy *= v;
                    float2 wa = p.z / 8.0 + T * 300.0 + float2(0, HALF_PI) + z * 0.5;
                    p.xy -= cos(wa) * 0.2;

                    // pass current depth into map for streak/twist effects
                    s = urchin_map(p, v, T, d);

                    r = length(p.xy);
                    f = cos(round(r) * T * TWO_PI) * 0.5 + 0.5;
                    k = H(0.2 - f / 3.0 + T + p.z / 200.0);
                    if (b) k = 1.0 - k;

                    float contrib = min(exp(s / -0.05), 50.0)
                        * (f + 0.01)
                        * min(z, 1.0)
                        * sqrt(saturate(cos(r * TWO_PI) * 0.5 + 0.5));
                    c += contrib * k * k;

                    d += s * clamp(z, 0.3, 0.9);
                    if (s < 1e-3 || d > 1e3) break;
                }

                float safeS = max(abs(s), 0.0001);
                c += min(exp(-p.z - f * 9.0) * z * k * 0.01 / safeS, 1.0);

                float2 j = p.xy / v + m;
                c /= clamp(dot(j, j) * 4.0, 0.04, 4.0);
                c = pow(max(c, 0.0001), 1.0 / 2.2);

                return c;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv * 2.0 - 1.0;
                uv.x *= 0.462;

                float iTime = _Time.y;
                float T = iTime / 300.0;

                float3 col = urchin_color(uv, T, iTime);
                col *= _Brightness;

                return half4(saturate(col), 1);
            }
            ENDHLSL
        }
    }
}