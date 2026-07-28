Shader "FidgetFlow/FibonacciTunnel"
{
    Properties
    {
        _ZoomSpeed ("Zoom Speed", Float) = 0.15
        _RotationSpeed ("Rotation Speed", Float) = 0.05
        _ColorSpeed ("Color Speed", Float) = 0.2
        _Brightness ("Brightness", Float) = 2.0
        _Divisions ("Divisions", Float) = 8.0
        _LineWidth ("Line Width", Float) = 0.15
        _GoldenSpiral ("Golden Spiral Mix", Range(0,1)) = 0.5
        _Layers ("Layers", Float) = 4.0

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
        ZWrite Off
        Cull Off

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
                float _ZoomSpeed;
                float _RotationSpeed;
                float _ColorSpeed;
                float _Brightness;
                float _Divisions;
                float _LineWidth;
                float _GoldenSpiral;
                float _Layers;

                float _MaskRadius;
                float _MaskSoftness;

                float _AudioBass;
                float _AudioMid;
                float _AudioHigh;
                float _AudioEnergy;
            CBUFFER_END

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

            float3 rainbow(float t)
            {
                t =
                    frac(t);

                return
                    0.5 +
                    0.5 *
                    cos(
                        TWO_PI *
                        (
                            t +
                            float3(
                                0.0,
                                0.333,
                                0.667
                            )
                        )
                    );
            }

            float4 fibLayer(
                float2 uv,
                float zoom,
                float iTime
            )
            {
                float r =
                    length(uv);

                if (r < 0.0001)
                {
                    return float4(
                        0,
                        0,
                        0,
                        0
                    );
                }

                float phi =
                    1.6180339887;

                float dynamicRotation =
                    _RotationSpeed *
                    (
                        1.0 +
                        _AudioBass *
                        3.0
                    );

                float angle =
                    atan2(
                        uv.y,
                        uv.x
                    ) +
                    iTime *
                    dynamicRotation;

                float logR =
                    log(
                        r +
                        0.001
                    ) +
                    zoom;

                float ringScale =
                    1.0 +
                    _AudioBass *
                    2.0;

                float ringPhase =
                    frac(
                        logR *
                        ringScale /
                        log(phi)
                    );

                float ring =
                    exp(
                        -pow(
                            (
                                ringPhase -
                                0.5
                            ) *
                            2.0,
                            2.0
                        ) /
                        (
                            _LineWidth *
                            _LineWidth
                        )
                    );

                float spokeDivisions =
                    _Divisions *
                    (
                        1.0 +
                        _AudioMid *
                        0.5
                    );

                float spokePhase =
                    frac(
                        angle *
                        spokeDivisions /
                        TWO_PI
                    );

                float spoke =
                    exp(
                        -pow(
                            (
                                spokePhase -
                                0.5
                            ) *
                            2.0,
                            2.0
                        ) /
                        (
                            _LineWidth *
                            _LineWidth
                        )
                    );

                float spiralTightness =
                    1.618 +
                    _AudioHigh *
                    2.0;

                float spiralPhase =
                    frac(
                        logR *
                        spiralTightness -
                        angle *
                        _Divisions
                    );

                float spiral =
                    exp(
                        -pow(
                            (
                                spiralPhase -
                                0.5
                            ) *
                            2.0,
                            2.0
                        ) /
                        (
                            _LineWidth *
                            _LineWidth
                        )
                    );

                float grid =
                    max(
                        ring,
                        spoke
                    );

                float finalGlow =
                    lerp(
                        grid,
                        max(
                            grid,
                            spiral
                        ),
                        _GoldenSpiral
                    );

                float radialFade =
                    smoothstep(
                        0.0,
                        0.05,
                        r
                    ) *
                    smoothstep(
                        1.6,
                        0.2,
                        r
                    );

                finalGlow *=
                    radialFade;

                float dynamicColorSpeed =
                    _ColorSpeed *
                    (
                        1.0 +
                        _AudioEnergy *
                        2.0
                    );

                float colorR =
                    logR *
                    0.3 +
                    iTime *
                    dynamicColorSpeed;

                float colorA =
                    angle /
                    TWO_PI +
                    iTime *
                    dynamicColorSpeed *
                    0.7;

                float3 col =
                    rainbow(
                        colorR
                    ) *
                    ring +
                    rainbow(
                        colorA
                    ) *
                    spoke *
                    0.8 +
                    rainbow(
                        colorR +
                        colorA
                    ) *
                    spiral *
                    0.6;

                col /=
                    max(
                        ring +
                        spoke *
                        0.8 +
                        spiral *
                        0.6,
                        0.001
                    );

                return float4(
                    col *
                    finalGlow,
                    finalGlow
                );
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float iTime =
                    _Time.y;

                /*
                    Use the untouched centered UV for the circular cutout.
                    This removes the visible square corners without changing
                    the tunnel's existing aspect correction.
                */
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

                float2 uv =
                    maskUV;

                /*
                    Keep the original tunnel correction exactly as it was.
                */
                uv.x *=
                    0.462;

                float phi =
                    1.6180339887;

                float logPhi =
                    log(phi);

                float dynamicZoom =
                    _ZoomSpeed *
                    (
                        1.0 +
                        _AudioBass *
                        2.5
                    );

                float phase =
                    frac(
                        iTime *
                        dynamicZoom /
                        logPhi
                    );

                int layers =
                    (int)_Layers;

                float3 totalColor =
                    float3(
                        0,
                        0,
                        0
                    );

                for (
                    int i = 0;
                    i < layers;
                    i++
                )
                {
                    float fi =
                        float(i);

                    float layerPhase =
                        frac(
                            phase +
                            fi /
                            _Layers
                        );

                    float zoom =
                        layerPhase *
                        logPhi;

                    float w =
                        sin(
                            layerPhase *
                            PI
                        );

                    w =
                        w *
                        w;

                    w *=
                        1.0 +
                        _AudioMid *
                        0.8;

                    float4 layer =
                        fibLayer(
                            uv,
                            zoom,
                            iTime
                        );

                    totalColor +=
                        layer.rgb *
                        w;
                }

                float dynamicBrightness =
                    _Brightness *
                    (
                        1.0 +
                        _AudioEnergy *
                        1.5
                    );

                totalColor *=
                    dynamicBrightness;

                float r =
                    length(uv);

                float coreGlow =
                    0.3 +
                    _AudioBass *
                    1.5;

                totalColor +=
                    rainbow(
                        iTime *
                        _ColorSpeed
                    ) *
                    exp(
                        -r *
                        8.0
                    ) *
                    coreGlow;

                return half4(
                    saturate(
                        totalColor
                    ),
                    circleMask
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}
