Shader "DeebSilk/Grass Wind Natural"
{
    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {}
        _Color ("Grass Color", Color) = (1,1,1,1)

        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.35

        [Header(Natural Wind)]
        _WindStrength ("Wind Strength", Range(0,1)) = 0.18
        _WindSpeed ("Wind Speed", Range(0,5)) = 1.0
        _WindScale ("Wind Scale", Range(0.1,5)) = 1.0

        [Header(Bending)]
        _BendStrength ("Bend Strength", Range(0,1)) = 0.30
        _Turbulence ("Turbulence", Range(0,1)) = 0.15

        [Header(Gusts)]
        _GustStrength ("Gust Strength", Range(0,1)) = 0.10
        _GustSpeed ("Gust Speed", Range(0,3)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "Queue" = "AlphaTest"
            "RenderType" = "TransparentCutout"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite On

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            float _Cutoff;

            float _WindStrength;
            float _WindSpeed;
            float _WindScale;

            float _BendStrength;
            float _Turbulence;

            float _GustStrength;
            float _GustSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);

                float3 localPos = v.vertex.xyz;

                float3 worldPos =
                    mul(
                        unity_ObjectToWorld,
                        v.vertex
                    ).xyz;

                float height =
                    saturate(v.uv.y);

                float bendMask =
                    height * height;

                bendMask =
                    smoothstep(
                        0.0,
                        1.0,
                        bendMask
                    );

                float2 windPos =
                    worldPos.xz *
                    _WindScale;

                float time =
                    _Time.y *
                    _WindSpeed;

                float waveLarge =
                    sin(
                        windPos.x * 0.45 +
                        windPos.y * 0.35 +
                        time
                    );

                float waveMedium =
                    sin(
                        windPos.x * 0.9 -
                        windPos.y * 0.55 +
                        time * 0.73
                    );

                float waveSmall =
                    sin(
                        windPos.x * 2.0 +
                        windPos.y * 1.4 +
                        time * 1.35
                    );

                float wind =
                    waveLarge * 0.60 +
                    waveMedium * 0.30 +
                    waveSmall *
                    _Turbulence *
                    0.10;

                float gust =
                    sin(
                        windPos.x * 0.18 +
                        windPos.y * 0.14 +
                        _Time.y *
                        _GustSpeed
                    );

                gust =
                    gust * 0.5 +
                    0.5;

                gust *=
                    _GustStrength;

                float windX =
                    wind;

                float windZ =
                    sin(
                        windPos.x * 0.55 +
                        windPos.y * 0.40 +
                        time * 0.82
                    );

                float finalStrength =
                    _WindStrength *
                    _BendStrength;

                finalStrength +=
                    gust;

                float3 bend =
                    float3(
                        0.0,
                        0.0,
                        0.0
                    );

                bend.x =
                    windX *
                    finalStrength *
                    bendMask;

                bend.z =
                    windZ *
                    finalStrength *
                    0.55 *
                    bendMask;

                localPos += bend;

                localPos.y +=
                    abs(wind) *
                    0.01 *
                    bendMask;

                v.vertex.xyz =
                    localPos;

                o.vertex =
                    UnityObjectToClipPos(
                        v.vertex
                    );

                o.uv =
                    TRANSFORM_TEX(
                        v.uv,
                        _MainTex
                    );

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex =
                    tex2D(
                        _MainTex,
                        i.uv
                    );

                clip(
                    tex.a -
                    _Cutoff
                );

                return tex * _Color;
            }

            ENDCG
        }
    }

    FallBack Off
}