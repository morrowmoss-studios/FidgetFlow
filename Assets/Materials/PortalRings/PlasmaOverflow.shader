Shader "FidgetFlow/PlasmaOverflow"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [HDR] _Tint ("Tint", Color) = (0.1, 0.8, 1.0, 1.0)

        _Opacity ("Opacity", Range(0, 1)) = 1
        _Brightness ("Brightness", Range(0, 8)) = 1.65
        _CoreStrength ("Core Strength", Range(0, 3)) = 1.05
        _RimStrength ("Rim Strength", Range(0, 4)) = 0.75
        _RimPower ("Rim Power", Range(0.25, 8)) = 2.8

        _FlowSpeed ("Flow Speed", Range(0, 6)) = 0.75
        _FlowScale ("Flow Scale", Range(0.25, 12)) = 3.5
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.38
        _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+35"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "PlasmaOverflow"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseColor;
                float4 _Tint;

                float _Opacity;
                float _Brightness;
                float _CoreStrength;
                float _RimStrength;
                float _RimPower;

                float _FlowSpeed;
                float _FlowScale;
                float _NoiseStrength;
                float _Seed;

            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);

                local = local * local * (3.0 - 2.0 * local);

                float a = Hash21(cell);
                float b = Hash21(cell + float2(1, 0));
                float c = Hash21(cell + float2(0, 1));
                float d = Hash21(cell + float2(1, 1));

                return lerp(
                    lerp(a, b, local.x),
                    lerp(c, d, local.x),
                    local.y
                );
            }

            float FBM(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;

                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    value += ValueNoise(p) * amplitude;

                    p = mul(
                        float2x2(
                            0.80, -0.60,
                            0.60,  0.80
                        ),
                        p
                    ) * 2.03;

                    amplitude *= 0.5;
                }

                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalize(normalInputs.normalWS);
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirection =
                    normalize(GetWorldSpaceViewDir(input.positionWS));

                float facing =
                    saturate(dot(normalWS, viewDirection));

                float rim =
                    pow(
                        saturate(1.0 - facing),
                        _RimPower
                    );

                float core =
                    pow(facing, 0.72);

                float radialDistance =
                    input.uv.y;

                float edgeMask =
                    1.0 -
                    smoothstep(
                        0.76,
                        1.0,
                        radialDistance
                    );

                float2 flowUV =
                    float2(
                        input.uv.x * _FlowScale + _Seed,
                        input.uv.y * _FlowScale -
                        _Time.y * _FlowSpeed
                    );

                float broadNoise = FBM(flowUV);

                float fineNoise =
                    FBM(
                        flowUV * 2.7 +
                        float2(
                            _Seed * 0.17,
                            -_Time.y * _FlowSpeed * 0.40
                        )
                    );

                float noisyBody =
                    edgeMask *
                    lerp(
                        1.0 - _NoiseStrength,
                        1.0,
                        broadNoise
                    );

                clip(noisyBody - 0.015);

                float3 baseColor =
                    _BaseColor.rgb *
                    _Tint.rgb;

                float plasmaDetail =
                    saturate(
                        broadNoise * 0.75 +
                        fineNoise * 0.55
                    );

                float lighting =
                    0.52 +
                    core * _CoreStrength +
                    rim * _RimStrength;

                float3 color =
                    baseColor *
                    lighting *
                    _Brightness;

                color +=
                    baseColor *
                    plasmaDetail *
                    0.85;

                color +=
                    plasmaDetail.xxx *
                    0.08;

                float alpha =
                    saturate(
                        noisyBody *
                        (
                            0.78 +
                            core * 0.22 +
                            rim * 0.08
                        ) *
                        _Opacity *
                        _Tint.a *
                        _BaseColor.a
                    );

                return half4(color, alpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
