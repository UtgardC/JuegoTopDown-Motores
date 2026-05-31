using UnityEngine;

public class AimCursorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private RectTransform sightCursor;
    [SerializeField] private RectTransform auxCursor;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Canvas targetCanvas;

    [Header("Cursor")]
    [SerializeField]
    [Tooltip("Determines if the operating system cursor should be hidden while the aim UI is active.")]
    private bool hideSystemCursor = true;

    [SerializeField]
    [Tooltip("Determines if the cursor should be confined inside the game window.")]
    private bool confineCursorToWindow = true;

    [SerializeField]
    [Tooltip("Prevents the aim cursors from following the mouse while the game is paused.")]
    private bool stopFollowingWhilePaused = true;

    private void Awake()
    {
        CacheReferences();
        ConfigureCursor();
    }

    private void OnEnable()
    {
        ConfigureCursor();
    }

    private void OnDisable()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        if (player == null || (stopFollowingWhilePaused && Time.timeScale <= 0f))
        {
            return;
        }

        UpdateAuxCursor();
        UpdateSightCursor();
    }

    private void UpdateAuxCursor()
    {
        if (auxCursor == null)
        {
            return;
        }

        SetCursorScreenPosition(auxCursor, ClampToScreen(player.RawMouseScreenPosition));
    }

    private void UpdateSightCursor()
    {
        if (sightCursor == null)
        {
            return;
        }

        Camera cameraToUse = GetGameplayCamera();
        if (cameraToUse == null)
        {
            return;
        }

        Vector3 sightScreenPosition = cameraToUse.WorldToScreenPoint(player.SightAimWorldPoint);
        if (sightScreenPosition.z < 0f)
        {
            return;
        }

        SetCursorScreenPosition(sightCursor, sightScreenPosition);
    }

    private void SetCursorScreenPosition(RectTransform cursor, Vector2 screenPosition)
    {
        RectTransform parentRect = cursor.parent as RectTransform;
        if (parentRect == null)
        {
            cursor.position = screenPosition;
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPosition,
            GetUICamera(),
            out Vector2 localPosition);

        cursor.anchoredPosition = localPosition;
    }

    private Vector2 ClampToScreen(Vector2 screenPosition)
    {
        return new Vector2(
            Mathf.Clamp(screenPosition.x, 0f, Screen.width),
            Mathf.Clamp(screenPosition.y, 0f, Screen.height));
    }

    private Camera GetGameplayCamera()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        return gameplayCamera;
    }

    private Camera GetUICamera()
    {
        if (targetCanvas == null || targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return targetCanvas.worldCamera;
    }

    private void CacheReferences()
    {
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }
    }

    private void ConfigureCursor()
    {
        Cursor.visible = !hideSystemCursor;
        Cursor.lockState = confineCursorToWindow ? CursorLockMode.Confined : CursorLockMode.None;
    }
}
