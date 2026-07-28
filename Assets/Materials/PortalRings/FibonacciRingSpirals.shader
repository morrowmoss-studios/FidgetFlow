Shader "FidgetFlow/FibonacciRingSpirals"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Brightness ("Brightness", Range(0, 8)) = 2.0
        _CoreStrength ("Core Strength", Range(0, 3)) = 1.0
        _EdgeSoftness ("Edge Softness", Range(0.1, 5)) = 1.35
        _FlowSpeed ("Flow Speed", Range(0, 6)) = 0.9
        _FlowStrength ("Flow Strength", Range(0, 1)) = 0.22
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+35"
        }

        Blend SrcAlpha One
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "FibonacciRingSpirals"

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
                float _CoreStrength;
                float _EdgeSoftness;
                float _FlowSpeed;
                float _FlowStrength;

            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );

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
                    abs(
                        input.uv.y -
                        0.5
                    ) *
                    2.0;

                float core =
                    pow(
                        saturate(
                            1.0 -
                            distanceFromCenter
                        ),
                        _EdgeSoftness
                    );

                float movingFlow =
                    0.88 +
                    sin(
                        input.uv.x *
                        18.0 -
                        _Time.y *
                        _FlowSpeed *
                        6.0
                    ) *
                    _FlowStrength;

                float3 color =
                    input.color.rgb *
                    (
                        core *
                        _Brightness +
                        core *
                        core *
                        _CoreStrength
                    ) *
                    movingFlow;

                float alpha =
                    input.color.a *
                    core;

                return half4(
                    color,
                    alpha
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}
