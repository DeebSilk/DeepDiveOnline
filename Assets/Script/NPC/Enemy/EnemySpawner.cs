using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    // =========================================================
    // GENERAL
    // =========================================================

    [Header("=== GENERAL ===")]

    [SerializeField] private bool spawnerEnabled = true;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool autoRespawn = true;

    [SerializeField, Min(0f)]
    private float initialSpawnDelay = 0f;

    [SerializeField, Min(0f)]
    private float respawnDelay = 5f;

    [SerializeField, Min(1)]
    private int maxConcurrentEnemies = 3;

    [Tooltip("0 = unlimited")]
    [SerializeField, Min(0)]
    private int maxTotalSpawns = 0;


    // =========================================================
    // ENEMY TYPES
    // =========================================================

    [Header("=== ENEMY TYPES ===")]

    [SerializeField]
    private List<EnemyDefinition> enemyTypes = new();


    // =========================================================
    // SPAWN AREA
    // =========================================================

    public enum SpawnMode
    {
        FixedPoint,
        RandomRadius,
        AroundPlayer
    }

    [Header("=== SPAWN AREA ===")]

    [SerializeField]
    private SpawnMode spawnMode = SpawnMode.RandomRadius;

    [SerializeField, Min(0f)]
    private float spawnRadius = 10f;

    [SerializeField]
    private bool randomRotation = true;


    // =========================================================
    // GROUND
    // =========================================================

    [Header("=== GROUND ===")]

    [SerializeField]
    private bool snapToGround = true;

    [SerializeField]
    private LayerMask groundLayer = ~0;

    [SerializeField, Min(0.1f)]
    private float groundCheckHeight = 100f;

    [SerializeField, Min(0.1f)]
    private float groundCheckDistance = 300f;

    [SerializeField, Min(0f)]
    private float groundOffset = 0.05f;


    // =========================================================
    // PLAYER
    // =========================================================

    [Header("=== PLAYER ===")]

    [SerializeField]
    private bool requirePlayerNearby = false;

    [SerializeField]
    private Transform player;

    [SerializeField, Min(0f)]
    private float activationDistance = 115f;

    [SerializeField, Min(0f)]
    private float deactivationDistance = 140f;

    [SerializeField, Min(0f)]
    private float minimumPlayerDistance = 3f;


    // =========================================================
    // OBSTACLES
    // =========================================================

    [Header("=== OBSTACLE CHECK ===")]

    [SerializeField]
    private bool checkObstacles = false;

    [SerializeField]
    private LayerMask obstacleLayer = 0;

    [SerializeField, Min(0.05f)]
    private float obstacleCheckRadius = 0.4f;

    [SerializeField, Min(1)]
    private int maxSpawnAttempts = 30;


    // =========================================================
    // VISUALS
    // =========================================================

    [Header("=== VISUALS ===")]

    [SerializeField]
    private GameObject spawnEffectPrefab;

    [SerializeField]
    private GameObject deathEffectPrefab;

    [SerializeField]
    private bool showSpawnRadius = true;

    [SerializeField]
    private bool showActivationRadius = true;

    [SerializeField]
    private bool showDeactivationRadius = true;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("=== DEBUG ===")]

    [SerializeField]
    private bool debugLogs = true;

    [SerializeField]
    private bool drawSpawnPoint = true;


    // =========================================================
    // RUNTIME
    // =========================================================

    private readonly List<GameObject> activeEnemies = new();

    private bool isSpawning;
    private bool isPaused;

    private int totalSpawned;
    private int totalKilled;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        FindPlayer();

        if (!spawnerEnabled)
            return;

        if (!spawnOnStart)
            return;

        if (initialSpawnDelay > 0f)
            StartCoroutine(InitialSpawnRoutine());
        else
            FillSpawnSlots();
    }


    private IEnumerator InitialSpawnRoutine()
    {
        yield return new WaitForSeconds(initialSpawnDelay);

        FillSpawnSlots();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        CleanupEnemies();

        if (!spawnerEnabled || isPaused)
            return;

        if (!requirePlayerNearby)
            return;

        FindPlayer();

        if (player == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        if (distance <= activationDistance)
        {
            FillSpawnSlots();
        }
    }


    // =========================================================
    // SPAWN
    // =========================================================

    [ContextMenu("Spawn Enemy")]
    public void SpawnEnemy()
    {
        if (!CanSpawn())
            return;

        if (isSpawning)
            return;

        StartCoroutine(SpawnRoutine());
    }


    private void FillSpawnSlots()
    {
        CleanupEnemies();

        if (!spawnerEnabled || isPaused)
            return;

        if (isSpawning)
            return;

        if (enemyTypes == null || enemyTypes.Count == 0)
        {
            LogWarning("Enemy Types is empty.");
            return;
        }

        if (activeEnemies.Count >= maxConcurrentEnemies)
            return;

        if (maxTotalSpawns > 0 &&
            totalSpawned >= maxTotalSpawns)
            return;

        StartCoroutine(SpawnRoutine());
    }


    private IEnumerator SpawnRoutine()
    {
        isSpawning = true;

        // =====================================================
        // SELECT ENEMY
        // =====================================================

        EnemyDefinition definition =
            PickWeightedEnemy();

        if (definition == null)
        {
            LogWarning("No valid EnemyDefinition found.");

            isSpawning = false;
            yield break;
        }


        // =====================================================
        // FIND POSITION
        // =====================================================

        if (!TryGetSpawnPosition(out Vector3 spawnPosition))
        {
            LogWarning(
                "Could not find valid spawn position. " +
                "Try disabling Check Obstacles or increasing " +
                "Ground Check Height/Distance."
            );

            isSpawning = false;
            yield break;
        }


        // =====================================================
        // ROTATION
        // =====================================================

        Quaternion rotation =
            randomRotation
                ? Quaternion.Euler(
                    0f,
                    Random.Range(0f, 360f),
                    0f)
                : transform.rotation;


        // =====================================================
        // EFFECT
        // =====================================================

        if (spawnEffectPrefab != null)
        {
            Instantiate(
                spawnEffectPrefab,
                spawnPosition,
                Quaternion.identity
            );
        }


        yield return null;


        // =====================================================
        // CREATE ENEMY
        // =====================================================

        GameObject enemy;

        if (definition.modelPrefab != null)
        {
            enemy = Instantiate(
                definition.modelPrefab,
                spawnPosition,
                rotation
            );
        }
        else
        {
            enemy =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            enemy.transform.SetPositionAndRotation(
                spawnPosition,
                rotation
            );

            enemy.transform.localScale =
                new Vector3(
                    0.8f,
                    1.2f,
                    0.8f
                );
        }


        enemy.name =
            $"{definition.displayName}_{Random.Range(1000, 9999)}";


        // =====================================================
        // COMPONENTS
        // =====================================================

        if (!enemy.TryGetComponent<CharacterController>(
                out _))
        {
            enemy.AddComponent<CharacterController>();
        }

        if (!enemy.TryGetComponent<Damageable>(
                out _))
        {
            enemy.AddComponent<Damageable>();
        }

        if (!enemy.TryGetComponent<EnemyController>(
                out EnemyController controller))
        {
            controller =
                enemy.AddComponent<EnemyController>();
        }


        // =====================================================
        // STATS
        // =====================================================

        var stats =
            definition.GetRandomizedStats();

        controller.ConfigureWithStats(
            health: stats.health,
            damage: stats.dmg,
            patrolSpeed: stats.pSpeed,
            chaseSpeed: stats.cSpeed,
            detectionRange: stats.detRange,
            attackRange: stats.atkRange,
            attackCooldown: stats.atkCd
        );


        // =====================================================
        // FALLBACK COLOR
        // =====================================================

        if (definition.modelPrefab == null)
        {
            Renderer renderer =
                enemy.GetComponentInChildren<Renderer>();

            if (renderer != null)
            {
                renderer.material.color =
                    definition.fallbackColor;
            }
        }


        // =====================================================
        // REGISTER
        // =====================================================

        activeEnemies.Add(enemy);

        totalSpawned++;

        Log(
            $"Spawned {enemy.name}. " +
            $"Active: {activeEnemies.Count}/{maxConcurrentEnemies}"
        );

        isSpawning = false;


        // =====================================================
        // MONITOR
        // =====================================================

        StartCoroutine(
            MonitorEnemy(enemy)
        );


        yield return null;

        FillSpawnSlots();
    }


    // =========================================================
    // MONITOR
    // =========================================================

    private IEnumerator MonitorEnemy(GameObject enemy)
    {
        Vector3 lastPosition =
            enemy != null
                ? enemy.transform.position
                : transform.position;

        while (enemy != null)
        {
            if (!enemy.activeInHierarchy)
                break;

            lastPosition =
                enemy.transform.position;

            yield return new WaitForSeconds(0.25f);
        }


        activeEnemies.Remove(enemy);

        if (enemy == null)
        {
            totalKilled++;

            Log(
                $"Enemy destroyed. " +
                $"Total killed: {totalKilled}"
            );
        }


        if (deathEffectPrefab != null)
        {
            Instantiate(
                deathEffectPrefab,
                lastPosition,
                Quaternion.identity
            );
        }


        if (!autoRespawn)
            yield break;

        if (!spawnerEnabled || isPaused)
            yield break;


        yield return new WaitForSeconds(
            respawnDelay
        );


        FillSpawnSlots();
    }


    // =========================================================
    // POSITION
    // =========================================================

    private bool TryGetSpawnPosition(
        out Vector3 result)
    {
        /*
         * Сначала пробуем случайные точки.
         */

        for (
            int i = 0;
            i < maxSpawnAttempts;
            i++)
        {
            Vector3 candidate =
                GetCandidatePosition();


            // -------------------------------------------------
            // GROUND
            // -------------------------------------------------

            if (snapToGround)
            {
                if (!TryFindGround(
                        candidate,
                        out candidate))
                {
                    LogDebug(
                        $"Attempt {i + 1}: ground not found."
                    );

                    continue;
                }
            }


            // -------------------------------------------------
            // PLAYER DISTANCE
            // -------------------------------------------------

            if (player != null)
            {
                float playerDistance =
                    Vector3.Distance(
                        candidate,
                        player.position
                    );

                if (
                    playerDistance <
                    minimumPlayerDistance)
                {
                    LogDebug(
                        $"Attempt {i + 1}: too close to player."
                    );

                    continue;
                }

                if (
                    requirePlayerNearby &&
                    playerDistance >
                    deactivationDistance)
                {
                    LogDebug(
                        $"Attempt {i + 1}: too far from player."
                    );

                    continue;
                }
            }


            // -------------------------------------------------
            // OBSTACLE
            // -------------------------------------------------

            if (checkObstacles &&
                obstacleLayer.value != 0)
            {
                /*
                 * ВАЖНО:
                 *
                 * Ground Layer исключается из obstacle mask.
                 *
                 * Поэтому даже если пользователь случайно
                 * поставит Everything, Terrain не станет
                 * препятствием.
                 */

                int safeMask =
                    obstacleLayer.value &
                    ~groundLayer.value;

                if (safeMask != 0)
                {
                    bool blocked =
                        Physics.CheckSphere(
                            candidate,
                            obstacleCheckRadius,
                            safeMask,
                            QueryTriggerInteraction.Ignore
                        );

                    if (blocked)
                    {
                        LogDebug(
                            $"Attempt {i + 1}: obstacle."
                        );

                        continue;
                    }
                }
            }


            // -------------------------------------------------
            // SUCCESS
            // -------------------------------------------------

            result =
                candidate +
                Vector3.up *
                groundOffset;

            if (drawSpawnPoint)
            {
                Debug.DrawRay(
                    result,
                    Vector3.up * 3f,
                    Color.green,
                    3f
                );
            }

            Log(
                $"Valid spawn position: {result}"
            );

            return true;
        }


        /*
         * FALLBACK
         *
         * Если Ground Layer настроен неправильно,
         * не блокируем игру полностью.
         */

        if (spawnMode == SpawnMode.FixedPoint)
        {
            result =
                transform.position +
                Vector3.up *
                groundOffset;

            LogWarning(
                "Using fallback fixed spawn position."
            );

            return true;
        }


        result =
            transform.position +
            Vector3.up *
            groundOffset;

        LogWarning(
            "Using fallback spawner position."
        );

        return true;
    }


    // =========================================================
    // CANDIDATE
    // =========================================================

    private Vector3 GetCandidatePosition()
    {
        switch (spawnMode)
        {
            case SpawnMode.FixedPoint:

                return transform.position;


            case SpawnMode.RandomRadius:
            {
                Vector2 offset =
                    Random.insideUnitCircle *
                    spawnRadius;

                return transform.position +
                       new Vector3(
                           offset.x,
                           0f,
                           offset.y
                       );
            }


            case SpawnMode.AroundPlayer:
            {
                if (player != null)
                {
                    Vector2 offset =
                        Random.insideUnitCircle *
                        spawnRadius;

                    return player.position +
                           new Vector3(
                               offset.x,
                               0f,
                               offset.y
                           );
                }

                return transform.position;
            }
        }

        return transform.position;
    }


    // =========================================================
    // GROUND
    // =========================================================

    private bool TryFindGround(
        Vector3 position,
        out Vector3 groundPosition)
    {
        Vector3 rayStart =
            position +
            Vector3.up *
            groundCheckHeight;


        if (Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                groundCheckDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore))
        {
            groundPosition =
                hit.point;

            return true;
        }


        groundPosition =
            position;

        return false;
    }


    // =========================================================
    // PICK ENEMY
    // =========================================================

    private EnemyDefinition PickWeightedEnemy()
    {
        float totalWeight = 0f;

        foreach (
            EnemyDefinition definition
            in enemyTypes)
        {
            if (definition == null)
                continue;

            if (definition.spawnWeight <= 0f)
                continue;

            totalWeight +=
                definition.spawnWeight;
        }


        if (totalWeight <= 0f)
            return null;


        float randomValue =
            Random.Range(
                0f,
                totalWeight
            );

        float cumulative = 0f;


        foreach (
            EnemyDefinition definition
            in enemyTypes)
        {
            if (definition == null)
                continue;

            if (definition.spawnWeight <= 0f)
                continue;

            cumulative +=
                definition.spawnWeight;

            if (randomValue <= cumulative)
                return definition;
        }


        return null;
    }


    // =========================================================
    // CAN SPAWN
    // =========================================================

    private bool CanSpawn()
    {
        CleanupEnemies();

        if (!spawnerEnabled)
            return false;

        if (isPaused)
            return false;

        if (isSpawning)
            return false;

        if (enemyTypes == null ||
            enemyTypes.Count == 0)
            return false;

        if (
            activeEnemies.Count >=
            maxConcurrentEnemies)
            return false;

        if (
            maxTotalSpawns > 0 &&
            totalSpawned >= maxTotalSpawns)
            return false;


        if (requirePlayerNearby)
        {
            FindPlayer();

            if (player == null)
                return false;

            float distance =
                Vector3.Distance(
                    transform.position,
                    player.position
                );

            if (distance >
                activationDistance)
            {
                return false;
            }
        }


        return true;
    }


    // =========================================================
    // PLAYER
    // =========================================================

    private void FindPlayer()
    {
        if (player != null)
            return;

        GameObject obj =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (obj != null)
        {
            player =
                obj.transform;
        }
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void CleanupEnemies()
    {
        activeEnemies.RemoveAll(
            enemy => enemy == null
        );
    }


    // =========================================================
    // PUBLIC CONTROL
    // =========================================================

    public void EnableSpawner()
    {
        spawnerEnabled = true;

        FillSpawnSlots();
    }


    public void DisableSpawner()
    {
        spawnerEnabled = false;
    }


    public void PauseSpawner()
    {
        isPaused = true;
    }


    public void ResumeSpawner()
    {
        isPaused = false;

        FillSpawnSlots();
    }


    // =========================================================
    // CLEAR
    // =========================================================

    [ContextMenu("Clear All Enemies")]
    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        activeEnemies.Clear();

        Log("All enemies cleared.");
    }


    [ContextMenu("Spawn Enemy Now")]
    private void SpawnEnemyNow()
    {
        SpawnEnemy();
    }


    [ContextMenu("Fill Spawn Slots")]
    private void FillSpawnSlotsNow()
    {
        FillSpawnSlots();
    }


    [ContextMenu("Reset Statistics")]
    private void ResetStatistics()
    {
        totalSpawned = 0;
        totalKilled = 0;
    }


    // =========================================================
    // GETTERS
    // =========================================================

    public int ActiveEnemyCount
    {
        get
        {
            CleanupEnemies();
            return activeEnemies.Count;
        }
    }

    public int TotalSpawned => totalSpawned;

    public int TotalKilled => totalKilled;

    public bool IsPaused => isPaused;

    public bool IsSpawnerEnabled =>
        spawnerEnabled;


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (showSpawnRadius)
        {
            Gizmos.color =
                new Color(
                    1f,
                    0.3f,
                    0.3f,
                    0.2f
                );

            Gizmos.DrawSphere(
                transform.position,
                spawnRadius
            );

            Gizmos.color =
                new Color(
                    1f,
                    0.3f,
                    0.3f,
                    0.9f
                );

            Gizmos.DrawWireSphere(
                transform.position,
                spawnRadius
            );
        }


        if (
            requirePlayerNearby &&
            showActivationRadius)
        {
            Gizmos.color =
                new Color(
                    0.2f,
                    1f,
                    0.2f,
                    0.8f
                );

            Gizmos.DrawWireSphere(
                transform.position,
                activationDistance
            );
        }


        if (
            requirePlayerNearby &&
            showDeactivationRadius)
        {
            Gizmos.color =
                new Color(
                    1f,
                    0.8f,
                    0.2f,
                    0.8f
                );

            Gizmos.DrawWireSphere(
                transform.position,
                deactivationDistance
            );
        }


        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            0.3f
        );
    }


    // =========================================================
    // LOG
    // =========================================================

    private void Log(string message)
    {
        if (!debugLogs)
            return;

        Debug.Log(
            $"[EnemySpawner] {gameObject.name}: {message}",
            this
        );
    }


    private void LogDebug(string message)
    {
        if (!debugLogs)
            return;

        Debug.Log(
            $"[EnemySpawner] {gameObject.name}: {message}",
            this
        );
    }


    private void LogWarning(string message)
    {
        Debug.LogWarning(
            $"[EnemySpawner] {gameObject.name}: {message}",
            this
        );
    }
}