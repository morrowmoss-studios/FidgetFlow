Shader "FidgetFlow/Starloom"
{
    Properties
    {
        [Header(Motion)]
        _Speed ("Base Speed", Range(0.0, 3.0)) = 1.0
        _DriftSpeed ("Drift Speed", Range(0.0, 2.0)) = 0.32
        _Tension ("Thread Bend", Range(0.0, 2.0)) = 0.75

        [Header(Network)]
        _NodeDensity ("Node Density", Range(3.0, 12.0)) = 6.0
        _ConnectionAmount ("Connection Amount", Range(0.0, 1.0)) = 0.92
        _ThreadWidth ("Thread Width", Range(0.001, 0.035)) = 0.0048
        _NodeSize ("Node Size", Range(0.004, 0.07)) = 0.015
        _DepthSpread ("Depth Spread", Range(0.0, 2.0)) = 0.65

        [Header(Music Actions)]
        _BassReaction ("Bass Reaction", Range(0.0, 3.0)) = 1.4
        _MidReaction ("Mid Reaction", Range(0.0, 3.0)) = 1.5
        _HighReaction ("High Reaction", Range(0.0, 3.0)) = 1.15
        _EnergyReaction ("Energy Reaction", Range(0.0, 3.0)) = 1.2

        _PulseSpeed ("Pulse Speed", Range(0.0, 8.0)) = 2.2
        _PulseCount ("Pulse Count", Range(1.0, 6.0)) = 2.5
        _PulseSharpness ("Pulse Sharpness", Range(2.0, 40.0)) = 18.0
        _JunctionBloom ("Junction Bloom", Range(0.0, 4.0)) = 1.5

        [Header(Cluster Surge)]
        _SurgeAmount ("Cluster Surge Amount", Range(0.0, 4.0)) = 1.6
        _SurgeRadius ("Cluster Surge Radius", Range(0.15, 1.5)) = 0.62
        _SurgeSpeed ("Cluster Surge Speed", Range(0.0, 3.0)) = 0.55
        _SurgeSharpness ("Cluster Surge Edge", Range(0.5, 6.0)) = 2.2
        _TravelSpeed ("Bright Node Travel Speed", Range(0.15, 6.0)) = 1.35
        _TravelTrail ("Bright Node Travel Trail", Range(0.0, 1.0)) = 0.62
        _TravelBeatBoost ("Audio Travel Boost", Range(0.0, 4.0)) = 1.8
        _AuraAmount ("Cluster Aura Amount", Range(0.0, 3.0)) = 0.75
        _AuraSize ("Cluster Aura Size", Range(0.2, 2.0)) = 0.9

        [Header(Atmosphere)]
        _BackgroundDust ("Background Dust", Range(0.0, 2.0)) = 0.38
        _GlowStrength ("Glow Strength", Range(0.0, 4.0)) = 1.15
        _Vignette ("Vignette", Range(0.0, 1.0)) = 0.16

        [Header(Color)]
        _ColorSpeed ("Color Speed", Range(0.0, 3.0)) = 0.5
        _Saturation ("Saturation", Range(0.0, 2.0)) = 1.15
        _Brightness ("Brightness", Range(0.0, 3.0)) = 1.0
        _Contrast ("Contrast", Range(0.5, 2.5)) = 1.05

        [Header(Audio)]
        _AudioBass ("Audio Bass", Range(0.0, 1.0)) = 0.0
        _AudioMid ("Audio Mid", Range(0.0, 1.0)) = 0.0
        _AudioHigh ("Audio High", Range(0.0, 1.0)) = 0.0
        _AudioEnergy ("Audio Energy", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "Starloom"

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
                float _Speed;
                float _DriftSpeed;
                float _Tension;

                float _NodeDensity;
                float _ConnectionAmount;
                float _ThreadWidth;
                float _NodeSize;
                float _DepthSpread;

                float _BassReaction;
                float _MidReaction;
                float _HighReaction;
                float _EnergyReaction;

                float _PulseSpeed;
                float _PulseCount;
                float _PulseSharpness;
                float _JunctionBloom;

                float _SurgeAmount;
                float _SurgeRadius;
                float _SurgeSpeed;
                float _SurgeSharpness;
                float _TravelSpeed;
                float _TravelTrail;
                float _TravelBeatBoost;
                float _AuraAmount;
                float _AuraSize;

                float _BackgroundDust;
                float _GlowStrength;
                float _Vignette;

                float _ColorSpeed;
                float _Saturation;
                float _Brightness;
                float _Contrast;

                float _AudioBass;
                float _AudioMid;
                float _AudioHigh;
                float _AudioEnergy;
            CBUFFER_END

            static const float LOOM_TWO_PI = 6.28318530718;

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv = input.uv;

                return output;
            }

            float Hash11(float inputValue)
            {
                return frac(
                    sin(inputValue * 127.1) *
                    43758.5453123
                );
            }

            float Hash21(float2 inputValue)
            {
                float3 hashValue =
                    frac(
                        float3(inputValue.xyx) *
                        0.1031
                    );

                hashValue +=
                    dot(
                        hashValue,
                        hashValue.yzx + 33.33
                    );

                return frac(
                    (hashValue.x + hashValue.y) *
                    hashValue.z
                );
            }

            float2 Hash22(float2 inputValue)
            {
                float3 hashValue =
                    frac(
                        float3(inputValue.xyx) *
                        float3(
                            0.1031,
                            0.1030,
                            0.0973
                        )
                    );

                hashValue +=
                    dot(
                        hashValue,
                        hashValue.yzx + 33.33
                    );

                return frac(
                    float2(
                        (hashValue.x + hashValue.y) *
                        hashValue.z,

                        (hashValue.x + hashValue.z) *
                        hashValue.y
                    )
                );
            }

            float3 Hash33(float3 inputValue)
            {
                float3 hashValue =
                    frac(
                        inputValue *
                        0.1031
                    );

                hashValue +=
                    dot(
                        hashValue,
                        hashValue.yxz + 33.33
                    );

                return frac(
                    (hashValue.xxy + hashValue.yxx) *
                    hashValue.zyx
                );
            }

            float2 Rotate2D(
                float2 positionValue,
                float angleValue
            )
            {
                float sineValue =
                    sin(angleValue);

                float cosineValue =
                    cos(angleValue);

                return float2(
                    cosineValue * positionValue.x -
                    sineValue * positionValue.y,

                    sineValue * positionValue.x +
                    cosineValue * positionValue.y
                );
            }

            float3 SpectralColor(float phaseValue)
            {
                float3 colorValue =
                    0.5 +
                    0.5 *
                    cos(
                        LOOM_TWO_PI *
                        (
                            phaseValue +
                            float3(
                                0.00,
                                0.33,
                                0.67
                            )
                        )
                    );

                float luminanceValue =
                    dot(
                        colorValue,
                        float3(
                            0.2126,
                            0.7152,
                            0.0722
                        )
                    );

                colorValue =
                    lerp(
                        luminanceValue.xxx,
                        colorValue,
                        _Saturation
                    );

                return max(
                    colorValue,
                    0.0
                );
            }

            float DistanceToSegment(
                float2 samplePosition,
                float2 startPosition,
                float2 endPosition,
                out float segmentAmount
            )
            {
                float2 segmentVector =
                    endPosition -
                    startPosition;

                float segmentLengthSquared =
                    dot(
                        segmentVector,
                        segmentVector
                    );

                segmentAmount =
                    saturate(
                        dot(
                            samplePosition -
                            startPosition,
                            segmentVector
                        ) /
                        max(
                            segmentLengthSquared,
                            0.000001
                        )
                    );

                float2 closestPosition =
                    startPosition +
                    segmentVector *
                    segmentAmount;

                return length(
                    samplePosition -
                    closestPosition
                );
            }

            float PointGlow(
                float2 samplePosition,
                float2 centerPosition,
                float radiusValue,
                float bloomValue
            )
            {
                float distanceValue =
                    length(
                        samplePosition -
                        centerPosition
                    );

                float coreValue =
                    1.0 -
                    smoothstep(
                        radiusValue * 0.18,
                        radiusValue,
                        distanceValue
                    );

                float haloValue =
                    radiusValue /
                    max(
                        distanceValue,
                        0.001
                    );

                haloValue =
                    min(
                        haloValue *
                        haloValue *
                        0.014 *
                        bloomValue,
                        3.0
                    );

                float nucleusValue =
                    1.0 -
                    smoothstep(
                        radiusValue * 0.055,
                        radiusValue * 0.22,
                        distanceValue
                    );

                return
                    coreValue +
                    haloValue +
                    nucleusValue * 0.58;
            }

            float2 GetSurgeCenter(
                float timeValue,
                float aspectValue,
                float bassValue,
                float energyValue
            )
            {
                // Stateless beat-driven node hopping: each travel step chooses a new
                // deterministic destination, then eases toward it instead of orbiting
                // around one permanent location.
                float audioDrive =
                    saturate(
                        bassValue * _BassReaction * 0.65 +
                        energyValue * _EnergyReaction * 0.85
                    );

                float travelClock =
                    timeValue *
                    max(_TravelSpeed, 0.01) *
                    (1.0 + audioDrive * _TravelBeatBoost);

                float travelStep = floor(travelClock);
                float travelAmount = frac(travelClock);

                travelAmount =
                    travelAmount * travelAmount *
                    (3.0 - 2.0 * travelAmount);

                float2 previousTarget =
                    Hash22(
                        float2(
                            travelStep,
                            travelStep * 1.731 + 19.17
                        )
                    ) * 2.0 - 1.0;

                float2 nextTarget =
                    Hash22(
                        float2(
                            travelStep + 1.0,
                            (travelStep + 1.0) * 1.731 + 19.17
                        )
                    ) * 2.0 - 1.0;

                // Keep targets inside the visible network while still using most
                // of the frame. X is corrected into the same aspect-scaled space
                // used by the rendered node positions.
                previousTarget *= float2(0.78 * aspectValue, 0.78);
                nextTarget *= float2(0.78 * aspectValue, 0.78);

                // Audio tightens the handoff so strong sounds visibly launch the
                // brightest point toward the next destination.
                float launchAmount =
                    saturate(
                        travelAmount +
                        audioDrive * 0.18
                    );

                launchAmount =
                    launchAmount * launchAmount *
                    (3.0 - 2.0 * launchAmount);

                return lerp(
                    previousTarget,
                    nextTarget,
                    launchAmount
                );
            }

            float ClusterSurge(
                float2 worldPosition,
                float2 surgeCenter,
                float bassValue,
                float energyValue
            )
            {
                float distanceValue =
                    length(
                        worldPosition -
                        surgeCenter
                    );

                float musicStrength =
                    saturate(
                        bassValue *
                        _BassReaction *
                        0.72 +
                        energyValue *
                        _EnergyReaction *
                        0.88
                    );

                float radiusValue =
                    _SurgeRadius *
                    0.58 *
                    (
                        0.82 +
                        musicStrength * 0.34
                    );

                float surgeValue =
                    1.0 -
                    smoothstep(
                        radiusValue * 0.2,
                        radiusValue,
                        distanceValue
                    );

                surgeValue =
                    pow(
                        saturate(surgeValue),
                        _SurgeSharpness
                    );

                float outerRing =
                    1.0 -
                    smoothstep(
                        0.025,
                        0.12,
                        abs(
                            distanceValue -
                            radiusValue * 0.72
                        )
                    );

                outerRing *=
                    musicStrength *
                    0.28;

                return
                    saturate(
                        surgeValue +
                        outerRing
                    ) *
                    musicStrength;
            }

            float ClusterAura(
                float2 worldPosition,
                float2 surgeCenter,
                float bassValue,
                float energyValue
            )
            {
                float distanceValue =
                    length(
                        worldPosition -
                        surgeCenter
                    );

                float musicStrength =
                    saturate(
                        bassValue *
                        _BassReaction *
                        0.55 +
                        energyValue *
                        _EnergyReaction *
                        0.85
                    );

                float auraRadius =
                    max(
                        _AuraSize,
                        0.001
                    );

                float auraValue =
                    exp(
                        -distanceValue *
                        distanceValue *
                        3.0 /
                        (
                            auraRadius *
                            auraRadius
                        )
                    );

                return
                    auraValue *
                    musicStrength *
                    _AuraAmount;
            }

            float3 GetNodePosition(
                float2 cellValue,
                float timeValue,
                float bassValue,
                float energyValue
            )
            {
                float3 randomValue =
                    Hash33(
                        float3(
                            cellValue,
                            7.37
                        )
                    );

                float3 nodePosition =
                    float3(
                        randomValue.xy - 0.5,
                        randomValue.z * 2.0 - 1.0
                    );

                float phaseValue =
                    Hash21(
                        cellValue + 19.7
                    ) *
                    LOOM_TWO_PI;

                float energyMotion =
                    energyValue *
                    _EnergyReaction;

                float driftTime =
                    timeValue *
                    _DriftSpeed *
                    (
                        1.0 +
                        energyMotion * 0.4
                    );

                nodePosition.x +=
                    sin(
                        driftTime * 0.43 +
                        phaseValue
                    ) *
                    (
                        0.065 +
                        energyMotion * 0.035
                    );

                nodePosition.y +=
                    cos(
                        driftTime * 0.34 +
                        phaseValue * 1.31
                    ) *
                    (
                        0.06 +
                        energyMotion * 0.035
                    );

                nodePosition.z +=
                    sin(
                        driftTime * 0.25 +
                        phaseValue * 0.71
                    ) *
                    0.28 *
                    _DepthSpread;

                float bassPull =
                    bassValue *
                    _BassReaction;

                nodePosition.xy *=
                    1.0 +
                    bassPull * 0.025;

                return nodePosition;
            }

            float2 GetNeighborOffset(
                int neighborIndex
            )
            {
                if (neighborIndex == 0)
                {
                    return float2(
                        1.0,
                        0.0
                    );
                }

                if (neighborIndex == 1)
                {
                    return float2(
                        0.0,
                        1.0
                    );
                }

                if (neighborIndex == 2)
                {
                    return float2(
                        1.0,
                        1.0
                    );
                }

                return float2(
                    -1.0,
                    1.0
                );
            }

            float CurvedThread(
                float2 samplePosition,
                float2 startPosition,
                float2 endPosition,
                float seedValue,
                float timeValue,
                float bassValue,
                float energyValue,
                out float threadAmount
            )
            {
                float2 directionVector =
                    endPosition -
                    startPosition;

                float nodeDistance =
                    length(
                        directionVector
                    );

                if (nodeDistance < 0.001)
                {
                    threadAmount = 0.0;
                    return 0.0;
                }

                float2 normalVector =
                    float2(
                        -directionVector.y,
                        directionVector.x
                    ) /
                    nodeDistance;

                float bassBend =
                    bassValue *
                    _BassReaction;

                float energyBend =
                    energyValue *
                    _EnergyReaction;

                float bendWave =
                    sin(
                        seedValue * 22.7 +
                        timeValue *
                        (
                            0.27 +
                            energyBend * 0.16
                        )
                    );

                float bendAmount =
                    bendWave *
                    nodeDistance *
                    _Tension *
                    (
                        0.022 +
                        bassBend * 0.017
                    );

                float2 midpointPosition =
                    lerp(
                        startPosition,
                        endPosition,
                        0.5
                    ) +
                    normalVector *
                    bendAmount;

                float firstAmount;
                float secondAmount;

                float firstDistance =
                    DistanceToSegment(
                        samplePosition,
                        startPosition,
                        midpointPosition,
                        firstAmount
                    );

                float secondDistance =
                    DistanceToSegment(
                        samplePosition,
                        midpointPosition,
                        endPosition,
                        secondAmount
                    );

                float useSecond =
                    step(
                        secondDistance,
                        firstDistance
                    );

                float chosenDistance =
                    lerp(
                        firstDistance,
                        secondDistance,
                        useSecond
                    );

                threadAmount =
                    lerp(
                        firstAmount * 0.5,
                        0.5 +
                        secondAmount * 0.5,
                        useSecond
                    );

                float widthValue =
                    _ThreadWidth *
                    (
                        1.0 +
                        bassValue *
                        _BassReaction *
                        0.2
                    );

                float coreValue =
                    1.0 -
                    smoothstep(
                        widthValue,
                        widthValue * 2.2,
                        chosenDistance
                    );

                float fineCoreValue =
                    1.0 -
                    smoothstep(
                        widthValue * 0.18,
                        widthValue * 0.72,
                        chosenDistance
                    );

                float filamentVariation =
                    0.78 +
                    0.22 *
                    sin(
                        threadAmount * 46.0 +
                        seedValue * 31.0 +
                        timeValue * 0.34
                    );

                fineCoreValue *=
                    filamentVariation;

                float haloValue =
                    widthValue /
                    max(
                        chosenDistance,
                        0.001
                    );

                haloValue =
                    min(
                        haloValue *
                        haloValue *
                        0.012 *
                        _GlowStrength,
                        1.2
                    );

                return
                    coreValue +
                    fineCoreValue * 0.48 +
                    haloValue;
            }

            float MusicPulse(
                float threadAmount,
                float seedValue,
                float timeValue,
                float midValue,
                float energyValue
            )
            {
                float midReaction =
                    midValue *
                    _MidReaction;

                float energyReaction =
                    energyValue *
                    _EnergyReaction;

                float pulseActivity =
                    saturate(
                        midReaction * 1.25 +
                        energyReaction * 0.18
                    );

                float pulseSpeed =
                    _PulseSpeed *
                    (
                        0.25 +
                        midReaction * 1.1 +
                        energyReaction * 0.25
                    );

                float pulseCoordinate =
                    threadAmount *
                    _PulseCount -
                    timeValue *
                    pulseSpeed *
                    (
                        0.16 +
                        seedValue * 0.18
                    );

                float pulseWave =
                    sin(
                        pulseCoordinate *
                        LOOM_TWO_PI
                    ) *
                    0.5 +
                    0.5;

                return
                    pow(
                        saturate(pulseWave),
                        _PulseSharpness
                    ) *
                    pulseActivity;
            }

            float BackgroundDust(
                float2 uvValue,
                float timeValue,
                float highValue
            )
            {
                float2 dustCoordinates =
                    uvValue * 92.0;

                dustCoordinates +=
                    float2(
                        timeValue * 0.01,
                        -timeValue * 0.007
                    );

                float2 dustCell =
                    floor(
                        dustCoordinates
                    );

                float2 localPosition =
                    frac(
                        dustCoordinates
                    ) -
                    0.5;

                float randomValue =
                    Hash21(
                        dustCell
                    );

                float2 dustOffset =
                    Hash22(
                        dustCell + 4.7
                    ) -
                    0.5;

                float distanceValue =
                    length(
                        localPosition -
                        dustOffset * 0.76
                    );

                float dustValue =
                    1.0 -
                    smoothstep(
                        0.012,
                        0.052,
                        distanceValue
                    );

                float existenceValue =
                    step(
                        0.978 -
                        highValue *
                        _HighReaction *
                        0.018,
                        randomValue
                    );

                float flickerValue =
                    0.35 +
                    0.65 *
                    pow(
                        saturate(
                            sin(
                                timeValue * 2.8 +
                                randomValue * 42.0
                            ) *
                            0.5 +
                            0.5
                        ),
                        5.0
                    );

                return
                    dustValue *
                    existenceValue *
                    flickerValue *
                    _BackgroundDust;
            }

            float3 BackgroundField(
                float2 screenPosition,
                float timeValue
            )
            {
                float2 fieldPosition =
                    screenPosition * 1.35;

                fieldPosition +=
                    float2(
                        sin(timeValue * 0.037),
                        cos(timeValue * 0.029)
                    ) *
                    0.12;

                float broadWave =
                    sin(fieldPosition.x * 3.1 + timeValue * 0.055) *
                    sin(fieldPosition.y * 2.7 - timeValue * 0.043);

                float crossWave =
                    sin(
                        (fieldPosition.x + fieldPosition.y) * 4.6 -
                        timeValue * 0.031
                    );

                float fieldValue =
                    saturate(
                        0.46 +
                        broadWave * 0.22 +
                        crossWave * 0.12
                    );

                fieldValue =
                    fieldValue * fieldValue *
                    (3.0 - 2.0 * fieldValue);

                float3 fieldColor =
                    SpectralColor(
                        fieldValue * 0.22 +
                        timeValue * 0.004 * _ColorSpeed
                    );

                return
                    fieldColor *
                    (0.006 + fieldValue * 0.012);
            }

            half4 frag(
            Varyings input
            ) : SV_Target
            {
                 // Hide everything outside the circular portal opening.
                float2 portalUV = input.uv - 0.5;
                float portalDistance = length(portalUV);

                clip(0.49 - portalDistance);

                float bassValue =
                    saturate(
                        _AudioBass
                    );

                float midValue =
                    saturate(
                        _AudioMid
                    );

                float highValue =
                    saturate(
                        _AudioHigh
                    );

                float energyValue =
                    saturate(
                        _AudioEnergy
                    );

                // Overall energy amplifies the individual bands instead of
                // replacing their separate musical personalities.
                float energyLift =
                    0.55 +
                    energyValue *
                    _EnergyReaction *
                    1.45;

                float bassMusic =
                    saturate(
                        bassValue *
                        energyLift
                    );

                float midMusic =
                    saturate(
                        midValue *
                        energyLift
                    );

                float highMusic =
                    saturate(
                        highValue *
                        energyLift
                    );

                float2 uvValue =
                    input.uv;

                float2 screenPosition =
                    uvValue * 2.0 -
                    1.0;

                float aspectValue =
                    _ScreenParams.x /
                    max(
                        _ScreenParams.y,
                        1.0
                    );

                screenPosition.x *=
                    aspectValue;

                float timeValue =
                    _Time.y *
                    _Speed;

                float energyMotion =
                    energyValue *
                    _EnergyReaction;

                float rotationValue =
                    sin(
                        timeValue * 0.055
                    ) *
                    (
                        0.012 +
                        energyMotion * 0.010 +
                        midMusic *
                        _MidReaction *
                        0.055
                    );

                screenPosition =
                    Rotate2D(
                        screenPosition,
                        rotationValue
                    );

                float bassBreath =
                    bassMusic *
                    _BassReaction;

                screenPosition /=
                    1.0 +
                    bassBreath * 0.072;

                float2 surgeCenter =
                    GetSurgeCenter(
                        timeValue,
                        aspectValue,
                        bassMusic,
                        saturate(
                            midMusic * 0.82 +
                            highMusic * 0.22
                        )
                    );

                float auraValue =
                    ClusterAura(
                        screenPosition,
                        surgeCenter,
                        bassMusic,
                        saturate(
                            midMusic * 0.72 +
                            highMusic * 0.28
                        )
                    );

                float surgePhase =
                    timeValue *
                    0.035 *
                    _ColorSpeed +
                    length(surgeCenter) * 0.18;

                float3 surgeColor =
                    SpectralColor(
                        surgePhase
                    );

                float3 finalColor =
                    float3(
                        0.0012,
                        0.0018,
                        0.0048
                    );

                finalColor +=
                    BackgroundField(
                        screenPosition,
                        timeValue
                    );

                finalColor +=
                    surgeColor *
                    auraValue *
                    float3(
                        0.055,
                        0.075,
                        0.13
                    );

                // Multiply the serialized material value so existing materials
                // actually become denser; changing only the property default does
                // not override values already stored in M_Starloom.mat.
                float densityValue =
                    max(
                        _NodeDensity * 1.75,
                        6.0
                    );

                float2 gridPosition =
                    screenPosition *
                    densityValue;

                float2 baseCell =
                    floor(
                        gridPosition
                    );

                float2 localPosition =
                    frac(
                        gridPosition
                    ) -
                    0.5;

                float3 threadColor =
                    float3(
                        0.0,
                        0.0,
                        0.0
                    );

                float3 nodeColor =
                    float3(
                        0.0,
                        0.0,
                        0.0
                    );

                float3 pulseColor =
                    float3(
                        0.0,
                        0.0,
                        0.0
                    );

                float3 surgeLightColor =
                    float3(
                        0.0,
                        0.0,
                        0.0
                    );

                float accumulatedLight =
                    0.0;

                for (
                    int verticalOffset = -1;
                    verticalOffset <= 1;
                    verticalOffset++
                )
                {
                    for (
                        int horizontalOffset = -1;
                        horizontalOffset <= 1;
                        horizontalOffset++
                    )
                    {
                        float2 cellOffset =
                            float2(
                                (float)horizontalOffset,
                                (float)verticalOffset
                            );

                        float2 currentCell =
                            baseCell +
                            cellOffset;

                        float3 currentNode3D =
                            GetNodePosition(
                                currentCell,
                                timeValue,
                                bassMusic,
                                saturate(
                                    midMusic * 0.86 +
                                    energyValue * 0.24
                                )
                            );

                        float perspectiveValue =
                            1.0 /
                            (
                                1.0 +
                                currentNode3D.z *
                                0.14 *
                                _DepthSpread
                            );

                        float2 currentNode =
                            (
                                cellOffset +
                                currentNode3D.xy
                            ) *
                            perspectiveValue;

                        float2 currentNodeWorld =
                            (
                                currentCell +
                                currentNode3D.xy
                            ) /
                            densityValue;

                        float nodeSurge =
                            ClusterSurge(
                                currentNodeWorld,
                                surgeCenter,
                                bassMusic,
                                saturate(
                                    midMusic * 0.70 +
                                    highMusic * 0.30
                                )
                            );

                        float nodeSeed =
                            Hash21(
                                currentCell + 7.37
                            );

                        float depthBrightness =
                            saturate(
                                0.68 +
                                currentNode3D.z *
                                0.2
                            );

                        float nodeRadius =
                            _NodeSize *
                            (
                                0.65 +
                                nodeSeed * 0.7
                            ) *
                            (
                                1.0 +
                                bassMusic *
                                _BassReaction *
                                1.65 +
                                nodeSurge *
                                _SurgeAmount *
                                0.5
                            );

                        float bloomValue =
                            1.0 +
                            bassMusic *
                            _BassReaction *
                            _JunctionBloom *
                            1.45 +
                            highMusic *
                            _HighReaction *
                            _JunctionBloom *
                            0.75 +
                            nodeSurge *
                            _SurgeAmount;

                        float nodeGlow =
                            PointGlow(
                                localPosition,
                                currentNode,
                                nodeRadius,
                                bloomValue
                            );

                        float nodeFlicker =
                            0.76 +
                            (
                                0.18 +
                                highMusic *
                                _HighReaction *
                                0.38
                            ) *
                            sin(
                                timeValue *
                                (
                                    1.0 +
                                    energyMotion * 0.35 +
                                    midMusic * _MidReaction * 2.2 +
                                    highMusic * _HighReaction * 6.5
                                ) +
                                nodeSeed * 35.0
                            );

                        nodeGlow *=
                            nodeFlicker *
                            depthBrightness;

                        float nodePhase =
                            nodeSeed +
                            currentNode3D.z * 0.12 +
                            timeValue *
                            0.012 *
                            _ColorSpeed;

                        float3 currentNodeColor =
                            SpectralColor(
                                nodePhase
                            );

                        nodeColor +=
                            currentNodeColor *
                            nodeGlow;

                        float nodeMicroGlint =
                            pow(
                                saturate(
                                    nodeGlow * 0.72
                                ),
                                2.4
                            ) *
                            (
                                0.12 +
                                highMusic *
                                _HighReaction *
                                0.42
                            );

                        nodeColor +=
                            lerp(
                                currentNodeColor,
                                float3(
                                    1.0,
                                    1.0,
                                    1.0
                                ),
                                0.82
                            ) *
                            nodeMicroGlint;

                        surgeLightColor +=
                            lerp(
                                currentNodeColor,
                                float3(
                                    1.0,
                                    1.0,
                                    1.0
                                ),
                                0.62
                            ) *
                            nodeGlow *
                            nodeSurge *
                            _SurgeAmount;

                        accumulatedLight +=
                            nodeGlow *
                            (
                                0.012 +
                                nodeSurge * 0.016
                            );

                        for (
                            int neighborIndex = 0;
                            neighborIndex < 4;
                            neighborIndex++
                        )
                        {
                            float2 neighborOffset =
                                GetNeighborOffset(
                                    neighborIndex
                                );

                            float2 neighborCell =
                                currentCell +
                                neighborOffset;

                            float3 neighborNode3D =
                                GetNodePosition(
                                    neighborCell,
                                    timeValue,
                                    bassMusic,
                                    saturate(
                                        midMusic * 0.86 +
                                        energyValue * 0.24
                                    )
                                );

                            float neighborPerspective =
                                1.0 /
                                (
                                    1.0 +
                                    neighborNode3D.z *
                                    0.14 *
                                    _DepthSpread
                                );

                            float2 neighborNode =
                                (
                                    cellOffset +
                                    neighborOffset +
                                    neighborNode3D.xy
                                ) *
                                neighborPerspective;

                            float2 neighborNodeWorld =
                                (
                                    neighborCell +
                                    neighborNode3D.xy
                                ) /
                                densityValue;

                            float2 connectionWorldPosition =
                                lerp(
                                    currentNodeWorld,
                                    neighborNodeWorld,
                                    0.5
                                );

                            float connectionSurge =
                                ClusterSurge(
                                    connectionWorldPosition,
                                    surgeCenter,
                                    bassMusic,
                                    saturate(
                                        midMusic * 0.70 +
                                        highMusic * 0.30
                                    )
                                );

                            float connectionSeed =
                                Hash21(
                                    currentCell +
                                    neighborOffset * 7.31 +
                                    11.9
                                );

                            float connectionBoost =
                                midMusic *
                                _MidReaction *
                                0.28 +
                                highMusic *
                                _HighReaction *
                                0.12 +
                                energyValue *
                                _EnergyReaction *
                                0.04;

                            float activationThreshold =
                                1.0 -
                                saturate(
                                    _ConnectionAmount +
                                    0.22 +
                                    connectionBoost
                                );

                            float connectionMask =
                                step(
                                    activationThreshold,
                                    connectionSeed
                                );

                            float threadAmount;

                            float threadValue =
                                CurvedThread(
                                    localPosition,
                                    currentNode,
                                    neighborNode,
                                    connectionSeed,
                                    timeValue,
                                    bassMusic,
                                    midMusic,
                                    threadAmount
                                );

                            float endpointFade =
                                smoothstep(
                                    0.0,
                                    0.008,
                                    threadAmount
                                ) *
                                smoothstep(
                                    0.0,
                                    0.008,
                                    1.0 -
                                    threadAmount
                                );

                            threadValue *=
                                connectionMask *
                                endpointFade *
                                depthBrightness;

                            float pulseValue =
                                MusicPulse(
                                    threadAmount,
                                    connectionSeed,
                                    timeValue,
                                    midMusic,
                                    saturate(
                                        highMusic * 0.72 +
                                        energyValue * 0.18
                                    )
                                );

                            pulseValue *=
                                threadValue;

                            float threadPhase =
                                connectionSeed +
                                threadAmount * 0.25 +
                                currentNode3D.z * 0.1 +
                                timeValue *
                                0.01 *
                                _ColorSpeed;

                            float3 currentThreadColor =
                                SpectralColor(
                                    threadPhase
                                );

                            float strandFiber =
                                0.86 +
                                0.14 *
                                sin(
                                    threadAmount * 58.0 +
                                    connectionSeed * 37.0 -
                                    timeValue * 0.48
                                );

                            float strandSpark =
                                pow(
                                    saturate(
                                        sin(
                                            threadAmount * 31.0 -
                                            timeValue *
                                            (
                                                1.6 +
                                                highMusic *
                                                _HighReaction *
                                                4.2
                                            ) +
                                            connectionSeed * 52.0
                                        ) *
                                        0.5 +
                                        0.5
                                    ),
                                    18.0
                                ) *
                                (
                                    0.05 +
                                    highMusic *
                                    _HighReaction *
                                    0.38
                                );

                            threadColor +=
                                currentThreadColor *
                                threadValue *
                                strandFiber *
                                (
                                    0.48 +
                                    midMusic *
                                    _MidReaction *
                                    0.92 +
                                    highMusic *
                                    _HighReaction *
                                    0.20 +
                                    energyValue *
                                    _EnergyReaction *
                                    0.05
                                );

                            threadColor +=
                                lerp(
                                    currentThreadColor,
                                    float3(
                                        1.0,
                                        1.0,
                                        1.0
                                    ),
                                    0.72
                                ) *
                                threadValue *
                                strandSpark;

                            pulseColor +=
                                lerp(
                                    currentThreadColor,
                                    float3(
                                        1.0,
                                        1.0,
                                        1.0
                                    ),
                                    0.78
                                ) *
                                pulseValue *
                                (
                                    0.72 +
                                    midMusic *
                                    _MidReaction *
                                    2.35 +
                                    highMusic *
                                    _HighReaction *
                                    0.32
                                );

                            surgeLightColor +=
                                lerp(
                                    currentThreadColor,
                                    surgeColor,
                                    0.58
                                ) *
                                threadValue *
                                connectionSurge *
                                _SurgeAmount *
                                (
                                    0.42 +
                                    bassMusic *
                                    _BassReaction *
                                    1.35 +
                                    midMusic *
                                    _MidReaction *
                                    0.58 +
                                    energyValue *
                                    _EnergyReaction *
                                    0.08
                                );

                            accumulatedLight +=
                                threadValue *
                                (
                                    0.008 +
                                    connectionSurge * 0.012
                                ) +
                                pulseValue *
                                0.022;
                        }
                    }
                }

                finalColor +=
                    threadColor;

                finalColor +=
                    nodeColor;

                finalColor +=
                    pulseColor;

                finalColor +=
                    surgeLightColor;

                float dustValue =
                    BackgroundDust(
                        uvValue,
                        timeValue,
                        highMusic
                    );

                float dustPhase =
                    Hash21(
                        floor(
                            uvValue * 92.0
                        )
                    ) +
                    timeValue *
                    0.007 *
                    _ColorSpeed;

                finalColor +=
                    SpectralColor(
                        dustPhase
                    ) *
                    dustValue *
                    (
                        0.12 +
                        highMusic *
                        _HighReaction *
                        1.55
                    );

                finalColor +=
                    finalColor *
                    saturate(
                        accumulatedLight
                    ) *
                    _GlowStrength *
                    (
                        0.075 +
                        bassMusic *
                        _BassReaction *
                        0.15 +
                        midMusic *
                        _MidReaction *
                        0.11 +
                        highMusic *
                        _HighReaction *
                        0.075 +
                        energyValue *
                        _EnergyReaction *
                        0.018
                    );

                float vignetteValue =
                    1.0 -
                    smoothstep(
                        0.72,
                        1.82,
                        length(
                            screenPosition
                        )
                    );

                finalColor *=
                    lerp(
                        1.0 -
                        _Vignette,
                        1.0,
                        vignetteValue
                    );

                finalColor *=
                    _Brightness;

                finalColor =
                    pow(
                        max(
                            finalColor,
                            0.0
                        ),
                        _Contrast
                    );

                finalColor =
                    finalColor /
                    (
                        1.0 +
                        finalColor * 0.36
                    );

                return half4(
                    finalColor,
                    1.0
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}