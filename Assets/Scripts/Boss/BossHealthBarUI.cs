using UnityEngine;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossActivationGroup activationGroup;
    [SerializeField] private HealthTarget bossHealth;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform fill;
    [SerializeField] private RectTransform damageFill;

    [Header("Panel Movement")]
    [SerializeField] private bool startHidden = true;
    [SerializeField] private float hiddenY = -140f;
    [SerializeField] private float visibleY = 0f;
    [SerializeField] private float panelMoveDuration = 0.35f;
    [SerializeField] private AnimationCurve panelMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Fill Width")]
    [SerializeField] private float fullWidthOverride = -1f;

    [Header("Damage Fill")]
    [SerializeField] private float damageFillDelay = 1f;
    [SerializeField] private float damageFillMoveDuration = 0.25f;
    [SerializeField] private AnimationCurve damageFillMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float fullWidth;
    private float lastNormalizedHealth = 1f;
    private float currentFillWidth;
    private float currentDamageFillWidth;
    private float damageFillStartWidth;
    private float damageFillTargetWidth;
    private float damageFillDelayTimer;
    private float damageFillMoveTimer;
    private bool damageFillIsMoving;
    private float panelStartY;
    private float panelTargetY;
    private float panelMoveTimer;
    private bool panelIsMoving;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = transform as RectTransform;
        }

        ResolveFullWidth();
    }

    private void OnEnable()
    {
        SubscribeGroup();
        SubscribeHealth();
        ResolveFullWidth();

        if (bossHealth != null)
        {
            SetImmediate(bossHealth.HealthNormalized);
        }

        if (activationGroup != null && activationGroup.IsActivated)
        {
            SetBossHealth(activationGroup.BossHealth);
            Show();
        }
        else if (startHidden)
        {
            SetPanelY(hiddenY);
        }
    }

    private void OnDisable()
    {
        UnsubscribeGroup();
        UnsubscribeHealth();
    }

    private void Update()
    {
        UpdatePanelMove();
        UpdateDamageFill();
    }

    private void OnValidate()
    {
        panelMoveDuration = Mathf.Max(0f, panelMoveDuration);
        damageFillDelay = Mathf.Max(0f, damageFillDelay);
        damageFillMoveDuration = Mathf.Max(0f, damageFillMoveDuration);
    }

    public void SetActivationGroup(BossActivationGroup newActivationGroup)
    {
        if (activationGroup == newActivationGroup)
        {
            return;
        }

        UnsubscribeGroup();
        activationGroup = newActivationGroup;
        SubscribeGroup();
    }

    public void SetBossHealth(HealthTarget newBossHealth)
    {
        if (bossHealth == newBossHealth)
        {
            return;
        }

        UnsubscribeHealth();
        bossHealth = newBossHealth;
        SubscribeHealth();

        if (bossHealth != null)
        {
            SetImmediate(bossHealth.HealthNormalized);
        }
    }

    public void Show()
    {
        StartPanelMove(visibleY);
    }

    public void Hide()
    {
        StartPanelMove(hiddenY);
    }

    private void SubscribeGroup()
    {
        if (activationGroup == null)
        {
            return;
        }

        activationGroup.Activated += HandleBossGroupActivated;
        activationGroup.Cleared += HandleBossGroupCleared;
    }

    private void UnsubscribeGroup()
    {
        if (activationGroup == null)
        {
            return;
        }

        activationGroup.Activated -= HandleBossGroupActivated;
        activationGroup.Cleared -= HandleBossGroupCleared;
    }

    private void SubscribeHealth()
    {
        if (bossHealth != null)
        {
            bossHealth.LifeChanged += HandleLifeChanged;
        }
    }

    private void UnsubscribeHealth()
    {
        if (bossHealth != null)
        {
            bossHealth.LifeChanged -= HandleLifeChanged;
        }
    }

    private void HandleBossGroupActivated(BossActivationGroup group)
    {
        SetBossHealth(group.BossHealth);
        Show();
    }

    private void HandleBossGroupCleared(BossActivationGroup group)
    {
        Hide();
    }

    private void HandleLifeChanged(float currentHealth, float maxHealth, float normalizedHealth)
    {
        normalizedHealth = Mathf.Clamp01(normalizedHealth);
        float previousWidth = GetWidth(lastNormalizedHealth);
        float newWidth = GetWidth(normalizedHealth);

        if (normalizedHealth < lastNormalizedHealth)
        {
            SetFillWidth(fill, newWidth);
            currentFillWidth = newWidth;
            SetFillWidth(damageFill, previousWidth);
            currentDamageFillWidth = previousWidth;
            damageFillTargetWidth = newWidth;
            damageFillDelayTimer = damageFillDelay;
            damageFillMoveTimer = 0f;
            damageFillIsMoving = false;
        }
        else
        {
            SetFillWidth(fill, newWidth);
            SetFillWidth(damageFill, newWidth);
            currentFillWidth = newWidth;
            currentDamageFillWidth = newWidth;
            damageFillTargetWidth = newWidth;
            damageFillDelayTimer = 0f;
            damageFillIsMoving = false;
        }

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
                damageFillStartWidth = currentDamageFillWidth;
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
            currentDamageFillWidth = damageFillTargetWidth;
            SetFillWidth(damageFill, currentDamageFillWidth);
            damageFillIsMoving = false;
            return;
        }

        damageFillMoveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(damageFillMoveTimer / damageFillMoveDuration);
        float easedT = damageFillMoveCurve != null ? damageFillMoveCurve.Evaluate(t) : t;
        currentDamageFillWidth = Mathf.Lerp(damageFillStartWidth, damageFillTargetWidth, easedT);
        SetFillWidth(damageFill, currentDamageFillWidth);

        if (t >= 1f)
        {
            damageFillIsMoving = false;
        }
    }

    private void StartPanelMove(float targetY)
    {
        if (panelRoot == null)
        {
            return;
        }

        panelStartY = panelRoot.anchoredPosition.y;
        panelTargetY = targetY;
        panelMoveTimer = 0f;

        if (panelMoveDuration <= 0f)
        {
            SetPanelY(panelTargetY);
            panelIsMoving = false;
            return;
        }

        panelIsMoving = true;
    }

    private void UpdatePanelMove()
    {
        if (!panelIsMoving || panelRoot == null)
        {
            return;
        }

        panelMoveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(panelMoveTimer / panelMoveDuration);
        float easedT = panelMoveCurve != null ? panelMoveCurve.Evaluate(t) : t;
        SetPanelY(Mathf.Lerp(panelStartY, panelTargetY, easedT));

        if (t >= 1f)
        {
            panelIsMoving = false;
        }
    }

    private void SetImmediate(float normalizedHealth)
    {
        lastNormalizedHealth = Mathf.Clamp01(normalizedHealth);
        currentFillWidth = GetWidth(lastNormalizedHealth);
        currentDamageFillWidth = currentFillWidth;
        damageFillTargetWidth = currentFillWidth;
        damageFillDelayTimer = 0f;
        damageFillIsMoving = false;
        SetFillWidth(fill, currentFillWidth);
        SetFillWidth(damageFill, currentDamageFillWidth);
    }

    private void ResolveFullWidth()
    {
        if (fullWidthOverride > 0f)
        {
            fullWidth = fullWidthOverride;
            return;
        }

        if (fill == null)
        {
            fullWidth = 0f;
            return;
        }

        fullWidth = fill.rect.width > 0f ? fill.rect.width : fill.sizeDelta.x;
    }

    private float GetWidth(float normalizedHealth)
    {
        return fullWidth * Mathf.Clamp01(normalizedHealth);
    }

    private void SetPanelY(float y)
    {
        if (panelRoot == null)
        {
            return;
        }

        Vector2 anchoredPosition = panelRoot.anchoredPosition;
        anchoredPosition.y = y;
        panelRoot.anchoredPosition = anchoredPosition;
    }

    private static void SetFillWidth(RectTransform target, float width)
    {
        if (target == null)
        {
            return;
        }

        Vector2 sizeDelta = target.sizeDelta;
        sizeDelta.x = Mathf.Max(0f, width);
        target.sizeDelta = sizeDelta;
    }
}
