Shader "Water/Planar URP"
{
    Properties
    {
        [Header(Water Colors)]
        _ShallowColor ("Shallow Color", Color) = (0.25, 0.75, 0.85, 1)
        _WaterColor ("Water Color", Color) = (0.05, 0.35, 0.55, 1)
        _DeepColor ("Deep Color", Color) = (0.01, 0.08, 0.18, 1)

        [Header(Depth)]
        _ShallowDepth ("Shallow Depth", Float) = 0.0
        _DeepDepth ("Deep Depth", Float) = 10.0
        _DepthColorStrength ("Depth Color Strength", Range(0, 2)) = 1.0

        [Header(Reflection)]
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.7
        _ReflectionBase ("Reflection Base", Range(0, 1)) = 0.1
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 5.0
        _ReflectionDistortion ("Reflection Distortion", Range(0, 1)) = 0.08

        [Header(Large Waves)]
        _LargeWaveScale ("Large Wave Scale", Float) = 0.08
        _LargeWaveSpeed ("Large Wave Speed", Float) = 0.25
        _LargeWaveStrength ("Large Wave Strength", Float) = 0.12

        [Header(Small Waves)]
        _SmallWaveScale ("Small Wave Scale", Float) = 0.35
        _SmallWaveSpeed ("Small Wave Speed", Float) = 0.7
        _SmallWaveStrength ("Small Wave Strength", Float) = 0.04

        [Header(Normals)]
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.9

        [Header(Transparency)]
        _Opacity ("Opacity", Range(0, 1)) = 0.8

        [Header(Sun Specular)]
        _SunSpecularPower ("Sun Specular Power", Range(1, 128)) = 64
        _SunSpecularStrength ("Sun Specular Strength", Range(0, 2)) = 0.5

        [Header(Footstep Waves)]
        _FootstepWaveNormalStrength ("Footstep Wave Normal Strength", Range(0, 2)) = 0.5
        _FootstepWaveSurfaceStrength ("Footstep Wave Surface Strength", Float) = 0.03
        _FootstepWaveLifetime ("Footstep Wave Lifetime", Float) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "WaterForward"

            Tags
            {
                "LightMode"="UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"


            // ============================================================
            // MATERIAL PROPERTIES
            // ============================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _ShallowColor;
                float4 _WaterColor;
                float4 _DeepColor;

                float _ShallowDepth;
                float _DeepDepth;
                float _DepthColorStrength;

                float _ReflectionStrength;
                float _ReflectionBase;
                float _FresnelPower;
                float _ReflectionDistortion;

                float _LargeWaveScale;
                float _LargeWaveSpeed;
                float _LargeWaveStrength;

                float _SmallWaveScale;
                float _SmallWaveSpeed;
                float _SmallWaveStrength;

                float _NormalStrength;
                float _Smoothness;

                float _Opacity;

                float _SunSpecularPower;
                float _SunSpecularStrength;

                float _FootstepWaveNormalStrength;
                float _FootstepWaveSurfaceStrength;
                float _FootstepWaveLifetime;

            CBUFFER_END


            // ============================================================
            // PLANAR REFLECTION
            // ============================================================

            TEXTURE2D(_WaterReflectionTex);
            SAMPLER(sampler_WaterReflectionTex);

            float4x4 _WaterReflectionVP;


            // ============================================================
            // FOOTSTEP WAVES
            // ============================================================

            float4 _FootstepWavePositions[16];
            float4 _FootstepWaveData[16];
            float _FootstepWaveCount;


            // ============================================================
            // STRUCTURES
            // ============================================================

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float4 screenPos  : TEXCOORD3;
            };


            // ============================================================
            // LARGE WAVE
            // ============================================================

            float LargeWave(float2 position)
            {
                float time =
                    _Time.y *
                    _LargeWaveSpeed;


                float wave1 =
                    sin(
                        position.x *
                        _LargeWaveScale +
                        time
                    );


                float wave2 =
                    cos(
                        position.y *
                        _LargeWaveScale *
                        1.37 +
                        time *
                        0.83
                    );


                float wave3 =
                    sin(
                        (position.x +
                         position.y) *
                        _LargeWaveScale *
                        0.65 +
                        time *
                        1.21
                    );


                return
                    (
                        wave1 * 0.5 +
                        wave2 * 0.3 +
                        wave3 * 0.2
                    )
                    *
                    _LargeWaveStrength;
            }


            // ============================================================
            // SMALL WAVE
            // ============================================================

            float SmallWave(float2 position)
            {
                float time =
                    _Time.y *
                    _SmallWaveSpeed;


                float wave1 =
                    sin(
                        position.x *
                        _SmallWaveScale *
                        7.0 +
                        time *
                        1.3
                    );


                float wave2 =
                    cos(
                        position.y *
                        _SmallWaveScale *
                        9.0 -
                        time
                    );


                float wave3 =
                    sin(
                        (position.x -
                         position.y) *
                        _SmallWaveScale *
                        5.0 +
                        time *
                        0.7
                    );


                return
                    (
                        wave1 * 0.4 +
                        wave2 * 0.35 +
                        wave3 * 0.25
                    )
                    *
                    _SmallWaveStrength;
            }


            // ============================================================
            // FOOTSTEP WAVE
            // ============================================================

            float FootstepSurfaceWave(
                float2 worldXZ,
                out float3 normalOffset
            )
            {
                float surface = 0.0;

                normalOffset =
                    float3(
                        0.0,
                        0.0,
                        0.0
                    );


                int count =
                    min(
                        (int)_FootstepWaveCount,
                        16
                    );


                for (int i = 0; i < 16; i++)
                {
                    if (i >= count)
                        break;


                    float2 center =
                        _FootstepWavePositions[i].xz;


                    // IMPORTANT:
                    // The variable is NOT called "distance".
                    // This avoids collision with HLSL distance().
                    float waveDistanceFromCenter =
                        distance(
                            worldXZ,
                            center
                        );


                    float startTime =
                        _FootstepWaveData[i].x;


                    float amplitude =
                        _FootstepWaveData[i].y;


                    float radius =
                        _FootstepWaveData[i].z;


                    float age =
                        _Time.y -
                        startTime;


                    if (age < 0.0)
                        continue;


                    float life =
                        saturate(
                            1.0 -
                            age /
                            max(
                                _FootstepWaveLifetime,
                                0.001
                            )
                        );


                    float waveDistance =
                        waveDistanceFromCenter -
                        radius * age;


                    float wave =
                        sin(
                            waveDistance *
                            8.0
                        );


                    float ring =
                        exp(
                            -abs(
                                waveDistance
                            ) *
                            2.5
                        );


                    float value =
                        wave *
                        ring *
                        life *
                        amplitude;


                    surface +=
                        value;


                    float2 dir =
                        worldXZ -
                        center;


                    float dirLength =
                        max(
                            length(dir),
                            0.001
                        );


                    dir /=
                        dirLength;


                    normalOffset.x +=
                        dir.x *
                        value *
                        _FootstepWaveNormalStrength;


                    normalOffset.z +=
                        dir.y *
                        value *
                        _FootstepWaveNormalStrength;
                }


                return
                    surface *
                    _FootstepWaveSurfaceStrength;
            }


            // ============================================================
            // VERTEX
            // ============================================================

            Varyings Vert(
                Attributes IN
            )
            {
                Varyings OUT;


                float3 positionWS =
                    TransformObjectToWorld(
                        IN.positionOS
                    );


                float2 worldXZ =
                    positionWS.xz;


                // --------------------------------------------------------
                // LARGE WAVE
                // --------------------------------------------------------

                float largeWave =
                    LargeWave(
                        worldXZ
                    );


                // --------------------------------------------------------
                // SMALL WAVE
                // --------------------------------------------------------

                float smallWave =
                    SmallWave(
                        worldXZ
                    );


                // --------------------------------------------------------
                // FOOTSTEP WAVE
                // --------------------------------------------------------

                float3 footstepNormalOffset;


                float footstepWave =
                    FootstepSurfaceWave(
                        worldXZ,
                        footstepNormalOffset
                    );


                // --------------------------------------------------------
                // FINAL HEIGHT
                // --------------------------------------------------------

                positionWS.y +=
                    largeWave +
                    smallWave +
                    footstepWave;


                // --------------------------------------------------------
                // WAVE NORMAL
                // --------------------------------------------------------

                float sampleDistance =
                    0.05;


                float heightCenter =
                    LargeWave(worldXZ) +
                    SmallWave(worldXZ);


                float heightX =
                    LargeWave(
                        worldXZ +
                        float2(
                            sampleDistance,
                            0
                        )
                    )
                    +
                    SmallWave(
                        worldXZ +
                        float2(
                            sampleDistance,
                            0
                        )
                    );


                float heightZ =
                    LargeWave(
                        worldXZ +
                        float2(
                            0,
                            sampleDistance
                        )
                    )
                    +
                    SmallWave(
                        worldXZ +
                        float2(
                            0,
                            sampleDistance
                        )
                    );


                float slopeX =
                    (
                        heightX -
                        heightCenter
                    )
                    /
                    sampleDistance;


                float slopeZ =
                    (
                        heightZ -
                        heightCenter
                    )
                    /
                    sampleDistance;


                float3 waveNormal =
                    normalize(
                        float3(
                            -slopeX *
                            _NormalStrength,

                            1.0,

                            -slopeZ *
                            _NormalStrength
                        )
                    );


                waveNormal +=
                    footstepNormalOffset;


                waveNormal =
                    normalize(
                        waveNormal
                    );


                OUT.positionWS =
                    positionWS;


                OUT.normalWS =
                    TransformObjectToWorldNormal(
                        waveNormal
                    );


                OUT.positionCS =
                    TransformWorldToHClip(
                        positionWS
                    );


                OUT.uv =
                    IN.uv;


                OUT.screenPos =
                    ComputeScreenPos(
                        OUT.positionCS
                    );


                return OUT;
            }


            // ============================================================
            // FRAGMENT
            // ============================================================

            half4 Frag(
                Varyings IN
            ) : SV_Target
            {
                // ========================================================
                // NORMAL
                // ========================================================

                float3 normalWS =
                    normalize(
                        IN.normalWS
                    );


                // ========================================================
                // VIEW DIRECTION
                // ========================================================

                float3 cameraPosition =
                    GetCameraPositionWS();


                float3 viewDirection =
                    normalize(
                        cameraPosition -
                        IN.positionWS
                    );


                // ========================================================
                // SCREEN UV
                // ========================================================

                float2 screenUV =
                    IN.screenPos.xy /
                    IN.screenPos.w;


                // ========================================================
                // SCENE DEPTH
                // ========================================================

                float rawSceneDepth =
                    SampleSceneDepth(
                        screenUV
                    );


                float sceneEyeDepth;


                #if UNITY_REVERSED_Z

                    sceneEyeDepth =
                        LinearEyeDepth(
                            rawSceneDepth,
                            _ZBufferParams
                        );

                #else

                    sceneEyeDepth =
                        LinearEyeDepth(
                            lerp(
                                UNITY_NEAR_CLIP_VALUE,
                                1.0,
                                rawSceneDepth
                            ),
                            _ZBufferParams
                        );

                #endif


                // ========================================================
                // WATER SURFACE EYE DEPTH
                // ========================================================

                float3 waterViewPosition =
                    TransformWorldToView(
                        IN.positionWS
                    );


                float waterEyeDepth =
                    -waterViewPosition.z;


                // ========================================================
                // TRUE WATER DEPTH
                // ========================================================

                float waterDepth =
                    max(
                        sceneEyeDepth -
                        waterEyeDepth,
                        0.0
                    );


                // ========================================================
                // DEPTH NORMALIZATION
                // ========================================================

                float depthRange =
                    max(
                        _DeepDepth -
                        _ShallowDepth,
                        0.001
                    );


                float depth01 =
                    saturate(
                        (
                            waterDepth -
                            _ShallowDepth
                        )
                        /
                        depthRange
                    );


                // ========================================================
                // WATER COLORS
                // ========================================================

                float3 shallowColor =
                    _ShallowColor.rgb;


                float3 middleColor =
                    _WaterColor.rgb;


                float3 deepColor =
                    _DeepColor.rgb;


                // --------------------------------------------------------
                // SHALLOW -> WATER
                // --------------------------------------------------------

                float shallowToWater =
                    saturate(
                        depth01 *
                        2.0
                    );


                float3 shallowWaterColor =
                    lerp(
                        shallowColor,
                        middleColor,
                        shallowToWater
                    );


                // --------------------------------------------------------
                // WATER -> DEEP
                // --------------------------------------------------------

                float waterToDeep =
                    saturate(
                        (
                            depth01 -
                            0.5
                        )
                        *
                        2.0
                    );


                float3 depthColor =
                    lerp(
                        shallowWaterColor,
                        deepColor,
                        waterToDeep
                    );


                // --------------------------------------------------------
                // DEPTH STRENGTH
                // --------------------------------------------------------

                float3 waterColor =
                    lerp(
                        _WaterColor.rgb,
                        depthColor,
                        saturate(
                            _DepthColorStrength
                        )
                    );


                // ========================================================
                // PLANAR REFLECTION
                // ========================================================

                float4 reflectionClip =
                    mul(
                        _WaterReflectionVP,
                        float4(
                            IN.positionWS,
                            1.0
                        )
                    );


                float validReflection =
                    step(
                        0.0001,
                        reflectionClip.w
                    );


                float safeW =
                    max(
                        reflectionClip.w,
                        0.0001
                    );


                float2 reflectionNDC =
                    reflectionClip.xy /
                    safeW;


                float2 reflectionUV =
                    reflectionNDC *
                    0.5 +
                    0.5;


                // --------------------------------------------------------
                // Keep current working reflection orientation
                // --------------------------------------------------------

                reflectionUV.y =
                    1.0 -
                    reflectionUV.y;


                // --------------------------------------------------------
                // Reflection distortion
                // --------------------------------------------------------

                float2 distortion =
                    normalWS.xz *
                    _ReflectionDistortion;


                reflectionUV +=
                    distortion;


                // --------------------------------------------------------
                // Reflection bounds
                // --------------------------------------------------------

                float reflectionInside =
                    step(
                        0.0,
                        reflectionUV.x
                    )
                    *
                    step(
                        0.0,
                        reflectionUV.y
                    )
                    *
                    step(
                        reflectionUV.x,
                        1.0
                    )
                    *
                    step(
                        reflectionUV.y,
                        1.0
                    );


                float4 reflectionSample =
                    SAMPLE_TEXTURE2D(
                        _WaterReflectionTex,
                        sampler_WaterReflectionTex,
                        reflectionUV
                    );


                float3 reflectionColor =
                    reflectionSample.rgb;


                // ========================================================
                // FRESNEL
                // ========================================================

                float fresnel =
                    pow(
                        1.0 -
                        saturate(
                            dot(
                                normalWS,
                                viewDirection
                            )
                        ),
                        _FresnelPower
                    );


                float reflectionAmount =
                    saturate(
                        _ReflectionBase +
                        fresnel *
                        _ReflectionStrength
                    );


                reflectionAmount *=
                    validReflection *
                    reflectionInside;


                // ========================================================
                // WATER + REFLECTION
                // ========================================================

                float3 finalColor =
                    lerp(
                        waterColor,
                        reflectionColor,
                        reflectionAmount
                    );


                // ========================================================
                // MAIN LIGHT
                // ========================================================

                Light mainLight =
                    GetMainLight();


                float3 halfDirection =
                    normalize(
                        mainLight.direction +
                        viewDirection
                    );


                float specular =
                    pow(
                        saturate(
                            dot(
                                normalWS,
                                halfDirection
                            )
                        ),
                        _SunSpecularPower
                    );


                specular *=
                    _SunSpecularStrength;


                finalColor +=
                    mainLight.color *
                    specular;


                // ========================================================
                // DEPTH BASED OPACITY
                // ========================================================

                float depthOpacity =
                    lerp(
                        0.45,
                        1.0,
                        depth01
                    );


                float finalAlpha =
                    _Opacity *
                    depthOpacity;


                finalAlpha =
                    saturate(
                        finalAlpha +
                        reflectionAmount *
                        0.15
                    );


                // ========================================================
                // FINAL
                // ========================================================

                return half4(
                    finalColor,
                    finalAlpha
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}

