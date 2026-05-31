using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponPickupTrigger : MonoBehaviour
{
    [SerializeField] private WeaponPickup pickup;

    private void Awake()
    {
        if (pickup == null)
        {
            pickup = GetComponentInParent<WeaponPickup>();
        }

        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        pickup?.TryGiveWeaponTo(other);
    }

    private void OnTriggerStay(Collider other)
    {
        pickup?.TryGiveWeaponTo(other);
    }
}
