Shader "Hidden/GrassVectorFieldBrush"
{
    Properties
    {
        _MainTex ("Vector Field", 2D) = "black" {}
        _BrushPosition ("Brush Position", Vector) = (0.5, 0.5, 0, 0)
        _BrushDirection ("Brush Direction", Vector) = (0, 0, 0, 0)
        _BrushSize ("Brush Size", Float) = 0.05
        _BrushStrength ("Brush Strength", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _BrushPosition;
            float4 _BrushDirection;
            float _BrushSize;
            float _BrushStrength;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS = float4(
                    input.positionOS.xy,
                    0.0,
                    1.0
                );

                output.uv = input.uv;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                float4 current = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    uv
                );

                float distanceToBrush = distance(
                    uv,
                    _BrushPosition.xy
                );

                float radius = max(_BrushSize, 0.0001);

                float mask = 1.0 - smoothstep(
                    radius * 0.5,
                    radius,
                    distanceToBrush
                );

                float2 direction = _BrushDirection.xy;

                float2 encodedDirection = direction * 0.5 + 0.5;

                float3 painted = float3(
                    encodedDirection.x,
                    encodedDirection.y,
                    0.0
                );

                float3 result = lerp(
                    current.rgb,
                    painted,
                    mask * _BrushStrength
                );

                return float4(result, 1.0);
            }

            ENDHLSL
        }
    }
}

