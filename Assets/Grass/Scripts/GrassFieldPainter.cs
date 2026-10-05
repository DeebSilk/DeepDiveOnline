using UnityEngine;

public class GrassFieldPainter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GrassVectorField vectorField;
    [SerializeField] private Transform player;

    [Header("Movement")]
    [SerializeField, Min(0.001f)]
    private float minimumMovement = 0.02f;

    private Vector3 previousPosition;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("GrassFieldPainter: Player не назначен.");
            enabled = false;
            return;
        }

        if (vectorField == null)
        {
            Debug.LogError("GrassFieldPainter: Vector Field не назначен.");
            enabled = false;
            return;
        }

        previousPosition = player.position;

        Debug.Log(
            "GrassFieldPainter: запущен. " +
            "Player = " + player.name +
            " | Position = " + player.position
        );
    }

    private void Update()
    {
        if (player == null || vectorField == null)
            return;

        Vector3 currentPosition = player.position;

        Vector3 movement = currentPosition - previousPosition;

        // Для травы учитываем только движение по земле.
        movement.y = 0f;

        float distance = movement.magnitude;

        // Игрок ещё не прошёл минимальное расстояние.
        if (distance < minimumMovement)
            return;

        Vector3 direction = movement.normalized;

        // Рисуем направление движения в Vector Field.
        vectorField.Paint(
            currentPosition,
            direction
        );

        Debug.Log(
            "GRASS PAINT | Position: " +
            currentPosition +
            " | Direction: " +
            direction +
            " | Distance: " +
            distance
        );

        // Запоминаем новую позицию.
        previousPosition = currentPosition;
    }
}