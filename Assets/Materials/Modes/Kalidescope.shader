Shader "FidgetFlow/Kaleidoscope"
{
    Properties
    {
        _FoldCount ("Fold Count", Float) = 6
        _NoiseScale ("Noise Scale", Float) = 7
        _FlowSpeed ("Flow Speed", Float) = 0.3
        _WarpStrength ("Warp Strength", Float) = 1.5
        _RotationSpeed ("Rotation Speed", Float) = 0.1
        _Octaves ("Noise Octaves", Range(1,6)) = 4
        _ColorRamp ("Color Ramp", 2D) = "white" {}
        _RampTiling ("Color Line Tightness", Float) = 1.0
        _RampOffset ("Color Scroll Speed", Float) = 0.0
        _RampContrast ("Color Smoothness", Range(0.1, 3)) = 1.0
        _AngleColorShift ("Angle Color Shift", Float) = 0.3

        _MaskRadius ("Circular Mask Radius", Range(0.5, 1.1)) = 0.995
        _MaskSoftness ("Circular Mask Softness", Range(0.001, 0.15)) = 0.025

        _AudioBass ("Audio Bass", Float) = 0
        _AudioMid ("Audio Mid", Float) = 0
        _AudioHigh ("Audio High", Float) = 0
        _AudioEnergy ("Audio Energy", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        ZTest LEqual
        Cull Off
        LOD 100

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

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

            CBUFFER_START(UnityPerMaterial)

                float _FoldCount;
                float _NoiseScale;
                float _FlowSpeed;
                float _WarpStrength;
                float _RotationSpeed;
                float _Octaves;

                float _RampTiling;
                float _RampOffset;
                float _RampContrast;
                float _AngleColorShift;

                float _MaskRadius;
                float _MaskSoftness;

                float _AudioBass;
                float _AudioMid;
                float _AudioHigh;
                float _AudioEnergy;

            CBUFFER_END

            TEXTURE2D(_ColorRamp);
            SAMPLER(sampler_ColorRamp);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(
                        IN.positionOS.xyz
                    );

                OUT.uv =
                    IN.uv;

                return OUT;
            }

            float2 hash2(float2 p)
            {
                p =
                    float2(
                        dot(
                            p,
                            float2(
                                127.1,
                                311.7
                            )
                        ),
                        dot(
                            p,
                            float2(
                                269.5,
                                183.3
                            )
                        )
                    );

                return
                    -1.0 +
                    2.0 *
                    frac(
                        sin(p) *
                        43758.5453123
                    );
            }

            float noise(float2 p)
            {
                float2 i =
                    floor(p);

                float2 f =
                    frac(p);

                float2 u =
                    f *
                    f *
                    (
                        3.0 -
                        2.0 *
                        f
                    );

                float n00 =
                    dot(
                        hash2(
                            i +
                            float2(0, 0)
                        ),
                        f -
                        float2(0, 0)
                    );

                float n10 =
                    dot(
                        hash2(
                            i +
                            float2(1, 0)
                        ),
                        f -
                        float2(1, 0)
                    );

                float n01 =
                    dot(
                        hash2(
                            i +
                            float2(0, 1)
                        ),
                        f -
                        float2(0, 1)
                    );

                float n11 =
                    dot(
                        hash2(
                            i +
                            float2(1, 1)
                        ),
                        f -
                        float2(1, 1)
                    );

                return
                    lerp(
                        lerp(
                            n00,
                            n10,
                            u.x
                        ),
                        lerp(
                            n01,
                            n11,
                            u.x
                        ),
                        u.y
                    );
            }

            float fbm(
                float2 p,
                int octaves
            )
            {
                float value =
                    0.0;

                float amplitude =
                    0.5;

                float frequency =
                    1.0;

                for (
                    int i = 0;
                    i < octaves;
                    i++
                )
                {
                    value +=
                        amplitude *
                        noise(
                            p *
                            frequency
                        );

                    frequency *=
                        2.0;

                    amplitude *=
                        0.5;
                }

                return value;
            }

            float2 kaleido(
                float2 uv,
                float folds,
                float rotation
            )
            {
                float2 centered =
                    uv -
                    0.5;

                float s =
                    sin(rotation);

                float c =
                    cos(rotation);

                centered =
                    float2(
                        c * centered.x -
                        s * centered.y,
                        s * centered.x +
                        c * centered.y
                    );

                float angle =
                    atan2(
                        centered.y,
                        centered.x
                    );

                float radius =
                    length(centered);

                angle =
                    fmod(
                        angle +
                        TWO_PI,
                        TWO_PI
                    );

                float segment =
                    TWO_PI /
                    folds;

                angle =
                    fmod(
                        angle,
                        segment
                    );

                angle =
                    abs(
                        angle -
                        segment *
                        0.5
                    );

                return
                    float2(
                        cos(angle),
                        sin(angle)
                    ) *
                    radius;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 maskUV =
                    IN.uv *
                    2.0 -
                    1.0;

                float maskDistance =
                    length(maskUV);

                float circleMask =
                    1.0 -
                    smoothstep(
                        _MaskRadius -
                        _MaskSoftness,
                        _MaskRadius,
                        maskDistance
                    );

                clip(
                    circleMask -
                    0.001
                );

                float iTime =
                    _Time.y;

                float dynamicFolds =
                    _FoldCount +
                    _AudioBass *
                    4.0;

                float dynamicWarp =
                    _WarpStrength *
                    (
                        1.0 +
                        _AudioBass *
                        2.0
                    );

                float dynamicFlow =
                    _FlowSpeed *
                    (
                        1.0 +
                        _AudioMid *
                        2.0
                    );

                float dynamicNoise =
                    _NoiseScale *
                    (
                        1.0 +
                        _AudioHigh *
                        0.5
                    );

                float dynamicRotation =
                    _RotationSpeed *
                    (
                        1.0 +
                        _AudioEnergy *
                        2.0
                    );

                float rotation =
                    iTime *
                    dynamicRotation;

                float2 uv =
                    kaleido(
                        IN.uv,
                        dynamicFolds,
                        rotation
                    );

                float angle =
                    atan2(
                        uv.y,
                        uv.x
                    );

                float2 warpOffset =
                    float2(
                        fbm(
                            uv *
                            dynamicNoise +
                            iTime *
                            dynamicFlow,
                            (int)_Octaves
                        ),
                        fbm(
                            uv *
                            dynamicNoise -
                            iTime *
                            dynamicFlow,
                            (int)_Octaves
                        )
                    );

                float n =
                    fbm(
                        (
                            uv +
                            warpOffset *
                            dynamicWarp
                        ) *
                        dynamicNoise,
                        (int)_Octaves
                    );

                n =
                    n *
                    0.5 +
                    0.5;

                n =
                    saturate(
                        (
                            n -
                            0.5
                        ) *
                        _RampContrast +
                        0.5
                    );

                float angleShift =
                    (
                        angle /
                        TWO_PI
                    ) *
                    _AngleColorShift;

                float dynamicColorSpeed =
                    _RampOffset +
                    _AudioEnergy *
                    0.05;

                float rampCoord =
                    frac(
                        n *
                        _RampTiling +
                        dynamicColorSpeed +
                        angleShift +
                        iTime *
                        0.02
                    );

                half4 rampColor =
                    SAMPLE_TEXTURE2D(
                        _ColorRamp,
                        sampler_ColorRamp,
                        float2(
                            rampCoord,
                            0.5
                        )
                    );

                rampColor.rgb *=
                    1.0 +
                    _AudioEnergy *
                    1.5;

                return half4(
                    saturate(
                        rampColor.rgb
                    ),
                    circleMask
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}