using UnityEngine;

public class Damageable : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;

    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, damage));

        if (currentHealth <= 0f && destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}
