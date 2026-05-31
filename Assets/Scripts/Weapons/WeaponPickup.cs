using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponDefinition definition;
    [SerializeField] private int remainingAmmo = -1;
    [SerializeField] private GameObject pickupRoot;

    private float pickupEnabledTime;

    public WeaponDefinition Definition => definition;
    public int RemainingAmmo => remainingAmmo;

    private void Awake()
    {
        if (pickupRoot == null)
        {
            pickupRoot = gameObject;
        }

        if (definition != null && remainingAmmo < 0)
        {
            remainingAmmo = definition.Ammo;
        }
    }

    public void SetRemainingAmmo(int ammo, float pickupLockoutSeconds)
    {
        remainingAmmo = Mathf.Max(0, ammo);
        pickupEnabledTime = Time.time + Mathf.Max(0f, pickupLockoutSeconds);
    }

    public void TryGiveWeaponTo(Collider other)
    {
        if (Time.time < pickupEnabledTime || definition == null || other == null)
        {
            return;
        }

        PlayerWeaponController playerWeaponController = other.GetComponentInParent<PlayerWeaponController>();
        if (playerWeaponController == null)
        {
            return;
        }

        if (playerWeaponController.TryPickupWeapon(this))
        {
            Destroy(pickupRoot != null ? pickupRoot : gameObject);
        }
    }
}
