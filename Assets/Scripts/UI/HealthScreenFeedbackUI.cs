using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class HealthScreenFeedbackUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Graphic frameGraphic;

    [Header("Colors")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color healColor = Color.green;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private float fadeTimer;
    private bool fading;

    private void Awake()
    {
        CacheReferences();
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (!fading)
        {
            return;
        }

        if (fadeDuration <= 0f)
        {
            SetAlpha(0f);
            fading = false;
            return;
        }

        fadeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(fadeTimer / fadeDuration);
        float alpha = fadeCurve != null ? fadeCurve.Evaluate(t) : 1f - t;
        SetAlpha(alpha);

        if (t >= 1f)
        {
            fading = false;
        }
    }

    private void OnValidate()
    {
        fadeDuration = Mathf.Max(0f, fadeDuration);
    }

    public void SetHealth(Health newHealth)
    {
        if (health == newHealth)
        {
            return;
        }

        Unsubscribe();
        health = newHealth;
        Subscribe();
    }

    public void ShowDamage()
    {
        PlayFeedback(damageColor);
    }

    public void ShowHeal()
    {
        PlayFeedback(healColor);
    }

    private void HandleDamaged(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        ShowDamage();
    }

    private void HandleHealed(float amount)
    {
        ShowHeal();
    }

    private void PlayFeedback(Color color)
    {
        CacheReferences();

        if (frameGraphic != null)
        {
            frameGraphic.color = color;
        }

        fadeTimer = 0f;
        fading = true;
        SetAlpha(1f);
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }

    private void Subscribe()
    {
        if (health == null)
        {
            return;
        }

        health.Damaged += HandleDamaged;
        health.Healed += HandleHealed;
    }

    private void Unsubscribe()
    {
        if (health == null)
        {
            return;
        }

        health.Damaged -= HandleDamaged;
        health.Healed -= HandleHealed;
    }

    private void CacheReferences()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (frameGraphic == null)
        {
            frameGraphic = GetComponent<Graphic>();
        }
    }
}
