using UnityEngine;

public class DynamicCameraTarget : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform aimTarget;

    [SerializeField]
    [Tooltip("0 = attached to the player, 1 = attached to the mouse. 0.3 is a good balance.")]
    [Range(0f, 1f)]
    private float aimWeight = 0.3f;

    [SerializeField]
    [Tooltip("Maximum distance the camera target can move away from the player.")]
    private float maxCameraOffset = 4f;

    [SerializeField]
    [Tooltip("Speed used to smooth normal camera target movement.")]
    private float smoothSpeed = 15f;

    [SerializeField]
    [Tooltip("Speed used to smooth camera target movement while the player is dashing.")]
    private float dashSmoothSpeed = 50f;

    private PlayerController playerController;

    private void Awake()
    {
        CachePlayerController();
    }

    private void OnValidate()
    {
        maxCameraOffset = Mathf.Max(0f, maxCameraOffset);
        smoothSpeed = Mathf.Max(0f, smoothSpeed);
        dashSmoothSpeed = Mathf.Max(0f, dashSmoothSpeed);
        CachePlayerController();
    }

    private void Update()
    {
        if (player == null || aimTarget == null)
        {
            return;
        }

        if (playerController == null)
        {
            CachePlayerController();
        }

        Vector3 desiredPosition = Vector3.Lerp(player.position, aimTarget.position, aimWeight);
        Vector3 offset = desiredPosition - player.position;

        if (offset.magnitude > maxCameraOffset)
        {
            offset = offset.normalized * maxCameraOffset;
        }

        Vector3 finalPosition = player.position + offset;
        float currentSmoothSpeed = playerController != null && playerController.IsDashing
            ? dashSmoothSpeed
            : smoothSpeed;

        transform.position = Vector3.Lerp(transform.position, finalPosition, currentSmoothSpeed * Time.deltaTime);
    }

    private void CachePlayerController()
    {
        playerController = player != null ? player.GetComponent<PlayerController>() : null;
    }
}