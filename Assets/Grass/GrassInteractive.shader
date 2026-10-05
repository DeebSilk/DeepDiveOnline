Shader "DeebSilk/Grass Interactive"
{
    Properties
    {
        _BaseMap ("Grass Texture", 2D) = "white" {}
        _BaseColor ("Grass Color", Color) = (1,1,1,1)

        _VectorField ("Vector Field", 2D) = "black" {}

        _FieldCenter ("Vector Field Center", Vector) = (0,0,0,0)
        _FieldWorldSize ("Vector Field World Size", Float) = 50

        _BendStrength ("Bend Strength", Range(0,5)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "GrassForward"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_VectorField);
            SAMPLER(sampler_VectorField);

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseColor;

                float4 _FieldCenter;

                float _FieldWorldSize;
                float _BendStrength;

            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionOS = input.positionOS;

                /*
                 * Для нашего Quad:
                 *
                 * Y = -0.5  → основание
                 * Y = +0.5  → верх
                 *
                 * Поэтому переводим Y в диапазон 0..1.
                 */
                float height01 = saturate(positionOS.y + 0.5);

                /*
                 * Получаем мировую позицию основания
                 * травинки.
                 */
                float3 worldPosition =
                    TransformObjectToWorld(positionOS);

                /*
                 * Переводим мировую XZ позицию
                 * в координаты Vector Field.
                 */
                float2 fieldUV;

                fieldUV.x =
                    (worldPosition.x - _FieldCenter.x)
                    / _FieldWorldSize
                    + 0.5;

                fieldUV.y =
                    (worldPosition.z - _FieldCenter.z)
                    / _FieldWorldSize
                    + 0.5;

                /*
                 * Проверяем, находится ли травинка
                 * внутри Vector Field.
                 */
                float insideField =
                    step(0.0, fieldUV.x) *
                    step(fieldUV.x, 1.0) *
                    step(0.0, fieldUV.y) *
                    step(fieldUV.y, 1.0);

                /*
                 * Читаем направление.
                 *
                 * В RT:
                 * 0..1
                 *
                 * Преобразуем обратно:
                 * -1..1
                 */
                float3 encoded =
                    SAMPLE_TEXTURE2D_LOD(
                        _VectorField,
                        sampler_VectorField,
                        fieldUV,
                        0
                    ).rgb;

                float2 direction =
                    encoded.xy * 2.0 - 1.0;

                /*
                 * Если Vector Field пустой:
                 *
                 * (0.5, 0.5)
                 *
                 * превращается в:
                 *
                 * (0,0)
                 */
                float directionLength =
                    length(direction);

                direction =
                    direction /
                    max(directionLength, 0.0001);

                /*
                 * Сила изгиба.
                 *
                 * У основания:
                 * 0
                 *
                 * У верхушки:
                 * 1
                 */
                float bend =
                    height01 *
                    height01 *
                    _BendStrength;

                /*
                 * Направление движения превращаем
                 * в смещение верхушки травинки.
                 */
                float3 bendOffset =
                    float3(
                        direction.x,
                        0.0,
                        direction.y
                    );

                bendOffset *= bend;

                bendOffset *= insideField;

                /*
                 * Наклоняем травинку.
                 *
                 * Основание практически не двигается,
                 * верх двигается сильнее.
                 */
                worldPosition += bendOffset;

                output.positionCS =
                    TransformWorldToHClip(
                        worldPosition
                    );

                output.uv = input.uv;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 textureColor =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );

                return textureColor * _BaseColor;
            }

            ENDHLSL
        }
    }
}