using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private bool isLocalPlayer = true;
    [SerializeField] private Camera cam;

    [Header("Interaction")]
    [SerializeField] private float distance = 3f;
    [SerializeField] private float interactionRadius = 2.5f;
    [SerializeField] private float interactionAngle = 70f;
    [SerializeField] private LayerMask interactionMask = Physics.AllLayers;

    private NPCController currentNPC;
    private ItemPickup nearbyItemPickup;
    private SwordPickup nearbySword;
    private PickupItem nearbyItem;

    private PlayerCombat playerCombat;
    private bool wasDialogueActive;

    private Collider[] nearbyColliders = new Collider[50];

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        playerCombat = GetComponent<PlayerCombat>();
    }

    private void OnValidate()
    {
        if (distance < 0.5f)
            distance = 0.5f;

        if (interactionRadius < 0.5f)
            interactionRadius = 0.5f;

        interactionAngle = Mathf.Clamp(interactionAngle, 10f, 180f);
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        UpdateTarget();

        bool dialogueActive =
            DialogueManager.Instance != null &&
            DialogueManager.Instance.IsDialogueActive;

        if (dialogueActive)
        {
            currentNPC?.HidePrompt();

            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                    DialogueManager.Instance.NextLine();

                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                    DialogueManager.Instance.EndDialogue();
            }

            wasDialogueActive = true;
            return;
        }

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame ||
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (TryPickupNearbyItem())
                return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (currentNPC != null)
            {
                currentNPC.Interact();
                return;
            }
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            DialogueManager.Instance?.NextLine();

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            DialogueManager.Instance?.EndDialogue();

        if (wasDialogueActive && !dialogueActive)
        {
            currentNPC?.HidePrompt();
            currentNPC = null;
        }

        wasDialogueActive = dialogueActive;
    }

    private bool TryPickupNearbyItem()
    {
        if (nearbyItemPickup != null)
        {
            if (nearbyItemPickup.TryPickup(true))
            {
                ClearPickupTargets();
                return true;
            }

            return false;
        }

        if (nearbySword != null)
        {
            SwordPickup sword = nearbySword;

            if (sword != null && sword.TryPickup(this))
            {
                ClearPickupTargets();
                return true;
            }

            return false;
        }

        if (nearbyItem != null)
        {
            PickupItem item = nearbyItem;

            if (item != null && TryPickupLegacyItem(item))
            {
                ClearPickupTargets();
                return true;
            }
        }

        return false;
    }

    private void ClearPickupTargets()
    {
        nearbyItemPickup = null;
        nearbySword = null;
        nearbyItem = null;
    }

    private void UpdateTarget()
    {
        ClearTargets();
        UpdateNPC();
        FindNearbyItems();
    }

    private void ClearTargets()
    {
        if (currentNPC != null)
            currentNPC.HidePrompt();

        currentNPC = null;
        nearbyItemPickup = null;
        nearbySword = null;
        nearbyItem = null;
    }

    private void UpdateNPC()
    {
        Ray ray = new Ray(
            cam.transform.position,
            cam.transform.forward
        );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                distance,
                interactionMask,
                QueryTriggerInteraction.Collide))
        {
            return;
        }

        NPCController npc =
            hit.collider.GetComponentInParent<NPCController>();

        if (npc != null)
        {
            currentNPC = npc;
            currentNPC.ShowPrompt();
        }
    }

    private void FindNearbyItems()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            interactionRadius,
            nearbyColliders,
            interactionMask,
            QueryTriggerInteraction.Collide
        );

        ItemPickup bestItemPickup = null;
        SwordPickup bestSword = null;
        PickupItem bestItem = null;

        float bestPickupDistance = float.MaxValue;
        float bestSwordDistance = float.MaxValue;
        float bestItemDistance = float.MaxValue;

        Vector3 playerForward = transform.forward;

        for (int i = 0; i < count; i++)
        {
            Collider col = nearbyColliders[i];

            if (col == null)
                continue;

            if (col.transform.root == transform.root)
                continue;

            ItemPickup itemPickup =
                col.GetComponentInParent<ItemPickup>();

            if (itemPickup != null)
            {
                if (IsWithinInteractionCone(
                        itemPickup.transform.position,
                        transform.position,
                        playerForward,
                        interactionAngle,
                        ref bestPickupDistance))
                {
                    bestItemPickup = itemPickup;
                }

                continue;
            }

            SwordPickup sword =
                col.GetComponentInParent<SwordPickup>();

            if (sword != null)
            {
                if (IsWithinInteractionCone(
                        sword.transform.position,
                        transform.position,
                        playerForward,
                        interactionAngle,
                        ref bestSwordDistance))
                {
                    bestSword = sword;
                }

                continue;
            }

            PickupItem item =
                col.GetComponentInParent<PickupItem>();

            if (item != null)
            {
                if (IsWithinInteractionCone(
                        item.transform.position,
                        transform.position,
                        playerForward,
                        interactionAngle,
                        ref bestItemDistance))
                {
                    bestItem = item;
                }
            }
        }

        nearbyItemPickup = bestItemPickup;
        nearbySword = bestSword;
        nearbyItem = bestItem;
    }

    private static bool IsWithinInteractionCone(
        Vector3 targetPosition,
        Vector3 playerPosition,
        Vector3 playerForward,
        float maxAngle,
        ref float bestDistance)
    {
        Vector3 direction = targetPosition - playerPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
            return false;

        float angle = Vector3.Angle(playerForward, direction.normalized);
        if (angle > maxAngle)
            return false;

        float sqrDistance = direction.sqrMagnitude;
        if (sqrDistance >= bestDistance)
            return false;

        bestDistance = sqrDistance;
        return true;
    }

    private bool TryPickupLegacyItem(PickupItem pickup)
    {
        if (pickup == null)
            return false;

        if (!pickup.CanPickup)
        {
            Debug.Log($"Pickup blocked: {pickup.gameObject.name}");
            return false;
        }

        if (pickup.itemData == null)
        {
            Debug.LogWarning(
                $"PickupItem '{pickup.gameObject.name}' has no ItemData.",
                pickup
            );

            return false;
        }

        return InventoryPickupService.TryPickupFromWorld(
            pickup.itemData,
            pickup.quantity,
            pickup.gameObject,
            autoEquipWeapon: pickup.itemData.IsWeapon,
            PickupCleanupMode.Disable
        );
    }

    public void SetNearbySword(SwordPickup sword)
    {
        if (sword == null)
            return;

        nearbySword = sword;
    }

    public void ClearNearbySword(SwordPickup sword)
    {
        if (nearbySword == sword)
            nearbySword = null;
    }

    private void OnGUI()
    {
        if (!isLocalPlayer)
            return;

        if (DialogueManager.Instance != null &&
            DialogueManager.Instance.IsDialogueActive)
            return;

        if (nearbyItemPickup != null)
        {
            DrawPickupPrompt(nearbyItemPickup.ItemData);
            return;
        }

        if (nearbySword != null)
        {
            DrawPickupPrompt(nearbySword.ItemData);
            return;
        }

        if (nearbyItem != null)
        {
            ItemData data = nearbyItem.itemData;
            DrawPickupPrompt(data, nearbyItem.gameObject.name);
        }
    }

    private static void DrawPickupPrompt(ItemData data, string fallbackName = "предмет")
    {
        string itemName = data != null ? data.itemName : fallbackName;
        DrawPrompt($"[E]  Подобрать {itemName}");
    }

    private static void DrawPrompt(string text)
    {
        const float width = 300f;
        const float height = 40f;

        Rect rect = new Rect(
            (Screen.width - width) * 0.5f,
            Screen.height * 0.72f,
            width,
            height
        );

        GUI.Box(rect, text);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Quaternion leftRotation = Quaternion.Euler(0f, -interactionAngle, 0f);
        Quaternion rightRotation = Quaternion.Euler(0f, interactionAngle, 0f);

        Gizmos.DrawRay(
            transform.position,
            leftRotation * forward * interactionRadius
        );

        Gizmos.DrawRay(
            transform.position,
            rightRotation * forward * interactionRadius
        );
    }
}
