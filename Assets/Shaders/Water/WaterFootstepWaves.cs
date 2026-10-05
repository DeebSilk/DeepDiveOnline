using UnityEngine;

[DisallowMultipleComponent]
public class WaterFootstepWaves : MonoBehaviour
{
    [Header("Water")]
    [SerializeField]
    private PlanarReflection water;

    [SerializeField]
    private float waterHeight = 0f;

    [Header("Feet")]
    [SerializeField]
    private Transform leftFoot;

    [SerializeField]
    private Transform rightFoot;

    [Header("Step Detection")]
    [SerializeField]
    private float footWaterTolerance = 0.25f;

    [SerializeField]
    private float minimumFootMovement = 0.03f;

    [SerializeField]
    private float minimumStepInterval = 0.18f;

    [Header("Wave")]
    [SerializeField, Range(1, 16)]
    private int maxWaves = 16;

    [SerializeField]
    private float waveSpeed = 1.6f;

    [SerializeField]
    private float waveStrength = 0.35f;

    [SerializeField]
    private float waveWidth = 0.10f;

    [SerializeField]
    private float waveLifetime = 1.5f;

    private Vector3 previousLeftFoot;
    private Vector3 previousRightFoot;

    private float leftStepTimer;
    private float rightStepTimer;

    private Vector4[] wavePositions;
    private Vector4[] waveData;

    private static readonly int WavePositionsID =
        Shader.PropertyToID("_FootstepWavePositions");

    private static readonly int WaveDataID =
        Shader.PropertyToID("_FootstepWaveData");

    private static readonly int WaveCountID =
        Shader.PropertyToID("_FootstepWaveCount");

    private void Awake()
    {
        maxWaves = Mathf.Clamp(maxWaves, 1, 16);

        wavePositions = new Vector4[maxWaves];
        waveData = new Vector4[maxWaves];

        ClearWaves();

        if (leftFoot != null)
        {
            previousLeftFoot = leftFoot.position;
        }

        if (rightFoot != null)
        {
            previousRightFoot = rightFoot.position;
        }
    }

    private void Update()
    {
        if (water == null)
        {
            return;
        }

        UpdateTimers();

        if (leftFoot != null)
        {
            ProcessFoot(
                leftFoot,
                ref previousLeftFoot,
                ref leftStepTimer
            );
        }

        if (rightFoot != null)
        {
            ProcessFoot(
                rightFoot,
                ref previousRightFoot,
                ref rightStepTimer
            );
        }

        UpdateWaves();

        SendToWaterShader();
    }

    private void UpdateTimers()
    {
        leftStepTimer += Time.deltaTime;
        rightStepTimer += Time.deltaTime;
    }

    private void ProcessFoot(
        Transform foot,
        ref Vector3 previousPosition,
        ref float stepTimer)
    {
        Vector3 currentPosition = foot.position;

        float heightDifference =
            Mathf.Abs(
                currentPosition.y - waterHeight
            );

        if (heightDifference > footWaterTolerance)
        {
            previousPosition = currentPosition;
            return;
        }

        Vector2 currentXZ =
            new Vector2(
                currentPosition.x,
                currentPosition.z
            );

        Vector2 previousXZ =
            new Vector2(
                previousPosition.x,
                previousPosition.z
            );

        float movement =
            Vector2.Distance(
                currentXZ,
                previousXZ
            );

        if (movement < minimumFootMovement)
        {
            previousPosition = currentPosition;
            return;
        }

        if (stepTimer < minimumStepInterval)
        {
            previousPosition = currentPosition;
            return;
        }

        CreateWave(
            new Vector3(
                currentPosition.x,
                waterHeight,
                currentPosition.z
            )
        );

        stepTimer = 0f;

        previousPosition = currentPosition;
    }

    private void CreateWave(Vector3 position)
    {
        for (int i = maxWaves - 1; i > 0; i--)
        {
            wavePositions[i] =
                wavePositions[i - 1];

            waveData[i] =
                waveData[i - 1];
        }

        wavePositions[0] =
            new Vector4(
                position.x,
                waterHeight,
                position.z,
                0f
            );

        waveData[0] =
            new Vector4(
                0f,
                waveSpeed,
                waveStrength,
                waveWidth
            );
    }

    private void UpdateWaves()
    {
        for (int i = 0; i < maxWaves; i++)
        {
            Vector4 data = waveData[i];

            data.x += Time.deltaTime;

            if (data.x > waveLifetime)
            {
                data.x = waveLifetime + 1f;
            }

            waveData[i] = data;
        }
    }

    private void SendToWaterShader()
    {
        Material material =
            water.GetWaterMaterial();

        if (material == null)
        {
            return;
        }

        material.SetVectorArray(
            WavePositionsID,
            wavePositions
        );

        material.SetVectorArray(
            WaveDataID,
            waveData
        );

        material.SetInt(
            WaveCountID,
            maxWaves
        );
    }

    private void ClearWaves()
    {
        if (wavePositions == null ||
            waveData == null)
        {
            return;
        }

        for (int i = 0; i < wavePositions.Length; i++)
        {
            wavePositions[i] =
                Vector4.zero;

            waveData[i] =
                new Vector4(
                    waveLifetime + 1f,
                    waveSpeed,
                    waveStrength,
                    waveWidth
                );
        }
    }

    public void CreateWaveAt(
        Vector3 worldPosition
    )
    {
        CreateWave(
            new Vector3(
                worldPosition.x,
                waterHeight,
                worldPosition.z
            )
        );
    }

    public void SetWaterHeight(
        float height
    )
    {
        waterHeight = height;
    }

    private void OnDisable()
    {
        ClearWaves();

        if (water == null)
        {
            return;
        }

        Material material =
            water.GetWaterMaterial();

        if (material == null)
        {
            return;
        }

        material.SetInt(
            WaveCountID,
            0
        );
    }
}

