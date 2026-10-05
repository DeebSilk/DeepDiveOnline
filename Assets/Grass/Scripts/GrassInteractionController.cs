using UnityEngine;

/// <summary>
/// Передаёт данные об игроке и параметры ветра в Shader Graph травы
/// через глобальные shader-свойства. Работает без Rigidbody и без
/// перебора отдельных объектов травы — подходит для любого количества
/// травинок, так как обновляются только глобальные переменные шейдера.
///
/// Повесьте этот скрипт на любой постоянно активный объект в сцене
/// (например GameManager) и укажите ссылку на Transform игрока.
/// </summary>
public class GrassInteractionController : MonoBehaviour
{
    [Header("Ссылка на игрока")]
    [Tooltip("Transform игрока (или любой другой точки, от которой должна гнуться трава)")]
    public Transform player;

    [Header("Взаимодействие с игроком")]
    [Tooltip("Радиус, в котором трава реагирует на игрока")]
    public float interactionRadius = 1.5f;

    [Tooltip("Сила сгибания травы при приближении игрока")]
    public float interactionStrength = 1.0f;

    [Tooltip("Скорость, с которой трава 'догоняет' реальную позицию игрока. " +
             "Чем меньше значение, тем плавнее (но медленнее) реакция и возврат травы.")]
    public float returnSpeed = 5f;

    [Header("Ветер")]
    [Tooltip("Сила покачивания травы от ветра")]
    public float windStrength = 0.15f;

    [Tooltip("Скорость колебаний ветра")]
    public float windSpeed = 1.0f;

    // Кэшированные ID свойств шейдера — быстрее, чем передавать строки каждый кадр
    private static readonly int PlayerPositionID = Shader.PropertyToID("_PlayerPosition");
    private static readonly int InteractionRadiusID = Shader.PropertyToID("_InteractionRadius");
    private static readonly int InteractionStrengthID = Shader.PropertyToID("_InteractionStrength");
    private static readonly int WindStrengthID = Shader.PropertyToID("_WindStrength");
    private static readonly int WindSpeedID = Shader.PropertyToID("_WindSpeed");

    // Сглаженная (с задержкой) позиция игрока — именно она уходит в шейдер.
    // Благодаря этому трава не дёргается резко и плавно возвращается в исходное
    // положение после ухода игрока, хотя сам шейдер не хранит состояние между кадрами.
    private Vector3 smoothedPlayerPosition;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogWarning("[GrassInteractionController] Player не назначен в инспекторе.");
            enabled = false;
            return;
        }

        smoothedPlayerPosition = player.position;
    }

    private void Update()
    {
        if (player == null) return;

        // Плавно подтягиваем "видимую шейдером" позицию игрока к реальной.
        // При уходе игрока это же сглаживание даёт эффект медленного
        // распрямления травы, так как влияние на траву зависит от расстояния
        // до smoothedPlayerPosition, а не до реальной позиции игрока.
        smoothedPlayerPosition = Vector3.Lerp(
            smoothedPlayerPosition,
            player.position,
            1f - Mathf.Exp(-returnSpeed * Time.deltaTime) // экспоненциальное сглаживание, не зависит от FPS
        );

        Shader.SetGlobalVector(PlayerPositionID, smoothedPlayerPosition);
        Shader.SetGlobalFloat(InteractionRadiusID, interactionRadius);
        Shader.SetGlobalFloat(InteractionStrengthID, interactionStrength);
        Shader.SetGlobalFloat(WindStrengthID, windStrength);
        Shader.SetGlobalFloat(WindSpeedID, windSpeed);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(player.position, interactionRadius);
    }
#endif
}
