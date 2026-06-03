using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class HealthPickup : MonoBehaviour
{
    [SerializeField] private float healAmount = 25f;
    [SerializeField] private bool collectAtFullHealth;
    [SerializeField] private float destroyDelay;
    [SerializeField] private UnityEvent onCollect = new UnityEvent();

    private Collider pickupCollider;
    private bool collected;

    public UnityEvent OnCollect => onCollect;

    private void Awake()
    {
        pickupCollider = GetComponent<Collider>();
        pickupCollider.isTrigger = true;
    }

    private void OnValidate()
    {
        healAmount = Mathf.Max(0f, healAmount);
        destroyDelay = Mathf.Max(0f, destroyDelay);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider other)
    {
        if (collected || other == null)
        {
            return;
        }

        Health targetHealth = other.GetComponentInParent<Health>();
        if (targetHealth == null)
        {
            return;
        }

        float previousHealth = targetHealth.CurrentHealth;
        targetHealth.Heal(healAmount);

        if (!collectAtFullHealth && targetHealth.CurrentHealth <= previousHealth)
        {
            return;
        }

        Collect();
    }

    private void Collect()
    {
        collected = true;

        if (pickupCollider != null)
        {
            pickupCollider.enabled = false;
        }

        onCollect.Invoke();
        Destroy(gameObject, destroyDelay);
    }
}
