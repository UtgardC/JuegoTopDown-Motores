using UnityEngine;

public class HealthChangeRotationUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private RectTransform target;

    [Header("Rotation")]
    [SerializeField] private float degreesPerFullHealthChange = 360f;
    [SerializeField] private bool healRotatesClockwise = true;
    [SerializeField] private bool ignoreFirstLifeChanged = true;
    [SerializeField] private float rotationDuration = 0.25f;
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useUnscaledTime;

    private float lastNormalizedHealth = 1f;
    private float currentAngle;
    private float startAngle;
    private float targetAngle;
    private float rotationTimer;
    private bool rotating;
    private bool receivedFirstLifeChanged;

    private void Reset()
    {
        target = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }
    }

    private void OnEnable()
    {
        Subscribe();

        currentAngle = GetCurrentZAngle();
        startAngle = currentAngle;
        targetAngle = currentAngle;
        lastNormalizedHealth = health != null ? Mathf.Clamp01(health.HealthNormalized) : 1f;
        rotating = false;
        receivedFirstLifeChanged = false;
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        UpdateRotation();
    }

    private void OnValidate()
    {
        rotationDuration = Mathf.Max(0f, rotationDuration);

        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }
    }

    public void SetHealth(Health newHealth)
    {
        if (health == newHealth)
        {
            return;
        }

        Unsubscribe();
        health = newHealth;
        lastNormalizedHealth = health != null ? Mathf.Clamp01(health.HealthNormalized) : 1f;
        receivedFirstLifeChanged = false;
        Subscribe();
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

        if (ignoreFirstLifeChanged && !receivedFirstLifeChanged)
        {
            lastNormalizedHealth = normalizedHealth;
            receivedFirstLifeChanged = true;
            return;
        }

        receivedFirstLifeChanged = true;

        float delta = normalizedHealth - lastNormalizedHealth;
        lastNormalizedHealth = normalizedHealth;

        if (Mathf.Approximately(delta, 0f))
        {
            return;
        }

        float direction = healRotatesClockwise ? -1f : 1f;
        float angleDelta = delta * degreesPerFullHealthChange * direction;

        startAngle = currentAngle;
        targetAngle += angleDelta;
        rotationTimer = 0f;
        rotating = true;
    }

    private void UpdateRotation()
    {
        if (!rotating)
        {
            return;
        }

        if (rotationDuration <= 0f)
        {
            currentAngle = targetAngle;
            ApplyRotation();
            rotating = false;
            return;
        }

        rotationTimer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float t = Mathf.Clamp01(rotationTimer / rotationDuration);
        float easedT = rotationCurve != null ? rotationCurve.Evaluate(t) : t;

        currentAngle = Mathf.LerpUnclamped(startAngle, targetAngle, easedT);
        ApplyRotation();

        if (t >= 1f)
        {
            currentAngle = targetAngle;
            ApplyRotation();
            rotating = false;
        }
    }

    private void ApplyRotation()
    {
        if (target != null)
        {
            target.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
        }
    }

    private float GetCurrentZAngle()
    {
        if (target == null)
        {
            return 0f;
        }

        return target.localEulerAngles.z;
    }
}
