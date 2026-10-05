using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Damageable))]
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private Color fillColor = new Color(0.9f, 0.15f, 0.15f, 1f);

    private Damageable health;
    private Transform barTransform;
    private Image fill;

    private void Awake()
    {
        health = GetComponent<Damageable>();
        health.HealthChanged += Refresh;
        CreateBar();
        Refresh(health.CurrentHealth, health.MaxHealth);
    }

    private void LateUpdate()
    {
        if (barTransform == null || Camera.main == null)
            return;

        barTransform.position = transform.position + offset;
        barTransform.rotation = Camera.main.transform.rotation;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.HealthChanged -= Refresh;
    }

    private void Refresh(float current, float maximum)
    {
        if (fill != null)
            fill.fillAmount = maximum <= 0f ? 0f : current / maximum;
    }

    private void CreateBar()
    {
        GameObject canvasObject = new GameObject("Health Bar", typeof(Canvas));
        canvasObject.transform.SetParent(transform);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObject.transform.localScale = Vector3.one * 0.01f;
        barTransform = canvasObject.transform;

        Image background = CreateImage("Background", barTransform, Color.black);
        background.rectTransform.sizeDelta = new Vector2(100f, 12f);
        fill = CreateImage("Fill", background.transform, fillColor);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.rectTransform.anchorMin = new Vector2(0f, 0f);
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = Vector2.one * 2f;
        fill.rectTransform.offsetMax = Vector2.one * -2f;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
        item.transform.SetParent(parent, false);
        Image image = item.GetComponent<Image>();
        image.color = color;
        return image;
    }
}
