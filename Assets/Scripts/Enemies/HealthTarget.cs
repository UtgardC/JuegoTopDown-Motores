using System;
using UnityEngine;
using UnityEngine.Events;

public interface IHealthReadable
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    float HealthNormalized { get; }
    bool IsDead { get; }
}

public interface IHealable
{
    void Heal(float amount);
}

public interface IKillable
{
    void Kill();
}

[Serializable]
public class HealthChangedEvent : UnityEvent<float, float, float>
{
}

[Serializable]
public class DamageTakenEvent : UnityEvent<float, Vector3, Vector3>
{
}

[Serializable]
public class HealedEvent : UnityEvent<float>
{
}

[Serializable]
public class HealthTargetEvent : UnityEvent<HealthTarget>
{
}

public class HealthTarget : MonoBehaviour, IDamageable, IHealthReadable, IHealable, IKillable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool startAtFullHealth = true;
    [SerializeField] private float startingHealth = 100f;
    [SerializeField] private bool invulnerable;
    [SerializeField] private bool destroyOnDeath;

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
    private HealthTargetEvent onDeath = new HealthTargetEvent();

    private float currentHealth;
    private bool initialized;
    private bool isDead;

    public event Action<float, float, float> LifeChanged;
    public event Action<float, Vector3, Vector3> Damaged;
    public event Action<float> Healed;
    public event Action<HealthTarget> Died;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool IsDead => isDead;

    public HealthChangedEvent OnLifeChanged => onLifeChanged;
    public DamageTakenEvent OnDamaged => onDamaged;
    public HealedEvent OnHealed => onHealed;
    public HealthTargetEvent OnDeath => onDeath;

    private void Awake()
    {
        InitializeHealth();
    }

    private void Start()
    {
        NotifyLifeChanged();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        startingHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        InitializeHealth();

        if (invulnerable || isDead)
        {
            return;
        }

        float appliedDamage = Mathf.Max(0f, damage);
        if (appliedDamage <= 0f)
        {
            return;
        }

        SetHealthInternal(currentHealth - appliedDamage);
        Damaged?.Invoke(appliedDamage, hitPoint, hitDirection);
        onDamaged.Invoke(appliedDamage, hitPoint, hitDirection);
    }

    public void Heal(float amount)
    {
        InitializeHealth();

        if (isDead)
        {
            return;
        }

        float appliedHeal = Mathf.Max(0f, amount);
        if (appliedHeal <= 0f)
        {
            return;
        }

        SetHealthInternal(currentHealth + appliedHeal);
        Healed?.Invoke(appliedHeal);
        onHealed.Invoke(appliedHeal);
    }

    public void SetHealth(float health)
    {
        InitializeHealth();
        SetHealthInternal(health);
    }

    public void ResetHealth()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;
        isDead = false;
        initialized = true;
        NotifyLifeChanged();
    }

    public void Kill()
    {
        InitializeHealth();

        if (isDead)
        {
            return;
        }

        SetHealthInternal(0f);
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
    }

    private void SetHealthInternal(float health)
    {
        float previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);

        if (Mathf.Approximately(previousHealth, currentHealth))
        {
            return;
        }

        NotifyLifeChanged();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void NotifyLifeChanged()
    {
        LifeChanged?.Invoke(currentHealth, maxHealth, HealthNormalized);
        onLifeChanged.Invoke(currentHealth, maxHealth, HealthNormalized);
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        Died?.Invoke(this);
        onDeath.Invoke(this);

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}
