Shader "FidgetFlow/GalaxyBackground"
{
    Properties
    {
        [Header(Background)]
        _BackgroundColor ("Background Color", Color) = (0.0, 0.0, 0.008, 1.0)
        _Seed ("Seed", Float) = 137.0

        [Header(Stars)]
        _StarDensity ("Star Density", Range(20, 300)) = 155
        _StarCoverage ("Star Coverage", Range(0.01, 0.35)) = 0.14
        _StarBrightness ("Star Brightness", Range(0, 4)) = 1.35
        _StarSize ("Star Size", Range(0.2, 3)) = 0.9
        _TwinkleAmount ("Twinkle Amount", Range(0, 1)) = 0.22
        _TwinkleSpeed ("Twinkle Speed", Range(0, 8)) = 0.85

        [Header(Pastel Palette)]
        [HDR] _PastelBlue ("Pastel Blue", Color) = (0.66, 0.84, 1.0, 1.0)
        [HDR] _PastelPink ("Pastel Pink", Color) = (1.0, 0.72, 0.84, 1.0)
        [HDR] _PastelLavender ("Pastel Lavender", Color) = (0.82, 0.72, 1.0, 1.0)
        [HDR] _PastelMint ("Pastel Mint", Color) = (0.70, 1.0, 0.86, 1.0)
        [HDR] _PastelGold ("Pastel Gold", Color) = (1.0, 0.92, 0.66, 1.0)

        [Header(Space Dust)]
        _DustScale ("Dust Scale", Range(0.2, 8)) = 1.65
        _DustStrength ("Dust Strength", Range(0, 0.5)) = 0.18
        _DustSoftness ("Dust Softness", Range(0.1, 3)) = 0.9
        _DustCoverage ("Dust Coverage", Range(0.2, 0.8)) = 0.43
        _DustDriftSpeed ("Dust Drift Speed", Range(0, 0.2)) = 0.018

        [Header(Shooting Star)]
        [Toggle] _EnableShootingStar ("Enable Shooting Star", Float) = 1
        _ShootingStarBrightness ("Shooting Star Brightness", Range(0, 8)) = 1.4
        _ShootingStarLength ("Shooting Star Length", Range(0.01, 0.20)) = 0.07
        _ShootingStarWidth ("Shooting Star Width", Range(0.0002, 0.008)) = 0.0012

    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Background"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "GalaxyBackground"

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
                float4 _BackgroundColor;
                float _Seed;

                float _StarDensity;
                float _StarCoverage;
                float _StarBrightness;
                float _StarSize;
                float _TwinkleAmount;
                float _TwinkleSpeed;

                float4 _PastelBlue;
                float4 _PastelPink;
                float4 _PastelLavender;
                float4 _PastelMint;
                float4 _PastelGold;

                float _DustScale;
                float _DustStrength;
                float _DustSoftness;
                float _DustCoverage;
                float _DustDriftSpeed;

                float _EnableShootingStar;
                float _ShootingStarBrightness;
                float _ShootingStarLength;
                float _ShootingStarWidth;

            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float hash11(float p)
            {
                return frac(sin(p * 127.1 + _Seed) * 43758.5453123);
            }

            float2 hash21(float p)
            {
                return frac(
                    sin(
                        float2(
                            p * 127.1 + 311.7,
                            p * 269.5 + 183.3
                        ) + _Seed
                    ) * 43758.5453123
                );
            }

            float hash21s(float2 p)
            {
                return frac(
                    sin(
                        dot(
                            p,
                            float2(127.1, 311.7)
                        ) + _Seed
                    ) * 43758.5453123
                );
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash21s(i);
                float b = hash21s(i + float2(1.0, 0.0));
                float c = hash21s(i + float2(0.0, 1.0));
                float d = hash21s(i + float2(1.0, 1.0));

                return lerp(
                    lerp(a, b, u.x),
                    lerp(c, d, u.x),
                    u.y
                );
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;

                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    value += noise(p) * amplitude;
                    p = p * 2.03 + float2(17.1, 9.2);
                    amplitude *= 0.5;
                }

                return value;
            }

            float3 pastelPalette(float selector)
            {
                selector = frac(selector) * 5.0;
                float3 color;

                if (selector < 1.0)
                {
                    color = lerp(_PastelBlue.rgb, _PastelPink.rgb, selector);
                }
                else if (selector < 2.0)
                {
                    color = lerp(_PastelPink.rgb, _PastelLavender.rgb, selector - 1.0);
                }
                else if (selector < 3.0)
                {
                    color = lerp(_PastelLavender.rgb, _PastelMint.rgb, selector - 2.0);
                }
                else if (selector < 4.0)
                {
                    color = lerp(_PastelMint.rgb, _PastelGold.rgb, selector - 3.0);
                }
                else
                {
                    color = lerp(_PastelGold.rgb, _PastelBlue.rgb, selector - 4.0);
                }

                return color;
            }

            float3 starLayer(
                float2 uv,
                float density,
                float sizeMultiplier,
                float brightnessMultiplier,
                float coverageMultiplier,
                float layerSeed
            )
            {
                float2 gridUV = uv * density;
                float2 cell = floor(gridUV);
                float2 local = frac(gridUV) - 0.5;

                float randomValue =
                    hash21s(
                        cell +
                        layerSeed
                    );

                float rarity =
                    step(
                        1.0 - saturate(_StarCoverage * coverageMultiplier),
                        randomValue
                    );

                float2 offset =
                    hash21(
                        randomValue * 173.3 +
                        layerSeed
                    ) -
                    0.5;

                local -=
                    offset *
                    0.72;

                float distanceToStar =
                    length(local);

                float randomSize =
                    lerp(
                        0.45,
                        1.4,
                        hash11(
                            randomValue * 91.7 +
                            layerSeed
                        )
                    );

                float size =
                    0.022 *
                    _StarSize *
                    sizeMultiplier *
                    randomSize;

                float core =
                    1.0 -
                    smoothstep(
                        size * 0.25,
                        size,
                        distanceToStar
                    );

                float glow =
                    exp(
                        -distanceToStar *
                        55.0 /
                        max(
                            sizeMultiplier,
                            0.1
                        )
                    ) *
                    0.22;

                float twinklePhase =
                    hash11(
                        randomValue * 251.9 +
                        layerSeed
                    ) *
                    TWO_PI;

                float twinkle =
                    1.0 +
                    sin(
                        _Time.y *
                        (
                            _TwinkleSpeed
                        ) *
                        lerp(
                            0.6,
                            1.6,
                            randomValue
                        ) +
                        twinklePhase
                    ) *
                    (
                        _TwinkleAmount
                    );

                twinkle =
                    max(
                        0.15,
                        twinkle
                    );

                float colorShift =
                    hash11(
                        randomValue * 403.1 +
                        layerSeed
                    );

                float3 color =
                    pastelPalette(
                        colorShift
                    );

                float naturalPulse =
                    1.0 +
                    0.035 *
                    sin(
                        _Time.y *
                        lerp(
                            0.35,
                            0.85,
                            randomValue
                        ) +
                        twinklePhase
                    );

                return
                    color *
                    rarity *
                    (
                        core +
                        glow
                    ) *
                    twinkle *
                    naturalPulse *
                    _StarBrightness *
                    brightnessMultiplier;
            }

            float3 spaceDust(float2 uv)
            {
                float2 drift =
                    float2(
                        _Time.y * _DustDriftSpeed,
                        -_Time.y * _DustDriftSpeed * 0.63
                    );

                float2 p =
                    uv *
                    _DustScale +
                    drift;

                float baseNoise =
                    fbm(p);

                float detailNoise =
                    fbm(
                        p * 1.9 +
                        float2(8.1, -3.7)
                    );

                float combinedDust =
                    baseNoise * 0.72 +
                    detailNoise * 0.28;

                float dustMask =
                    smoothstep(
                        _DustCoverage,
                        min(_DustCoverage + 0.22, 0.98),
                        combinedDust
                    );

                dustMask =
                    pow(
                        saturate(dustMask),
                        _DustSoftness
                    );

                float colorField =
                    fbm(
                        p * 0.55 +
                        float2(21.7, 4.3)
                    );

                float3 dustColor =
                    pastelPalette(
                        colorField
                    );

                float dustBreath =
                    0.96 +
                    0.04 *
                    sin(
                        _Time.y *
                        0.16 +
                        colorField *
                        TWO_PI
                    );

                return
                    dustColor *
                    dustMask *
                    _DustStrength *
                    dustBreath;
            }

            float3 shootingStar(float2 uv)
            {
                if (_EnableShootingStar < 0.5)
                {
                    return 0.0;
                }

                /*
                    Reliable one-way event:
                    - one cycle every 42 seconds
                    - visible for about 5.5 seconds
                    - random position and direction per cycle
                */
                const float cycleLength = 42.0;
                const float visibleDuration = 5.5;

                float absoluteTime = _Time.y;
                float cycle = floor(absoluteTime / cycleLength);
                float timeInCycle = fmod(absoluteTime, cycleLength);

                /*
                    Put the event somewhere between 4 and 28 seconds
                    into each cycle so it does not always happen immediately.
                */
                float eventStart =
                    lerp(
                        4.0,
                        28.0,
                        hash11(cycle * 19.7 + 2.3)
                    );

                float progress =
                    saturate(
                        (timeInCycle - eventStart) /
                        visibleDuration
                    );

                float active =
                    step(eventStart, timeInCycle) *
                    step(timeInCycle, eventStart + visibleDuration);

                if (active <= 0.0)
                {
                    return 0.0;
                }

                /*
                    Keep the whole path inside the screen.
                    Start in the upper half and drift gently down-right.
                */
                float2 start =
                    float2(
                        lerp(
                            0.08,
                            0.62,
                            hash11(cycle * 13.4 + 5.1)
                        ),
                        lerp(
                            0.62,
                            0.90,
                            hash11(cycle * 17.2 + 8.6)
                        )
                    );

                float angle =
                    lerp(
                        -0.72,
                        -0.42,
                        hash11(cycle * 23.8 + 4.0)
                    );

                float2 direction =
                    normalize(
                        float2(
                            cos(angle),
                            sin(angle)
                        )
                    );

                float easedProgress =
                    progress *
                    progress *
                    (3.0 - 2.0 * progress);

                float2 head =
                    start +
                    direction *
                    easedProgress *
                    0.18;

                float2 delta = uv - head;

                float along =
                    dot(
                        delta,
                        -direction
                    );

                float across =
                    abs(
                        dot(
                            delta,
                            float2(
                                -direction.y,
                                direction.x
                            )
                        )
                    );

                float distantLength =
                    _ShootingStarLength * 0.32;

                float distantWidth =
                    _ShootingStarWidth * 0.28;

                float trail =
                    smoothstep(
                        distantLength,
                        0.0,
                        along
                    ) *
                    step(
                        0.0,
                        along
                    );

                float width =
                    1.0 -
                    smoothstep(
                        0.0,
                        distantWidth,
                        across
                    );

                float headGlow =
                    exp(
                        -length(delta) * 520.0
                    );

                float fadeIn =
                    smoothstep(
                        0.0,
                        0.16,
                        progress
                    );

                float fadeOut =
                    1.0 -
                    smoothstep(
                        0.72,
                        1.0,
                        progress
                    );

                float fade =
                    fadeIn *
                    fadeOut;

                float3 color =
                    lerp(
                        float3(1.0, 1.0, 1.0),
                        _PastelBlue.rgb,
                        0.12
                    );

                return
                    color *
                    (
                        trail * width * 0.55 +
                        headGlow * 0.35
                    ) *
                    fade *
                    _ShootingStarBrightness *
                    0.22;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 centered = uv - 0.5;

                float3 color =
                    _BackgroundColor.rgb;

                color +=
                    spaceDust(
                        centered
                    );

                color +=
                    starLayer(
                        uv,
                        _StarDensity,
                        0.75,
                        0.60,
                        1.30,
                        11.0
                    );

                color +=
                    starLayer(
                        uv + float2(0.17, 0.09),
                        _StarDensity * 0.62,
                        1.15,
                        0.90,
                        1.00,
                        37.0
                    );

                color +=
                    starLayer(
                        uv + float2(-0.08, 0.21),
                        _StarDensity * 0.34,
                        1.80,
                        1.25,
                        0.55,
                        83.0
                    );

                color +=
                    shootingStar(
                        uv
                    );

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
