Shader "FidgetFlow/LavaLampBlobs"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [HDR] _CellColor ("Cell Color", Color) = (0.1, 0.8, 1.0, 1.0)

        _Opacity ("Opacity", Range(0, 1)) = 1
        _Glow ("Glow", Range(0, 8)) = 1.65
        _FresnelPower ("Fresnel Power", Range(0.25, 8)) = 3.5
        _CoreStrength ("Core Strength", Range(0, 3)) = 1.15

        _Wobble ("Shape Wobble", Range(0, 0.8)) = 0.25
        _WobbleSpeed ("Wobble Speed", Range(0, 8)) = 1.6
        _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+30"
        }

        Blend SrcAlpha OneMinusSrcAlpha

        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "LavaLampBlobs"

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
                float4 _CellColor;

                float _Opacity;
                float _Glow;
                float _FresnelPower;
                float _CoreStrength;

                float _Wobble;
                float _WobbleSpeed;
                float _Seed;

            CBUFFER_END

            float ShapeNoise(
                float3 position,
                float seed,
                float timeValue
            )
            {
                float a =
                    sin(
                        position.x * 11.7 +
                        position.y * 7.3 +
                        seed +
                        timeValue
                    );

                float b =
                    sin(
                        position.y * 13.1 +
                        position.z * 9.2 +
                        seed * 1.37 -
                        timeValue * 1.23
                    );

                float c =
                    sin(
                        position.z * 10.4 +
                        position.x * 8.6 +
                        seed * 2.11 +
                        timeValue * 0.74
                    );

                return
                    (a + b + c) /
                    3.0;
            }

            float3 LavaPalette(float t)
            {
                t = frac(t);

                float3 magenta =
                    float3(1.00, 0.03, 0.78);

                float3 violet =
                    float3(0.42, 0.05, 1.00);

                float3 cyan =
                    float3(0.00, 0.95, 1.00);

                float3 green =
                    float3(0.18, 1.00, 0.18);

                float3 yellow =
                    float3(1.00, 0.95, 0.02);

                float3 orange =
                    float3(1.00, 0.28, 0.02);

                float section =
                    t * 6.0;

                float localT =
                    frac(section);

                localT =
                    localT *
                    localT *
                    (3.0 - 2.0 * localT);

                if (section < 1.0)
                    return lerp(
                        magenta,
                        violet,
                        localT
                    );

                if (section < 2.0)
                    return lerp(
                        violet,
                        cyan,
                        localT
                    );

                if (section < 3.0)
                    return lerp(
                        cyan,
                        green,
                        localT
                    );

                if (section < 4.0)
                    return lerp(
                        green,
                        yellow,
                        localT
                    );

                if (section < 5.0)
                    return lerp(
                        yellow,
                        orange,
                        localT
                    );

                return lerp(
                    orange,
                    magenta,
                    localT
                );
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float timeValue =
                    _Time.y *
                    _WobbleSpeed;

                float shapeNoise =
                    ShapeNoise(
                        input.positionOS.xyz,
                        _Seed,
                        timeValue
                    );

                float3 displacedPosition =
                    input.positionOS.xyz +
                    input.normalOS *
                    shapeNoise *
                    _Wobble *
                    0.08;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        displacedPosition
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

                float fresnel =
                    pow(
                        saturate(
                            1.0 -
                            facing
                        ),
                        _FresnelPower
                    );

                float core =
                    pow(
                        facing,
                        0.85
                    );

                float pulse =
                    0.94 +
                    sin(
                        _Time.y *
                        _WobbleSpeed *
                        1.7 +
                        _Seed
                    ) *
                    0.06;

                /*
                    ONE solid spectrum color per blob.

                    _Seed is unique per spawned blob, so each blob receives
                    one color from the full Lava Lamp palette. The color does
                    not vary across the blob's surface.
                */
                float palettePosition =
                    frac(
                        _Seed *
                        0.61803398875
                    );

                float3 blobColor =
                    LavaPalette(
                        palettePosition
                    );

                float3 baseColor =
                    _BaseColor.rgb *
                    blobColor;

                float coreLighting =
                    0.72 +
                    core *
                    _CoreStrength;

                float rimLighting =
                    fresnel *
                    _Glow;

                float3 color =
                    baseColor *
                    (
                        coreLighting +
                        rimLighting
                    ) *
                    pulse;

                float highlight =
                    pow(
                        facing,
                        5.0
                    );

                color +=
                    baseColor *
                    highlight *
                    0.55;

                color +=
                    highlight.xxx *
                    0.22;

                float alphaShape =
                    saturate(
                        0.82 +
                        core * 0.28 +
                        fresnel * 0.08
                    );

                float alpha =
                    saturate(
                        alphaShape *
                        _Opacity *
                        _CellColor.a *
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
