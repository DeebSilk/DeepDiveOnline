using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Graphic))]
public class UIButtonAnimator : MonoBehaviour, IPointerClickHandler
{
    [Header("Click")]
    [SerializeField] private UnityEvent onClick;
    [SerializeField] private Button uiButton;

    private Graphic targetGraphic;

    private void Awake()
    {
        targetGraphic = GetComponent<Graphic>() ?? GetComponentInChildren<Graphic>();

        if (targetGraphic == null)
        {
            Debug.LogWarning("UIButtonAnimator: Graphic missing. UI clicks require a Graphic with Raycast Target enabled.", this);
        }
        else if (!targetGraphic.raycastTarget)
        {
            targetGraphic.raycastTarget = true;
        }

        if (uiButton == null)
            uiButton = GetComponent<Button>();

        if (uiButton != null)
        {
            uiButton.onClick.AddListener(InvokeClickFromButton);
        }

        EnsureEventSystemExists();
    }

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null)
            return;

        EventSystem existing = Object.FindAnyObjectByType<EventSystem>();
        if (existing != null)
            return;

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
        Debug.LogWarning("UIButtonAnimator: EventSystem was missing, so a new one was created automatically.", this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // A Button already invokes its onClick event. Handling both paths would
        // invoke the configured action twice.
        if (uiButton == null)
            InvokeClick();
    }

    private void InvokeClickFromButton()
    {
        InvokeClick();
    }

    private void InvokeClick()
    {
        onClick?.Invoke();
    }
}
