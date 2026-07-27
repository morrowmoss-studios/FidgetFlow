Shader "FidgetFlow/PlasmaCorona"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Brightness ("Brightness", Range(0, 8)) = 2.2
        _Softness ("Edge Softness", Range(0.1, 4)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+25"
        }

        Blend SrcAlpha One
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "PlasmaCorona"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Brightness;
                float _Softness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.color =
                    input.color *
                    _BaseColor;

                output.uv =
                    input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float distanceFromCenter =
                    abs(input.uv.y - 0.5) * 2.0;

                float core =
                    pow(
                        saturate(
                            1.0 -
                            distanceFromCenter
                        ),
                        _Softness
                    );

                half4 color =
                    input.color;

                color.rgb *=
                    core *
                    _Brightness;

                color.a *=
                    core;

                return color;
            }

            ENDHLSL
        }
    }

    FallBack Off
}