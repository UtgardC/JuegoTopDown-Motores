using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves a UI crosshair using the current mouse position from the new Input System.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CrosshairFollow : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Determines if the operating system cursor should be hidden while the crosshair is active.")]
    private bool hideSystemCursor = true;

    [SerializeField]
    [Tooltip("Determines if the cursor should be confined inside the game window.")]
    private bool confineCursorToWindow = true;

    private RectTransform crosshairRectTransform;

    private void Awake()
    {
        crosshairRectTransform = GetComponent<RectTransform>();
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
        if (Mouse.current == null)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        crosshairRectTransform.position = mousePosition;
    }

    private void ConfigureCursor()
    {
        Cursor.visible = !hideSystemCursor;

        if (confineCursorToWindow)
        {
            Cursor.lockState = CursorLockMode.Confined;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}