using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class HealthEvent : UnityEvent<Health>
{
}

public class Health : MonoBehaviour, IDamageable, IHealthReadable, IHealable, IKillable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool startAtFullHealth = true;
    [SerializeField] private float startingHealth = 100f;

    [Header("Damage Immunity")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("A hit must deal more than this percentage of max health to trigger damage immunity. 0.05 = 5%.")]
    private float damageImmunityThresholdNormalized = 0.05f;

    [SerializeField]
    [Tooltip("Seconds of damage immunity granted after a hit passes the threshold.")]
    private float damageImmunityDuration = 0.3f;

    [Header("Events")]
    [SerializeField]
    [Tooltip("Current health, max health, normalized health.")]
    private HealthChangedEvent onLifeChanged = new HealthChangedEvent();

    [SerializeField]
    [Tooltip("Damage amount, hit point, hit direction.")]
    private DamageTakenEvent onDamaged = new DamageTakenEvent();

    [SerializeField]
    [Tooltip("Heal amount.")]
    private HealedEvent onHealed = new HealedEvent();

    [SerializeField]
    [Tooltip("Fired once when health reaches zero.")]
    private HealthEvent onDeath = new HealthEvent();

    [Header("Runtime State")]
    [SerializeField, ReadOnlyField] private float currentHealth;
    [SerializeField, ReadOnlyField] private float currentHealthNormalized;
    [SerializeField, ReadOnlyField] private bool isDead;
    [SerializeField, ReadOnlyField] private bool isDamageImmune;
    [SerializeField, ReadOnlyField] private float damageImmunityRemaining;

    private float damageImmuneUntilTime;
    private bool initialized;

    public event Action<float, float, float> LifeChanged;
    public event Action<float, Vector3, Vector3> Damaged;
    public event Action<float> Healed;
    public event Action<Health> Died;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthNormalized => currentHealthNormalized;
    public bool IsDead => isDead;
    public bool IsDamageImmune => Time.time < damageImmuneUntilTime;
    public float DamageImmunityRemaining => Mathf.Max(0f, damageImmuneUntilTime - Time.time);

    public HealthChangedEvent OnLifeChanged => onLifeChanged;
    public DamageTakenEvent OnDamaged => onDamaged;
    public HealedEvent OnHealed => onHealed;
    public HealthEvent OnDeath => onDeath;

    private void Awake()
    {
        InitializeHealth();
    }

    private void Start()
    {
        NotifyLifeChanged();
    }

    private void Update()
    {
        UpdateRuntimeState();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        startingHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);
        damageImmunityDuration = Mathf.Max(0f, damageImmunityDuration);
        UpdateRuntimeState();
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        Damage(damage, hitPoint, hitDirection);
    }

    public bool Damage(float damage)
    {
        return Damage(damage, transform.position, Vector3.zero);
    }

    public bool Damage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        InitializeHealth();
        UpdateRuntimeState();

        if (isDead || isDamageImmune)
        {
            return false;
        }

        float requestedDamage = Mathf.Max(0f, damage);
        float appliedDamage = Mathf.Min(currentHealth, requestedDamage);
        if (appliedDamage <= 0f)
        {
            return false;
        }

        currentHealth = Mathf.Clamp(currentHealth - appliedDamage, 0f, maxHealth);

        if (appliedDamage > maxHealth * damageImmunityThresholdNormalized)
        {
            damageImmuneUntilTime = Time.time + damageImmunityDuration;
        }

        UpdateRuntimeState();
        NotifyLifeChanged();

        Damaged?.Invoke(appliedDamage, hitPoint, hitDirection);
        onDamaged.Invoke(appliedDamage, hitPoint, hitDirection);

        if (currentHealth <= 0f)
        {
            Die();
        }

        return true;
    }

    public void Heal(float amount)
    {
        InitializeHealth();

        if (isDead)
        {
            return;
        }

        float appliedHeal = Mathf.Min(maxHealth - currentHealth, Mathf.Max(0f, amount));
        if (appliedHeal <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth + appliedHeal, 0f, maxHealth);
        UpdateRuntimeState();
        NotifyLifeChanged();

        Healed?.Invoke(appliedHeal);
        onHealed.Invoke(appliedHeal);
    }

    public void SetHealth(float health)
    {
        InitializeHealth();

        bool wasDead = isDead;
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        if (currentHealth > 0f)
        {
            isDead = false;
        }

        UpdateRuntimeState();
        NotifyLifeChanged();

        if (currentHealth <= 0f && !wasDead)
        {
            Die();
        }
    }

    public void ResetHealth()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;
        isDead = false;
        damageImmuneUntilTime = 0f;
        initialized = true;
        UpdateRuntimeState();
        NotifyLifeChanged();
    }

    public void Kill()
    {
        InitializeHealth();

        if (isDead)
        {
            return;
        }

        currentHealth = 0f;
        UpdateRuntimeState();
        NotifyLifeChanged();
        Die();
    }

    private void InitializeHealth()
    {
        if (initialized)
        {
            return;
        }

        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = startAtFullHealth ? maxHealth : Mathf.Clamp(startingHealth, 0f, maxHealth);
        isDead = currentHealth <= 0f;
        initialized = true;
        UpdateRuntimeState();
    }

    private void NotifyLifeChanged()
    {
        LifeChanged?.Invoke(currentHealth, maxHealth, HealthNormalized);
        onLifeChanged.Invoke(currentHealth, maxHealth, HealthNormalized);
    }

    private void UpdateRuntimeState()
    {
        currentHealthNormalized = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        isDamageImmune = Time.time < damageImmuneUntilTime;
        damageImmunityRemaining = Mathf.Max(0f, damageImmuneUntilTime - Time.time);
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        UpdateRuntimeState();
        Died?.Invoke(this);
        onDeath.Invoke(this);
    }
}
