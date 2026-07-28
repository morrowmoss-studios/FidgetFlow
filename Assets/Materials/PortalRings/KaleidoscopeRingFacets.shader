Shader "FidgetFlow/KaleidoscopeRingFacets"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [HDR] _Tint ("Tint", Color) = (0.1, 0.8, 1.0, 1.0)

        _Opacity ("Opacity", Range(0, 1)) = 1
        _Brightness ("Brightness", Range(0, 8)) = 1.8
        _MirrorStrength ("Mirror Strength", Range(0, 3)) = 1.15
        _EdgeGlow ("Edge Glow", Range(0, 4)) = 0.85
        _EdgePower ("Edge Power", Range(0.25, 8)) = 2.2

        _ShimmerSpeed ("Shimmer Speed", Range(0, 8)) = 1.15
        _ShimmerStrength ("Shimmer Strength", Range(0, 1)) = 0.25
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
            Name "KaleidoscopeRingFacets"

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
                float _MirrorStrength;
                float _EdgeGlow;
                float _EdgePower;

                float _ShimmerSpeed;
                float _ShimmerStrength;
                float _Seed;

            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(
                        input.normalOS
                    );

                output.positionHCS =
                    positionInputs.positionCS;

                output.positionWS =
                    positionInputs.positionWS;

                output.normalWS =
                    normalize(
                        normalInputs.normalWS
                    );

                output.uv =
                    input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS =
                    normalize(
                        input.normalWS
                    );

                float3 viewDirection =
                    normalize(
                        GetWorldSpaceViewDir(
                            input.positionWS
                        )
                    );

                float facing =
                    saturate(
                        dot(
                            normalWS,
                            viewDirection
                        )
                    );

                float edge =
                    pow(
                        saturate(
                            1.0 -
                            facing
                        ),
                        _EdgePower
                    );

                float mirrorFlash =
                    pow(
                        facing,
                        3.5
                    ) *
                    _MirrorStrength;

                float shimmer =
                    1.0 +
                    sin(
                        _Time.y *
                        _ShimmerSpeed *
                        6.0 +
                        _Seed
                    ) *
                    _ShimmerStrength;

                float3 color =
                    _BaseColor.rgb *
                    _Tint.rgb;

                color *=
                    (
                        0.45 +
                        mirrorFlash +
                        edge *
                        _EdgeGlow
                    ) *
                    _Brightness *
                    shimmer;

                color +=
                    mirrorFlash.xxx *
                    0.16;

                float alpha =
                    saturate(
                        (
                            0.72 +
                            mirrorFlash *
                            0.20 +
                            edge *
                            0.10
                        ) *
                        _Opacity *
                        _Tint.a *
                        _BaseColor.a
                    );

                return half4(
                    color,
                    alpha
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}
