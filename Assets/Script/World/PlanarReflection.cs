using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public sealed class PlanarReflection : MonoBehaviour
{
    [Header("=== WATER ===")]

    [SerializeField]
    private Renderer waterRenderer;

    [SerializeField]
    private Transform reflectionPlane;


    [Header("=== SOURCE CAMERA ===")]

    [SerializeField]
    private Camera sourceCamera;


    [Header("=== REFLECTION ===")]

    [SerializeField]
    private LayerMask reflectionLayers = -1;

    [SerializeField]
    [Range(0.25f, 1.0f)]
    private float resolutionScale = 1.0f;

    [SerializeField]
    [Min(16)]
    private int minimumResolution = 512;

    [SerializeField]
    [Min(16)]
    private int maximumResolution = 2048;


    [Header("=== QUALITY ===")]

    [SerializeField]
    [Range(1, 8)]
    private int antiAliasing = 4;

    [SerializeField]
    private bool reflectSkybox = true;

    [SerializeField]
    private bool flipReflectionY = true;


    [Header("=== CLIPPING ===")]

    [SerializeField]
    [Min(0.001f)]
    private float clipPlaneOffset = 0.05f;


    [Header("=== PERFORMANCE ===")]

    [SerializeField]
    private bool renderInEditor = false;

    [SerializeField]
    [Range(1, 4)]
    private int updateEveryNFrames = 1;


    private Camera reflectionCamera;
    private UniversalAdditionalCameraData reflectionCameraData;
    private RenderTexture reflectionTexture;

    private int lastWidth;
    private int lastHeight;
    private int lastMSAA;

    private int frameCounter;

    private bool isRenderingReflection;


    private static readonly int ReflectionTextureID =
        Shader.PropertyToID("_WaterReflectionTex");

    private static readonly int ReflectionVPID =
        Shader.PropertyToID("_WaterReflectionVP");

    private static readonly int ReflectionFlipYID =
        Shader.PropertyToID("_WaterReflectionFlipY");


    // =====================================================================
    // UNITY
    // =====================================================================

    private void OnEnable()
    {
        EnsureReferences();
        EnsureReflectionCamera();
        EnsureReflectionTexture();

        frameCounter = 0;
        isRenderingReflection = false;

        if (Application.isPlaying || renderInEditor)
        {
            RenderReflection();
        }
    }


    private void OnDisable()
    {
        ClearShaderData();

        ReleaseReflectionTexture();
        DestroyReflectionCamera();

        isRenderingReflection = false;
    }


    private void OnDestroy()
    {
        ClearShaderData();

        ReleaseReflectionTexture();
        DestroyReflectionCamera();

        isRenderingReflection = false;
    }


    private void LateUpdate()
    {
        if (!enabled)
        {
            return;
        }

        if (!Application.isPlaying && !renderInEditor)
        {
            return;
        }

        if (isRenderingReflection)
        {
            return;
        }

        frameCounter++;

        int interval =
            Mathf.Max(
                1,
                updateEveryNFrames
            );

        if (frameCounter < interval)
        {
            return;
        }

        frameCounter = 0;

        RenderReflection();
    }


    // =====================================================================
    // PUBLIC API
    // =====================================================================

    public Material GetWaterMaterial()
    {
        if (waterRenderer == null)
        {
            waterRenderer = GetComponent<Renderer>();
        }

        if (waterRenderer == null)
        {
            return null;
        }

        return waterRenderer.sharedMaterial;
    }


    public Renderer GetWaterRenderer()
    {
        if (waterRenderer == null)
        {
            waterRenderer = GetComponent<Renderer>();
        }

        return waterRenderer;
    }


    public RenderTexture GetReflectionTexture()
    {
        return reflectionTexture;
    }


    // =====================================================================
    // REFERENCES
    // =====================================================================

    private void EnsureReferences()
    {
        if (waterRenderer == null)
        {
            waterRenderer = GetComponent<Renderer>();
        }

        if (reflectionPlane == null)
        {
            reflectionPlane = transform;
        }

        if (sourceCamera == null)
        {
            sourceCamera = Camera.main;
        }
    }


    // =====================================================================
    // REFLECTION CAMERA
    // =====================================================================

    private void EnsureReflectionCamera()
    {
        if (reflectionCamera != null)
        {
            return;
        }

        GameObject cameraObject =
            new GameObject(
                "Water Reflection Camera"
            );

        cameraObject.hideFlags =
            HideFlags.HideAndDontSave;

        reflectionCamera =
            cameraObject.AddComponent<Camera>();

        reflectionCamera.enabled = false;

        reflectionCamera.cameraType =
            CameraType.Reflection;

        reflectionCamera.allowHDR = true;

        reflectionCamera.allowMSAA =
            antiAliasing > 1;

        // IMPORTANT:
        // stereoTargetEye is Built-in Render Pipeline only.
        // This project uses URP, so XR is disabled through
        // UniversalAdditionalCameraData instead.

        reflectionCamera.useOcclusionCulling = false;

        reflectionCameraData =
            reflectionCamera
                .GetUniversalAdditionalCameraData();

        reflectionCameraData.renderType =
            CameraRenderType.Base;

        reflectionCameraData.renderPostProcessing =
            false;

        reflectionCameraData.requiresColorOption =
            CameraOverrideOption.Off;

        reflectionCameraData.requiresDepthOption =
            CameraOverrideOption.Off;

        reflectionCameraData.allowXRRendering =
            false;
    }


    // =====================================================================
    // REFLECTION TEXTURE
    // =====================================================================

    private void EnsureReflectionTexture()
    {
        if (sourceCamera == null)
        {
            return;
        }


        int sourceWidth =
            Mathf.Max(
                1,
                sourceCamera.pixelWidth
            );

        int sourceHeight =
            Mathf.Max(
                1,
                sourceCamera.pixelHeight
            );


        float scale =
            Mathf.Clamp(
                resolutionScale,
                0.25f,
                1.0f
            );


        int width =
            Mathf.Max(
                16,
                Mathf.RoundToInt(
                    sourceWidth * scale
                )
            );


        int height =
            Mathf.Max(
                16,
                Mathf.RoundToInt(
                    sourceHeight * scale
                )
            );


        // ---------------------------------------------------------------
        // PRESERVE CAMERA ASPECT RATIO
        // ---------------------------------------------------------------

        float aspect =
            sourceHeight > 0
                ? (float)sourceWidth /
                  sourceHeight
                : 1.7777778f;


        int maxResolution =
            Mathf.Max(
                16,
                maximumResolution
            );


        int minResolution =
            Mathf.Clamp(
                minimumResolution,
                16,
                maxResolution
            );


        // Limit the LONGEST side to maximumResolution.
        if (width >= height)
        {
            if (width > maxResolution)
            {
                width = maxResolution;

                height =
                    Mathf.RoundToInt(
                        width / aspect
                    );
            }
        }
        else
        {
            if (height > maxResolution)
            {
                height = maxResolution;

                width =
                    Mathf.RoundToInt(
                        height * aspect
                    );
            }
        }


        // ---------------------------------------------------------------
        // MINIMUM RESOLUTION
        // ---------------------------------------------------------------

        if (width < minResolution)
        {
            width = minResolution;

            height =
                Mathf.RoundToInt(
                    width / aspect
                );
        }

        if (height < minResolution)
        {
            height = minResolution;

            width =
                Mathf.RoundToInt(
                    height * aspect
                );
        }


        width =
            Mathf.Max(
                16,
                width
            );

        height =
            Mathf.Max(
                16,
                height
            );


        int msaa =
            GetValidMSAASamples(
                antiAliasing
            );


        // ---------------------------------------------------------------
        // ALREADY CORRECT
        // ---------------------------------------------------------------

        if (
            reflectionTexture != null &&
            reflectionTexture.IsCreated() &&
            lastWidth == width &&
            lastHeight == height &&
            lastMSAA == msaa
        )
        {
            return;
        }


        ReleaseReflectionTexture();


        // ---------------------------------------------------------------
        // CREATE REFLECTION TEXTURE
        // ---------------------------------------------------------------

        RenderTextureDescriptor descriptor =
            new RenderTextureDescriptor(
                width,
                height,
                RenderTextureFormat.ARGBHalf,
                24
            );


        descriptor.msaaSamples =
            msaa;

        descriptor.useMipMap = false;

        descriptor.autoGenerateMips = false;

        descriptor.depthBufferBits = 24;

        descriptor.sRGB =
            QualitySettings.activeColorSpace ==
            ColorSpace.Linear;

        descriptor.enableRandomWrite = false;


        reflectionTexture =
            new RenderTexture(
                descriptor
            );


        reflectionTexture.name =
            "Water Reflection";


        reflectionTexture.filterMode =
            FilterMode.Bilinear;


        reflectionTexture.wrapMode =
            TextureWrapMode.Clamp;


        reflectionTexture.anisoLevel = 2;


        reflectionTexture.Create();


        lastWidth = width;
        lastHeight = height;
        lastMSAA = msaa;
    }


    private static int GetValidMSAASamples(
        int requested
    )
    {
        if (requested >= 8)
        {
            return 8;
        }

        if (requested >= 4)
        {
            return 4;
        }

        if (requested >= 2)
        {
            return 2;
        }

        return 1;
    }


    // =====================================================================
    // MAIN REFLECTION
    // =====================================================================

    private void RenderReflection()
    {
        if (isRenderingReflection)
        {
            return;
        }


        EnsureReferences();


        if (sourceCamera == null)
        {
            return;
        }


        if (!sourceCamera.isActiveAndEnabled)
        {
            return;
        }


        EnsureReflectionCamera();
        EnsureReflectionTexture();


        if (
            reflectionCamera == null ||
            reflectionTexture == null
        )
        {
            return;
        }


        if (!reflectionTexture.IsCreated())
        {
            reflectionTexture.Create();
        }


        isRenderingReflection = true;


        try
        {
            RenderReflectionInternal();
        }
        finally
        {
            isRenderingReflection = false;
        }
    }


    // =====================================================================
    // REFLECTION RENDER
    // =====================================================================

    private void RenderReflectionInternal()
    {
        // ---------------------------------------------------------------
        // WATER PLANE
        // ---------------------------------------------------------------

        Vector3 planePosition =
            reflectionPlane != null
                ? reflectionPlane.position
                : transform.position;


        Vector3 planeNormal =
            reflectionPlane != null
                ? reflectionPlane.up
                : transform.up;


        if (
            planeNormal.sqrMagnitude
            <
            0.000001f
        )
        {
            planeNormal =
                Vector3.up;
        }


        planeNormal.Normalize();


        // ---------------------------------------------------------------
        // REFLECTION MATRIX
        // ---------------------------------------------------------------

        Matrix4x4 reflectionMatrix =
            CalculateReflectionMatrix(
                planePosition,
                planeNormal
            );


        // ---------------------------------------------------------------
        // REFLECT CAMERA POSITION
        // ---------------------------------------------------------------

        Vector3 sourcePosition =
            sourceCamera.transform.position;


        Vector3 reflectedPosition =
            reflectionMatrix.MultiplyPoint(
                sourcePosition
            );


        // ---------------------------------------------------------------
        // REFLECT CAMERA DIRECTION
        // ---------------------------------------------------------------

        Vector3 sourceForward =
            sourceCamera.transform.forward;


        Vector3 sourceUp =
            sourceCamera.transform.up;


        Vector3 reflectedForward =
            reflectionMatrix.MultiplyVector(
                sourceForward
            );


        Vector3 reflectedUp =
            reflectionMatrix.MultiplyVector(
                sourceUp
            );


        if (
            reflectedForward.sqrMagnitude
            <
            0.000001f
        )
        {
            reflectedForward =
                Vector3.forward;
        }


        if (
            reflectedUp.sqrMagnitude
            <
            0.000001f
        )
        {
            reflectedUp =
                Vector3.up;
        }


        reflectedForward.Normalize();
        reflectedUp.Normalize();


        // ---------------------------------------------------------------
        // REFLECTION CAMERA TRANSFORM
        // ---------------------------------------------------------------

        reflectionCamera.transform.position =
            reflectedPosition;


        reflectionCamera.transform.rotation =
            Quaternion.LookRotation(
                reflectedForward,
                reflectedUp
            );


        // ---------------------------------------------------------------
        // CAMERA SETTINGS
        // ---------------------------------------------------------------

        reflectionCamera.fieldOfView =
            sourceCamera.fieldOfView;


        reflectionCamera.orthographic =
            sourceCamera.orthographic;


        reflectionCamera.orthographicSize =
            sourceCamera.orthographicSize;


        reflectionCamera.nearClipPlane =
            sourceCamera.nearClipPlane;


        reflectionCamera.farClipPlane =
            sourceCamera.farClipPlane;


        reflectionCamera.aspect =
            sourceCamera.aspect;


        reflectionCamera.allowHDR =
            sourceCamera.allowHDR;


        reflectionCamera.allowMSAA =
            antiAliasing > 1;


        reflectionCamera.backgroundColor =
            sourceCamera.backgroundColor;


        reflectionCamera.clearFlags =
            reflectSkybox
                ? sourceCamera.clearFlags
                : CameraClearFlags.SolidColor;


        // ---------------------------------------------------------------
        // LAYERS
        // ---------------------------------------------------------------

        int reflectionMask =
            reflectionLayers.value;


        if (waterRenderer != null)
        {
            int waterLayer =
                waterRenderer.gameObject.layer;


            if (
                waterLayer >= 0 &&
                waterLayer < 32
            )
            {
                reflectionMask &=
                    ~(1 << waterLayer);
            }
        }


        reflectionCamera.cullingMask =
            reflectionMask;


        // ---------------------------------------------------------------
        // REFLECTED VIEW MATRIX
        // ---------------------------------------------------------------

        Matrix4x4 reflectedWorldToCamera =
            sourceCamera.worldToCameraMatrix *
            reflectionMatrix;


        reflectionCamera.worldToCameraMatrix =
            reflectedWorldToCamera;


        // ---------------------------------------------------------------
        // OBLIQUE CLIPPING
        // ---------------------------------------------------------------

        Vector4 cameraSpacePlane =
            CameraSpacePlane(
                reflectedWorldToCamera,
                planePosition,
                planeNormal
            );


        Matrix4x4 projectionMatrix =
            sourceCamera.projectionMatrix;


        projectionMatrix =
            CalculateObliqueProjection(
                projectionMatrix,
                cameraSpacePlane
            );


        reflectionCamera.projectionMatrix =
            projectionMatrix;


        // ---------------------------------------------------------------
        // TARGET
        // ---------------------------------------------------------------

        reflectionCamera.targetTexture =
            reflectionTexture;


        // ---------------------------------------------------------------
        // URP RENDER REQUEST
        // ---------------------------------------------------------------

        UniversalRenderPipeline.SingleCameraRequest request =
            new UniversalRenderPipeline.SingleCameraRequest();


        request.destination =
            reflectionTexture;


        if (
            !RenderPipeline.SupportsRenderRequest(
                reflectionCamera,
                request
            )
        )
        {
            Debug.LogWarning(
                "PlanarReflection: " +
                "The active URP renderer does not support " +
                "SingleCameraRequest.",
                this
            );

            return;
        }


        // ---------------------------------------------------------------
        // INVERT CULLING
        // ---------------------------------------------------------------

        bool previousInvertCulling =
            GL.invertCulling;


        GL.invertCulling =
            !previousInvertCulling;


        try
        {
            RenderPipeline.SubmitRenderRequest(
                reflectionCamera,
                request
            );
        }
        finally
        {
            GL.invertCulling =
                previousInvertCulling;
        }


        // ---------------------------------------------------------------
        // REFLECTION MATRIX FOR SHADER
        // ---------------------------------------------------------------

        Matrix4x4 finalWorldToCamera =
            reflectionCamera.worldToCameraMatrix;


        Matrix4x4 gpuProjection =
            GL.GetGPUProjectionMatrix(
                reflectionCamera.projectionMatrix,
                true
            );


        Matrix4x4 reflectionVP =
            gpuProjection *
            finalWorldToCamera;


        // ---------------------------------------------------------------
        // SEND TO SHADER
        // ---------------------------------------------------------------

        Shader.SetGlobalTexture(
            ReflectionTextureID,
            reflectionTexture
        );


        Shader.SetGlobalMatrix(
            ReflectionVPID,
            reflectionVP
        );


        Shader.SetGlobalFloat(
            ReflectionFlipYID,
            flipReflectionY
                ? 1.0f
                : 0.0f
        );
    }


    // =====================================================================
    // REFLECTION MATRIX
    // =====================================================================

    private static Matrix4x4 CalculateReflectionMatrix(
        Vector3 planePosition,
        Vector3 planeNormal
    )
    {
        float d =
            -Vector3.Dot(
                planeNormal,
                planePosition
            );


        float x =
            planeNormal.x;

        float y =
            planeNormal.y;

        float z =
            planeNormal.z;


        Matrix4x4 matrix =
            Matrix4x4.identity;


        matrix.m00 =
            1.0f -
            2.0f * x * x;

        matrix.m01 =
            -2.0f * x * y;

        matrix.m02 =
            -2.0f * x * z;

        matrix.m03 =
            -2.0f * x * d;


        matrix.m10 =
            -2.0f * y * x;

        matrix.m11 =
            1.0f -
            2.0f * y * y;

        matrix.m12 =
            -2.0f * y * z;

        matrix.m13 =
            -2.0f * y * d;


        matrix.m20 =
            -2.0f * z * x;

        matrix.m21 =
            -2.0f * z * y;

        matrix.m22 =
            1.0f -
            2.0f * z * z;

        matrix.m23 =
            -2.0f * z * d;


        return matrix;
    }


    // =====================================================================
    // CAMERA SPACE PLANE
    // =====================================================================

    private Vector4 CameraSpacePlane(
        Matrix4x4 worldToCamera,
        Vector3 position,
        Vector3 normal
    )
    {
        Vector3 offsetPosition =
            position +
            normal *
            clipPlaneOffset;


        Vector3 cameraPosition =
            worldToCamera.MultiplyPoint(
                offsetPosition
            );


        Vector3 cameraNormal =
            worldToCamera
                .MultiplyVector(
                    normal
                )
                .normalized;


        return new Vector4(
            cameraNormal.x,
            cameraNormal.y,
            cameraNormal.z,
            -Vector3.Dot(
                cameraPosition,
                cameraNormal
            )
        );
    }


    // =====================================================================
    // OBLIQUE PROJECTION
    // =====================================================================

    private static Matrix4x4 CalculateObliqueProjection(
        Matrix4x4 projection,
        Vector4 clipPlane
    )
    {
        Vector4 q =
            projection.inverse *
            new Vector4(
                Mathf.Sign(
                    clipPlane.x
                ),
                Mathf.Sign(
                    clipPlane.y
                ),
                1.0f,
                1.0f
            );


        float denominator =
            Vector4.Dot(
                clipPlane,
                q
            );


        if (
            Mathf.Abs(
                denominator
            )
            <
            0.000001f
        )
        {
            return projection;
        }


        Vector4 c =
            clipPlane *
            (
                2.0f /
                denominator
            );


        projection.m20 =
            c.x -
            projection.m30;


        projection.m21 =
            c.y -
            projection.m31;


        projection.m22 =
            c.z -
            projection.m32;


        projection.m23 =
            c.w -
            projection.m33;


        return projection;
    }


    // =====================================================================
    // CLEANUP
    // =====================================================================

    private void ReleaseReflectionTexture()
    {
        if (reflectionTexture == null)
        {
            return;
        }


        if (reflectionTexture.IsCreated())
        {
            reflectionTexture.Release();
        }


        DestroyObjectSafe(
            reflectionTexture
        );


        reflectionTexture = null;


        lastWidth = 0;
        lastHeight = 0;
        lastMSAA = 0;
    }


    private void DestroyReflectionCamera()
    {
        if (reflectionCamera == null)
        {
            return;
        }


        GameObject cameraObject =
            reflectionCamera.gameObject;


        reflectionCamera = null;
        reflectionCameraData = null;


        DestroyObjectSafe(
            cameraObject
        );
    }


    private static void DestroyObjectSafe(
        Object target
    )
    {
        if (target == null)
        {
            return;
        }


        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }


    // =====================================================================
    // SHADER CLEANUP
    // =====================================================================

    private static void ClearShaderData()
    {
        Shader.SetGlobalTexture(
            ReflectionTextureID,
            null
        );


        Shader.SetGlobalMatrix(
            ReflectionVPID,
            Matrix4x4.identity
        );


        Shader.SetGlobalFloat(
            ReflectionFlipYID,
            0.0f
        );
    }
}