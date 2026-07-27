Shader "UI/RainbowPortalTransition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Transition)]
        _Progress ("Progress", Range(0, 1)) = 0
        _Center ("Portal Center", Vector) = (0.5, 0.5, 0, 0)

        [Header(Tunnel Shape)]
        _SwirlStrength ("Swirl Strength", Range(0, 12)) = 5
        _Rotation ("Rotation", Range(-10, 10)) = 1.5
        _Zoom ("Forward Speed", Range(0, 20)) = 7
        _TunnelDepth ("Tunnel Depth", Range(1, 20)) = 8
        _StreakLength ("Streak Length", Range(0.1, 8)) = 2.5

        [Header(Rainbow)]
        _ColorCycles ("Color Cycles", Range(1, 12)) = 5
        _Saturation ("Saturation", Range(0, 2)) = 1.0
        _Brightness ("Brightness", Range(0, 4)) = 0.82
        _ColorShift ("Color Shift", Range(-2, 2)) = 0

        [Header(Detail)]
        _BandDensity ("Band Density", Range(1, 40)) = 14
        _BandSharpness ("Band Sharpness", Range(0.25, 8)) = 2
        _WarpStrength ("Warp Strength", Range(0, 2)) = 0.45
        _NoiseScale ("Noise Scale", Range(1, 30)) = 8

        [Header(Coverage)]
        _EdgeSoftness ("Edge Softness", Range(0.001, 1)) = 0.35
        _Opacity ("Opacity", Range(0, 1)) = 1
        _OverlayAlpha ("Overlay Alpha", Range(0,1)) = 0
        _CoreBrightness ("Core Brightness", Range(0, 5)) = 0.85

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "RainbowPortalTransition"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _Progress;
            float4 _Center;

            float _SwirlStrength;
            float _Rotation;
            float _Zoom;
            float _TunnelDepth;
            float _StreakLength;

            float _ColorCycles;
            float _Saturation;
            float _Brightness;
            float _ColorShift;

            float _BandDensity;
            float _BandSharpness;
            float _WarpStrength;
            float _NoiseScale;

            float _EdgeSoftness;
            float _Opacity;
            float _OverlayAlpha;
            float _CoreBrightness;
            float _UseUIAlphaClip;

            static const float TWO_PI = 6.28318530718;

            v2f vert(appdata_t v)
            {
                v2f output;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = v.vertex;
                output.vertex = UnityObjectToClipPos(v.vertex);
                output.texcoord = v.texcoord;
                output.color = v.color * _Color;

                return output;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);

                local = local * local * (3.0 - 2.0 * local);

                float a = hash21(cell);
                float b = hash21(cell + float2(1.0, 0.0));
                float c = hash21(cell + float2(0.0, 1.0));
                float d = hash21(cell + float2(1.0, 1.0));

                return lerp(
                    lerp(a, b, local.x),
                    lerp(c, d, local.x),
                    local.y
                );
            }

            // Darker jewel-toned palette matching the portal artwork.
            float3 portalPalette(float t)
            {
                t = frac(t);

                float3 deepBlue = float3(0.015, 0.035, 0.120);
                float3 violet   = float3(0.180, 0.055, 0.380);
                float3 magenta  = float3(0.620, 0.070, 0.350);
                float3 ember    = float3(0.900, 0.220, 0.075);
                float3 gold     = float3(0.960, 0.620, 0.130);
                float3 green    = float3(0.080, 0.570, 0.380);
                float3 cyan     = float3(0.060, 0.590, 0.690);
                float3 indigo   = float3(0.055, 0.110, 0.360);

                float scaled = t * 8.0;
                float segment = floor(scaled);
                float blend = frac(scaled);

                blend = blend * blend * (3.0 - 2.0 * blend);

                if (segment < 1.0)
                    return lerp(deepBlue, violet, blend);

                if (segment < 2.0)
                    return lerp(violet, magenta, blend);

                if (segment < 3.0)
                    return lerp(magenta, ember, blend);

                if (segment < 4.0)
                    return lerp(ember, gold, blend);

                if (segment < 5.0)
                    return lerp(gold, green, blend);

                if (segment < 6.0)
                    return lerp(green, cyan, blend);

                if (segment < 7.0)
                    return lerp(cyan, indigo, blend);

                return lerp(indigo, deepBlue, blend);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = input.texcoord;

                float progress = saturate(_Progress);

                float easedProgress =
                    progress * progress * (3.0 - 2.0 * progress);

                float2 center = _Center.xy;
                float2 p = uv - center;

                float aspect =
                    _ScreenParams.x /
                    max(_ScreenParams.y, 1.0);

                p.x *= aspect;

                float radius =
                    max(length(p), 0.0001);

                float angle =
                    atan2(p.y, p.x);

                float time = _Time.y;

                float travel =
                    time * (0.15 + easedProgress * _Zoom);

                float inverseRadius =
                    1.0 / (radius + 0.055);

                float swirlOffset =
                    inverseRadius * _SwirlStrength
                    + travel * _Rotation;

                float wrappedAngle =
                    angle + swirlOffset;

                float depth =
                    inverseRadius * _TunnelDepth
                    - travel;

                /*
                    Seam-safe circular coordinates.

                    Raw atan2 jumps from PI to -PI at the left side of
                    the circle. Using sine and cosine here means both
                    sides evaluate to the same value, removing the seam.
                */
                float2 circularA = float2(
                    cos(wrappedAngle),
                    sin(wrappedAngle)
                );

                float2 circularB = float2(
                    cos(wrappedAngle * 2.0),
                    sin(wrappedAngle * 2.0)
                );

                float noiseA = valueNoise(
                    float2(
                        circularA.x * 1.7 +
                        circularB.y * 0.55,

                        depth * 0.12 +
                        circularA.y * 1.25
                    )
                    * _NoiseScale
                    * 0.18
                );

                float noiseB = valueNoise(
                    float2(
                        depth * 0.16 +
                        circularB.x * 0.8,

                        circularA.y * 1.6 +
                        circularB.y * 0.45
                    )
                    * _NoiseScale
                    * 0.12
                );

                float warp =
                    (noiseA - 0.5) * _WarpStrength
                    + (noiseB - 0.5)
                    * _WarpStrength
                    * 0.5;

                /*
                    Do not add angle directly to this coordinate.
                    The circular sine terms preserve the spiral while
                    remaining perfectly continuous around 360 degrees.
                */
                float tunnelCoordinate =
                    depth
                    + sin(wrappedAngle) * 1.55
                    + sin(wrappedAngle * 2.0) * 0.48
                    + warp * 3.0;

                float bandWave =
                    sin(
                        tunnelCoordinate
                        * _BandDensity
                    );

                float secondaryWave =
                    sin(
                        tunnelCoordinate
                        * (_BandDensity * 0.47)
                        - wrappedAngle * 3.0
                    );

                float fineWave =
                    sin(
                        depth
                        * _BandDensity
                        * 1.85
                        + wrappedAngle * 5.0
                    );

                float mainBands =
                    0.5 + 0.5 * bandWave;

                mainBands = pow(
                    saturate(mainBands),
                    max(_BandSharpness, 0.01)
                );

                float secondaryBands =
                    0.5 + 0.5 * secondaryWave;

                float fineBands =
                    0.5 + 0.5 * fineWave;

                float energy =
                    mainBands * 0.55
                    + secondaryBands * 0.28
                    + fineBands * 0.17;

                float streakCoordinate =
                    frac(
                        depth * 0.075
                        + sin(wrappedAngle)
                        * 0.025
                    );

                float streak =
                    1.0
                    - abs(
                        streakCoordinate * 2.0 - 1.0
                    );

                streak = pow(
                    saturate(streak),
                    max(
                        0.15,
                        5.0 /
                        max(_StreakLength, 0.1)
                    )
                );

                energy +=
                    streak
                    * easedProgress
                    * 0.42;

                // Darker base keeps the effect closer to the portal.
                float luminousBase =
                    0.10
                    + noiseA * 0.13
                    + secondaryBands * 0.08;

                energy =
                    max(energy, luminousBase);

                /*
                    Seam-safe hue coordinate.

                    Angular variation is generated through sine waves
                    rather than raw angle values, so the palette cannot
                    split at the atan2 boundary.
                */
                float paletteCoordinate =
                    depth
                    * 0.025
                    * _ColorCycles
                    + sin(wrappedAngle) * 0.12
                    + sin(wrappedAngle * 2.0) * 0.035
                    + travel * 0.035
                    + _ColorShift;

                float3 rainbow =
                    portalPalette(paletteCoordinate);

                float luminance =
                    dot(
                        rainbow,
                        float3(0.2126, 0.7152, 0.0722)
                    );

                rainbow = lerp(
                    luminance.xxx,
                    rainbow,
                    saturate(_Saturation)
                );

                float core =
                    exp(
                        -radius
                        * (7.0 - easedProgress * 3.5)
                    );

                float3 finalColor =
                    rainbow
                    * energy
                    * _Brightness;

                finalColor +=
                    rainbow
                    * core
                    * _CoreBrightness;

                float expansionRadius =
                    lerp(
                        0.015,
                        1.65,
                        easedProgress
                    );

                float reveal =
                    1.0
                    - smoothstep(
                        expansionRadius - _EdgeSoftness,
                        expansionRadius,
                        radius
                    );

                float edgeGlow =
                    1.0
                    - abs(
                        saturate(
                            (
                                radius
                                - expansionRadius
                                + _EdgeSoftness
                            )
                            / max(_EdgeSoftness, 0.001)
                        )
                        * 2.0 - 1.0
                    );

                edgeGlow =
                    pow(
                        saturate(edgeGlow),
                        2.0
                    );

                finalColor +=
                    rainbow
                    * edgeGlow
                    * easedProgress
                    * 0.22;

                float alpha =
                    lerp(
                    reveal * saturate(0.68 + energy * 0.30),
                    reveal,
                    saturate((progress - 0.80) / 0.20)
                    )
                    * _Opacity;

                alpha *=
                    smoothstep(
                        0.0,
                        0.025,
                        progress
                    );

                fixed4 spriteSample =
                    tex2D(_MainTex, uv)
                    + _TextureSampleAdd;

                fixed4 outputColor;

                outputColor.rgb =
                    finalColor
                    * input.color.rgb
                    * spriteSample.rgb;

                outputColor.a =
                    alpha
                    * _OverlayAlpha
                    * input.color.a;

                outputColor.a *=
                    UnityGet2DClipping(
                        input.worldPosition.xy,
                        _ClipRect
                    );

                #ifdef UNITY_UI_ALPHACLIP
                clip(outputColor.a - 0.001);
                #endif

                return outputColor;
            }

            ENDHLSL
        }
    }
}