using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CreateEditableHudCanvas
{
    private const string RootName = "HUD Canvas";

    [MenuItem("Tools/HUD/Create Editable HUD Canvas")]
    public static void Create()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            EditorGUIUtility.PingObject(existing);
            Debug.Log("HUD Canvas already exists. Select it in Hierarchy to edit it.");
            return;
        }

        EnsureEventSystem();

        GameObject root = new GameObject(RootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(HUDCanvasController));
        Undo.RegisterCreatedObjectUndo(root, "Create editable HUD Canvas");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Color panel = new Color(0.035f, 0.05f, 0.08f, 0.9f);
        Color accent = new Color(0.2f, 0.82f, 0.35f, 1f);

        Image healthPanel = ImageElement("Health Panel", root.transform, panel);
        SetRect(healthPanel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(300f, 42f));
        Image healthFill = ImageElement("Health Fill", healthPanel.transform, accent);
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = 0;
        Stretch(healthFill.rectTransform, 4f);
        Text healthText = TextElement("Health Label", healthPanel.transform, font, 18, TextAnchor.MiddleCenter);
        Stretch(healthText.rectTransform, 4f);

        Button inventoryButton = ButtonElement("Inventory Button", root.transform, font, "INVENTORY", panel);
        SetRect(inventoryButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-176f, -28f), new Vector2(138f, 44f));
        Button pauseButton = ButtonElement("Pause Button", root.transform, font, "PAUSE", panel);
        SetRect(pauseButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(138f, 44f));

        GameObject hotbar = new GameObject("Hotbar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        hotbar.transform.SetParent(root.transform, false);
        RectTransform hotbarRect = hotbar.GetComponent<RectTransform>();
        SetRect(hotbarRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(624f, 64f));
        HorizontalLayoutGroup layout = hotbar.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        Image[] icons = new Image[9];
        Text[] amounts = new Text[9];
        for (int i = 0; i < 9; i++)
        {
            Image slot = ImageElement($"Slot {i + 1}", hotbar.transform, panel);
            LayoutElement slotLayout = slot.gameObject.AddComponent<LayoutElement>();
            slotLayout.preferredWidth = 64f;
            slotLayout.preferredHeight = 64f;

            Image icon = ImageElement("Icon", slot.transform, Color.white);
            icon.preserveAspect = true;
            icon.enabled = false;
            Stretch(icon.rectTransform, 8f);
            icons[i] = icon;

            Text number = TextElement("Number", slot.transform, font, 15, TextAnchor.UpperLeft);
            number.text = (i + 1).ToString();
            SetRect(number.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -4f), new Vector2(22f, 18f));

            Text amount = TextElement("Amount", slot.transform, font, 15, TextAnchor.LowerRight);
            SetRect(amount.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-6f, 4f), new Vector2(36f, 18f));
            amounts[i] = amount;
        }

        HUDCanvasController controller = root.GetComponent<HUDCanvasController>();
        InventoryUI inventory = Object.FindAnyObjectByType<InventoryUI>(FindObjectsInactive.Include);
        PauseMenu pause = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        controller.Configure(healthFill, healthText, hotbar, icons, amounts, inventory, pause);
        UnityEventTools.AddPersistentListener(inventoryButton.onClick, controller.ToggleInventory);
        UnityEventTools.AddPersistentListener(pauseButton.onClick, controller.TogglePause);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Debug.Log("Editable HUD Canvas created. Save the scene to keep it.");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
    }

    private static Image ImageElement(string name, Transform parent, Color color)
    {
        GameObject element = new GameObject(name, typeof(RectTransform), typeof(Image));
        element.transform.SetParent(parent, false);
        Image image = element.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text TextElement(string name, Transform parent, Font font, int size, TextAnchor alignment)
    {
        GameObject element = new GameObject(name, typeof(RectTransform), typeof(Text));
        element.transform.SetParent(parent, false);
        Text text = element.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static Button ButtonElement(string name, Transform parent, Font font, string label, Color color)
    {
        Image image = ImageElement(name, parent, color);
        Button button = image.gameObject.AddComponent<Button>();
        Text text = TextElement("Label", image.transform, font, 17, TextAnchor.MiddleCenter);
        text.text = label;
        Stretch(text.rectTransform, 4f);
        return button;
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
    }
}
