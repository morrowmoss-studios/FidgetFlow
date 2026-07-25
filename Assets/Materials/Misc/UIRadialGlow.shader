Shader "UI/RadialGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Tint", Color) = (1, 1, 1, 1)

        _GlowPower ("Glow Power", Range(0.25, 8.0)) = 2.0
        _InnerStrength ("Inner Strength", Range(0.0, 2.0)) = 1.0
        _EdgeFade ("Edge Fade", Range(0.01, 0.5)) = 0.15

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            Name "RadialGlow"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;

            float4 _Color;
            float4 _TextureSampleAdd;
            float4 _ClipRect;

            float _GlowPower;
            float _InnerStrength;
            float _EdgeFade;

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.worldPosition = input.positionOS;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * _Color;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Preserve the sprite's original alpha shape.
                half4 spriteSample =
                    tex2D(_MainTex, input.uv) + _TextureSampleAdd;

                // Convert UVs from 0–1 into coordinates centered on zero.
                float2 centeredUV = input.uv - float2(0.5, 0.5);

                // Distance from the center.
                // Multiplying by 2 makes the outer edge approximately 1.
                float radialDistance = length(centeredUV) * 2.0;

                // Smoothly fade toward transparent near the outside.
                float radialMask = 1.0 - smoothstep(
                    1.0 - _EdgeFade,
                    1.0,
                    radialDistance
                );

                // Shape the brightness falloff.
                radialMask = pow(
                    saturate(1.0 - radialDistance),
                    _GlowPower
                ) * radialMask;

                radialMask *= _InnerStrength;

                half4 finalColor = input.color;

                finalColor.a *=
                    spriteSample.a *
                    saturate(radialMask);

                #ifdef UNITY_UI_CLIP_RECT
                    finalColor.a *= UnityGet2DClipping(
                        input.worldPosition.xy,
                        _ClipRect
                    );
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(finalColor.a - 0.001);
                #endif

                return finalColor;
            }

            ENDHLSL
        }
    }
}