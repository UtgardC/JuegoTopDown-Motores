using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private Transform fireOrigin;

    [Header("Masks")]
    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private LayerMask meleeMask = ~0;

    [Header("Dropping")]
    [SerializeField] private float dropImpulse = 8f;
    [SerializeField] private float dropUpwardImpulse = 1f;
    [SerializeField] private float pickupLockoutAfterDrop = 1f;

    [Header("Debug")]
    [SerializeField] private bool drawWeaponUsePreview;

    private readonly HashSet<IDamageable> meleeHits = new HashSet<IDamageable>();

    private WeaponDefinition currentWeapon;
    private GameObject currentWeaponModel;
    private int currentAmmo;
    private float nextFireTime;
    private bool fireHeld;

    public WeaponDefinition CurrentWeapon => currentWeapon;
    public int CurrentAmmo => currentAmmo;
    public bool HasWeapon => currentWeapon != null;

    private void Awake()
    {
        if (fireOrigin == null)
        {
            fireOrigin = transform;
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (fireHeld && currentWeapon != null && currentWeapon.Automatic)
        {
            TryUseWeapon();
        }
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            fireHeld = false;
            return;
        }

        if (!context.started && !context.performed)
        {
            return;
        }

        fireHeld = true;

        if (context.performed)
        {
            TryUseWeapon();
        }
    }

    public void OnDropWeapon(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            DropWeapon();
        }
    }

    public bool TryPickupWeapon(WeaponPickup pickup)
    {
        if (HasWeapon || pickup == null || pickup.Definition == null)
        {
            return false;
        }

        EquipWeapon(pickup.Definition, pickup.RemainingAmmo);
        return true;
    }

    public void EquipWeapon(WeaponDefinition definition, int ammo)
    {
        ClearEquippedModel();

        currentWeapon = definition;
        currentAmmo = Mathf.Max(0, ammo);
        nextFireTime = 0f;
        fireHeld = false;

        if (currentWeapon != null && currentWeapon.Model != null)
        {
            Transform parent = weaponSocket != null ? weaponSocket : transform;
            currentWeaponModel = Instantiate(currentWeapon.Model, parent);
            currentWeaponModel.transform.localPosition = Vector3.zero;
            currentWeaponModel.transform.localRotation = Quaternion.identity;
            currentWeaponModel.transform.localScale = Vector3.one;
        }
    }

    public void DropWeapon()
    {
        if (!HasWeapon)
        {
            return;
        }

        if (currentWeapon.PickupPrefab == null)
        {
            Debug.LogWarning($"{currentWeapon.WeaponName} has no pickup prefab assigned, so it cannot be dropped.", this);
            return;
        }

        Vector3 direction = GetShootDirection();
        Vector3 spawnPosition = GetFireOriginPosition() + direction;
        Quaternion spawnRotation = Quaternion.LookRotation(direction, Vector3.up);

        WeaponPickup pickup = CreatePickup(spawnPosition, spawnRotation);
        pickup.SetRemainingAmmo(currentAmmo, pickupLockoutAfterDrop);

        Rigidbody pickupRigidbody = pickup.GetComponentInParent<Rigidbody>();
        if (pickupRigidbody != null)
        {
            Vector3 impulse = (direction * dropImpulse) + (Vector3.up * dropUpwardImpulse);
            pickupRigidbody.AddForce(impulse, ForceMode.Impulse);
        }

        ClearWeapon();
    }

    private void TryUseWeapon()
    {
        if (!CanUseWeapon())
        {
            return;
        }

        currentAmmo--;
        nextFireTime = Time.time + currentWeapon.FireRate;

        switch (currentWeapon.Mode)
        {
            case WeaponMode.Spread:
                FireSpread();
                break;

            case WeaponMode.Melee:
                FireMelee();
                break;

            case WeaponMode.Normal:
            default:
                FireRaycast(currentWeapon.Damage);
                break;
        }
    }

    private bool CanUseWeapon()
    {
        return currentWeapon != null
            && currentAmmo > 0
            && Time.timeScale > 0f
            && Time.time >= nextFireTime;
    }

    private void FireRaycast(float damage)
    {
        Ray ray = new Ray(GetFireOriginPosition(), GetShootDirection());

        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.Range, raycastMask, QueryTriggerInteraction.Ignore))
        {
            ApplyDamage(hit.collider, damage, hit.point, ray.direction);
        }
    }

    private void FireSpread()
    {
        int pelletCount = currentWeapon.PelletCount;
        float damagePerPellet = currentWeapon.DamagePerPellet;
        float angleStep = pelletCount > 1 ? currentWeapon.SpreadAngle / (pelletCount - 1) : 0f;
        float startAngle = currentWeapon.SpreadAngle * -0.5f;
        Vector3 baseDirection = GetShootDirection();
        Vector3 origin = GetFireOriginPosition();

        for (int i = 0; i < pelletCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector3 pelletDirection = Quaternion.AngleAxis(angle, Vector3.up) * baseDirection;
            Ray ray = new Ray(origin, pelletDirection);

            if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.Range, raycastMask, QueryTriggerInteraction.Ignore))
            {
                ApplyDamage(hit.collider, damagePerPellet, hit.point, pelletDirection);
            }
        }
    }

    private void FireMelee()
    {
        Vector3 direction = GetShootDirection();
        float radius = currentWeapon.Range;
        Vector3 center = GetFireOriginPosition() + (direction * radius);
        Collider[] hits = Physics.OverlapSphere(center, radius, meleeMask, QueryTriggerInteraction.Ignore);

        meleeHits.Clear();

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable == null || meleeHits.Contains(damageable))
            {
                continue;
            }

            meleeHits.Add(damageable);
            damageable.TakeDamage(currentWeapon.Damage, hits[i].ClosestPoint(center), direction);
        }
    }

    private void ApplyDamage(Collider targetCollider, float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        IDamageable damageable = targetCollider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage, hitPoint, hitDirection);
        }
    }

    private WeaponPickup CreatePickup(Vector3 position, Quaternion rotation)
    {
        return Instantiate(currentWeapon.PickupPrefab, position, rotation);
    }

    private Vector3 GetFireOriginPosition()
    {
        return fireOrigin != null ? fireOrigin.position : transform.position;
    }

    private Vector3 GetShootDirection()
    {
        Vector3 direction = transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return Vector3.forward;
        }

        return direction.normalized;
    }

    private void ClearWeapon()
    {
        currentWeapon = null;
        currentAmmo = 0;
        nextFireTime = 0f;
        fireHeld = false;
        ClearEquippedModel();
    }

    private void ClearEquippedModel()
    {
        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawWeaponUsePreview || currentWeapon == null)
        {
            return;
        }

        Gizmos.color = Color.red;

        switch (currentWeapon.Mode)
        {
            case WeaponMode.Spread:
                DrawSpreadPreview();
                break;

            case WeaponMode.Melee:
                DrawMeleePreview();
                break;

            case WeaponMode.Normal:
            default:
                DrawRaycastPreview(GetShootDirection());
                break;
        }
    }

    private void DrawRaycastPreview(Vector3 direction)
    {
        Vector3 origin = GetFireOriginPosition();
        Gizmos.DrawLine(origin, origin + (direction * currentWeapon.Range));
    }

    private void DrawSpreadPreview()
    {
        int pelletCount = currentWeapon.PelletCount;
        float angleStep = pelletCount > 1 ? currentWeapon.SpreadAngle / (pelletCount - 1) : 0f;
        float startAngle = currentWeapon.SpreadAngle * -0.5f;
        Vector3 baseDirection = GetShootDirection();

        for (int i = 0; i < pelletCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector3 pelletDirection = Quaternion.AngleAxis(angle, Vector3.up) * baseDirection;
            DrawRaycastPreview(pelletDirection);
        }
    }

    private void DrawMeleePreview()
    {
        Vector3 direction = GetShootDirection();
        float radius = currentWeapon.Range;
        Vector3 center = GetFireOriginPosition() + (direction * radius);
        Gizmos.DrawWireSphere(center, radius);
    }
}
