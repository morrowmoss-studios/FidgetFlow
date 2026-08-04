Shader "FidgetFlow/Plasma"
{
    Properties
    {
        [Header(Motion)]
        _Speed ("Flow Speed", Range(0.0, 3.0)) = 0.22
        _RotationSpeed ("Rotation Speed", Range(-1.0, 1.0)) = 0.04
        _FlowStrength ("Flow Strength", Range(0.0, 3.0)) = 1.15

        [Header(Structure)]
        _Segments ("Soft Symmetry Segments", Range(2, 16)) = 8
        _NoiseScale ("Noise Scale", Range(0.5, 8.0)) = 2.2
        _WarpStrength ("Warp Strength", Range(0.0, 3.0)) = 1.3
        _FilamentSharpness ("Filament Sharpness", Range(1.0, 12.0)) = 5.5
        _DetailStrength ("Fine Detail", Range(0.0, 2.0)) = 0.8
        _RadialStretch ("Radial Stretch", Range(0.25, 4.0)) = 1.6

        [Header(Composition)]
        _CenterDarkness ("Center Darkness", Range(0.0, 1.0)) = 0.42
        _EdgeFalloff ("Edge Falloff", Range(0.0, 3.0)) = 0.65
        _CoreGlowIntensity ("Core Glow", Range(0.0, 5.0)) = 0.8
        _CoreGlowFalloff ("Core Glow Falloff", Range(1.0, 20.0)) = 7.0
        _Brightness ("Overall Brightness", Range(0.0, 4.0)) = 1.35
        _Contrast ("Contrast", Range(0.5, 3.0)) = 1.3
        _Saturation ("Color Saturation", Range(0.0, 2.0)) = 1.15

        [Header(Portal Mask)]
        _PortalRadius ("Portal Radius", Range(0.5, 1.1)) = 1.0
        _PortalEdgeSoftness ("Portal Edge Softness", Range(0.001, 0.15)) = 0.025

        [Header(Color)]
        _ColorShift ("Color Shift", Range(0.0, 1.0)) = 0.0
        _ColorSpeed ("Color Motion", Range(0.0, 1.0)) = 0.06
        _CoreGlowColor ("Core Glow Color", Color) = (0.25, 0.85, 1.0, 1.0)
        _BackgroundColor ("Background Color", Color) = (0.002, 0.008, 0.018, 1.0)

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
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        LOD 100

        Cull Off
        ZWrite On
        ZTest LEqual

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "FidgetFlowPlasma"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define PI 3.14159265
            #define TAU 6.28318531

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

                float _Speed;
                float _RotationSpeed;
                float _FlowStrength;

                float _Segments;
                float _NoiseScale;
                float _WarpStrength;
                float _FilamentSharpness;
                float _DetailStrength;
                float _RadialStretch;

                float _CenterDarkness;
                float _EdgeFalloff;
                float _CoreGlowIntensity;
                float _CoreGlowFalloff;
                float _Brightness;
                float _Contrast;
                float _Saturation;

                float _PortalRadius;
                float _PortalEdgeSoftness;

                float _ColorShift;
                float _ColorSpeed;
                float4 _CoreGlowColor;
                float4 _BackgroundColor;

                float _AudioBass;
                float _AudioMid;
                float _AudioHigh;
                float _AudioEnergy;

            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv = input.uv;

                return output;
            }

            float2 Rotate2D(float2 p, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);

                return float2(
                    c * p.x - s * p.y,
                    s * p.x + c * p.y
                );
            }

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

                local =
                    local *
                    local *
                    (3.0 - 2.0 * local);

                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));

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
                for (int octave = 0; octave < 5; octave++)
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

            float RidgedFBM(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.55;

                [unroll]
                for (int octave = 0; octave < 4; octave++)
                {
                    float noiseValue = ValueNoise(p);

                    float ridge =
                        1.0 -
                        abs(noiseValue * 2.0 - 1.0);

                    ridge *= ridge;
                    value += ridge * amplitude;

                    p = mul(
                        float2x2(
                            0.82, -0.57,
                            0.57,  0.82
                        ),
                        p
                    );

                    p *= 2.11;
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

            float3 EnergyPalette(float t)
            {
                t = frac(t);

                float3 teal =
                    float3(0.02, 0.90, 0.74);

                float3 cyan =
                    float3(0.08, 0.42, 1.00);

                float3 magenta =
                    float3(0.94, 0.05, 0.72);

                float3 orange =
                    float3(1.00, 0.50, 0.02);

                float3 yellow =
                    float3(0.80, 1.00, 0.05);

                float section = t * 5.0;
                float localT = frac(section);

                localT =
                    localT *
                    localT *
                    (3.0 - 2.0 * localT);

                if (section < 1.0)
                    return lerp(teal, cyan, localT);

                if (section < 2.0)
                    return lerp(cyan, magenta, localT);

                if (section < 3.0)
                    return lerp(magenta, orange, localT);

                if (section < 4.0)
                    return lerp(orange, yellow, localT);

                return lerp(yellow, teal, localT);
            }

            half4 frag(Varyings input) : SV_Target
            {
                /*
                    OBJECT UV SPACE

                    The visual now stays attached to this quad instead
                    of sampling the entire screen.
                */

                float2 uv =
                    input.uv * 2.0 - 1.0;

                float radius =
                    length(uv);

                /*
                    CIRCULAR PORTAL MASK

                    Outside the portal becomes transparent.
                */

                float portalAlpha =
                    1.0 -
                    smoothstep(
                        _PortalRadius -
                        _PortalEdgeSoftness,
                        _PortalRadius,
                        radius
                    );

                clip(portalAlpha - 0.001);

                /*
                    AUDIO BOOSTS
                */

                float bass =
                    saturate(_AudioBass * 6.0);

                float mid =
                    saturate(_AudioMid * 7.0);

                float high =
                    saturate(_AudioHigh * 9.0);

                float energy =
                    saturate(_AudioEnergy * 7.0);

                /*
                    MOTION
                */

                float dynamicSpeed =
                    _Speed *
                    (
                        1.0 +
                        energy * 1.25 +
                        bass * 0.35
                    );

                float time =
                    _Time.y *
                    dynamicSpeed;

                float rotation =
                    _Time.y *
                    _RotationSpeed;

                rotation +=
                    mid * 0.14;

                rotation +=
                    sin(_Time.y * 1.7) *
                    mid *
                    0.035;

                uv =
                    Rotate2D(
                        uv,
                        rotation
                    );

                radius =
                    length(uv);

                float angle =
                    atan2(
                        uv.y,
                        uv.x
                    );

                /*
                    FIXED WHOLE-NUMBER SEGMENT COUNT
                */

                float segmentCount =
                    max(
                        floor(_Segments + 0.5),
                        2.0
                    );

                float segmentSize =
                    TAU /
                    segmentCount;

                float normalizedAngle =
                    frac(
                        (angle + PI) /
                        TAU
                    );

                float segmentPosition =
                    frac(
                        normalizedAngle *
                        segmentCount
                    );

                float foldedAngle =
                    abs(
                        segmentPosition -
                        0.5
                    ) *
                    segmentSize;

                /*
                    POLAR DOMAIN
                */

                float audioStretch =
                    1.0 +
                    bass * 0.45;

                float radialBreathing =
                    1.0 +
                    sin(
                        time * 2.2
                    ) *
                    bass *
                    0.08;

                float2 polarField =
                    float2(
                        foldedAngle *
                        _RadialStretch *
                        audioStretch *
                        radialBreathing,

                        log(radius + 0.12) *
                        (
                            1.8 -
                            bass * 0.28
                        )
                    );

                /*
                    DOMAIN WARP
                */

                float2 warpInput =
                    polarField *
                    (_NoiseScale * 0.55);

                warpInput +=
                    float2(
                        time * 0.17,
                        -time * 0.24
                    );

                float warpA =
                    FBM(warpInput);

                float warpB =
                    FBM(
                        warpInput +
                        float2(5.27, 1.93) +
                        warpA * 1.8
                    );

                float2 domainWarp =
                    float2(
                        warpA - 0.5,
                        warpB - 0.5
                    );

                float dynamicWarp =
                    _WarpStrength *
                    (
                        1.0 +
                        bass * 2.4 +
                        energy * 1.35
                    );

                float dynamicFlow =
                    _FlowStrength *
                    (
                        1.0 +
                        bass * 0.75
                    );

                float2 warpedPolar =
                    polarField +
                    domainWarp *
                    dynamicWarp *
                    dynamicFlow;

                /*
                    FILAMENT STRUCTURE
                */

                float radialFlow =
                    warpedPolar.y * 2.3;

                radialFlow -=
                    time * 0.7;

                radialFlow +=
                    warpA * 2.0;

                radialFlow +=
                    bass *
                    sin(
                        radius * 14.0 -
                        time * 4.0
                    ) *
                    0.7;

                float angularFlow =
                    warpedPolar.x *
                    segmentCount *
                    1.2;

                angularFlow +=
                    warpB * 3.0;

                float broadWaves =
                    sin(
                        radialFlow * 2.0 +
                        angularFlow
                    ) *
                    0.5 +
                    0.5;

                float crossedWaves =
                    sin(
                        radialFlow * 1.35 -
                        angularFlow * 1.7
                    ) *
                    0.5 +
                    0.5;

                float ridgeNoise =
                    RidgedFBM(
                        warpedPolar *
                        float2(2.2, 1.25) +
                        float2(
                            time * 0.09,
                            -time * 0.34
                        )
                    );

                float fineNoise =
                    RidgedFBM(
                        warpedPolar *
                        float2(5.0, 2.8) -
                        float2(
                            time * 0.18,
                            time * 0.12
                        )
                    );

                float filamentBase =
                    broadWaves *
                    crossedWaves *
                    ridgeNoise;

                filamentBase +=
                    fineNoise *
                    _DetailStrength *
                    (
                        0.26 +
                        high * 0.34
                    );

                float dynamicSharpness =
                    _FilamentSharpness *
                    (
                        1.0 +
                        high * 0.95
                    );

                dynamicSharpness *=
                    lerp(
                        1.0,
                        0.78,
                        bass
                    );

                float filaments =
                    pow(
                        saturate(filamentBase),
                        dynamicSharpness
                    );

                /*
                    SOFT CLOUD BODY
                */

                float cloudBody =
                    FBM(
                        warpedPolar *
                        float2(1.35, 0.9) +
                        float2(
                            -time * 0.08,
                            time * 0.15
                        )
                    );

                cloudBody =
                    smoothstep(
                        0.30 - bass * 0.06,
                        0.78 - bass * 0.08,
                        cloudBody
                    );

                float energyField =
                    filaments * 1.65 +
                    cloudBody * 0.34 +
                    ridgeNoise * 0.16;

                /*
                    BASS PULSE
                */

                float bassPulse =
                    sin(
                        radius * 18.0 -
                        time * 5.0
                    ) *
                    0.5 +
                    0.5;

                bassPulse =
                    pow(
                        bassPulse,
                        5.0
                    ) *
                    bass;

                energyField +=
                    bassPulse *
                    ridgeNoise *
                    0.85;

                /*
                    CENTER CONTROL
                */

                float centerMask =
                    smoothstep(
                        0.0,
                        max(_CenterDarkness, 0.001),
                        radius
                    );

                energyField *=
                    lerp(
                        1.0 - _CenterDarkness,
                        1.0,
                        centerMask
                    );

                /*
                    EDGE CONTROL
                */

                float edgeMask =
                    saturate(
                        1.0 -
                        radius *
                        _EdgeFalloff *
                        0.42
                    );

                edgeMask =
                    smoothstep(
                        0.0,
                        1.0,
                        edgeMask
                    );

                energyField *=
                    lerp(
                        0.38,
                        1.0,
                        edgeMask
                    );

                /*
                    COLOR
                */

                float hue =
                    normalizedAngle;

                hue +=
                    radius * 0.11;

                hue +=
                    warpA * 0.25;

                hue -=
                    warpB * 0.17;

                hue +=
                    _ColorShift;

                hue +=
                    _Time.y *
                    _ColorSpeed;

                hue +=
                    mid * 0.48;

                float3 mainColor =
                    EnergyPalette(hue);

                float secondaryHue =
                    hue +
                    0.18 +
                    fineNoise * 0.12 +
                    high * 0.08;

                float3 secondaryColor =
                    EnergyPalette(
                        secondaryHue
                    );

                float3 color =
                    lerp(
                        mainColor,
                        secondaryColor,
                        saturate(
                            fineNoise * 0.85
                        )
                    );

                color =
                    AdjustSaturation(
                        color,
                        _Saturation
                    );

                /*
                    GLOW BODY
                */

                float glowBody =
                    pow(
                        saturate(energyField),
                        1.35
                    );

                float hotCore =
                    pow(
                        saturate(filaments),
                        0.42
                    );

                color *=
                    glowBody *
                    1.5;

                color +=
                    mainColor *
                    hotCore *
                    (
                        0.85 +
                        high * 1.1
                    );

                color +=
                    hotCore.xxx *
                    (
                        0.16 +
                        high * 0.55
                    );

                /*
                    CENTER GLOW
                */

                float coreGlow =
                    exp(
                        -radius *
                        _CoreGlowFalloff
                    );

                coreGlow *=
                    _CoreGlowIntensity *
                    (
                        1.0 +
                        bass * 4.0 +
                        energy * 1.5
                    );

                color +=
                    _CoreGlowColor.rgb *
                    coreGlow *
                    (
                        0.35 +
                        filaments
                    );

                /*
                    FINAL AUDIO LIGHTING
                */

                color *=
                    1.0 +
                    energy * 1.15;

                color +=
                    mainColor *
                    filaments *
                    bass *
                    1.8;

                color +=
                    secondaryColor *
                    fineNoise *
                    high *
                    0.95;

                color +=
                    mainColor *
                    cloudBody *
                    mid *
                    0.28;

                color =
                    max(
                        color,
                        0.0
                    );

                color =
                    pow(
                        color,
                        1.0 /
                        max(_Contrast, 0.001)
                    );

                float audioBrightness =
                    1.0 +
                    energy * 0.65 +
                    bass * 0.35;

                color *=
                    _Brightness *
                    audioBrightness;

                float visibleEnergy =
                    saturate(
                        energyField * 1.4 +
                        coreGlow * 0.25 +
                        bassPulse * 0.35
                    );

                color =
                    lerp(
                        _BackgroundColor.rgb,
                        color,
                        visibleEnergy
                    );

                return half4(
                    color,
                    portalAlpha
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}