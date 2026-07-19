Shader "FidgetFlow/Bioluminescence"
{
    Properties
    {
        [Header(Motion)]
        _Speed ("Drift Speed", Range(0.0, 2.0)) = 0.12
        _CurrentStrength ("Current Strength", Range(0.0, 2.0)) = 0.42
        _Turbulence ("Turbulence", Range(0.0, 2.0)) = 0.30

        [Header(Jellyfish)]
        _JellyDensity ("Jellyfish Density", Range(0.15, 2.0)) = 0.55
        _JellySize ("Jellyfish Size", Range(0.15, 1.25)) = 0.62
        _JellyVariation ("Size Variation", Range(0.0, 1.0)) = 0.55
        _TentacleLength ("Tentacle Length", Range(0.4, 2.5)) = 1.25
        _TentacleFlow ("Tentacle Flow", Range(0.0, 2.0)) = 0.85
        _JellySoftness ("Body Softness", Range(0.1, 1.5)) = 0.65
        _JellyBrightness ("Jelly Brightness", Range(0.0, 3.0)) = 1.0

        [Header(Bubbles)]
        _BubbleAmount ("Bubble Amount", Range(0.0, 3.0)) = 1.1
        _BubbleSize ("Bubble Size", Range(0.25, 2.0)) = 0.8
        _BubbleSpeed ("Bubble Speed", Range(0.0, 2.0)) = 0.75
        _BubbleWobble ("Bubble Wobble", Range(0.0, 2.0)) = 0.8
        _BubbleBrightness ("Bubble Brightness", Range(0.0, 3.0)) = 0.75

        [Header(Particles)]
        _PlanktonAmount ("Plankton Amount", Range(0.0, 3.0)) = 1.0
        _PlanktonSize ("Plankton Size", Range(0.2, 2.0)) = 0.75
        _PlanktonGlow ("Plankton Glow", Range(0.0, 3.0)) = 0.9
        _MarineSnowAmount ("Marine Snow", Range(0.0, 3.0)) = 0.85
        _MarineSnowSize ("Marine Snow Size", Range(0.2, 2.0)) = 0.75

        [Header(Atmosphere)]
        _LightShafts ("Light Shafts", Range(0.0, 2.0)) = 0.45
        _Caustics ("Caustics", Range(0.0, 2.0)) = 0.25
        _WaterHaze ("Water Haze", Range(0.0, 2.0)) = 0.45
        _Vignette ("Vignette", Range(0.0, 2.0)) = 0.65
        _Brightness ("Brightness", Range(0.0, 3.0)) = 1.0
        _Contrast ("Contrast", Range(0.5, 2.5)) = 1.15
        _Saturation ("Saturation", Range(0.0, 2.0)) = 1.1

        [Header(Color)]
        _DeepColor ("Deep Water", Color) = (0.001, 0.004, 0.015, 1)
        _MidColor ("Mid Water", Color) = (0.0, 0.035, 0.08, 1)
        _ShallowColor ("Upper Water", Color) = (0.0, 0.12, 0.18, 1)
        _BiolumeA ("Biolume A", Color) = (0.0, 0.95, 0.85, 1)
        _BiolumeB ("Biolume B", Color) = (0.16, 0.35, 1.0, 1)
        _AccentColor ("Accent", Color) = (0.55, 0.12, 1.0, 1)
        _AccentRarity ("Accent Rarity", Range(0.0, 1.0)) = 0.18

        [Header(Audio Response)]
        _BassResponse ("Bass Response", Range(0.0, 6.0)) = 2.0
        _MidResponse ("Mid Response", Range(0.0, 6.0)) = 1.8
        _HighResponse ("High Response", Range(0.0, 6.0)) = 2.2
        _EnergyResponse ("Energy Response", Range(0.0, 6.0)) = 1.6

        [Header(Audio Reactivity)]
        _AudioBass ("Audio Bass", Float) = 0
        _AudioMid ("Audio Mid", Float) = 0
        _AudioHigh ("Audio High", Float) = 0
        _AudioEnergy ("Audio Energy", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "BioluminescentDeepSea"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)

                float _Speed;
                float _CurrentStrength;
                float _Turbulence;

                float _JellyDensity;
                float _JellySize;
                float _JellyVariation;
                float _TentacleLength;
                float _TentacleFlow;
                float _JellySoftness;
                float _JellyBrightness;

                float _BubbleAmount;
                float _BubbleSize;
                float _BubbleSpeed;
                float _BubbleWobble;
                float _BubbleBrightness;

                float _PlanktonAmount;
                float _PlanktonSize;
                float _PlanktonGlow;
                float _MarineSnowAmount;
                float _MarineSnowSize;

                float _LightShafts;
                float _Caustics;
                float _WaterHaze;
                float _Vignette;
                float _Brightness;
                float _Contrast;
                float _Saturation;

                float4 _DeepColor;
                float4 _MidColor;
                float4 _ShallowColor;
                float4 _BiolumeA;
                float4 _BiolumeB;
                float4 _AccentColor;
                float _AccentRarity;

                float _BassResponse;
                float _MidResponse;
                float _HighResponse;
                float _EnergyResponse;

                float _AudioBass;
                float _AudioMid;
                float _AudioHigh;
                float _AudioEnergy;

            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float Hash11(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            float2 Hash22(float2 p)
            {
                float3 p3 = frac(
                    float3(p.x, p.y, p.x) *
                    float3(0.1031, 0.1030, 0.0973)
                );

                p3 += dot(p3, p3.yzx + 33.33);

                return frac(
                    (p3.xx + p3.yz) *
                    p3.zy
                );
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float a = Hash11(dot(i, float2(1.0, 57.0)));
                float b = Hash11(dot(i + float2(1.0, 0.0), float2(1.0, 57.0)));
                float c = Hash11(dot(i + float2(0.0, 1.0), float2(1.0, 57.0)));
                float d = Hash11(dot(i + float2(1.0, 1.0), float2(1.0, 57.0)));

                return lerp(
                    lerp(a, b, f.x),
                    lerp(c, d, f.x),
                    f.y
                );
            }

            float FBM(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;

                [unroll]
                for (int octave = 0; octave < 4; octave++)
                {
                    value += ValueNoise(p) * amplitude;

                    p = mul(
                        float2x2(
                            0.80, -0.60,
                            0.60,  0.80
                        ),
                        p
                    );

                    p *= 2.03;
                    amplitude *= 0.5;
                }

                return value;
            }

            float3 AdjustSaturation(float3 color, float saturation)
            {
                float luminance = dot(
                    color,
                    float3(0.2126, 0.7152, 0.0722)
                );

                return lerp(
                    luminance.xxx,
                    color,
                    saturation
                );
            }

            float3 GetBiolumeColor(float seed, float depth, float midDrive)
            {
                float blendValue =
                    frac(
                        seed * 1.73 +
                        depth * 0.41 +
                        midDrive * 0.05
                    );

                float3 color =
                    lerp(
                        _BiolumeA.rgb,
                        _BiolumeB.rgb,
                        smoothstep(
                            0.15,
                            0.85,
                            blendValue
                        )
                    );

                float accentSeed =
                    Hash11(
                        seed * 91.7 +
                        depth * 13.4
                    );

                float accentMask =
                    step(
                        1.0 - _AccentRarity,
                        accentSeed
                    );

                color =
                    lerp(
                        color,
                        _AccentColor.rgb,
                        accentMask * 0.55
                    );

                return color;
            }

            void EvaluateJelly(
                float2 p,
                float seed,
                float depth,
                float time,
                float bassDrive,
                float midDrive,
                float highDrive,
                out float body,
                out float rim,
                out float aura,
                out float filaments,
                out float sparkle
            )
            {
                float sizeRandom =
                    lerp(
                        1.0 - _JellyVariation,
                        1.0 + _JellyVariation,
                        Hash11(seed * 7.19)
                    );

                float pulse =
                    sin(
                        time * 1.4 +
                        seed * 17.0
                    ) *
                    0.5 +
                    0.5;

                pulse =
                    pulse * pulse;

                float size =
                    _JellySize *
                    sizeRandom *
                    (
                        1.0 +
                        bassDrive * 0.12 +
                        pulse * 0.06
                    );

                float2 q =
                    p /
                    max(
                        size,
                        0.001
                    );

                q.x +=
                    sin(
                        q.y * 2.4 +
                        time * 0.8 +
                        seed * 8.0
                    ) *
                    0.025 *
                    _Turbulence;

                float verticalStretch =
                    lerp(
                        0.82,
                        1.18,
                        Hash11(seed * 3.7)
                    );

                q.y *= verticalStretch;

                float2 bellSpace =
                    float2(
                        q.x,
                        q.y + 0.12
                    );

                float domeDistance =
                    length(
                        float2(
                            bellSpace.x,
                            bellSpace.y * 0.82
                        )
                    );

                float lowerFade =
                    smoothstep(
                        -0.26,
                        0.02,
                        q.y
                    );

                float dome =
                    1.0 -
                    smoothstep(
                        0.56,
                        0.98 + _JellySoftness * 0.12,
                        domeDistance
                    );

                dome *=
                    lowerFade;

                float inner =
                    1.0 -
                    smoothstep(
                        0.08,
                        0.78,
                        domeDistance
                    );

                inner *=
                    lowerFade;

                body =
                    dome *
                    (
                        0.14 +
                        inner * 0.45
                    );

                float rimDistance =
                    abs(
                        domeDistance - 0.72
                    );

                rim =
                    1.0 -
                    smoothstep(
                        0.02,
                        0.11 + _JellySoftness * 0.06,
                        rimDistance
                    );

                rim *=
                    lowerFade;

                float underside =
                    exp(
                        -abs(q.y + 0.035) * 17.0
                    );

                underside *=
                    1.0 -
                    smoothstep(
                        0.18,
                        0.72,
                        abs(q.x)
                    );

                rim =
                    saturate(
                        rim * 0.55 +
                        underside * 0.35
                    );

                aura =
                    1.0 -
                    smoothstep(
                        0.62,
                        1.85,
                        domeDistance
                    );

                aura *=
                    smoothstep(
                        -0.72,
                        0.24,
                        q.y
                    );

                aura *=
                    0.42;

                filaments = 0.0;

                [unroll]
                for (int filamentIndex = 0; filamentIndex < 5; filamentIndex++)
                {
                    float fi =
                        (float)filamentIndex;

                    float startX =
                        lerp(
                            -0.48,
                            0.48,
                            fi / 4.0
                        );

                    float progress =
                        saturate(
                            (
                                -q.y -
                                0.02
                            ) /
                            max(
                                _TentacleLength,
                                0.001
                            )
                        );

                    float mainWave =
                        sin(
                            progress * 7.5 +
                            time * 1.25 +
                            seed * 10.0 +
                            fi * 1.8
                        );

                    float secondaryWave =
                        sin(
                            progress * 14.0 -
                            time * 0.65 +
                            fi * 2.6
                        );

                    float filamentX =
                        startX +
                        mainWave *
                        0.07 *
                        _TentacleFlow *
                        progress +
                        secondaryWave *
                        0.018 *
                        _TentacleFlow;

                    float filamentDistance =
                        abs(
                            q.x -
                            filamentX
                        );

                    float filamentWidth =
                        lerp(
                            0.025,
                            0.007,
                            progress
                        );

                    float filamentMask =
                        1.0 -
                        smoothstep(
                            filamentWidth,
                            filamentWidth * 2.6,
                            filamentDistance
                        );

                    float verticalMask =
                        smoothstep(
                            0.03,
                            -0.08,
                            q.y
                        );

                    verticalMask *=
                        1.0 -
                        smoothstep(
                            _TentacleLength * 0.72,
                            _TentacleLength,
                            -q.y
                        );

                    float fadeNoise =
                        sin(
                            progress * 19.0 +
                            time * 1.5 +
                            fi * 4.1
                        ) *
                        0.5 +
                        0.5;

                    filaments +=
                        filamentMask *
                        verticalMask *
                        lerp(
                            0.22,
                            0.72,
                            fadeNoise
                        );
                }

                filaments =
                    saturate(
                        filaments
                    );

                sparkle =
                    sin(
                        domeDistance * 30.0 -
                        time * 4.5 +
                        seed * 21.0
                    ) *
                    0.5 +
                    0.5;

                sparkle =
                    pow(
                        sparkle,
                        7.0
                    );

                sparkle *=
                    rim *
                    (
                        0.25 +
                        highDrive * 0.45
                    );
            }

            void EvaluateJellyLayer(
                float2 uv,
                float layerScale,
                float depth,
                float layerStrength,
                float time,
                float bassDrive,
                float midDrive,
                float highDrive,
                out float body,
                out float rim,
                out float aura,
                out float filaments,
                out float sparkle,
                out float3 colorAccum,
                out float weightAccum
            )
            {
                float density =
                    max(
                        _JellyDensity,
                        0.05
                    );

                float2 layerUV =
                    uv *
                    layerScale *
                    density;

                float currentNoise =
                    FBM(
                        uv * 0.75 +
                        float2(
                            time * 0.035,
                            -time * 0.025
                        ) +
                        depth * 9.0
                    );

                layerUV.x +=
                    time *
                    (
                        0.035 +
                        depth * 0.015
                    );

                layerUV.y +=
                    time *
                    (
                        0.065 +
                        depth * 0.025
                    );

                layerUV.x +=
                    (
                        currentNoise - 0.5
                    ) *
                    _CurrentStrength *
                    0.55;

                float2 baseCell =
                    floor(layerUV);

                float2 local =
                    frac(layerUV);

                body = 0.0;
                rim = 0.0;
                aura = 0.0;
                filaments = 0.0;
                sparkle = 0.0;
                colorAccum = 0.0;
                weightAccum = 0.0;

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 offset =
                            float2(
                                (float)x,
                                (float)y
                            );

                        float2 cellID =
                            baseCell +
                            offset;

                        float seed =
                            Hash11(
                                dot(
                                    cellID,
                                    float2(
                                        17.13,
                                        91.77
                                    )
                                ) +
                                depth * 23.0
                            );

                        float spawnMask =
                            step(
                                0.72,
                                Hash11(
                                    seed * 43.0 +
                                    depth
                                )
                            );

                        float2 randomValue =
                            Hash22(
                                cellID +
                                depth * 31.0
                            );

                        float2 center =
                            offset +
                            0.18 +
                            randomValue * 0.64;

                        center.x +=
                            sin(
                                time *
                                (
                                    0.42 +
                                    seed * 0.25
                                ) +
                                seed * 13.0
                            ) *
                            0.08 *
                            _Turbulence;

                        center.y +=
                            cos(
                                time *
                                (
                                    0.30 +
                                    seed * 0.22
                                ) +
                                seed * 8.0
                            ) *
                            0.05;

                        float2 organismPosition =
                            local -
                            center;

                        float jellyBody;
                        float jellyRim;
                        float jellyAura;
                        float jellyFilaments;
                        float jellySparkle;

                        EvaluateJelly(
                            organismPosition,
                            seed,
                            depth,
                            time + depth * 2.0,
                            bassDrive,
                            midDrive,
                            highDrive,
                            jellyBody,
                            jellyRim,
                            jellyAura,
                            jellyFilaments,
                            jellySparkle
                        );

                        float depthFade =
                            lerp(
                                1.0,
                                0.42,
                                depth
                            );

                        float contribution =
                            spawnMask *
                            layerStrength *
                            depthFade;

                        jellyBody *=
                            contribution;

                        jellyRim *=
                            contribution;

                        jellyAura *=
                            contribution;

                        jellyFilaments *=
                            contribution;

                        jellySparkle *=
                            contribution;

                        body += jellyBody;
                        rim += jellyRim;
                        aura += jellyAura;
                        filaments += jellyFilaments;
                        sparkle += jellySparkle;

                        float colorWeight =
                            jellyBody +
                            jellyRim +
                            jellyFilaments;

                        float3 jellyColor =
                            GetBiolumeColor(
                                seed,
                                depth,
                                midDrive
                            );

                        colorAccum +=
                            jellyColor *
                            colorWeight;

                        weightAccum +=
                            colorWeight;
                    }
                }

                body = saturate(body);
                rim = saturate(rim);
                aura = saturate(aura);
                filaments = saturate(filaments);
                sparkle = saturate(sparkle);
            }

            float BubbleLayer(
                float2 uv,
                float scale,
                float time,
                float seedOffset,
                float highDrive
            )
            {
                float2 bubbleUV =
                    uv *
                    scale;

                bubbleUV.y -=
                    time *
                    _BubbleSpeed;

                float2 cell =
                    floor(bubbleUV);

                float2 local =
                    frac(bubbleUV);

                float2 randomValue =
                    Hash22(
                        cell +
                        seedOffset
                    );

                float spawn =
                    step(
                        1.0 -
                        saturate(
                            _BubbleAmount * 0.18
                        ),
                        Hash11(
                            dot(
                                cell,
                                float2(
                                    12.7,
                                    78.3
                                )
                            ) +
                            seedOffset
                        )
                    );

                float wobble =
                    sin(
                        time * 1.8 +
                        randomValue.y * 17.0 +
                        bubbleUV.y * 1.2
                    ) *
                    0.10 *
                    _BubbleWobble;

                float2 center =
                    0.15 +
                    randomValue * 0.70;

                center.x +=
                    wobble;

                float radius =
                    lerp(
                        0.018,
                        0.065,
                        randomValue.x
                    ) *
                    _BubbleSize;

                float distanceToCenter =
                    length(
                        local -
                        center
                    );

                float rimDistance =
                    abs(
                        distanceToCenter -
                        radius
                    );

                float bubbleRim =
                    1.0 -
                    smoothstep(
                        radius * 0.10,
                        radius * 0.42,
                        rimDistance
                    );

                float highlight =
                    1.0 -
                    smoothstep(
                        radius * 0.10,
                        radius * 0.45,
                        length(
                            local -
                            (
                                center +
                                float2(
                                    -radius * 0.30,
                                    radius * 0.28
                                )
                            )
                        )
                    );

                return
                    spawn *
                    (
                        bubbleRim * 0.58 +
                        highlight * 0.42
                    ) *
                    (
                        0.50 +
                        highDrive * 0.18
                    );
            }

            float PlanktonLayer(
                float2 uv,
                float scale,
                float time,
                float seedOffset,
                float highDrive,
                float energyDrive
            )
            {
                float2 planktonUV =
                    uv *
                    scale;

                planktonUV +=
                    float2(
                        time * 0.018,
                        -time * 0.012
                    );

                planktonUV.x +=
                    sin(
                        planktonUV.y * 0.9 +
                        time
                    ) *
                    0.04;

                float2 cell =
                    floor(planktonUV);

                float2 local =
                    frac(planktonUV);

                float2 randomValue =
                    Hash22(
                        cell +
                        seedOffset
                    );

                float2 center =
                    0.08 +
                    randomValue * 0.84;

                float particleDistance =
                    length(
                        local -
                        center
                    );

                float particleSize =
                    lerp(
                        0.006,
                        0.020,
                        randomValue.x
                    ) *
                    _PlanktonSize;

                float particle =
                    1.0 -
                    smoothstep(
                        particleSize,
                        particleSize * 3.0,
                        particleDistance
                    );

                float flicker =
                    sin(
                        time * 3.2 +
                        randomValue.y * 27.0
                    ) *
                    0.5 +
                    0.5;

                flicker =
                    pow(
                        flicker,
                        4.0
                    );

                return
                    particle *
                    (
                        0.20 +
                        flicker *
                        (
                            0.45 +
                            highDrive * 0.25 +
                            energyDrive * 0.12
                        )
                    );
            }

            float MarineSnowLayer(
                float2 uv,
                float scale,
                float time,
                float seedOffset
            )
            {
                float2 snowUV =
                    uv *
                    scale;

                snowUV.y -=
                    time * 0.055;

                snowUV.x +=
                    sin(
                        snowUV.y * 0.7 +
                        time * 0.5
                    ) *
                    0.035;

                float2 cell =
                    floor(snowUV);

                float2 local =
                    frac(snowUV);

                float2 randomValue =
                    Hash22(
                        cell +
                        seedOffset
                    );

                float2 center =
                    0.08 +
                    randomValue * 0.84;

                float particleDistance =
                    length(
                        local -
                        center
                    );

                float particleSize =
                    lerp(
                        0.008,
                        0.025,
                        randomValue.x
                    ) *
                    _MarineSnowSize;

                float particle =
                    1.0 -
                    smoothstep(
                        particleSize,
                        particleSize * 2.8,
                        particleDistance
                    );

                return
                    particle *
                    lerp(
                        0.10,
                        0.42,
                        randomValue.y
                    );
            }

            float LightShaftField(float2 uv, float time)
            {
                float shiftedX =
                    uv.x +
                    sin(
                        uv.y * 1.4 +
                        time * 0.15
                    ) *
                    0.08;

                float shaftNoise =
                    FBM(
                        float2(
                            shiftedX * 2.1,
                            uv.y * 0.35 -
                            time * 0.035
                        )
                    );

                float shafts =
                    smoothstep(
                        0.54,
                        0.82,
                        shaftNoise
                    );

                float verticalFade =
                    smoothstep(
                        -1.0,
                        0.9,
                        uv.y
                    );

                return
                    shafts *
                    verticalFade;
            }

            float CausticField(float2 uv, float time)
            {
                float n1 =
                    FBM(
                        uv * 3.4 +
                        float2(
                            time * 0.035,
                            -time * 0.025
                        )
                    );

                float n2 =
                    FBM(
                        uv * 5.2 +
                        float2(
                            -time * 0.028,
                            time * 0.040
                        )
                    );

                float caustic =
                    smoothstep(
                        0.64,
                        0.88,
                        n1 * 0.60 +
                        n2 * 0.40
                    );

                return caustic;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV =
                    input.positionHCS.xy /
                    _ScreenParams.xy;

                float2 uv =
                    screenUV * 2.0 -
                    1.0;

                float aspect =
                    _ScreenParams.x /
                    max(
                        _ScreenParams.y,
                        1.0
                    );

                uv.x *=
                    aspect;

                float bassDrive =
                    max(
                        _AudioBass,
                        0.0
                    ) *
                    _BassResponse;

                float midDrive =
                    max(
                        _AudioMid,
                        0.0
                    ) *
                    _MidResponse;

                float highDrive =
                    max(
                        _AudioHigh,
                        0.0
                    ) *
                    _HighResponse;

                float energyDrive =
                    max(
                        _AudioEnergy,
                        0.0
                    ) *
                    _EnergyResponse;

                float time =
                    _Time.y *
                    _Speed *
                    (
                        1.0 +
                        energyDrive * 0.10
                    );

                float depthGradient =
                    saturate(
                        screenUV.y
                    );

                float3 waterColor =
                    lerp(
                        _DeepColor.rgb,
                        _MidColor.rgb,
                        smoothstep(
                            0.0,
                            0.72,
                            depthGradient
                        )
                    );

                waterColor =
                    lerp(
                        waterColor,
                        _ShallowColor.rgb,
                        smoothstep(
                            0.68,
                            1.0,
                            depthGradient
                        )
                    );

                float haze =
                    FBM(
                        uv * 0.75 +
                        float2(
                            time * 0.012,
                            -time * 0.008
                        )
                    );

                waterColor +=
                    _MidColor.rgb *
                    haze *
                    _WaterHaze *
                    0.22;

                float shafts =
                    LightShaftField(
                        uv,
                        time
                    ) *
                    _LightShafts;

                waterColor +=
                    _ShallowColor.rgb *
                    shafts *
                    (
                        0.20 +
                        energyDrive * 0.05
                    );

                float caustics =
                    CausticField(
                        uv,
                        time
                    ) *
                    _Caustics;

                waterColor +=
                    _BiolumeA.rgb *
                    caustics *
                    0.07;

                float nearBody;
                float nearRim;
                float nearAura;
                float nearFilaments;
                float nearSparkle;
                float3 nearColorAccum;
                float nearWeight;

                EvaluateJellyLayer(
                    uv,
                    0.72,
                    0.10,
                    1.0,
                    time,
                    bassDrive,
                    midDrive,
                    highDrive,
                    nearBody,
                    nearRim,
                    nearAura,
                    nearFilaments,
                    nearSparkle,
                    nearColorAccum,
                    nearWeight
                );

                float middleBody;
                float middleRim;
                float middleAura;
                float middleFilaments;
                float middleSparkle;
                float3 middleColorAccum;
                float middleWeight;

                EvaluateJellyLayer(
                    uv,
                    1.08,
                    0.50,
                    0.62,
                    time * 0.82,
                    bassDrive,
                    midDrive,
                    highDrive,
                    middleBody,
                    middleRim,
                    middleAura,
                    middleFilaments,
                    middleSparkle,
                    middleColorAccum,
                    middleWeight
                );

                float farBody;
                float farRim;
                float farAura;
                float farFilaments;
                float farSparkle;
                float3 farColorAccum;
                float farWeight;

                EvaluateJellyLayer(
                    uv,
                    1.55,
                    0.88,
                    0.28,
                    time * 0.62,
                    bassDrive,
                    midDrive,
                    highDrive,
                    farBody,
                    farRim,
                    farAura,
                    farFilaments,
                    farSparkle,
                    farColorAccum,
                    farWeight
                );

                float totalWeight =
                    nearWeight +
                    middleWeight +
                    farWeight;

                float3 jellyColor =
                    (
                        nearColorAccum +
                        middleColorAccum +
                        farColorAccum
                    ) /
                    max(
                        totalWeight,
                        0.001
                    );

                jellyColor =
                    AdjustSaturation(
                        jellyColor,
                        _Saturation
                    );

                float totalBody =
                    saturate(
                        nearBody +
                        middleBody +
                        farBody
                    );

                float totalRim =
                    saturate(
                        nearRim +
                        middleRim +
                        farRim
                    );

                float totalAura =
                    saturate(
                        nearAura +
                        middleAura +
                        farAura
                    );

                float totalFilaments =
                    saturate(
                        nearFilaments +
                        middleFilaments +
                        farFilaments
                    );

                float totalSparkle =
                    saturate(
                        nearSparkle +
                        middleSparkle +
                        farSparkle
                    );

                float jellyPulse =
                    1.0 +
                    bassDrive * 0.15;

                float3 jellyLight =
                    jellyColor *
                    (
                        totalBody * 0.55 +
                        totalRim * 1.05 +
                        totalFilaments * 0.65 +
                        totalAura * 0.30
                    ) *
                    jellyPulse *
                    _JellyBrightness;

                jellyLight +=
                    _BiolumeA.rgb *
                    totalSparkle *
                    (
                        0.45 +
                        highDrive * 0.18
                    );

                float bubbles =
                    BubbleLayer(
                        uv,
                        7.0,
                        time,
                        0.0,
                        highDrive
                    );

                bubbles +=
                    BubbleLayer(
                        uv + float2(2.3, 1.7),
                        11.0,
                        time * 0.76,
                        17.0,
                        highDrive
                    ) *
                    0.55;

                bubbles *=
                    _BubbleBrightness;

                float plankton =
                    PlanktonLayer(
                        uv,
                        17.0,
                        time,
                        0.0,
                        highDrive,
                        energyDrive
                    );

                plankton +=
                    PlanktonLayer(
                        uv + float2(3.1, 4.2),
                        29.0,
                        time * 0.72,
                        31.0,
                        highDrive,
                        energyDrive
                    ) *
                    0.55;

                plankton *=
                    _PlanktonAmount *
                    _PlanktonGlow;

                float marineSnow =
                    MarineSnowLayer(
                        uv,
                        12.0,
                        time,
                        0.0
                    );

                marineSnow +=
                    MarineSnowLayer(
                        uv + float2(1.8, 3.9),
                        22.0,
                        time * 0.63,
                        23.0
                    ) *
                    0.60;

                marineSnow *=
                    _MarineSnowAmount;

                float3 color =
                    waterColor;

                color +=
                    jellyLight;

                color +=
                    lerp(
                        _BiolumeA.rgb,
                        _BiolumeB.rgb,
                        screenUV.y
                    ) *
                    plankton *
                    0.55;

                color +=
                    float3(
                        0.55,
                        0.82,
                        1.0
                    ) *
                    bubbles *
                    0.55;

                color +=
                    float3(
                        0.28,
                        0.45,
                        0.58
                    ) *
                    marineSnow *
                    0.30;

                float vignetteDistance =
                    length(
                        float2(
                            uv.x / max(aspect, 0.001),
                            uv.y
                        )
                    );

                float vignette =
                    1.0 -
                    smoothstep(
                        0.45,
                        1.22,
                        vignetteDistance
                    ) *
                    _Vignette;

                color *=
                    vignette;

                color *=
                    1.0 +
                    energyDrive * 0.06;

                color =
                    max(
                        color,
                        0.0
                    );

                color =
                    pow(
                        color,
                        1.0 /
                        max(
                            _Contrast,
                            0.001
                        )
                    );

                color *=
                    _Brightness;

                return half4(
                    color,
                    1.0
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}