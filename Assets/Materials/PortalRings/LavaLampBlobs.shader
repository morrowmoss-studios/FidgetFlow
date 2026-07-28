Shader "FidgetFlow/LavaLampBlobs"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [HDR] _CellColor ("Cell Color", Color) = (0.1, 0.8, 1.0, 1.0)
        _Opacity ("Opacity", Range(0, 1)) = 1
        _Glow ("Glow", Range(0, 8)) = 2.2
        _FresnelPower ("Fresnel Power", Range(0.25, 8)) = 2.0
        _CoreStrength ("Core Strength", Range(0, 2)) = 0.45
        _Wobble ("Shape Wobble", Range(0, 0.8)) = 0.25
        _WobbleSpeed ("Wobble Speed", Range(0, 8)) = 1.6
        _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+30"
        }

        Blend SrcAlpha One
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "PlasmaCells"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _CellColor;
                float _Opacity;
                float _Glow;
                float _FresnelPower;
                float _CoreStrength;
                float _Wobble;
                float _WobbleSpeed;
                float _Seed;
            CBUFFER_END

            float ShapeNoise(float3 position, float seed, float timeValue)
            {
                float a = sin(position.x * 11.7 + position.y * 7.3 + seed + timeValue);
                float b = sin(position.y * 13.1 + position.z * 9.2 + seed * 1.37 - timeValue * 1.23);
                float c = sin(position.z * 10.4 + position.x * 8.6 + seed * 2.11 + timeValue * 0.74);
                return (a + b + c) / 3.0;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float timeValue = _Time.y * _WobbleSpeed;
                float shapeNoise = ShapeNoise(input.positionOS.xyz, _Seed, timeValue);
                float3 displacedPosition = input.positionOS.xyz + input.normalOS * shapeNoise * _Wobble * 0.08;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(displacedPosition);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalize(normalInputs.normalWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDirection = normalize(GetWorldSpaceViewDir(input.positionWS));
                float facing = saturate(dot(normalize(input.normalWS), viewDirection));
                float fresnel = pow(saturate(1.0 - facing), _FresnelPower);
                float core = pow(facing, 1.6);
                float pulse = 0.88 + sin(_Time.y * _WobbleSpeed * 1.7 + _Seed) * 0.12;

                float3 color = _BaseColor.rgb * _CellColor.rgb;
                color *= (fresnel * _Glow + core * _CoreStrength) * pulse;

                float alpha = saturate((fresnel * 0.85 + core * 0.55) * _Opacity * _CellColor.a * _BaseColor.a);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
