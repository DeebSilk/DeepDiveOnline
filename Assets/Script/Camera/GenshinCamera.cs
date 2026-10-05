using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class GenshinCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera")]
    public float distance = 5f;
    public float sensitivity = 100f;
    public float minY = -30f;
    public float maxY = 70f;
    public float heightOffset = 1.5f;
    public float crouchHeightOffset = 0.9f;
    public float firstPersonCrouchHeight = 1.2f;
    public float runDistance = 6f;
    public float cameraDistanceSmoothSpeed = 8f;
    public float sprintCameraSmoothTime = 0.2f;
    public float enclosedSpaceZoomScale = 0.8f;
    public float enclosedSpaceStartDistance = 4.5f;
    public float corridorCheckRadius = 0.8f;

    [Header("Camera Clip Indicator")]
    public bool showClipIndicator = true;
    public GameObject clipIndicatorObject;
    public Color clipIndicatorColor = Color.red;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;
    public float cameraCollisionRadius = 0.3f;
    public float cameraCollisionOffset = 0.2f;
    public float minCollisionDistance = 0.8f;
    public float minFollowDistance = 2.2f;

    [Header("First Person")]
    public bool enableFirstPersonToggle = true;

    public float firstPersonHeight = 1.7f;
    public float firstPersonPitch = 20f;

    public bool isFirstPerson = false;

    public float transitionDuration = 0.25f;

    [Header("First Person Anchor")]
    public bool useFirstPersonAnchor = true;
    public Transform firstPersonAnchor;

    public float firstPersonForwardOffset = 0.15f;

    public bool smoothFirstPersonCamera = true;
    public float firstPersonPositionSmoothTime = 0.08f;
    public float firstPersonRotationSmoothSpeed = 25f;

    // =========================================================
    // PRIVATE
    // =========================================================

    private float rotationX;
    private float rotationY;

    private Vector2 lookInput;

    private PlayerControls controls;
    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;

    private float currentDistance;
    private float distanceVelocity;

    private bool isCameraClippedByCorridor;

    private bool transitionActive;
    private float transitionTimer;

    private Vector3 transitionFromPosition;
    private Quaternion transitionFromRotation;

    private Vector3 firstPersonVelocity;

    private Transform autoHeadAnchor;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        controls = new PlayerControls();

        controls.Player.Look.performed += OnLookPerformed;
        controls.Player.Look.canceled += OnLookCanceled;
    }

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning(
                "[GenshinCamera] Target is not assigned.",
                this
            );

            return;
        }

        playerMovement =
            target.GetComponent<PlayerMovement>();

        playerCombat =
            target.GetComponent<PlayerCombat>();

        FindAutoHeadAnchor();

        currentDistance = distance;

        Vector3 angles =
            transform.eulerAngles;

        rotationX = angles.y;
        rotationY = angles.x;

        if (GameSettings.Instance != null)
        {
            ApplySensitivity(
                GameSettings.CurrentSensitivity
            );
        }
        else
        {
            ApplySensitivity(
                PlayerPrefs.GetFloat(
                    "Sensitivity",
                    sensitivity
                )
            );
        }
    }

    private void OnEnable()
    {
        controls?.Enable();
    }

    private void OnDisable()
    {
        controls?.Disable();

        lookInput = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (controls == null)
            return;

        controls.Player.Look.performed -=
            OnLookPerformed;

        controls.Player.Look.canceled -=
            OnLookCanceled;

        controls.Dispose();
        controls = null;
    }

    // =========================================================
    // INPUT
    // =========================================================

    private void OnLookPerformed(
        InputAction.CallbackContext context
    )
    {
        lookInput =
            context.ReadValue<Vector2>();
    }

    private void OnLookCanceled(
        InputAction.CallbackContext context
    )
    {
        lookInput = Vector2.zero;
    }

    // =========================================================
    // CAMERA UPDATE
    // =========================================================

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (
            GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentState !=
            GameState.Gameplay
        )
        {
            return;
        }

        if (
            DialogueManager.Instance != null &&
            DialogueManager.Instance.IsDialogueActive
        )
        {
            return;
        }

        // -----------------------------------------------------
        // FPS TOGGLE
        // -----------------------------------------------------

        if (
            enableFirstPersonToggle &&
            Keyboard.current != null &&
            Keyboard.current.zKey.wasPressedThisFrame
        )
        {
            ToggleFirstPerson();
        }

        // -----------------------------------------------------
        // CAMERA LOOK
        // -----------------------------------------------------

        rotationX +=
            lookInput.x *
            sensitivity *
            Time.deltaTime;

        rotationY -=
            lookInput.y *
            sensitivity *
            Time.deltaTime;

        rotationY =
            Mathf.Clamp(
                rotationY,
                minY,
                maxY
            );

        Vector3 desiredPosition;
        Quaternion desiredRotation;

        // =====================================================
        // FIRST PERSON
        // =====================================================

        if (isFirstPerson)
        {
            Quaternion cameraRotation =
                Quaternion.Euler(
                    rotationY,
                    rotationX,
                    0f
                );

            Vector3 headPosition =
                GetFirstPersonAnchorPosition();

            Vector3 forwardOffset =
                cameraRotation *
                Vector3.forward *
                firstPersonForwardOffset;

            desiredPosition =
                headPosition +
                forwardOffset;

            desiredPosition =
                ResolveFirstPersonCollision(
                    headPosition,
                    desiredPosition
                );

            desiredRotation =
                cameraRotation;

            // -------------------------------------------------
            // IMPORTANT
            //
            // PlayerMovement already uses Camera.main.forward
            // and Camera.main.right.
            //
            // Therefore the Player must follow the horizontal
            // camera rotation exactly as before.
            // -------------------------------------------------

            target.rotation =
                Quaternion.Euler(
                    0f,
                    rotationX,
                    0f
                );
        }

        // =====================================================
        // THIRD PERSON
        // =====================================================

        else
        {
            float targetDistance =
                GetTargetDistance();

            currentDistance =
                Mathf.SmoothDamp(
                    currentDistance,
                    targetDistance,
                    ref distanceVelocity,
                    sprintCameraSmoothTime,
                    Mathf.Infinity,
                    Time.deltaTime
                );

            Quaternion rotation =
                Quaternion.Euler(
                    rotationY,
                    rotationX,
                    0f
                );

            Vector3 offset =
                rotation *
                Vector3.forward *
                currentDistance;

            Vector3 targetPivot =
                target.position +
                Vector3.up *
                GetCurrentHeightOffset();

            desiredPosition =
                targetPivot -
                offset;

            desiredPosition =
                ResolveCameraCollision(
                    targetPivot,
                    desiredPosition
                );

            desiredPosition =
                AdjustForNarrowCorridors(
                    targetPivot,
                    desiredPosition,
                    -offset.normalized,
                    out bool corridorClipped
                );

            isCameraClippedByCorridor =
                corridorClipped;

            if (clipIndicatorObject != null)
            {
                clipIndicatorObject.SetActive(
                    showClipIndicator &&
                    isCameraClippedByCorridor
                );
            }

            desiredRotation =
                Quaternion.LookRotation(
                    targetPivot -
                    desiredPosition
                );
        }

        // =====================================================
        // COMBAT STATE
        // =====================================================

        if (playerCombat != null)
        {
            playerCombat.IsFirstPersonModeActive =
                isFirstPerson;
        }

        // =====================================================
        // TRANSITION
        // =====================================================

        if (transitionActive)
        {
            transitionTimer +=
                Time.deltaTime;

            float t =
                transitionDuration > 0.0001f
                    ? Mathf.Clamp01(
                        transitionTimer /
                        transitionDuration
                    )
                    : 1f;

            transform.position =
                Vector3.Lerp(
                    transitionFromPosition,
                    desiredPosition,
                    t
                );

            transform.rotation =
                Quaternion.Slerp(
                    transitionFromRotation,
                    desiredRotation,
                    t
                );

            if (t >= 1f)
            {
                transitionActive = false;
            }
        }
        else
        {
            if (
                isFirstPerson &&
                smoothFirstPersonCamera
            )
            {
                transform.position =
                    Vector3.SmoothDamp(
                        transform.position,
                        desiredPosition,
                        ref firstPersonVelocity,
                        firstPersonPositionSmoothTime
                    );

                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        desiredRotation,
                        firstPersonRotationSmoothSpeed *
                        Time.deltaTime
                    );
            }
            else
            {
                transform.position =
                    desiredPosition;

                transform.rotation =
                    desiredRotation;
            }
        }
    }

    // =========================================================
    // FIRST PERSON
    // =========================================================

    private void ToggleFirstPerson()
    {
        isFirstPerson =
            !isFirstPerson;

        transitionActive = true;
        transitionTimer = 0f;

        transitionFromPosition =
            transform.position;

        transitionFromRotation =
            transform.rotation;

        if (isFirstPerson)
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;

            rotationY =
                Mathf.Clamp(
                    rotationY,
                    minY,
                    maxY
                );

            // НЕ скрываем модель здесь.
            //
            // CharacterVisualController отвечает
            // за визуальное состояние персонажа.
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;
        }

        if (playerCombat != null)
        {
            playerCombat.IsFirstPersonModeActive =
                isFirstPerson;
        }

        Debug.Log(
            $"[GenshinCamera] First Person: {isFirstPerson}"
        );
    }

    // =========================================================
    // FIRST PERSON ANCHOR
    // =========================================================

    private void FindAutoHeadAnchor()
    {
        autoHeadAnchor = null;

        if (target == null)
            return;

        if (firstPersonAnchor != null)
            return;

        Transform[] transforms =
            target.GetComponentsInChildren<Transform>(
                true
            );

        // -----------------------------------------------------
        // Сначала ищем голову
        // -----------------------------------------------------

        foreach (
            Transform child
            in transforms
        )
        {
            string name =
                child.name.ToLowerInvariant();

            if (
                name.Contains("head") ||
                name.Contains("eye")
            )
            {
                if (
                    child.position.y >
                    target.position.y + 0.3f
                )
                {
                    autoHeadAnchor =
                        child;

                    Debug.Log(
                        $"[GenshinCamera] Auto head anchor: {child.name}"
                    );

                    return;
                }
            }
        }

        // -----------------------------------------------------
        // Fallback
        // -----------------------------------------------------

        foreach (
            Transform child
            in transforms
        )
        {
            string name =
                child.name.ToLowerInvariant();

            if (
                name.Contains("neck") ||
                name.Contains("spine") ||
                name.Contains("chest")
            )
            {
                if (
                    child.position.y >
                    target.position.y + 0.3f
                )
                {
                    autoHeadAnchor =
                        child;

                    Debug.Log(
                        $"[GenshinCamera] Fallback anchor: {child.name}"
                    );

                    return;
                }
            }
        }

        Debug.Log(
            "[GenshinCamera] No head anchor found. Using height offset."
        );
    }

    private Vector3 GetFirstPersonAnchorPosition()
    {
        float currentHeight =
            playerMovement != null &&
            playerMovement.IsCrouching
                ? firstPersonCrouchHeight
                : firstPersonHeight;

        // -----------------------------------------------------
        // Explicit anchor
        // -----------------------------------------------------

        if (
            useFirstPersonAnchor &&
            firstPersonAnchor != null
        )
        {
            if (
                playerMovement != null &&
                playerMovement.IsCrouching
            )
            {
                return firstPersonAnchor.position -
                       Vector3.up *
                       (
                           firstPersonHeight -
                           firstPersonCrouchHeight
                       );
            }

            return firstPersonAnchor.position;
        }

        // -----------------------------------------------------
        // Automatically found head
        // -----------------------------------------------------

        if (
            useFirstPersonAnchor &&
            autoHeadAnchor != null
        )
        {
            if (
                playerMovement != null &&
                playerMovement.IsCrouching
            )
            {
                return autoHeadAnchor.position -
                       Vector3.up *
                       (
                           firstPersonHeight -
                           firstPersonCrouchHeight
                       );
            }

            return autoHeadAnchor.position;
        }

        // -----------------------------------------------------
        // Fallback
        // -----------------------------------------------------

        return target.position +
               Vector3.up *
               currentHeight;
    }

    // =========================================================
    // THIRD PERSON HEIGHT
    // =========================================================

    private float GetCurrentHeightOffset()
    {
        if (
            playerMovement != null &&
            playerMovement.IsCrouching
        )
        {
            return crouchHeightOffset;
        }

        return heightOffset;
    }

    // =========================================================
    // DISTANCE
    // =========================================================

    private float GetTargetDistance()
    {
        float targetDistance =
            playerMovement != null &&
            playerMovement.IsSprinting
                ? runDistance
                : distance;

        if (playerMovement == null)
            return targetDistance;

        Vector3 pivot =
            target.position +
            Vector3.up *
            GetCurrentHeightOffset();

        float clearance =
            GetCorridorClearance(
                pivot,
                transform.forward
            );

        if (
            clearance <
            enclosedSpaceStartDistance
        )
        {
            float blend =
                1f -
                Mathf.InverseLerp(
                    minFollowDistance,
                    enclosedSpaceStartDistance,
                    clearance
                );

            float zoom =
                Mathf.Lerp(
                    1f,
                    enclosedSpaceZoomScale,
                    blend
                );

            return Mathf.Max(
                targetDistance * zoom,
                minFollowDistance
            );
        }

        return targetDistance;
    }

    // =========================================================
    // CAMERA COLLISION
    // =========================================================

    private Vector3 ResolveCameraCollision(
        Vector3 fromPosition,
        Vector3 desiredPosition
    )
    {
        Vector3 direction =
            desiredPosition -
            fromPosition;

        float cameraDistance =
            direction.magnitude;

        if (cameraDistance <= 0.01f)
            return desiredPosition;

        direction.Normalize();

        RaycastHit[] hits =
            Physics.SphereCastAll(
                fromPosition,
                cameraCollisionRadius,
                direction,
                cameraDistance,
                collisionMask,
                QueryTriggerInteraction.Ignore
            );

        float closestDistance =
            cameraDistance;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (
                IsColliderPartOfTarget(
                    hit.collider
                )
            )
            {
                continue;
            }

            closestDistance =
                Mathf.Min(
                    closestDistance,
                    hit.distance
                );
        }

        if (
            closestDistance <
            cameraDistance
        )
        {
            float safeDistance =
                Mathf.Max(
                    closestDistance -
                    cameraCollisionOffset,
                    minCollisionDistance
                );

            return
                fromPosition +
                direction *
                safeDistance;
        }

        return desiredPosition;
    }

    // =========================================================
    // FIRST PERSON COLLISION
    // =========================================================

    private Vector3 ResolveFirstPersonCollision(
        Vector3 fromPosition,
        Vector3 desiredPosition
    )
    {
        Vector3 direction =
            desiredPosition -
            fromPosition;

        float cameraDistance =
            direction.magnitude;

        if (cameraDistance <= 0.01f)
            return desiredPosition;

        direction.Normalize();

        if (
            Physics.SphereCast(
                fromPosition,
                cameraCollisionRadius,
                direction,
                out RaycastHit hit,
                cameraDistance,
                collisionMask,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            if (
                hit.collider != null &&
                IsColliderPartOfTarget(
                    hit.collider
                )
            )
            {
                return desiredPosition;
            }

            float safeDistance =
                Mathf.Max(
                    hit.distance -
                    cameraCollisionOffset,
                    0f
                );

            return
                fromPosition +
                direction *
                safeDistance;
        }

        return desiredPosition;
    }

    // =========================================================
    // TARGET COLLISION
    // =========================================================

    private bool IsColliderPartOfTarget(
        Collider collider
    )
    {
        if (target == null)
            return false;

        if (collider == null)
            return false;

        return
            collider.transform == target ||
            collider.transform.IsChildOf(target);
    }

    // =========================================================
    // CORRIDOR
    // =========================================================

    private Vector3 AdjustForNarrowCorridors(
        Vector3 pivot,
        Vector3 desiredPosition,
        Vector3 direction,
        out bool corridorClipped
    )
    {
        float clearance =
            GetCorridorClearance(
                pivot,
                direction
            );

        float currentCameraDistance =
            Vector3.Distance(
                pivot,
                desiredPosition
            );

        if (
            clearance +
            cameraCollisionOffset <
            currentCameraDistance
        )
        {
            float safeDistance =
                Mathf.Max(
                    clearance -
                    cameraCollisionOffset,
                    minCollisionDistance,
                    minFollowDistance
                );

            corridorClipped = true;

            return
                pivot +
                (
                    desiredPosition -
                    pivot
                ).normalized *
                safeDistance;
        }

        corridorClipped = false;

        return desiredPosition;
    }

    private float GetCorridorClearance(
        Vector3 pivot,
        Vector3 direction
    )
    {
        Collider[] hits =
            Physics.OverlapSphere(
                pivot,
                corridorCheckRadius,
                collisionMask,
                QueryTriggerInteraction.Ignore
            );

        float minDistance =
            currentDistance;

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            if (
                IsColliderPartOfTarget(
                    hit
                )
            )
            {
                continue;
            }

            Vector3 closestPoint =
                hit.ClosestPoint(pivot);

            Vector3 delta =
                closestPoint -
                pivot;

            if (
                delta.sqrMagnitude <
                0.01f
            )
            {
                continue;
            }

            float angle =
                Vector3.Angle(
                    direction,
                    delta.normalized
                );

            if (angle > 70f)
                continue;

            if (delta.y < -0.3f)
                continue;

            minDistance =
                Mathf.Min(
                    minDistance,
                    delta.magnitude
                );
        }

        return minDistance;
    }

    // =========================================================
    // SENSITIVITY
    // =========================================================

    public void ApplySensitivity(
        float sensitivityValue
    )
    {
        sensitivity =
            Mathf.Clamp(
                sensitivityValue,
                0.1f,
                10f
            );
    }

    // =========================================================
    // DEBUG UI
    // =========================================================

    private void OnGUI()
    {
        if (
            clipIndicatorObject == null &&
            showClipIndicator &&
            isCameraClippedByCorridor
        )
        {
            GUI.color =
                clipIndicatorColor;

            GUI.Label(
                new Rect(
                    10,
                    10,
                    260,
                    24
                ),
                "Camera clipping in corridor!"
            );

            GUI.color =
                Color.white;
        }
    }
}