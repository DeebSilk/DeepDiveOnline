Shader "SAO/Water Planar"
{
    Properties
    {
        [Header(Water Colors)]

        _ShallowColor
        (
            "Shallow Color",
            Color
        ) = (0.08, 0.42, 0.48, 1)

        _WaterColor
        (
            "Water Color",
            Color
        ) = (0.015, 0.18, 0.28, 1)

        _DeepColor
        (
            "Deep Color",
            Color
        ) = (0.005, 0.035, 0.10, 1)

        [Header(Color Gradient)]

        _ShallowDepth
        (
            "Shallow Depth",
            Range(0.1, 5)
        ) = 1.5

        _DeepDepth
        (
            "Deep Depth",
            Range(1, 30)
        ) = 8

        _DepthColorStrength
        (
            "Depth Color Strength",
            Range(0, 2)
        ) = 1

        [Header(Reflection)]

        _ReflectionStrength
        (
            "Reflection Strength",
            Range(0, 1)
        ) = 1

        _ReflectionBase
        (
            "Reflection Base",
            Range(0, 1)
        ) = 0.15

        _FresnelPower
        (
            "Fresnel Power",
            Range(0.5, 8)
        ) = 3

        _ReflectionDistortion
        (
            "Reflection Distortion",
            Range(0, 0.05)
        ) = 0.005

        [Header(Large Waves)]

        _LargeWaveScale
        (
            "Large Wave Scale",
            Float
        ) = 0.55

        _LargeWaveSpeed
        (
            "Large Wave Speed",
            Float
        ) = 0.20

        _LargeWaveStrength
        (
            "Large Wave Strength",
            Range(0, 1)
        ) = 0.4

        [Header(Small Waves)]

        _SmallWaveScale
        (
            "Small Wave Scale",
            Float
        ) = 3.5

        _SmallWaveSpeed
        (
            "Small Wave Speed",
            Float
        ) = 0.65

        _SmallWaveStrength
        (
            "Small Wave Strength",
            Range(0, 1)
        ) = 0.2

        [Header(Surface)]

        _NormalStrength
        (
            "Normal Strength",
            Range(0, 3)
        ) = 1.2

        _Smoothness
        (
            "Smoothness",
            Range(0, 1)
        ) = 0.95

        _Opacity
        (
            "Opacity",
            Range(0.1, 1)
        ) = 0.8

        [Header(Sun)]

        _SunSpecularPower
        (
            "Sun Specular Power",
            Range(16, 512)
        ) = 160

        _SunSpecularStrength
        (
            "Sun Specular Strength",
            Range(0, 3)
        ) = 0.8

        [Header(Footstep Waves)]

        _FootstepWaveNormalStrength
        (
            "Footstep Wave Normal Strength",
            Range(0, 5)
        ) = 2.0

        _FootstepWaveSurfaceStrength
        (
            "Footstep Wave Surface Strength",
            Range(0, 0.2)
        ) = 0.035

        _FootstepWaveLifetime
        (
            "Footstep Wave Lifetime",
            Range(0.2, 5)
        ) = 1.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        LOD 500

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Back

        Pass
        {
            Tags
            {
                "LightMode"="ForwardBase"
            }

            CGPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            // =========================================================
            // STRUCTURES
            // =========================================================

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;

                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;

                float4 reflectionPos : TEXCOORD3;

                float eyeDepth : TEXCOORD4;
                float4 screenPos : TEXCOORD5;
            };

            // =========================================================
            // COLORS
            // =========================================================

            fixed4 _ShallowColor;
            fixed4 _WaterColor;
            fixed4 _DeepColor;

            float _ShallowDepth;
            float _DeepDepth;
            float _DepthColorStrength;

            // =========================================================
            // DEPTH
            // =========================================================

            sampler2D _CameraDepthTexture;

            // =========================================================
            // REFLECTION
            // =========================================================

            sampler2D _WaterReflectionTex;
            float4x4 _WaterReflectionVP;

            float _ReflectionStrength;
            float _ReflectionBase;
            float _FresnelPower;
            float _ReflectionDistortion;

            // =========================================================
            // LARGE WAVES
            // =========================================================

            float _LargeWaveScale;
            float _LargeWaveSpeed;
            float _LargeWaveStrength;

            // =========================================================
            // SMALL WAVES
            // =========================================================

            float _SmallWaveScale;
            float _SmallWaveSpeed;
            float _SmallWaveStrength;

            // =========================================================
            // SURFACE
            // =========================================================

            float _NormalStrength;
            float _Smoothness;
            float _Opacity;

            // =========================================================
            // SUN
            // =========================================================

            float _SunSpecularPower;
            float _SunSpecularStrength;

            // =========================================================
            // FOOTSTEP WAVES
            // =========================================================

            // xyz = world position
            // w   = unused

            float4 _FootstepWavePositions[16];

            // x = age
            // y = speed
            // z = strength
            // w = width

            float4 _FootstepWaveData[16];

            int _FootstepWaveCount;

            float _FootstepWaveNormalStrength;
            float _FootstepWaveSurfaceStrength;
            float _FootstepWaveLifetime;

            // =========================================================
            // LARGE WAVE
            // =========================================================

            float GetLargeWave(float3 worldPos)
            {
                float t =
                    _Time.y *
                    _LargeWaveSpeed;

                float a =
                    sin(
                        worldPos.x *
                        _LargeWaveScale +
                        t
                    );

                float b =
                    sin(
                        worldPos.z *
                        (_LargeWaveScale * 0.78) +
                        t * 1.17
                    );

                float c =
                    sin(
                        (worldPos.x +
                         worldPos.z) *
                        (_LargeWaveScale * 0.43) +
                        t * 0.73
                    );

                float d =
                    sin(
                        (worldPos.x -
                         worldPos.z) *
                        (_LargeWaveScale * 0.31) +
                        t * 1.42
                    );

                return
                    (a + b + c + d) *
                    0.25;
            }

            // =========================================================
            // SMALL WAVE
            // =========================================================

            float GetSmallWave(float3 worldPos)
            {
                float t =
                    _Time.y *
                    _SmallWaveSpeed;

                float a =
                    sin(
                        worldPos.x *
                        _SmallWaveScale +
                        t * 1.7
                    );

                float b =
                    sin(
                        worldPos.z *
                        (_SmallWaveScale * 1.23) -
                        t * 1.15
                    );

                float c =
                    sin(
                        (worldPos.x -
                         worldPos.z) *
                        (_SmallWaveScale * 0.82) +
                        t * 2.1
                    );

                float d =
                    cos(
                        (worldPos.x +
                         worldPos.z) *
                        (_SmallWaveScale * 0.61) -
                        t * 1.45
                    );

                return
                    (a + b + c + d) *
                    0.25;
            }

            // =========================================================
            // WATER HEIGHT
            // =========================================================

            float GetBaseWaterHeight(float3 worldPos)
            {
                float large =
                    GetLargeWave(worldPos) *
                    _LargeWaveStrength;

                float small =
                    GetSmallWave(worldPos) *
                    _SmallWaveStrength;

                return large + small;
            }

            // =========================================================
            // SINGLE FOOTSTEP WAVE
            // =========================================================

            float GetFootstepWave(
                float3 worldPos,
                float3 wavePosition,
                float age,
                float speedValue,
                float strengthValue,
                float widthValue
            )
            {
                // Центр кольца.
                float2 delta =
                    worldPos.xz -
                    wavePosition.xz;

                float distanceFromCenter =
                    length(delta);

                // Радиус растущей волны.
                float radius =
                    age *
                    speedValue;

                // Расстояние от текущего кольца.
                float ringDistance =
                    abs(
                        distanceFromCenter -
                        radius
                    );

                float safeWidth =
                    max(
                        widthValue,
                        0.01
                    );

                // Толщина кольца.
                float ring =
                    exp(
                        -(
                            ringDistance *
                            ringDistance
                        ) /
                        (
                            safeWidth *
                            safeWidth
                        )
                    );

                // С течением времени волна исчезает.
                float ageFade =
                    saturate(
                        1.0 -
                        age /
                        max(
                            _FootstepWaveLifetime,
                            0.01
                        )
                    );

                // Плавное затухание.
                ageFade =
                    ageFade *
                    ageFade;

                return
                    ring *
                    strengthValue *
                    ageFade;
            }

            // =========================================================
            // ALL FOOTSTEP WAVES
            // =========================================================

            float GetAllFootstepWaves(
                float3 worldPos
            )
            {
                float result = 0.0;

                for (int i = 0; i < 16; i++)
                {
                    if (i >= _FootstepWaveCount)
                    {
                        break;
                    }

                    float4 waveData =
                        _FootstepWaveData[i];

                    result +=
                        GetFootstepWave(
                            worldPos,
                            _FootstepWavePositions[i].xyz,
                            waveData.x,
                            waveData.y,
                            waveData.z,
                            waveData.w
                        );
                }

                return result;
            }

            // =========================================================
            // NORMAL FROM BASE WAVES
            // =========================================================

            float3 GetBaseWaterNormal(
                float3 worldPos
            )
            {
                float e = 0.035;

                float center =
                    GetBaseWaterHeight(
                        worldPos
                    );

                float sampleX =
                    GetBaseWaterHeight(
                        worldPos +
                        float3(e, 0, 0)
                    );

                float sampleZ =
                    GetBaseWaterHeight(
                        worldPos +
                        float3(0, 0, e)
                    );

                float dx =
                    (sampleX - center) /
                    e;

                float dz =
                    (sampleZ - center) /
                    e;

                return normalize(
                    float3(
                        -dx *
                        _NormalStrength,

                        1.0,

                        -dz *
                        _NormalStrength
                    )
                );
            }

            // =========================================================
            // FINAL WATER NORMAL
            // =========================================================

            float3 GetWaterNormal(
                float3 worldPos
            )
            {
                float3 normal =
                    GetBaseWaterNormal(
                        worldPos
                    );

                float epsilon =
                    0.035;

                float rippleCenter =
                    GetAllFootstepWaves(
                        worldPos
                    );

                float rippleX =
                    GetAllFootstepWaves(
                        worldPos +
                        float3(
                            epsilon,
                            0,
                            0
                        )
                    );

                float rippleZ =
                    GetAllFootstepWaves(
                        worldPos +
                        float3(
                            0,
                            0,
                            epsilon
                        )
                    );

                float rippleDX =
                    (rippleX -
                     rippleCenter) /
                    epsilon;

                float rippleDZ =
                    (rippleZ -
                     rippleCenter) /
                    epsilon;

                // Волны от ног.
                normal.x +=
                    rippleDX *
                    _FootstepWaveNormalStrength;

                normal.z +=
                    rippleDZ *
                    _FootstepWaveNormalStrength;

                return normalize(
                    normal
                );
            }

            // =========================================================
            // VERTEX
            // =========================================================

            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos =
                    mul(
                        unity_ObjectToWorld,
                        v.vertex
                    ).xyz;

                // -----------------------------------------------------
                // Обычные волны
                // -----------------------------------------------------

                float baseWave =
                    GetBaseWaterHeight(
                        worldPos
                    );

                worldPos.y +=
                    baseWave *
                    0.035;

                // -----------------------------------------------------
                // Волны от ног
                // -----------------------------------------------------

                float footstepWave =
                    GetAllFootstepWaves(
                        worldPos
                    );

                worldPos.y +=
                    footstepWave *
                    _FootstepWaveSurfaceStrength;

                // -----------------------------------------------------
                // Position
                // -----------------------------------------------------

                o.vertex =
                    UnityWorldToClipPos(
                        worldPos
                    );

                o.worldPos =
                    worldPos;

                o.worldNormal =
                    UnityObjectToWorldNormal(
                        v.normal
                    );

                o.viewDir =
                    _WorldSpaceCameraPos.xyz -
                    worldPos;

                // -----------------------------------------------------
                // Planar Reflection
                // -----------------------------------------------------

                o.reflectionPos =
                    mul(
                        _WaterReflectionVP,
                        float4(
                            worldPos,
                            1.0
                        )
                    );

                // -----------------------------------------------------
                // Screen coordinates
                // -----------------------------------------------------

                o.screenPos =
                    ComputeScreenPos(
                        o.vertex
                    );

                float3 viewSpace =
                    mul(
                        UNITY_MATRIX_V,
                        float4(
                            worldPos,
                            1.0
                        )
                    ).xyz;

                o.eyeDepth =
                    -viewSpace.z;

                return o;
            }

            // =========================================================
            // FRAGMENT
            // =========================================================

            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewDir =
                    normalize(
                        i.viewDir
                    );

                // -----------------------------------------------------
                // Water normal
                // -----------------------------------------------------

                float3 normal =
                    GetWaterNormal(
                        i.worldPos
                    );

                // -----------------------------------------------------
                // Fresnel
                // -----------------------------------------------------

                float fresnel =
                    pow(
                        1.0 -
                        saturate(
                            dot(
                                normal,
                                viewDir
                            )
                        ),
                        _FresnelPower
                    );

                // =====================================================
                // DEPTH
                // =====================================================

                float sceneDepth =
                    LinearEyeDepth(
                        SAMPLE_DEPTH_TEXTURE_PROJ(
                            _CameraDepthTexture,
                            UNITY_PROJ_COORD(
                                i.screenPos
                            )
                        )
                    );

                float waterDepth =
                    max(
                        sceneDepth -
                        i.eyeDepth,
                        0.0
                    );

                float shallowFactor =
                    saturate(
                        waterDepth /
                        max(
                            _ShallowDepth,
                            0.001
                        )
                    );

                float deepFactor =
                    saturate(
                        (
                            waterDepth -
                            _ShallowDepth
                        )
                        /
                        max(
                            _DeepDepth -
                            _ShallowDepth,
                            0.001
                        )
                    );

                deepFactor =
                    smoothstep(
                        0.0,
                        1.0,
                        deepFactor
                    );

                // =====================================================
                // WATER COLOR
                // =====================================================

                float3 shallowWater =
                    lerp(
                        _ShallowColor.rgb,
                        _WaterColor.rgb,
                        shallowFactor
                    );

                float3 waterColor =
                    lerp(
                        shallowWater,
                        _DeepColor.rgb,
                        deepFactor
                    );

                waterColor *=
                    _DepthColorStrength;

                // =====================================================
                // PLANAR REFLECTION
                // =====================================================

                float reflectionW =
                    max(
                        i.reflectionPos.w,
                        0.0001
                    );

                float2 reflectionUV =
                    i.reflectionPos.xy /
                    reflectionW;

                reflectionUV =
                    reflectionUV *
                    0.5 +
                    0.5;

                #if UNITY_UV_STARTS_AT_TOP

                    reflectionUV.y =
                        1.0 -
                        reflectionUV.y;

                #endif

                // Волны немного искажают отражение.
                reflectionUV +=
                    normal.xz *
                    _ReflectionDistortion;

                reflectionUV =
                    clamp(
                        reflectionUV,
                        0.001,
                        0.999
                    );

                float3 reflection =
                    tex2D(
                        _WaterReflectionTex,
                        reflectionUV
                    ).rgb;

                // =====================================================
                // REFLECTION MIX
                // =====================================================

                float reflectionAmount =
                    _ReflectionBase +
                    fresnel *
                    (
                        1.0 -
                        _ReflectionBase
                    );

                reflectionAmount *=
                    _ReflectionStrength;

                // На глубине чуть сильнее.
                reflectionAmount =
                    lerp(
                        reflectionAmount,
                        reflectionAmount * 1.12,
                        deepFactor
                    );

                reflectionAmount =
                    saturate(
                        reflectionAmount
                    );

                float3 finalColor =
                    lerp(
                        waterColor,
                        reflection,
                        reflectionAmount
                    );

                // =====================================================
                // SUN
                // =====================================================

                float3 lightDirection;

                if (_WorldSpaceLightPos0.w == 0.0)
                {
                    lightDirection =
                        normalize(
                            _WorldSpaceLightPos0.xyz
                        );
                }
                else
                {
                    lightDirection =
                        normalize(
                            _WorldSpaceLightPos0.xyz -
                            i.worldPos
                        );
                }

                float3 halfDirection =
                    normalize(
                        viewDir +
                        lightDirection
                    );

                float sunSpecular =
                    pow(
                        saturate(
                            dot(
                                normal,
                                halfDirection
                            )
                        ),
                        _SunSpecularPower
                    );

                finalColor +=
                    sunSpecular *
                    _LightColor0.rgb *
                    _SunSpecularStrength *
                    _Smoothness;

                // =====================================================
                // FINAL
                // =====================================================

                return fixed4(
                    saturate(
                        finalColor
                    ),
                    saturate(
                        _Opacity
                    )
                );
            }

            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}