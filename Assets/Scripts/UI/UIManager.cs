using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    private enum PauseMenuEasing
    {
        Linear,
        SmoothStep,
        SineInOut,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        CustomCurve
    }

    [Header("Pause Menu")]
    [SerializeField]
    [Tooltip("Objeto padre que contiene todos los elementos del menu de pausa.")]
    private RectTransform pauseMenuRoot;

    [SerializeField]
    [Tooltip("Posicion Y del menu cuando esta visible.")]
    private float pauseMenuVisibleY = 0f;

    [SerializeField]
    [Tooltip("Posicion Y del menu cuando esta oculto.")]
    private float pauseMenuHiddenY = 900f;

    [SerializeField]
    [Tooltip("Tiempo que tarda el menu en bajar al pausar.")]
    private float pauseMenuShowDuration = 0.35f;

    [SerializeField]
    [Tooltip("Curva de movimiento usada para mostrar y ocultar el menu.")]
    private PauseMenuEasing pauseMenuEasing = PauseMenuEasing.SmoothStep;

    [SerializeField]
    [Tooltip("Curva personalizada. Solo se usa cuando Pause Menu Easing esta en Custom Curve.")]
    private AnimationCurve customPauseMenuCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    [Tooltip("Si esta activo, el menu arranca oculto y desactivado.")]
    private bool hidePauseMenuOnAwake = true;

    private Coroutine pauseMenuAnimation;
    private bool isPaused;
    private float previousTimeScale = 1f;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockState;

    private float PauseMenuHideDuration => pauseMenuShowDuration * 0.5f;

    private void Awake()
    {
        if (hidePauseMenuOnAwake && pauseMenuRoot != null)
        {
            SetPauseMenuY(pauseMenuHiddenY);
            pauseMenuRoot.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void OnDisable()
    {
        if (!isPaused)
        {
            return;
        }

        Time.timeScale = previousTimeScale;
        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLockState;
        isPaused = false;
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused || pauseMenuRoot == null)
        {
            return;
        }

        isPaused = true;
        previousTimeScale = Time.timeScale;
        previousCursorVisible = Cursor.visible;
        previousCursorLockState = Cursor.lockState;

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        pauseMenuRoot.gameObject.SetActive(true);
        PlayPauseMenuAnimation(pauseMenuVisibleY, pauseMenuShowDuration, false);
    }

    public void ResumeGame()
    {
        if (!isPaused || pauseMenuRoot == null)
        {
            return;
        }

        isPaused = false;
        Time.timeScale = previousTimeScale;
        Cursor.visible = false;
        Cursor.lockState = previousCursorLockState;

        PlayPauseMenuAnimation(pauseMenuHiddenY, PauseMenuHideDuration, true);
    }

    private void PlayPauseMenuAnimation(float targetY, float duration, bool deactivateWhenFinished)
    {
        if (pauseMenuAnimation != null)
        {
            StopCoroutine(pauseMenuAnimation);
        }

        pauseMenuAnimation = StartCoroutine(AnimatePauseMenu(targetY, duration, deactivateWhenFinished));
    }

    private IEnumerator AnimatePauseMenu(float targetY, float duration, bool deactivateWhenFinished)
    {
        float startY = pauseMenuRoot.anchoredPosition.y;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = EvaluatePauseMenuCurve(t);

            SetPauseMenuY(Mathf.Lerp(startY, targetY, t));
            yield return null;
        }

        SetPauseMenuY(targetY);

        if (deactivateWhenFinished)
        {
            pauseMenuRoot.gameObject.SetActive(false);
        }

        pauseMenuAnimation = null;
    }

    private float EvaluatePauseMenuCurve(float t)
    {
        t = Mathf.Clamp01(t);

        switch (pauseMenuEasing)
        {
            case PauseMenuEasing.Linear:
                return t;

            case PauseMenuEasing.SineInOut:
                return 0.5f - (0.5f * Mathf.Cos(t * Mathf.PI));

            case PauseMenuEasing.EaseInQuad:
                return t * t;

            case PauseMenuEasing.EaseOutQuad:
            {
                float inverseT = 1f - t;
                return 1f - (inverseT * inverseT);
            }

            case PauseMenuEasing.EaseInOutQuad:
                return t < 0.5f
                    ? 2f * t * t
                    : 1f - (Mathf.Pow(-2f * t + 2f, 2f) * 0.5f);

            case PauseMenuEasing.EaseInCubic:
                return t * t * t;

            case PauseMenuEasing.EaseOutCubic:
            {
                float inverseT = 1f - t;
                return 1f - (inverseT * inverseT * inverseT);
            }

            case PauseMenuEasing.EaseInOutCubic:
                if (t < 0.5f)
                {
                    return 4f * t * t * t;
                }

                float cubicT = -2f * t + 2f;
                return 1f - (cubicT * cubicT * cubicT * 0.5f);

            case PauseMenuEasing.CustomCurve:
                if (customPauseMenuCurve == null || customPauseMenuCurve.length == 0)
                {
                    return t;
                }

                return Mathf.Clamp01(customPauseMenuCurve.Evaluate(t));

            case PauseMenuEasing.SmoothStep:
            default:
                return Mathf.SmoothStep(0f, 1f, t);
        }
    }

    private void SetPauseMenuY(float y)
    {
        Vector2 anchoredPosition = pauseMenuRoot.anchoredPosition;
        anchoredPosition.y = y;
        pauseMenuRoot.anchoredPosition = anchoredPosition;
    }
}
