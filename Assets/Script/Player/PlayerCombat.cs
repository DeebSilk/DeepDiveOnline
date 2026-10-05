using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class PlayerCombat : MonoBehaviour
{
    [Header("=== HITBOX ===")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private LayerMask enemyMask = ~0;

    [Header("=== WEAPON ===")]
    [SerializeField] private Transform weaponEquipPoint;

    [Header("=== DAMAGE ===")]
    [SerializeField] private float baseAttackDamage = 24f;

    [SerializeField]
    private float[] comboDamageMultipliers =
        { 1f, 1.2f, 1.8f };

    [Header("=== TIMING ===")]
    [SerializeField] private float attackCooldown = 0.25f;
    [SerializeField] private float comboWindow = 0.7f;

    [SerializeField]
    private float attackMoveSpeedMultiplier = 0.35f;

    [Header("=== LUNGE / DASH ===")]
    [SerializeField]
    private float[] attackDashForces =
        { 1.2f, 1.2f, 2.8f };

    [SerializeField]
    private float lungeDuration = 0.15f;

    [SerializeField]
    private bool _disableLungeInCrouch = true;

    [SerializeField]
    private bool _disableLungeInFirstPerson = true;

    [Header("=== ATTACK TIMING ===")]
    [SerializeField]
    private float[] attackHitDelays =
        { 0.2f, 0.22f, 0.45f };

    [SerializeField]
    private float[] attackEndDelays =
        { 0.45f, 0.5f, 0.9f };

    [Header("=== ANIMATOR ===")]
    [SerializeField]
    private string attackTrigger = "Attack";

    [SerializeField]
    private string attackIndexParameter = "AttackIndex";

    [Header("=== EFFECTS ===")]
    [SerializeField]
    private ParticleSystem attackParticles;

    private Animator animator;
    private CharacterController controller;
    private PlayerMovement playerMovement;

    private Coroutine attackRoutine;
    private Coroutine lungeRoutine;

    private RuntimeAnimatorController defaultAnimatorController;

    private GameObject currentWeaponInstance;

    private int attackTriggerHash;
    private int attackIndexParameterHash;

    private float nextAttackTime;
    private float comboTimeout;

    private int comboIndex;
    private int activeComboIndex;

    private bool hasAttackedThisStep;
    private bool wasRootMotionEnabled;

    private bool IsDialogueActive =>
        DialogueManager.Instance != null &&
        DialogueManager.Instance.IsDialogueActive;

    private bool IsGameplay =>
        GameStateManager.Instance == null ||
        GameStateManager.Instance.CurrentState == GameState.Gameplay;

    // =========================================================
    // PUBLIC PROPERTIES
    // =========================================================

    public ItemData CurrentEquippedItem
    {
        get;
        private set;
    }

    public bool IsAttacking
    {
        get;
        private set;
    }

    public bool IsWeaponEquipped =>
        currentWeaponInstance != null;

    public bool IsCrouching
    {
        get;
        set;
    }

    public bool DisableLungeInCrouch =>
        _disableLungeInCrouch;

    public bool IsFirstPersonModeActive
    {
        get;
        set;
    }

    public bool DisableLungeInFirstPerson =>
        _disableLungeInFirstPerson;

    public float MoveSpeedMultiplier =>
        IsAttacking
            ? attackMoveSpeedMultiplier
            : 1f;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        EquipmentManager.GetOrCreate();
        InventoryManager.GetOrCreate();

        animator =
            GetComponent<Animator>();

        controller =
            GetComponent<CharacterController>();

        playerMovement =
            GetComponent<PlayerMovement>();

        defaultAnimatorController =
            animator.runtimeAnimatorController;

        if (attackOrigin == null)
        {
            attackOrigin =
                transform;
        }

        attackTriggerHash =
            Animator.StringToHash(
                attackTrigger
            );

        attackIndexParameterHash =
            Animator.StringToHash(
                attackIndexParameter
            );
    }

    private void Update()
    {
        if (!IsGameplay ||
            IsDialogueActive)
        {
            return;
        }

        if (CombatInput.GetButtonDown("Attack"))
        {
            TryAttack();
        }

        if (
            comboIndex > 0 &&
            Time.time > comboTimeout &&
            !IsAttacking
        )
        {
            ResetCombo();
        }
    }

    // =========================================================
    // ATTACK START
    // =========================================================

    private void TryAttack()
    {
        if (IsAttacking)
            return;

        if (Time.time < nextAttackTime)
            return;

        if (!IsWeaponEquipped)
            return;

        int maxCombo =
            comboDamageMultipliers != null &&
            comboDamageMultipliers.Length > 0
                ? comboDamageMultipliers.Length
                : 1;

        if (
            comboIndex == 0 ||
            Time.time > comboTimeout
        )
        {
            comboIndex = 1;
        }
        else
        {
            comboIndex =
                Mathf.Min(
                    comboIndex + 1,
                    maxCombo
                );
        }

        activeComboIndex =
            comboIndex;

        hasAttackedThisStep =
            false;

        comboTimeout =
            Time.time +
            comboWindow;

        nextAttackTime =
            Time.time +
            GetDelayForCombo(
                attackEndDelays,
                comboIndex,
                attackCooldown
            );

        IsAttacking =
            true;

        wasRootMotionEnabled =
            animator.applyRootMotion;

        bool disableLunge =
            (
                IsFirstPersonModeActive &&
                DisableLungeInFirstPerson
            )
            ||
            (
                IsCrouching &&
                DisableLungeInCrouch
            );

        if (disableLunge)
        {
            animator.applyRootMotion =
                false;
        }

        animator.SetInteger(
            attackIndexParameterHash,
            comboIndex
        );

        animator.ResetTrigger(
            attackTriggerHash
        );

        animator.SetTrigger(
            attackTriggerHash
        );

        if (attackRoutine != null)
        {
            StopCoroutine(
                attackRoutine
            );
        }

        attackRoutine =
            StartCoroutine(
                AttackRoutine(
                    activeComboIndex,
                    disableLunge
                )
            );
    }

    // =========================================================
    // ATTACK ROUTINE
    // =========================================================

    private IEnumerator AttackRoutine(
        int index,
        bool disableLunge)
    {
        float hitDelay =
            GetDelayForCombo(
                attackHitDelays,
                index,
                0.25f
            );

        float endDelay =
            GetDelayForCombo(
                attackEndDelays,
                index,
                0.55f
            );

        float dashDistance =
            GetDelayForCombo(
                attackDashForces,
                index,
                1f
            );

        float actualLungeDuration =
            0f;

        if (
            !disableLunge &&
            playerMovement != null &&
            dashDistance > 0f &&
            lungeDuration > 0f
        )
        {
            actualLungeDuration =
                Mathf.Min(
                    lungeDuration,
                    hitDelay
                );

            if (actualLungeDuration > 0f)
            {
                if (lungeRoutine != null)
                {
                    StopCoroutine(
                        lungeRoutine
                    );
                }

                /*
                 * PlayerMovement.PerformAttackLunge()
                 * возвращает IEnumerator.
                 *
                 * Поэтому передаём IEnumerator
                 * напрямую в StartCoroutine().
                 */
                lungeRoutine =
                    StartCoroutine(
                        playerMovement.PerformAttackLunge(
                            dashDistance,
                            actualLungeDuration
                        )
                    );
            }
        }

        if (hitDelay > actualLungeDuration)
        {
            yield return new WaitForSeconds(
                hitDelay -
                actualLungeDuration
            );
        }

        if (
            IsAttacking &&
            !hasAttackedThisStep
        )
        {
            PerformAttack();
        }

        float remaining =
            Mathf.Max(
                endDelay -
                hitDelay,
                0f
            );

        if (remaining > 0f)
        {
            yield return new WaitForSeconds(
                remaining
            );
        }

        EndAttackLogic();
    }

    // =========================================================
    // CANCEL ATTACK
    // =========================================================

    public void CancelAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(
                attackRoutine
            );

            attackRoutine =
                null;
        }

        if (lungeRoutine != null)
        {
            StopCoroutine(
                lungeRoutine
            );

            lungeRoutine =
                null;
        }

        if (playerMovement != null)
        {
            playerMovement.ResetMovementState();
        }

        IsAttacking =
            false;

        hasAttackedThisStep =
            false;

        if (animator != null)
        {
            animator.applyRootMotion =
                wasRootMotionEnabled;

            animator.ResetTrigger(
                attackTriggerHash
            );

            if (animator.isActiveAndEnabled)
            {
                AnimatorStateInfo state =
                    animator.GetCurrentAnimatorStateInfo(0);

                if (!state.IsName("Movement"))
                {
                    animator.CrossFadeInFixedTime(
                        "Movement",
                        0.1f
                    );
                }
            }
        }

        ResetCombo();
    }

    // =========================================================
    // EQUIP ITEM
    // =========================================================

    public void EquipItem(ItemData item)
    {
        TryEquipItem(item);
    }

    public bool TryEquipItem(ItemData item)
    {
        if (item == null)
            return false;

        if (!item.IsWeapon)
            return false;

        if (item.equipPrefab == null)
        {
            Debug.LogWarning(
                $"[PlayerCombat] Item '{item.itemName}' has no equipPrefab assigned.",
                this
            );

            return false;
        }

        if (weaponEquipPoint == null)
        {
            Debug.LogWarning(
                "[PlayerCombat] Weapon Equip Point is not assigned.",
                this
            );

            return false;
        }

        CancelAttack();

        Unequip();

        CurrentEquippedItem =
            item;

        currentWeaponInstance =
            Instantiate(
                item.equipPrefab,
                weaponEquipPoint,
                false
            );

        CompensateWeaponScale(
            currentWeaponInstance.transform,
            item.equipPrefab.transform.localScale
        );

        DisableWeaponPhysics(
            currentWeaponInstance
        );

        ApplyAnimatorOverride(
            item
        );

        return true;
    }

    // =========================================================
    // EQUIP WORLD WEAPON
    // =========================================================

    public void EquipWorldWeapon(
        GameObject weaponInstance,
        ItemData item)
    {
        if (weaponInstance == null)
            return;

        if (weaponEquipPoint == null)
        {
            Debug.LogWarning(
                "[PlayerCombat] Weapon Equip Point is not assigned.",
                this
            );

            return;
        }

        if (
            item == null ||
            !item.IsWeapon
        )
        {
            Debug.LogWarning(
                "[PlayerCombat] World weapon has invalid ItemData.",
                this
            );

            return;
        }

        CancelAttack();

        Unequip();

        CurrentEquippedItem =
            item;

        currentWeaponInstance =
            weaponInstance;

        currentWeaponInstance.transform.SetParent(
            weaponEquipPoint,
            false
        );

        CompensateWeaponScale(
            currentWeaponInstance.transform,
            currentWeaponInstance.transform.localScale
        );

        DisableWeaponPhysics(
            currentWeaponInstance
        );

        ApplyAnimatorOverride(
            item
        );
    }

    // =========================================================
    // UNEQUIP
    // =========================================================

    public void Unequip()
    {
        CancelAttack();

        if (currentWeaponInstance != null)
        {
            Destroy(
                currentWeaponInstance
            );
        }

        currentWeaponInstance =
            null;

        CurrentEquippedItem =
            null;

        RestoreDefaultAnimator();
    }

    // =========================================================
    // WEAPON PHYSICS
    // =========================================================

    private void CompensateWeaponScale(
        Transform weaponTransform,
        Vector3 prefabLocalScale)
    {
        if (weaponTransform == null || weaponEquipPoint == null)
            return;

        Vector3 parentLossyScale = weaponEquipPoint.lossyScale;

        weaponTransform.localScale = new Vector3(
            SafeDivide(prefabLocalScale.x, parentLossyScale.x),
            SafeDivide(prefabLocalScale.y, parentLossyScale.y),
            SafeDivide(prefabLocalScale.z, parentLossyScale.z)
        );
    }

    private static float SafeDivide(float value, float divisor)
    {
        if (Mathf.Abs(divisor) < 0.0001f)
            return value;

        return value / divisor;
    }

    private static void DisableWeaponPhysics(
        GameObject weapon)
    {
        if (weapon == null)
            return;

        Collider[] colliders =
            weapon.GetComponentsInChildren<Collider>(
                true
            );

        foreach (
            Collider weaponCollider
            in colliders
        )
        {
            if (weaponCollider == null)
                continue;

            weaponCollider.enabled =
                false;
        }

        Rigidbody[] rigidbodies =
            weapon.GetComponentsInChildren<Rigidbody>(
                true
            );

        foreach (
            Rigidbody weaponBody
            in rigidbodies
        )
        {
            if (weaponBody == null)
                continue;

            weaponBody.linearVelocity =
                Vector3.zero;

            weaponBody.angularVelocity =
                Vector3.zero;

            weaponBody.isKinematic =
                true;

            weaponBody.useGravity =
                false;
        }
    }

    // =========================================================
    // ANIMATOR
    // =========================================================

    private void ApplyAnimatorOverride(
        ItemData item)
    {
        if (item == null)
            return;

        if (item.animatorOverride == null)
            return;

        if (animator == null)
            return;

        animator.runtimeAnimatorController =
            item.animatorOverride;
    }

    private void RestoreDefaultAnimator()
    {
        if (
            animator != null &&
            defaultAnimatorController != null
        )
        {
            animator.runtimeAnimatorController =
                defaultAnimatorController;
        }
    }

    // =========================================================
    // HIT
    // =========================================================

    public void PerformAttack()
    {
        if (!IsAttacking)
            return;

        if (hasAttackedThisStep)
            return;

        hasAttackedThisStep =
            true;

        float damage =
            GetCurrentAttackDamage() *
            GetDamageMultiplier(
                activeComboIndex
            );

        float currentAttackRange =
            GetCurrentAttackRange();

        if (attackOrigin == null)
        {
            attackOrigin =
                transform;
        }

        Collider[] hits =
            Physics.OverlapSphere(
                attackOrigin.position,
                currentAttackRange,
                enemyMask,
                QueryTriggerInteraction.Ignore
            );

        HashSet<Damageable> damagedTargets =
            new HashSet<Damageable>();

        foreach (
            Collider hit
            in hits
        )
        {
            if (hit == null)
                continue;

            if (
                hit.transform.IsChildOf(
                    transform
                )
            )
            {
                continue;
            }

            Damageable damageable =
                hit.GetComponentInParent<Damageable>();

            if (
                damageable != null &&
                damagedTargets.Add(
                    damageable
                )
            )
            {
                damageable.TakeDamage(
                    damage
                );
            }
        }
    }

    // =========================================================
    // PARTICLES
    // =========================================================

    public void PlayParticle()
    {
        if (attackParticles == null)
            return;

        attackParticles.Play();
    }

    // =========================================================
    // ANIMATION EVENT
    // =========================================================

    public void OnAttackEnd()
    {
        EndAttackLogic();
    }

    private void EndAttackLogic()
    {
        if (!IsAttacking)
            return;

        IsAttacking =
            false;

        hasAttackedThisStep =
            false;

        if (animator != null)
        {
            animator.applyRootMotion =
                wasRootMotionEnabled;
        }

        if (attackRoutine != null)
        {
            StopCoroutine(
                attackRoutine
            );

            attackRoutine =
                null;
        }

        if (lungeRoutine != null)
        {
            StopCoroutine(
                lungeRoutine
            );

            lungeRoutine =
                null;
        }
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    private float GetCurrentAttackDamage()
    {
        if (
            CurrentEquippedItem != null &&
            CurrentEquippedItem.damage > 0f
        )
        {
            return CurrentEquippedItem.damage;
        }

        return baseAttackDamage;
    }

    private float GetCurrentAttackRange()
    {
        if (
            CurrentEquippedItem != null &&
            CurrentEquippedItem.attackRange > 0f
        )
        {
            return CurrentEquippedItem.attackRange;
        }

        return attackRange;
    }

    // =========================================================
    // COMBO
    // =========================================================

    private void ResetCombo()
    {
        comboIndex =
            0;

        activeComboIndex =
            0;

        comboTimeout =
            0f;

        hasAttackedThisStep =
            false;

        if (animator != null)
        {
            animator.SetInteger(
                attackIndexParameterHash,
                0
            );
        }
    }

    private static float GetDelayForCombo(
        float[] values,
        int comboStep,
        float fallback)
    {
        if (
            values == null ||
            values.Length == 0
        )
        {
            return fallback;
        }

        int safeIndex =
            Mathf.Clamp(
                comboStep - 1,
                0,
                values.Length - 1
            );

        return Mathf.Max(
            values[safeIndex],
            0f
        );
    }

    private float GetDamageMultiplier(
        int index)
    {
        if (
            comboDamageMultipliers == null ||
            comboDamageMultipliers.Length == 0
        )
        {
            return 1f;
        }

        int safeIndex =
            Mathf.Clamp(
                index - 1,
                0,
                comboDamageMultipliers.Length - 1
            );

        return Mathf.Max(
            comboDamageMultipliers[safeIndex],
            0f
        );
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Transform origin =
            attackOrigin != null
                ? attackOrigin
                : transform;

        Gizmos.color =
            Color.red;

        Gizmos.DrawWireSphere(
            origin.position,
            GetCurrentAttackRange()
        );
    }
}
