using UnityEngine;

public class GrassVectorField : MonoBehaviour
{
    [Header("Vector Field")]
    [SerializeField] private RenderTexture vectorField;
    [SerializeField, Min(0.1f)] private float worldSize = 50f;

    [Header("Brush")]
    [SerializeField, Min(0.1f)] private float brushSize = 2f;
    [SerializeField, Range(0f, 1f)] private float brushStrength = 1f;

    private Texture2D brushTexture;
    private Material brushMaterial;

    private static readonly int BrushPositionID = Shader.PropertyToID("_BrushPosition");
    private static readonly int BrushDirectionID = Shader.PropertyToID("_BrushDirection");
    private static readonly int BrushSizeID = Shader.PropertyToID("_BrushSize");
    private static readonly int BrushStrengthID = Shader.PropertyToID("_BrushStrength");

    private void Awake()
    {
        if (vectorField == null)
        {
            Debug.LogError("GrassVectorField: Render Texture не назначен.");
            return;
        }

        CreateBrush();
        ClearVectorField();
    }

    private void CreateBrush()
    {
        brushTexture = new Texture2D(
            64,
            64,
            TextureFormat.RGBA32,
            false,
            true
        );

        Color[] pixels = new Color[64 * 64];

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float u = (x + 0.5f) / 64f;
                float v = (y + 0.5f) / 64f;

                float dx = u * 2f - 1f;
                float dy = v * 2f - 1f;

                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = 1f - Mathf.SmoothStep(0.7f, 1f, distance);

                pixels[y * 64 + x] = new Color(0.5f, 0.5f, 0f, alpha);
            }
        }

        brushTexture.SetPixels(pixels);
        brushTexture.Apply();

        brushMaterial = new Material(Shader.Find("Hidden/GrassVectorFieldBrush"));

        if (brushMaterial == null)
        {
            Debug.LogError(
                "GrassVectorField: не найден Shader 'Hidden/GrassVectorFieldBrush'."
            );
        }
    }

    private void ClearVectorField()
    {
        if (vectorField == null)
            return;

        RenderTexture previous = RenderTexture.active;

        RenderTexture.active = vectorField;
        GL.Clear(true, true, new Color(0.5f, 0.5f, 0f, 1f));
        RenderTexture.active = previous;
    }

    public void Paint(Vector3 worldPosition, Vector3 direction)
    {
        if (vectorField == null || brushMaterial == null)
            return;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        Vector2 uv = WorldToUV(worldPosition);

        brushMaterial.SetVector(
            BrushPositionID,
            new Vector4(uv.x, uv.y, 0f, 0f)
        );

        brushMaterial.SetVector(
            BrushDirectionID,
            new Vector4(direction.x, direction.z, 0f, 0f)
        );

        brushMaterial.SetFloat(
            BrushSizeID,
            brushSize / worldSize
        );

        brushMaterial.SetFloat(
            BrushStrengthID,
            brushStrength
        );

        RenderTexture temporary = RenderTexture.GetTemporary(
            vectorField.descriptor
        );

        Graphics.Blit(
            vectorField,
            temporary
        );

        Graphics.Blit(
            temporary,
            vectorField,
            brushMaterial
        );

        RenderTexture.ReleaseTemporary(temporary);
    }

    private Vector2 WorldToUV(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);

        float u = local.x / worldSize + 0.5f;
        float v = local.z / worldSize + 0.5f;

        return new Vector2(u, v);
    }

    private void OnDestroy()
    {
        if (brushTexture != null)
        {
            Destroy(brushTexture);
        }

        if (brushMaterial != null)
        {
            Destroy(brushMaterial);
        }
    }
}

