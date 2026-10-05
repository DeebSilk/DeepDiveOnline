﻿using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerCombat))]
public class PlayerMovement : MonoBehaviour
{
    [Header("=== MOVEMENT ===")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float crouchSpeed = 1.2f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("=== ACCELERATION & INERTIA ===")]
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 14f;
    [SerializeField, Range(0f, 1f)] private float airControl = 0.7f;

    [Header("=== JUMP & GRAVITY ===")]
    [SerializeField] private float jumpHeight = 1.6f;
    [SerializeField] private float gravity = -15f;
    [SerializeField, Min(1f)] private float fallMultiplier = 1.8f;
    [SerializeField, Min(1f)] private float lowJumpMultiplier = 1.2f;
    [SerializeField] private float groundedStickForce = -5f;

    [Header("=== LANDING ===")]
    [SerializeField] private float landingSmoothTime = 0.12f;

    [Header("=== CROUCH ===")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standingCenterY = 1f;
    [SerializeField] private float crouchCenterY = 0.5f;

    [Header("=== JUMP ASSIST ===")]
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("=== LOCAL PLAYER ===")]
    [SerializeField] private bool isLocalPlayer = true;

    [Header("=== STAMINA ===")]
    [SerializeField, Min(0f)] private float staminaDrainPerSecond = 20f;

    [Header("=== DASH ===")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.2f;

    private CharacterController controller;
    private Animator animator;
    private PlayerControls controls;
    private PlayerCombat playerCombat;
    private PlayerResources playerResources;
    private Transform cameraTransform;

    private Vector2 moveInput;
    private Vector3 velocity;

    private float currentSpeed;
    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool sprint;
    private bool crouch;
    private bool isDashing;
    private bool wasGroundedLastFrame;

    // Lunge атаки полностью контролируется PlayerMovement.
    private bool isAttackLunging;

    private Coroutine dashRoutine;
    private Coroutine landingRoutine;

    public bool IsCrouching => crouch;
    public bool IsSprinting => sprint;
    public bool IsGrounded => controller != null && controller.isGrounded;
    public bool IsDashing => isDashing;
    public bool IsAttackLunging => isAttackLunging;

    private bool IsDialogueActive =>
        DialogueManager.Instance != null &&
        DialogueManager.Instance.IsDialogueActive;

    private bool IsGameplay =>
        GameStateManager.Instance == null ||
        GameStateManager.Instance.CurrentState == GameState.Gameplay;

    private float TargetSpeed =>
        crouch
            ? crouchSpeed
            : sprint
                ? runSpeed
                : walkSpeed;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerCombat = GetComponent<PlayerCombat>();
        playerResources = GetComponent<PlayerResources>();

        if (playerResources == null)
        {
            Debug.LogWarning(
                "[PlayerMovement] PlayerResources not found. " +
                "Stamina drain is disabled.",
                this
            );
        }

        cameraTransform =
            Camera.main != null
                ? Camera.main.transform
                : null;

        if (cameraTransform == null)
        {
            Debug.LogWarning(
                "[PlayerMovement] Main Camera not found.",
                this
            );
        }

        controls = new PlayerControls();

        controls.Player.Move.performed += OnMovePerformed;
        controls.Player.Move.canceled += OnMoveCanceled;

        controls.Player.Sprint.performed += OnSprintPerformed;
        controls.Player.Sprint.canceled += OnSprintCanceled;

        controls.Player.Crouch.performed += OnCrouchPerformed;
        controls.Player.Crouch.canceled += OnCrouchCanceled;

        controls.Player.Jump.performed += OnJumpPerformed;
    }

    private void OnEnable()
    {
        controls?.Enable();
    }

    private void OnDisable()
    {
        controls?.Disable();

        moveInput = Vector2.zero;
        sprint = false;

        StopMovementCoroutines();

        isDashing = false;
        isAttackLunging = false;
    }

    private void Start()
    {
        wasGroundedLastFrame = IsGrounded;
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (!IsGameplay || IsDialogueActive)
        {
            StopInputMovement();
            UpdateAnimation();

            return;
        }

        UpdateTimers();
        UpdateCrouchHeight();

        HandleMovement();
        HandleGravity();

        DetectLanding();

        if (playerCombat != null)
        {
            playerCombat.IsCrouching = crouch;
        }

        HandleOtherActions();
        UpdateAnimation();

        wasGroundedLastFrame = IsGrounded;
    }

    // =========================================================
    // INPUT
    // =========================================================

    private void OnMovePerformed(
        InputAction.CallbackContext context)
    {
        moveInput =
            context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(
        InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private void OnSprintPerformed(
        InputAction.CallbackContext context)
    {
        sprint = true;
    }

    private void OnSprintCanceled(
        InputAction.CallbackContext context)
    {
        sprint = false;
    }

    private void OnCrouchPerformed(
        InputAction.CallbackContext context)
    {
        crouch = true;
    }

    private void OnCrouchCanceled(
        InputAction.CallbackContext context)
    {
        crouch = false;
    }

    private void OnJumpPerformed(
        InputAction.CallbackContext context)
    {
        jumpBufferTimer = jumpBufferTime;
    }

    // =========================================================
    // TIMERS
    // =========================================================

    private void UpdateTimers()
    {
        if (IsGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        jumpBufferTimer -= Time.deltaTime;
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private void HandleMovement()
    {
        // Во время dash или attack lunge
        // CharacterController контролируется
        // отдельной корутиной.

        if (isDashing || isAttackLunging)
            return;

        Vector3 direction =
            GetMoveDirection();

        bool hasInput =
            direction.sqrMagnitude > 0.001f;

        ConsumeSprintStamina(hasInput);

        float combatMultiplier =
            GetCombatSpeedMultiplier();

        float speedTarget =
            hasInput
                ? TargetSpeed * combatMultiplier
                : 0f;

        float blend =
            hasInput
                ? acceleration *
                  (IsGrounded ? 1f : airControl)
                : deceleration;

        currentSpeed =
            Mathf.Lerp(
                currentSpeed,
                speedTarget,
                blend * Time.deltaTime
            );

        if (hasInput)
        {
            RotateTowards(direction);
        }

        controller.Move(
            direction *
            currentSpeed *
            Time.deltaTime
        );

        TryJump();
    }

    private void ConsumeSprintStamina(
        bool hasInput)
    {
        if (!sprint ||
            crouch ||
            !hasInput ||
            playerResources == null ||
            staminaDrainPerSecond <= 0f)
        {
            return;
        }

        bool staminaConsumed =
            playerResources.ConsumeStamina(
                staminaDrainPerSecond * Time.deltaTime
            );

        if (!staminaConsumed)
        {
            sprint = false;
        }
    }

    private Vector3 GetMoveDirection()
    {
        Vector3 input =
            new Vector3(
                moveInput.x,
                0f,
                moveInput.y
            );

        if (input.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        if (cameraTransform == null)
        {
            return input.normalized;
        }

        Vector3 forward =
            cameraTransform.forward;

        Vector3 right =
            cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        if (forward.sqrMagnitude > 0.001f)
            forward.Normalize();

        if (right.sqrMagnitude > 0.001f)
            right.Normalize();

        Vector3 direction =
            forward * input.z +
            right * input.x;

        return direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : Vector3.zero;
    }

    private void RotateTowards(
        Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }

    // =========================================================
    // JUMP
    // =========================================================

    private void TryJump()
    {
        if (jumpBufferTimer <= 0f)
            return;

        if (coyoteTimer <= 0f)
            return;

        if (crouch)
            return;

        if (isDashing || isAttackLunging)
        {
            return;
        }

        if (
            playerCombat != null &&
            playerCombat.IsAttacking
        )
        {
            playerCombat.CancelAttack();
        }

        animator.SetTrigger("Jump");

        velocity.y =
            Mathf.Sqrt(
                jumpHeight *
                -2f *
                gravity
            );

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    // =========================================================
    // GRAVITY
    // =========================================================

    private void HandleGravity()
    {
        if (IsGrounded && velocity.y < 0f)
        {
            velocity.y =
                groundedStickForce;

            return;
        }

        float gravityMultiplier;

        bool jumpHeld =
            controls != null &&
            controls.Player.Jump.IsPressed();

        if (velocity.y < 0f)
        {
            gravityMultiplier =
                fallMultiplier;
        }
        else if (
            velocity.y > 0f &&
            !jumpHeld
        )
        {
            gravityMultiplier =
                lowJumpMultiplier;
        }
        else
        {
            gravityMultiplier = 1f;
        }

        velocity.y +=
            gravity *
            gravityMultiplier *
            Time.deltaTime;

        const float terminalVelocity = -40f;

        if (velocity.y < terminalVelocity)
        {
            velocity.y =
                terminalVelocity;
        }

        controller.Move(
            Vector3.up *
            velocity.y *
            Time.deltaTime
        );
    }

    // =========================================================
    // LANDING
    // =========================================================

    private void DetectLanding()
    {
        if (
            IsGrounded &&
            !wasGroundedLastFrame
        )
        {
            if (landingRoutine != null)
            {
                StopCoroutine(
                    landingRoutine
                );
            }

            landingRoutine =
                StartCoroutine(
                    SmoothLanding()
                );
        }
    }

    private IEnumerator SmoothLanding()
    {
        float elapsed = 0f;

        float startVelocity =
            Mathf.Min(
                velocity.y,
                -2f
            );

        while (
            elapsed <
            landingSmoothTime
        )
        {
            if (!enabled)
                yield break;

            elapsed +=
                Time.deltaTime;

            float t =
                landingSmoothTime > 0.0001f
                    ? elapsed / landingSmoothTime
                    : 1f;

            velocity.y =
                Mathf.Lerp(
                    startVelocity,
                    groundedStickForce,
                    t
                );

            yield return null;
        }

        velocity.y =
            groundedStickForce;

        landingRoutine = null;
    }

    // =========================================================
    // CROUCH
    // =========================================================

    private void UpdateCrouchHeight()
    {
        if (
            !crouch &&
            !CanStandUp()
        )
        {
            crouch = true;
        }

        float targetHeight =
            crouch
                ? crouchHeight
                : standingHeight;

        float targetCenterY =
            crouch
                ? crouchCenterY
                : standingCenterY;

        controller.height =
            Mathf.Lerp(
                controller.height,
                targetHeight,
                Time.deltaTime * 12f
            );

        Vector3 center =
            controller.center;

        center.y =
            Mathf.Lerp(
                center.y,
                targetCenterY,
                Time.deltaTime * 12f
            );

        controller.center =
            center;
    }

    private bool CanStandUp()
    {
        float radius =
            Mathf.Max(
                controller.radius - 0.02f,
                0.01f
            );

        Vector3 center =
            transform.position +
            controller.center;

        float currentTop =
            controller.height * 0.5f;

        float targetTop =
            standingHeight * 0.5f;

        float castDistance =
            targetTop -
            currentTop;

        if (castDistance <= 0f)
            return true;

        return !Physics.SphereCast(
            center,
            radius,
            Vector3.up,
            out _,
            castDistance,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    // =========================================================
    // DASH
    // =========================================================

    private void HandleOtherActions()
    {
        if (
            !CombatInput.GetButtonDown("Dash") ||
            isDashing ||
            isAttackLunging
        )
        {
            return;
        }

        Vector3 dashDirection =
            GetMoveDirection();

        if (dashDirection.sqrMagnitude < 0.01f)
        {
            dashDirection =
                transform.forward;
        }

        if (
            playerCombat != null &&
            playerCombat.IsAttacking
        )
        {
            playerCombat.CancelAttack();
        }

        bool firstPersonBlocked =
            playerCombat != null &&
            playerCombat.IsFirstPersonModeActive &&
            playerCombat.DisableLungeInFirstPerson;

        if (firstPersonBlocked)
        {
            animator.SetTrigger("Dash");
            return;
        }

        if (dashRoutine != null)
        {
            StopCoroutine(
                dashRoutine
            );
        }

        dashRoutine =
            StartCoroutine(
                DashRoutine(
                    dashDirection
                )
            );
    }

    private IEnumerator DashRoutine(
        Vector3 dashDirection)
    {
        isDashing = true;

        animator.SetTrigger("Dash");

        float elapsed = 0f;

        while (
            elapsed <
            dashDuration
        )
        {
            if (
                !enabled ||
                controller == null ||
                !controller.enabled
            )
            {
                break;
            }

            controller.Move(
                dashDirection *
                dashSpeed *
                Time.deltaTime
            );

            elapsed +=
                Time.deltaTime;

            yield return null;
        }

        isDashing = false;
        dashRoutine = null;
    }

    // =========================================================
    // ATTACK LUNGE
    // =========================================================

    public IEnumerator PerformAttackLunge(
        float distance,
        float duration)
    {
        if (!enabled)
            yield break;

        if (
            controller == null ||
            !controller.enabled
        )
        {
            yield break;
        }

        if (
            distance <= 0f ||
            duration <= 0f
        )
        {
            yield break;
        }

        if (isDashing)
            yield break;

        isAttackLunging = true;

        float elapsed = 0f;

        while (
            elapsed <
            duration
        )
        {
            if (
                !enabled ||
                !controller.enabled
            )
            {
                break;
            }

            float frameDistance =
                distance /
                duration *
                Time.deltaTime;

            controller.Move(
                transform.forward *
                frameDistance
            );

            elapsed +=
                Time.deltaTime;

            yield return null;
        }

        isAttackLunging = false;
    }

    // =========================================================
    // COMBAT
    // =========================================================

    private float GetCombatSpeedMultiplier()
    {
        if (playerCombat == null)
            return 1f;

        return playerCombat.MoveSpeedMultiplier;
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        float speedValue = 0f;

        if (moveInput.magnitude > 0.1f)
        {
            speedValue =
                crouch
                    ? 0.2f
                    : sprint
                        ? 1f
                        : 0.5f;
        }

        animator.SetFloat(
            "Speed",
            speedValue,
            0.1f,
            Time.deltaTime
        );

        animator.SetBool(
            "Grounded",
            IsGrounded
        );

        animator.SetBool(
            "Crouching",
            crouch
        );

        animator.SetFloat(
            "VerticalSpeed",
            velocity.y
        );
    }

    // =========================================================
    // STATE
    // =========================================================

    private void StopInputMovement()
    {
        moveInput = Vector2.zero;
        currentSpeed = 0f;
        sprint = false;
    }

    private void StopMovementCoroutines()
    {
        if (dashRoutine != null)
        {
            StopCoroutine(
                dashRoutine
            );

            dashRoutine = null;
        }

        if (landingRoutine != null)
        {
            StopCoroutine(
                landingRoutine
            );

            landingRoutine = null;
        }
    }

    // =========================================================
    // EXTERNAL RESET
    // =========================================================

    public void ResetMovementState()
    {
        StopMovementCoroutines();

        moveInput = Vector2.zero;
        velocity = Vector3.zero;

        currentSpeed = 0f;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;

        sprint = false;
        isDashing = false;
        isAttackLunging = false;

        if (
            controller != null &&
            controller.enabled
        )
        {
            wasGroundedLastFrame =
                controller.isGrounded;
        }
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (controls == null)
            return;

        controls.Player.Move.performed -=
            OnMovePerformed;

        controls.Player.Move.canceled -=
            OnMoveCanceled;

        controls.Player.Sprint.performed -=
            OnSprintPerformed;

        controls.Player.Sprint.canceled -=
            OnSprintCanceled;

        controls.Player.Crouch.performed -=
            OnCrouchPerformed;

        controls.Player.Crouch.canceled -=
            OnCrouchCanceled;

        controls.Player.Jump.performed -=
            OnJumpPerformed;

        controls.Dispose();
        controls = null;
    }
}

