using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private Image[] fills = new Image[2];
    [SerializeField] private Image[] damageFills = new Image[2];

    [Header("Fill Amount")]
    [SerializeField, Range(0f, 1f)] private float emptyFillAmount = 0f;
    [SerializeField, Range(0f, 1f)] private float fullFillAmount = 0.5f;

    [Header("Damage Fill")]
    [SerializeField] private float damageFillDelay = 1f;
    [SerializeField] private float damageFillMoveDuration = 0.25f;
    [SerializeField] private AnimationCurve damageFillMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Heal Fill")]
    [SerializeField] private float healFillMoveDuration = 0.15f;
    [SerializeField] private AnimationCurve healFillMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float lastNormalizedHealth = 1f;
    private float currentFillAmount;
    private float healFillStartAmount;
    private float healFillTargetAmount;
    private float healFillMoveTimer;
    private bool healFillIsMoving;
    private float damageFillTargetAmount;
    private float damageFillStartAmount;
    private float currentDamageFillAmount;
    private float damageFillDelayTimer;
    private float damageFillMoveTimer;
    private bool damageFillIsMoving;

    private void OnEnable()
    {
        Subscribe();

        if (health != null)
        {
            SetImmediate(health.HealthNormalized);
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        UpdateHealFill();
        UpdateDamageFill();
    }

    private void OnValidate()
    {
        emptyFillAmount = Mathf.Clamp01(emptyFillAmount);
        fullFillAmount = Mathf.Clamp01(fullFillAmount);
        damageFillDelay = Mathf.Max(0f, damageFillDelay);
        damageFillMoveDuration = Mathf.Max(0f, damageFillMoveDuration);
        healFillMoveDuration = Mathf.Max(0f, healFillMoveDuration);
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

        if (health != null)
        {
            SetImmediate(health.HealthNormalized);
        }
    }

    private void Subscribe()
    {
        if (health != null)
        {
            health.LifeChanged += HandleLifeChanged;
        }
    }

    private void Unsubscribe()
    {
        if (health != null)
        {
            health.LifeChanged -= HandleLifeChanged;
        }
    }

    private void HandleLifeChanged(float currentHealth, float maxHealth, float normalizedHealth)
    {
        normalizedHealth = Mathf.Clamp01(normalizedHealth);

        float previousFillAmount = GetFillAmount(lastNormalizedHealth);
        float newFillAmount = GetFillAmount(normalizedHealth);

        if (normalizedHealth < lastNormalizedHealth)
        {
            healFillIsMoving = false;
            SetDamageFills(previousFillAmount);
            currentDamageFillAmount = previousFillAmount;
            damageFillTargetAmount = newFillAmount;
            damageFillDelayTimer = damageFillDelay;
            damageFillMoveTimer = 0f;
            damageFillIsMoving = false;
            SetFills(newFillAmount);
            currentFillAmount = newFillAmount;
        }
        else if (normalizedHealth > lastNormalizedHealth)
        {
            SetDamageFills(newFillAmount);
            currentDamageFillAmount = newFillAmount;
            damageFillTargetAmount = newFillAmount;
            damageFillDelayTimer = 0f;
            damageFillIsMoving = false;
            StartHealFillMove(newFillAmount);
        }
        else
        {
            SetFills(newFillAmount);
            currentFillAmount = newFillAmount;
        }

        lastNormalizedHealth = normalizedHealth;
    }

    private void StartHealFillMove(float targetAmount)
    {
        healFillStartAmount = currentFillAmount;
        healFillTargetAmount = targetAmount;
        healFillMoveTimer = 0f;

        if (healFillMoveDuration <= 0f)
        {
            currentFillAmount = healFillTargetAmount;
            SetFills(currentFillAmount);
            healFillIsMoving = false;
            return;
        }

        healFillIsMoving = true;
    }

    private void UpdateHealFill()
    {
        if (!healFillIsMoving)
        {
            return;
        }

        healFillMoveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(healFillMoveTimer / healFillMoveDuration);
        float easedT = healFillMoveCurve != null ? healFillMoveCurve.Evaluate(t) : t;
        currentFillAmount = Mathf.Lerp(healFillStartAmount, healFillTargetAmount, easedT);
        SetFills(currentFillAmount);

        if (t >= 1f)
        {
            currentFillAmount = healFillTargetAmount;
            SetFills(currentFillAmount);
            healFillIsMoving = false;
        }
    }

    private void UpdateDamageFill()
    {
        if (damageFills == null || damageFills.Length == 0)
        {
            return;
        }

        if (damageFillDelayTimer > 0f)
        {
            damageFillDelayTimer -= Time.deltaTime;
            if (damageFillDelayTimer <= 0f)
            {
                damageFillStartAmount = currentDamageFillAmount;
                damageFillMoveTimer = 0f;
                damageFillIsMoving = true;
            }

            return;
        }

        if (!damageFillIsMoving)
        {
            return;
        }

        if (damageFillMoveDuration <= 0f)
        {
            SetDamageFills(damageFillTargetAmount);
            currentDamageFillAmount = damageFillTargetAmount;
            damageFillIsMoving = false;
            return;
        }

        damageFillMoveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(damageFillMoveTimer / damageFillMoveDuration);
        float easedT = damageFillMoveCurve != null ? damageFillMoveCurve.Evaluate(t) : t;
        currentDamageFillAmount = Mathf.Lerp(damageFillStartAmount, damageFillTargetAmount, easedT);
        SetDamageFills(currentDamageFillAmount);

        if (t >= 1f)
        {
            damageFillIsMoving = false;
        }
    }

    private void SetImmediate(float normalizedHealth)
    {
        lastNormalizedHealth = Mathf.Clamp01(normalizedHealth);
        float fillAmount = GetFillAmount(lastNormalizedHealth);
        SetFills(fillAmount);
        SetDamageFills(fillAmount);
        currentFillAmount = fillAmount;
        healFillTargetAmount = fillAmount;
        healFillMoveTimer = 0f;
        healFillIsMoving = false;
        currentDamageFillAmount = fillAmount;
        damageFillTargetAmount = fillAmount;
        damageFillDelayTimer = 0f;
        damageFillIsMoving = false;
    }

    private float GetFillAmount(float normalizedHealth)
    {
        return Mathf.Lerp(emptyFillAmount, fullFillAmount, Mathf.Clamp01(normalizedHealth));
    }

    private void SetFills(float fillAmount)
    {
        SetFillAmount(fills, fillAmount);
    }

    private void SetDamageFills(float fillAmount)
    {
        SetFillAmount(damageFills, fillAmount);
    }

    private static void SetFillAmount(Image[] targets, float fillAmount)
    {
        if (targets == null)
        {
            return;
        }

        fillAmount = Mathf.Clamp01(fillAmount);
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].fillAmount = fillAmount;
            }
        }
    }
}
