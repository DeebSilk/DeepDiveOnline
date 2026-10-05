using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Damageable))]
public class EnemyController : MonoBehaviour
{
    /// <summary>
    /// Вызывается, когда враг входит или выходит из боя.
    /// true  = вошёл в бой.
    /// false = вышел из боя.
    /// </summary>
    public static event Action<EnemyController, bool> CombatStateChanged;

    private static readonly HashSet<EnemyController> ActiveCombatEnemies =
        new HashSet<EnemyController>();

    public static int ActiveCombatEnemyCount
    {
        get
        {
            CleanupCombatRegistry();
            return ActiveCombatEnemies.Count;
        }
    }

    [Header("Movement")]
    [SerializeField, Min(0f)] private float patrolRadius = 4f;
    [SerializeField, Min(0.1f)] private float patrolSpeed = 1.6f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 3.2f;
    [SerializeField, Min(0.1f)] private float rotationSpeed = 10f;

    [Header("Gravity & Physics")]
    [SerializeField] private float gravity = -15f;
    [SerializeField, Min(1f)] private float fallMultiplier = 1.8f;
    [SerializeField] private float groundedStickForce = -5f;

    [Header("Awareness")]
    [SerializeField, Min(0.1f)] private float detectionRange = 9f;
    [SerializeField, Min(0.1f)] private float loseTargetRange = 13f;
    [SerializeField, Min(0.1f)] private float attackRange = 1.7f;

    [Header("Attack")]
    [SerializeField, Min(1f)] private float damage = 12f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.1f;
    [SerializeField, Min(0f)] private float attackWindup = 0.25f;

    private CharacterController controller;
    private Damageable health;

    private Transform player;

    private Vector3 homePosition;
    private Vector3 patrolTarget;

    private float verticalVelocity;
    private float nextAttackTime;

    private bool hasTarget;
    private bool isDead;
    private bool isInCombat;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        health = GetComponent<Damageable>();

        health.SetDestroyOnDeath(false);
        health.Died += Die;

        homePosition = transform.position;

        PickNewPatrolTarget();
    }

    private void Start()
    {
        FindPlayer();

        if (player == null)
        {
            Debug.LogWarning(
                $"[EnemyController] {gameObject.name} could not find PlayerHealth in scene!",
                this
            );
        }
    }

    private void OnDestroy()
    {
        if (health != null)
            health.Died -= Die;

        ExitCombat();

        ActiveCombatEnemies.Remove(this);
    }

    public void Configure(EnemyDefinition definition)
    {
        if (definition == null)
            return;

        health ??= GetComponent<Damageable>();

        health.SetMaxHealth(
            definition.maxHealth
        );

        damage = definition.damage;
        patrolSpeed = definition.patrolSpeed;
        chaseSpeed = definition.chaseSpeed;
        detectionRange = definition.detectionRange;
        attackRange = definition.attackRange;
        attackCooldown = definition.attackCooldown;
    }

    /// <summary>
    /// Configures the enemy with directly specified
    /// potentially randomized stats.
    /// Used by EnemySpawner after rolling random values.
    /// </summary>
    public void ConfigureWithStats(
        float health,
        float damage,
        float patrolSpeed,
        float chaseSpeed,
        float detectionRange,
        float attackRange,
        float attackCooldown)
    {
        this.health ??= GetComponent<Damageable>();

        this.health.SetMaxHealth(
            Mathf.Max(1f, health)
        );

        this.damage =
            Mathf.Max(1f, damage);

        this.patrolSpeed =
            Mathf.Max(0.1f, patrolSpeed);

        this.chaseSpeed =
            Mathf.Max(0.1f, chaseSpeed);

        this.detectionRange =
            Mathf.Max(0.1f, detectionRange);

        this.attackRange =
            Mathf.Max(0.1f, attackRange);

        this.attackCooldown =
            Mathf.Max(0.1f, attackCooldown);
    }

    private void Update()
    {
        if (isDead)
            return;

        if (player == null)
            FindPlayer();

        UpdateTargetState();

        ApplyGravity();

        if (hasTarget)
            ChaseAndAttack();
        else
            Patrol();
    }

    private void FindPlayer()
    {
        PlayerHealth playerHealth =
            FindAnyObjectByType<PlayerHealth>();

        if (playerHealth != null)
            player = playerHealth.transform;
    }

    private void UpdateTargetState()
    {
        if (player == null)
        {
            hasTarget = false;
            ExitCombat();

            return;
        }

        float distance =
            HorizontalDistanceTo(
                player.position
            );

        if (!hasTarget &&
            distance <= detectionRange)
        {
            hasTarget = true;

            EnterCombat();
        }
        else if (hasTarget &&
                 distance > loseTargetRange)
        {
            hasTarget = false;

            ExitCombat();
        }
    }

    private void EnterCombat()
    {
        if (isDead || isInCombat)
            return;

        isInCombat = true;

        ActiveCombatEnemies.Add(this);

        CombatStateChanged?.Invoke(
            this,
            true
        );
    }

    private void ExitCombat()
    {
        if (!isInCombat)
            return;

        isInCombat = false;

        ActiveCombatEnemies.Remove(this);

        CombatStateChanged?.Invoke(
            this,
            false
        );
    }

    private void Patrol()
    {
        if (HorizontalDistanceTo(
                patrolTarget
            ) <= 0.4f)
        {
            PickNewPatrolTarget();
        }

        MoveTowards(
            patrolTarget,
            patrolSpeed
        );
    }

    private void ChaseAndAttack()
    {
        if (player == null)
            return;

        float distance =
            HorizontalDistanceTo(
                player.position
            );

        if (distance > attackRange)
        {
            MoveTowards(
                player.position,
                chaseSpeed
            );

            return;
        }

        FaceTowards(
            player.position
        );

        if (Time.time >= nextAttackTime)
        {
            StartCoroutine(
                AttackRoutine()
            );
        }
    }

    private IEnumerator AttackRoutine()
    {
        nextAttackTime =
            Time.time + attackCooldown;

        yield return new WaitForSeconds(
            attackWindup
        );

        if (isDead ||
            player == null ||
            HorizontalDistanceTo(
                player.position
            ) > attackRange + 0.25f)
        {
            yield break;
        }

        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(
                damage
            );
        }
    }

    private void MoveTowards(
        Vector3 destination,
        float speed)
    {
        Vector3 direction =
            destination - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        FaceTowards(
            destination
        );

        controller.Move(
            direction.normalized *
            speed *
            Time.deltaTime
        );
    }

    private void FaceTowards(
        Vector3 destination)
    {
        Vector3 direction =
            destination - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity =
                groundedStickForce;

            return;
        }

        float currentGravityMultiplier =
            verticalVelocity < 0f
                ? fallMultiplier
                : 1f;

        verticalVelocity +=
            gravity *
            currentGravityMultiplier *
            Time.deltaTime;

        const float terminalVelocity = -40f;

        if (verticalVelocity <
            terminalVelocity)
        {
            verticalVelocity =
                terminalVelocity;
        }

        controller.Move(
            Vector3.up *
            verticalVelocity *
            Time.deltaTime
        );
    }

    private void PickNewPatrolTarget()
    {
        Vector2 point =
            UnityEngine.Random.insideUnitCircle *
            patrolRadius;

        patrolTarget =
            homePosition +
            new Vector3(
                point.x,
                0f,
                point.y
            );
    }

    private float HorizontalDistanceTo(
        Vector3 point)
    {
        Vector3 offset =
            point - transform.position;

        offset.y = 0f;

        return offset.magnitude;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        hasTarget = false;

        ExitCombat();

        if (controller != null)
            controller.enabled = false;

        StartCoroutine(
            DestroyAfterDeath()
        );
    }

    private IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(
            2f
        );

        Destroy(gameObject);
    }

    private static void CleanupCombatRegistry()
    {
        if (ActiveCombatEnemies.Count == 0)
            return;

        List<EnemyController> invalidEnemies =
            null;

        foreach (EnemyController enemy
                 in ActiveCombatEnemies)
        {
            if (enemy == null ||
                !enemy.isInCombat ||
                enemy.isDead)
            {
                invalidEnemies ??=
                    new List<EnemyController>();

                invalidEnemies.Add(
                    enemy
                );
            }
        }

        if (invalidEnemies == null)
            return;

        foreach (EnemyController enemy
                 in invalidEnemies)
        {
            ActiveCombatEnemies.Remove(
                enemy
            );
        }
    }
}