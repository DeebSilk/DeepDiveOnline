using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class WaterReflection : MonoBehaviour
{
    [Header("Reflection")]
    [SerializeField, Range(0.25f, 1f)]
    private float textureScale = 0.5f;

    [SerializeField, Range(256, 2048)]
    private int maxTextureSize = 1024;

    [SerializeField]
    private LayerMask reflectionMask = -1;

    [SerializeField]
    private float clipPlaneOffset = 0.03f;

    [Header("Options")]
    [SerializeField]
    private bool excludeWaterLayer = true;

    [SerializeField]
    private bool renderOnlyInPlayMode = true;

    private Renderer waterRenderer;
    private Camera reflectionCamera;
    private RenderTexture reflectionTexture;
    private Material waterMaterial;

    private static readonly int ReflectionTextureID =
        Shader.PropertyToID("_ReflectionTex");

    private static readonly int ReflectionViewProjID =
        Shader.PropertyToID("_ReflectionViewProj");

    private static readonly int WaterHeightID =
        Shader.PropertyToID("_WaterHeight");

    private void OnEnable()
    {
        waterRenderer = GetComponent<Renderer>();
        waterMaterial = waterRenderer.material;

        Camera.onPreCull -= HandleCameraPreCull;
        Camera.onPreCull += HandleCameraPreCull;
    }

    private void OnDisable()
    {
        Camera.onPreCull -= HandleCameraPreCull;
        Cleanup();
    }

    private void OnDestroy()
    {
        Camera.onPreCull -= HandleCameraPreCull;
        Cleanup();
    }

    private void HandleCameraPreCull(Camera sourceCamera)
    {
        if (!enabled)
            return;

        if (sourceCamera == null)
            return;

        if (sourceCamera == reflectionCamera)
            return;

        if (renderOnlyInPlayMode && !Application.isPlaying)
            return;

        if (sourceCamera.cameraType != CameraType.Game)
            return;

        if (!sourceCamera.isActiveAndEnabled)
            return;

        if (waterRenderer == null)
            waterRenderer = GetComponent<Renderer>();

        if (waterRenderer == null || !waterRenderer.enabled)
            return;

        RenderReflection(sourceCamera);
    }

    private void RenderReflection(Camera sourceCamera)
    {
        CreateReflectionCamera(sourceCamera);
        CreateReflectionTexture(sourceCamera);

        if (reflectionCamera == null ||
            reflectionTexture == null)
            return;

        Vector3 planePosition = transform.position;
        Vector3 planeNormal = transform.up.normalized;

        // ---------------------------------------------------------
        // Reflection matrix
        // ---------------------------------------------------------

        Matrix4x4 reflectionMatrix =
            CalculateReflectionMatrix(
                planePosition,
                planeNormal
            );

        // ---------------------------------------------------------
        // Reflection camera
        // ---------------------------------------------------------

        reflectionCamera.worldToCameraMatrix =
            sourceCamera.worldToCameraMatrix *
            reflectionMatrix;

        reflectionCamera.projectionMatrix =
            sourceCamera.projectionMatrix;

        // ---------------------------------------------------------
        // Oblique clipping plane
        // ---------------------------------------------------------

        Vector4 clipPlane =
            CameraSpacePlane(
                reflectionCamera,
                planePosition,
                planeNormal,
                1.0f
            );

        reflectionCamera.projectionMatrix =
            reflectionCamera.CalculateObliqueMatrix(
                clipPlane
            );

        reflectionCamera.cullingMask =
            GetReflectionCullingMask();

        reflectionCamera.clearFlags =
            sourceCamera.clearFlags;

        reflectionCamera.backgroundColor =
            sourceCamera.backgroundColor;

        reflectionCamera.allowHDR =
            sourceCamera.allowHDR;

        reflectionCamera.allowMSAA = false;

        reflectionCamera.targetTexture =
            reflectionTexture;

        // ---------------------------------------------------------
        // Render reflection
        // ---------------------------------------------------------

        GL.invertCulling = true;

        try
        {
            reflectionCamera.Render();
        }
        finally
        {
            GL.invertCulling = false;
        }

        // ---------------------------------------------------------
        // Send data to shader
        // ---------------------------------------------------------

        if (waterMaterial == null)
            waterMaterial = waterRenderer.material;

        if (waterMaterial == null)
            return;

        waterMaterial.SetTexture(
            ReflectionTextureID,
            reflectionTexture
        );

        waterMaterial.SetFloat(
            WaterHeightID,
            transform.position.y
        );

        Matrix4x4 viewProjection =
            reflectionCamera.projectionMatrix *
            reflectionCamera.worldToCameraMatrix;

        waterMaterial.SetMatrix(
            ReflectionViewProjID,
            viewProjection
        );
    }

    private int GetReflectionCullingMask()
    {
        int mask = reflectionMask.value;

        if (excludeWaterLayer)
        {
            int waterLayer = gameObject.layer;

            if (waterLayer >= 0 && waterLayer < 32)
            {
                mask &= ~(1 << waterLayer);
            }
        }

        return mask;
    }

    private void CreateReflectionCamera(
        Camera sourceCamera)
    {
        if (reflectionCamera != null)
            return;

        GameObject obj =
            new GameObject(
                "Water Reflection Camera"
            );

        obj.hideFlags =
            HideFlags.HideAndDontSave;

        reflectionCamera =
            obj.AddComponent<Camera>();

        reflectionCamera.enabled = false;

        reflectionCamera.cameraType =
            CameraType.Reflection;

        reflectionCamera.stereoTargetEye =
            StereoTargetEyeMask.None;

        reflectionCamera.allowMSAA = false;
    }

    private void CreateReflectionTexture(
        Camera sourceCamera)
    {
        int width = Mathf.Clamp(
            Mathf.RoundToInt(
                sourceCamera.pixelWidth *
                textureScale
            ),
            256,
            maxTextureSize
        );

        int height = Mathf.Clamp(
            Mathf.RoundToInt(
                sourceCamera.pixelHeight *
                textureScale
            ),
            256,
            maxTextureSize
        );

        if (reflectionTexture != null &&
            reflectionTexture.width == width &&
            reflectionTexture.height == height)
        {
            return;
        }

        ReleaseReflectionTexture();

        reflectionTexture =
            new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGBHalf
            );

        reflectionTexture.name =
            "Water Reflection";

        reflectionTexture.filterMode =
            FilterMode.Bilinear;

        reflectionTexture.wrapMode =
            TextureWrapMode.Clamp;

        reflectionTexture.useMipMap = false;
        reflectionTexture.autoGenerateMips = false;

        reflectionTexture.Create();
    }

    public Material GetWaterMaterial()
    {
        if (waterMaterial != null)
            return waterMaterial;

        if (waterRenderer == null)
            waterRenderer =
                GetComponent<Renderer>();

        if (waterRenderer == null)
            return null;

        waterMaterial =
            waterRenderer.material;

        return waterMaterial;
    }

    private Vector4 CameraSpacePlane(
        Camera camera,
        Vector3 planePosition,
        Vector3 planeNormal,
        float sideSign)
    {
        Vector3 offset =
            planePosition +
            planeNormal * clipPlaneOffset;

        Matrix4x4 matrix =
            camera.worldToCameraMatrix;

        Vector3 cameraPosition =
            matrix.MultiplyPoint(offset);

        Vector3 cameraNormal =
            matrix.MultiplyVector(planeNormal)
                .normalized;

        cameraNormal *= sideSign;

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

    private Matrix4x4 CalculateReflectionMatrix(
        Vector3 planePosition,
        Vector3 planeNormal)
    {
        float d =
            -Vector3.Dot(
                planeNormal,
                planePosition
            );

        float x = planeNormal.x;
        float y = planeNormal.y;
        float z = planeNormal.z;

        Matrix4x4 matrix =
            Matrix4x4.identity;

        matrix.m00 = 1f - 2f * x * x;
        matrix.m01 = -2f * x * y;
        matrix.m02 = -2f * x * z;
        matrix.m03 = -2f * d * x;

        matrix.m10 = -2f * y * x;
        matrix.m11 = 1f - 2f * y * y;
        matrix.m12 = -2f * y * z;
        matrix.m13 = -2f * d * y;

        matrix.m20 = -2f * z * x;
        matrix.m21 = -2f * z * y;
        matrix.m22 = 1f - 2f * z * z;
        matrix.m23 = -2f * d * z;

        return matrix;
    }

    private void ReleaseReflectionTexture()
    {
        if (reflectionTexture == null)
            return;

        if (reflectionTexture.IsCreated())
            reflectionTexture.Release();

        DestroyImmediate(
            reflectionTexture
        );

        reflectionTexture = null;
    }

    private void Cleanup()
    {
        ReleaseReflectionTexture();

        if (reflectionCamera != null)
        {
            DestroyImmediate(
                reflectionCamera.gameObject
            );

            reflectionCamera = null;
        }

        waterMaterial = null;

        GL.invertCulling = false;
    }
}