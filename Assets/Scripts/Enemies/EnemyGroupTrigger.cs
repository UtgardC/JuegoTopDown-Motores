using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyGroupTrigger : MonoBehaviour
{
    [SerializeField] private EnemyActivationGroup activationGroup;
    [SerializeField] private bool disableAfterActivation = true;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
        activationGroup = GetComponentInParent<EnemyActivationGroup>();
    }

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activationGroup == null)
        {
            return;
        }

        Health playerHealth = other.GetComponentInParent<Health>();
        if (playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        activationGroup.Activate(playerHealth);

        if (disableAfterActivation)
        {
            gameObject.SetActive(false);
        }
    }
}
