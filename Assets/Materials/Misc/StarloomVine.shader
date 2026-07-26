Shader "FidgetFlow/StarloomVine"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Glow ("Glow", Range(1,8)) = 2.0
        _Softness ("Softness", Range(0.05,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "StarloomVine"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

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
                float _Glow;
                float _Softness;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color * _BaseColor;
                OUT.uv = IN.uv;

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float d = abs(IN.uv.y - 0.5) * 2.0;
                float edge = pow(saturate(1.0 - d), _Softness * 4.0);

                half4 c = IN.color;
                c.rgb *= edge * _Glow;
                c.a *= edge;

                return c;
            }

            ENDHLSL
        }
    }

    FallBack Off
}