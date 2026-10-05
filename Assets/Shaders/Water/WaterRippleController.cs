using UnityEngine;

[DisallowMultipleComponent]
public class WaterRippleController : MonoBehaviour
{
    [Header("=== REFERENCES ===")]

    [SerializeField]
    private PlanarReflection water;

    [SerializeField]
    private Transform target;


    [Header("=== WATER ===")]

    [SerializeField]
    private float waterHeight = 0f;


    [Header("=== RIPPLE ===")]

    [SerializeField, Range(1, 8)]
    private int maxRipples = 8;

    [SerializeField]
    private float minimumDistance = 0.15f;

    [SerializeField]
    private float spawnInterval = 0.08f;

    [SerializeField]
    private float rippleLifetime = 1.5f;

    [SerializeField]
    private float rippleSpeed = 1.5f;

    [SerializeField]
    private float rippleStrength = 0.25f;

    [SerializeField]
    private float rippleWidth = 0.12f;


    [Header("=== DEBUG ===")]

    [SerializeField]
    private bool drawDebug = true;


    private Vector4[] ripplePositions;
    private Vector4[] rippleData;

    private Vector3 lastTargetPosition;
    private float spawnTimer;


    private static readonly int RipplePositionsID =
        Shader.PropertyToID("_RipplePositions");

    private static readonly int RippleDataID =
        Shader.PropertyToID("_RippleData");

    private static readonly int RippleCountID =
        Shader.PropertyToID("_RippleCount");

    private static readonly int DebugRippleID =
        Shader.PropertyToID("_DebugRipple");


    // =====================================================================
    // UNITY
    // =====================================================================

    private void Awake()
    {
        InitializeArrays();

        if (target != null)
        {
            lastTargetPosition =
                target.position;
        }
    }


    private void OnEnable()
    {
        if (
            ripplePositions == null ||
            rippleData == null ||
            ripplePositions.Length != maxRipples ||
            rippleData.Length != maxRipples
        )
        {
            InitializeArrays();
        }
    }


    private void Update()
    {
        if (water == null)
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        spawnTimer +=
            Time.deltaTime;

        UpdateTargetRipple();

        UpdateRippleAges();

        SendRipplesToShader();
    }


    private void OnDisable()
    {
        ClearRipples();

        ClearShaderRippleData();
    }


    // =====================================================================
    // INITIALIZATION
    // =====================================================================

    private void InitializeArrays()
    {
        maxRipples =
            Mathf.Clamp(
                maxRipples,
                1,
                8
            );

        ripplePositions =
            new Vector4[maxRipples];

        rippleData =
            new Vector4[maxRipples];

        ClearRipples();
    }


    // =====================================================================
    // TARGET MOVEMENT
    // =====================================================================

    private void UpdateTargetRipple()
    {
        Vector3 currentPosition =
            target.position;

        Vector2 currentXZ =
            new Vector2(
                currentPosition.x,
                currentPosition.z
            );

        Vector2 previousXZ =
            new Vector2(
                lastTargetPosition.x,
                lastTargetPosition.z
            );

        float distance =
            Vector2.Distance(
                currentXZ,
                previousXZ
            );

        if (
            distance >= minimumDistance &&
            spawnTimer >= spawnInterval
        )
        {
            Vector3 ripplePosition =
                new Vector3(
                    currentPosition.x,
                    waterHeight,
                    currentPosition.z
                );

            AddRipple(
                ripplePosition
            );

            spawnTimer = 0f;
        }

        lastTargetPosition =
            currentPosition;
    }


    // =====================================================================
    // ADD RIPPLE
    // =====================================================================

    private void AddRipple(
        Vector3 worldPosition
    )
    {
        for (
            int i = maxRipples - 1;
            i > 0;
            i--
        )
        {
            ripplePositions[i] =
                ripplePositions[i - 1];

            rippleData[i] =
                rippleData[i - 1];
        }


        ripplePositions[0] =
            new Vector4(
                worldPosition.x,
                waterHeight,
                worldPosition.z,
                0f
            );


        rippleData[0] =
            new Vector4(
                0f,
                rippleSpeed,
                rippleStrength,
                rippleWidth
            );
    }


    // =====================================================================
    // RIPPLE AGE
    // =====================================================================

    private void UpdateRippleAges()
    {
        for (
            int i = 0;
            i < maxRipples;
            i++
        )
        {
            Vector4 data =
                rippleData[i];

            data.x +=
                Time.deltaTime;

            if (
                data.x >
                rippleLifetime
            )
            {
                data.x =
                    rippleLifetime + 1f;
            }

            rippleData[i] =
                data;
        }
    }


    // =====================================================================
    // SEND TO SHADER
    // =====================================================================

    private void SendRipplesToShader()
    {
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


        material.SetVectorArray(
            RipplePositionsID,
            ripplePositions
        );


        material.SetVectorArray(
            RippleDataID,
            rippleData
        );


        material.SetInt(
            RippleCountID,
            maxRipples
        );


        material.SetFloat(
            DebugRippleID,
            drawDebug
                ? 1f
                : 0f
        );
    }


    // =====================================================================
    // CLEAR
    // =====================================================================

    private void ClearRipples()
    {
        if (
            ripplePositions == null ||
            rippleData == null
        )
        {
            return;
        }

        for (
            int i = 0;
            i < ripplePositions.Length;
            i++
        )
        {
            ripplePositions[i] =
                Vector4.zero;

            rippleData[i] =
                new Vector4(
                    rippleLifetime + 1f,
                    rippleSpeed,
                    0f,
                    rippleWidth
                );
        }
    }


    private void ClearShaderRippleData()
    {
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
            RippleCountID,
            0
        );

        material.SetFloat(
            DebugRippleID,
            0f
        );
    }


    // =====================================================================
    // PUBLIC
    // =====================================================================

    public void CreateRipple(
        Vector3 worldPosition
    )
    {
        AddRipple(
            new Vector3(
                worldPosition.x,
                waterHeight,
                worldPosition.z
            )
        );
    }


    public void SetTarget(
        Transform newTarget
    )
    {
        target =
            newTarget;

        if (target != null)
        {
            lastTargetPosition =
                target.position;
        }
    }


    public void SetWaterHeight(
        float height
    )
    {
        waterHeight =
            height;
    }


    // =====================================================================
    // DEBUG
    // =====================================================================

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(
            new Vector3(
                target.position.x,
                waterHeight,
                target.position.z
            ),
            0.2f
        );
    }
}