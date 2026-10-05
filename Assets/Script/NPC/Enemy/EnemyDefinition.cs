using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Game/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [Header("Identity")]
    public string enemyId = "enemy";
    public string displayName = "Enemy";
    public GameObject modelPrefab;
    public Color fallbackColor = Color.red;
    [Min(0.01f)] public float spawnWeight = 1f;

    [Header("Stats - Base Values")]
    [Min(1f)] public float maxHealth = 100f;
    [Min(1f)] public float damage = 12f;
    [Min(0.1f)] public float patrolSpeed = 1.6f;
    [Min(0.1f)] public float chaseSpeed = 3.2f;
    [Min(0.1f)] public float detectionRange = 9f;
    [Min(0.1f)] public float attackRange = 1.7f;
    [Min(0.1f)] public float attackCooldown = 1.1f;

    [Header("Stats - Randomization Range")]
    [Tooltip("± random range added to base maxHealth. Example: base=100, range=20 => 80-120")]
    public float healthRandomRange = 20f;
    [Tooltip("± random range added to base damage")]
    public float damageRandomRange = 5f;
    [Tooltip("± random range added to base patrolSpeed")]
    public float patrolSpeedRandomRange = 0.3f;
    [Tooltip("± random range added to base chaseSpeed")]
    public float chaseSpeedRandomRange = 0.5f;
    [Tooltip("± random range added to base detectionRange")]
    public float detectionRangeRandomRange = 2f;
    [Tooltip("± random range added to base attackRange")]
    public float attackRangeRandomRange = 0.3f;
    [Tooltip("± random range added to base attackCooldown")]
    public float attackCooldownRandomRange = 0.3f;

    /// <summary>
    /// Creates a randomized stat set based on base values + random ranges.
    /// </summary>
    public (float health, float dmg, float pSpeed, float cSpeed, float detRange, float atkRange, float atkCd) GetRandomizedStats()
    {
        return (
            health: maxHealth + Random.Range(-healthRandomRange, healthRandomRange),
            dmg: damage + Random.Range(-damageRandomRange, damageRandomRange),
            pSpeed: patrolSpeed + Random.Range(-patrolSpeedRandomRange, patrolSpeedRandomRange),
            cSpeed: chaseSpeed + Random.Range(-chaseSpeedRandomRange, chaseSpeedRandomRange),
            detRange: detectionRange + Random.Range(-detectionRangeRandomRange, detectionRangeRandomRange),
            atkRange: attackRange + Random.Range(-attackRangeRandomRange, attackRangeRandomRange),
            atkCd: attackCooldown + Random.Range(-attackCooldownRandomRange, attackCooldownRandomRange)
        );
    }
}