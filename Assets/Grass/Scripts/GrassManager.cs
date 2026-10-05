using UnityEngine;

public class GrassManager : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private Mesh grassMesh;
    [SerializeField] private Material grassMaterial;
    [SerializeField] private Transform player;

    [Header("TEST")]
    [SerializeField] private float distanceFromPlayer = 3f;
    [SerializeField] private float grassHeight = 2f;
    [SerializeField] private float grassWidth = 1f;

    private GameObject testGrass;

    private void Start()
    {
        CreateTestGrass();
    }

    [ContextMenu("Create Test Grass")]
    public void CreateTestGrass()
    {
        if (testGrass != null)
        {
            Destroy(testGrass);
            testGrass = null;
        }

        if (grassMesh == null)
        {
            Debug.LogError("GrassManager: Grass Mesh не назначен.");
            return;
        }

        if (grassMaterial == null)
        {
            Debug.LogError("GrassManager: Grass Material не назначен.");
            return;
        }

        if (player == null)
        {
            Debug.LogError(
                "GrassManager: Player не назначен. " +
                "Перетащи Ch36_nonPBR в Player."
            );
            return;
        }

        Vector3 position =
            player.position +
            player.forward * distanceFromPlayer;

        if (targetTerrain != null)
        {
            position.y =
                targetTerrain.SampleHeight(position) +
                targetTerrain.GetPosition().y;
        }

        testGrass = new GameObject("TEST_GRASS");

        testGrass.transform.position = position;
        testGrass.transform.rotation =
            Quaternion.Euler(0f, 0f, 0f);

        testGrass.transform.localScale =
            new Vector3(
                grassWidth,
                grassHeight,
                1f
            );

        MeshFilter meshFilter =
            testGrass.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            testGrass.AddComponent<MeshRenderer>();

        meshFilter.sharedMesh = grassMesh;
        meshRenderer.sharedMaterial = grassMaterial;

        Debug.Log(
            "TEST GRASS СОЗДАН. Position = " +
            position
        );

        Debug.Log(
            "TEST GRASS Mesh = " +
            grassMesh.name
        );

        Debug.Log(
            "TEST GRASS Material = " +
            grassMaterial.name
        );
    }

    private void OnDestroy()
    {
        if (testGrass != null)
        {
            Destroy(testGrass);
        }
    }
}

