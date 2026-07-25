Shader "FidgetFlow/PortalRingUniversal"
{
    Properties
    {
        [Header(Base)]
        [HDR] _BaseColor ("Base Color", Color) = (0.12, 0.20, 0.75, 1.0)
        [HDR] _GlowColor ("Glow Color", Color) = (0.25, 0.60, 1.35, 1.0)
        [HDR] _PacketColor ("Energy Packet Color", Color) = (0.75, 0.90, 1.60, 1.0)

        _BaseBrightness ("Base Brightness", Range(0.0, 3.0)) = 0.42
        _EmissionStrength ("Emission Strength", Range(0.0, 5.0)) = 0.70

        [Header(Energy Packets)]
        _PacketCount ("Packet Count", Range(1.0, 6.0)) = 2.0
        _PacketSpeed ("Packet Speed", Range(-3.0, 3.0)) = 0.18
        _PacketWidth ("Packet Width", Range(0.005, 0.25)) = 0.055
        _PacketStrength ("Packet Strength", Range(0.0, 4.0)) = 0.55
        _PacketSharpness ("Packet Sharpness", Range(1.0, 12.0)) = 4.0

        [Header(Secondary Packet)]
        _SecondaryPacketSpeed ("Secondary Speed", Range(-3.0, 3.0)) = -0.10
        _SecondaryPacketWidth ("Secondary Width", Range(0.005, 0.25)) = 0.035
        _SecondaryPacketStrength ("Secondary Strength", Range(0.0, 2.0)) = 0.18

        [Header(Breathing)]
        _BreathSpeed ("Breath Speed", Range(0.0, 4.0)) = 0.30
        _BreathAmount ("Breath Amount", Range(0.0, 0.5)) = 0.035

        [Header(Subtle Surface Variation)]
        _VariationScale ("Variation Scale", Range(1.0, 30.0)) = 7.0
        _VariationSpeed ("Variation Speed", Range(0.0, 2.0)) = 0.06
        _VariationAmount ("Variation Amount", Range(0.0, 0.5)) = 0.035

        [Header(Audio)]
        _AudioBass ("Audio Bass", Range(0.0, 1.0)) = 0.0
        _AudioMid ("Audio Mid", Range(0.0, 1.0)) = 0.0
        _AudioHigh ("Audio High", Range(0.0, 1.0)) = 0.0
        _AudioEnergy ("Audio Energy", Range(0.0, 1.0)) = 0.0

        _AudioBrightnessBoost ("Audio Brightness Boost", Range(0.0, 2.0)) = 0.16
        _AudioPacketBoost ("Audio Packet Boost", Range(0.0, 3.0)) = 0.35
        _AudioSpeedBoost ("Audio Speed Boost", Range(0.0, 2.0)) = 0.20

        [Header(Rendering)]
        _Alpha ("Alpha", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Back
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "UniversalPortalRing"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirectionWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GlowColor;
                float4 _PacketColor;

                float _BaseBrightness;
                float _EmissionStrength;

                float _PacketCount;
                float _PacketSpeed;
                float _PacketWidth;
                float _PacketStrength;
                float _PacketSharpness;

                float _SecondaryPacketSpeed;
                float _SecondaryPacketWidth;
                float _SecondaryPacketStrength;

                float _BreathSpeed;
                float _BreathAmount;

                float _VariationScale;
                float _VariationSpeed;
                float _VariationAmount;

                float _AudioBass;
                float _AudioMid;
                float _AudioHigh;
                float _AudioEnergy;

                float _AudioBrightnessBoost;
                float _AudioPacketBoost;
                float _AudioSpeedBoost;

                float _Alpha;
            CBUFFER_END

            static const float PORTAL_TWO_PI = 6.28318530718;

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);

                output.positionHCS =
                    positionInputs.positionCS;

                output.positionOS =
                    input.positionOS.xyz;

                output.normalWS =
                    normalize(normalInputs.normalWS);

                output.viewDirectionWS =
                    normalize(
                        GetWorldSpaceViewDir(
                            positionInputs.positionWS
                        )
                    );

                return output;
            }

            float Hash11(float value)
            {
                return frac(
                    sin(value * 127.1) *
                    43758.5453123
                );
            }

            float WrappedDistance(
                float firstValue,
                float secondValue
            )
            {
                float distanceValue =
                    abs(
                        firstValue -
                        secondValue
                    );

                return min(
                    distanceValue,
                    1.0 - distanceValue
                );
            }

            float EnergyPacket(
                float ringCoordinate,
                float packetPosition,
                float packetWidth,
                float packetSharpness
            )
            {
                float distanceValue =
                    WrappedDistance(
                        ringCoordinate,
                        packetPosition
                    );

                float packetValue =
                    1.0 -
                    smoothstep(
                        packetWidth * 0.18,
                        packetWidth,
                        distanceValue
                    );

                return pow(
                    saturate(packetValue),
                    packetSharpness
                );
            }

            half4 frag(Varyings input) : SV_Target
            {
                float audioBass =
                    saturate(_AudioBass);

                float audioMid =
                    saturate(_AudioMid);

                float audioHigh =
                    saturate(_AudioHigh);

                float audioEnergy =
                    saturate(_AudioEnergy);

                float timeValue =
                    _Time.y;

                /*
                    Object-space angle around the torus.

                    This ignores the torus UV map completely, so there are no
                    barcode seams or stretched vertical bands.
                */
                float angleValue =
                    atan2(
                        input.positionOS.z,
                        input.positionOS.x
                    );

                float ringCoordinate =
                    frac(
                        angleValue /
                        PORTAL_TWO_PI +
                        0.5
                    );

                float speedMultiplier =
                    1.0 +
                    audioEnergy *
                    _AudioSpeedBoost;

                float packetTotal =
                    0.0;

                int packetCount =
                    clamp(
                        (int)round(_PacketCount),
                        1,
                        6
                    );

                /*
                    Primary packets.

                    Each packet is evenly separated but receives a tiny
                    deterministic offset so the movement does not feel mechanical.
                */
                for (int packetIndex = 0;
                     packetIndex < 6;
                     packetIndex++)
                {
                    if (packetIndex >= packetCount)
                    {
                        break;
                    }

                    float packetSeed =
                        Hash11(
                            packetIndex + 3.17
                        );

                    float packetOffset =
                        (
                            (float)packetIndex /
                            max(
                                (float)packetCount,
                                1.0
                            )
                        );

                    packetOffset +=
                        (
                            packetSeed -
                            0.5
                        ) *
                        0.08;

                    float packetPosition =
                        frac(
                            packetOffset +
                            timeValue *
                            _PacketSpeed *
                            speedMultiplier
                        );

                    packetTotal +=
                        EnergyPacket(
                            ringCoordinate,
                            packetPosition,
                            _PacketWidth,
                            _PacketSharpness
                        );
                }

                packetTotal =
                    saturate(packetTotal);

                /*
                    One faint counter-moving packet prevents the ring from
                    looking like a single light going around a racetrack.
                */
                float secondaryPosition =
                    frac(
                        0.31 +
                        timeValue *
                        _SecondaryPacketSpeed *
                        speedMultiplier
                    );

                float secondaryPacket =
                    EnergyPacket(
                        ringCoordinate,
                        secondaryPosition,
                        _SecondaryPacketWidth,
                        _PacketSharpness * 0.75
                    );

                /*
                    Very subtle non-UV surface variation.
                */
                float variation =
                    sin(
                        angleValue *
                        _VariationScale +
                        timeValue *
                        _VariationSpeed
                    );

                variation +=
                    sin(
                        angleValue *
                        (_VariationScale * 0.47) -
                        timeValue *
                        (_VariationSpeed * 0.63)
                    ) *
                    0.5;

                variation *=
                    _VariationAmount;

                float breath =
                    sin(
                        timeValue *
                        _BreathSpeed *
                        PORTAL_TWO_PI
                    ) *
                    0.5 +
                    0.5;

                breath *=
                    _BreathAmount;

                /*
                    Very restrained edge shaping so the torus keeps some depth
                    without requiring scene lighting.
                */
                float facingValue =
                    saturate(
                        dot(
                            input.normalWS,
                            input.viewDirectionWS
                        )
                    );

                float softShape =
                    lerp(
                        0.76,
                        1.0,
                        pow(
                            facingValue,
                            0.7
                        )
                    );

                float baseBrightness =
                    _BaseBrightness *
                    softShape;

                baseBrightness *=
                    1.0 +
                    breath +
                    variation;

                baseBrightness *=
                    1.0 +
                    audioEnergy *
                    _AudioBrightnessBoost;

                float packetAudioBoost =
                    1.0 +
                    audioEnergy *
                    _AudioPacketBoost +
                    audioHigh *
                    _AudioPacketBoost *
                    0.30;

                float primaryLight =
                    packetTotal *
                    _PacketStrength *
                    packetAudioBoost;

                float secondaryLight =
                    secondaryPacket *
                    _SecondaryPacketStrength *
                    (
                        1.0 +
                        audioMid *
                        _AudioPacketBoost *
                        0.25
                    );

                float3 finalColor =
                    _BaseColor.rgb *
                    baseBrightness;

                finalColor +=
                    _GlowColor.rgb *
                    (
                        breath * 0.18 +
                        audioBass *
                        _AudioBrightnessBoost *
                        0.10
                    ) *
                    _EmissionStrength;

                finalColor +=
                    _PacketColor.rgb *
                    primaryLight *
                    _EmissionStrength;

                finalColor +=
                    _GlowColor.rgb *
                    secondaryLight *
                    _EmissionStrength;

                return half4(
                    finalColor,
                    _Alpha
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}