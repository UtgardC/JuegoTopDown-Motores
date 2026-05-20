using UnityEngine;

/// <summary>
/// Posiciona un objeto entre el jugador y el punto de apuntado, limitando la distancia máxima.
/// </summary>
public class DynamicCameraTarget : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform aimTarget;

    [SerializeField]
    [Tooltip("0 = Pegado al jugador, 1 = Pegado al mouse. 0.3 es un buen balance.")]
    [Range(0f, 1f)]
    private float aimWeight = 0.3f;

    [SerializeField]
    [Tooltip("Distancia máxima que la cámara puede alejarse del jugador.")]
    private float maxCameraOffset = 4f;

    [SerializeField]
    [Tooltip("Velocidad con la que el objetivo de la cámara se mueve (suavizado).")]
    private float smoothSpeed = 15f;

    private void Update()
    {
        if (player == null || aimTarget == null) return;

        // 1. Calculamos el punto intermedio exacto basado en el peso
        Vector3 desiredPosition = Vector3.Lerp(player.position, aimTarget.position, aimWeight);

        // 2. Calculamos el vector desde el jugador hacia ese punto intermedio
        Vector3 offset = desiredPosition - player.position;

        // 3. Si ese vector es más largo que nuestro límite, lo recortamos (Clamp)
        if (offset.magnitude > maxCameraOffset)
        {
            offset = offset.normalized * maxCameraOffset;
        }

        // 4. La posición final es el jugador más ese vector limitado
        Vector3 finalPosition = player.position + offset;

        // 5. Movemos este objeto suavemente hacia esa posición final
        transform.position = Vector3.Lerp(transform.position, finalPosition, smoothSpeed * Time.deltaTime);
    }
}