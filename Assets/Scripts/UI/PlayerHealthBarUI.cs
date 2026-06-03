using UnityEngine;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private RectTransform fill;
    [SerializeField] private RectTransform damageFill;

    [Header("Positions")]
    [SerializeField] private float fullPositionX = 0f;
    [SerializeField] private bool useFillWidthAsEmptyPosition = true;
    [SerializeField] private float emptyPositionX = 300f;

    [Header("Damage Fill")]
    [SerializeField] private float damageFillDelay = 1f;
    [SerializeField] private float damageFillMoveDuration = 0.25f;
    [SerializeField] private AnimationCurve damageFillMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float lastNormalizedHealth = 1f;
    private float damageFillTargetX;
    private float damageFillStartX;
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
        UpdateDamageFill();
    }

    private void OnValidate()
    {
        damageFillDelay = Mathf.Max(0f, damageFillDelay);
        damageFillMoveDuration = Mathf.Max(0f, damageFillMoveDuration);
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

        float previousFillX = fill != null
            ? fill.anchoredPosition.x
            : GetFillPositionX(lastNormalizedHealth);

        float newFillX = GetFillPositionX(normalizedHealth);

        if (normalizedHealth < lastNormalizedHealth)
        {
            SetFillX(damageFill, previousFillX);
            damageFillTargetX = newFillX;
            damageFillDelayTimer = damageFillDelay;
            damageFillMoveTimer = 0f;
            damageFillIsMoving = false;
        }
        else if (normalizedHealth > lastNormalizedHealth)
        {
            SetFillX(damageFill, newFillX);
            damageFillTargetX = newFillX;
            damageFillDelayTimer = 0f;
            damageFillIsMoving = false;
        }

        SetFillX(fill, newFillX);
        lastNormalizedHealth = normalizedHealth;
    }

    private void UpdateDamageFill()
    {
        if (damageFill == null)
        {
            return;
        }

        if (damageFillDelayTimer > 0f)
        {
            damageFillDelayTimer -= Time.deltaTime;
            if (damageFillDelayTimer <= 0f)
            {
                damageFillStartX = damageFill.anchoredPosition.x;
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
            SetFillX(damageFill, damageFillTargetX);
            damageFillIsMoving = false;
            return;
        }

        damageFillMoveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(damageFillMoveTimer / damageFillMoveDuration);
        float easedT = damageFillMoveCurve != null ? damageFillMoveCurve.Evaluate(t) : t;
        SetFillX(damageFill, Mathf.Lerp(damageFillStartX, damageFillTargetX, easedT));

        if (t >= 1f)
        {
            damageFillIsMoving = false;
        }
    }

    private void SetImmediate(float normalizedHealth)
    {
        lastNormalizedHealth = Mathf.Clamp01(normalizedHealth);
        float positionX = GetFillPositionX(lastNormalizedHealth);
        SetFillX(fill, positionX);
        SetFillX(damageFill, positionX);
        damageFillTargetX = positionX;
        damageFillDelayTimer = 0f;
        damageFillIsMoving = false;
    }

    private float GetFillPositionX(float normalizedHealth)
    {
        float lostHealth = 1f - Mathf.Clamp01(normalizedHealth);
        return Mathf.Lerp(fullPositionX, GetEmptyPositionX(), lostHealth);
    }

    private float GetEmptyPositionX()
    {
        if (useFillWidthAsEmptyPosition && fill != null)
        {
            return fill.rect.width;
        }

        return emptyPositionX;
    }

    private void SetFillX(RectTransform targetFill, float positionX)
    {
        if (targetFill == null)
        {
            return;
        }

        Vector2 anchoredPosition = targetFill.anchoredPosition;
        anchoredPosition.x = positionX;
        targetFill.anchoredPosition = anchoredPosition;
    }
}
