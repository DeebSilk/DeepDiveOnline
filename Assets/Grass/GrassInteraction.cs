using UnityEngine;

public class GrassInteraction : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Interaction")]
    [SerializeField, Min(0f)] private float interactionRadius = 1.5f;
    [SerializeField, Min(0f)] private float interactionStrength = 1f;
    [SerializeField, Min(0f)] private float interactionFalloff = 2f;
    [SerializeField, Min(0f)] private float returnSpeed = 5f;

    [Header("Wind")]
    [SerializeField, Min(0f)] private float windStrength = 0.15f;
    [SerializeField, Min(0f)] private float windSpeed = 1f;
    [SerializeField, Min(0f)] private float windScale = 3f;

    private static readonly int PlayerPositionID =
        Shader.PropertyToID("_Player_Position");

    private static readonly int InteractionRadiusID =
        Shader.PropertyToID("_Interaction_Radius");

    private static readonly int InteractionStrengthID =
        Shader.PropertyToID("_Interaction_Strength");

    private static readonly int InteractionFalloffID =
        Shader.PropertyToID("_Interaction_Falloff");

    private static readonly int WindStrengthID =
        Shader.PropertyToID("_Wind_Strength");

    private static readonly int WindSpeedID =
        Shader.PropertyToID("_Wind_Speed");

    private static readonly int WindScaleID =
        Shader.PropertyToID("_Wind_Phase_Scale");

    private Vector3 smoothedPlayerPosition;

    private void OnEnable()
    {
        if (player != null)
        {
            smoothedPlayerPosition = player.position;
        }
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogWarning(
                "[GrassInteraction] Player reference is not assigned. " +
                "Grass interaction is disabled.",
                this
            );

            enabled = false;
            return;
        }

        smoothedPlayerPosition =
            player.position;
    }

    private void Update()
    {
        if (player == null)
            return;

        float smoothing =
            1f - Mathf.Exp(
                -returnSpeed * Time.deltaTime
            );

        smoothedPlayerPosition =
            Vector3.Lerp(
                smoothedPlayerPosition,
                player.position,
                smoothing
            );

        Shader.SetGlobalVector(
            PlayerPositionID,
            smoothedPlayerPosition
        );

        Shader.SetGlobalFloat(
            InteractionRadiusID,
            interactionRadius
        );

        Shader.SetGlobalFloat(
            InteractionStrengthID,
            interactionStrength
        );

        Shader.SetGlobalFloat(
            InteractionFalloffID,
            interactionFalloff
        );

        Shader.SetGlobalFloat(
            WindStrengthID,
            windStrength
        );

        Shader.SetGlobalFloat(
            WindSpeedID,
            windSpeed
        );

        Shader.SetGlobalFloat(
            WindScaleID,
            windScale
        );
    }

    private void OnValidate()
    {
        interactionRadius =
            Mathf.Max(interactionRadius, 0f);

        interactionStrength =
            Mathf.Max(interactionStrength, 0f);

        interactionFalloff =
            Mathf.Max(interactionFalloff, 0f);

        returnSpeed =
            Mathf.Max(returnSpeed, 0f);

        windStrength =
            Mathf.Max(windStrength, 0f);

        windSpeed =
            Mathf.Max(windSpeed, 0f);

        windScale =
            Mathf.Max(windScale, 0f);
    }
}