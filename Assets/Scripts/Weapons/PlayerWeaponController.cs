using System;
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

    [Header("Unarmed Melee")]
    [SerializeField] private float unarmedMeleeHitboxSize = 1f;
    [SerializeField] private float unarmedMeleeDamage = 8f;
    [SerializeField] private float unarmedMeleeCooldown = 0.35f;
    [SerializeField] private GameObject unarmedMeleeImpactEffectPrefab;

    [Header("Bullet Trails")]
    [SerializeField] private BulletTrailEffect bulletTrailPrefab;
    [SerializeField] private bool spawnTrailsForSpread = true;

    [Header("Debug")]
    [SerializeField] private bool drawWeaponUsePreview;
    [SerializeField] private bool logWeaponDamage;

    private readonly HashSet<IDamageable> meleeHits = new HashSet<IDamageable>();
    private readonly Dictionary<IDamageable, SpreadDamageHit> spreadHits = new Dictionary<IDamageable, SpreadDamageHit>();

    private WeaponDefinition currentWeapon;
    private GameObject currentWeaponModel;
    private WeaponVisualEffects currentWeaponVisualEffects;
    private int currentAmmo;
    private float nextFireTime;
    private bool fireHeld;

    public WeaponDefinition CurrentWeapon => currentWeapon;
    public int CurrentAmmo => currentAmmo;
    public bool HasWeapon => currentWeapon != null;

    public event Action<WeaponDefinition, int> WeaponChanged;
    public event Action<int> AmmoChanged;
    public event Action<WeaponDefinition, int> WeaponFired;
    public event Action<WeaponDefinition> EmptyWeaponUsed;
    public event Action UnarmedMeleeUsed;
    public event Action<Vector3> UnarmedMeleeImpact;

    private struct SpreadDamageHit
    {
        public float Damage;
        public Vector3 HitPoint;
        public Vector3 HitDirection;
    }

    private void Awake()
    {
        if (fireOrigin == null)
        {
            fireOrigin = transform;
        }
    }

    private void OnValidate()
    {
        unarmedMeleeHitboxSize = Mathf.Max(0f, unarmedMeleeHitboxSize);
        unarmedMeleeDamage = Mathf.Max(0f, unarmedMeleeDamage);
        unarmedMeleeCooldown = Mathf.Max(0f, unarmedMeleeCooldown);
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
            currentWeaponVisualEffects = currentWeaponModel.GetComponentInChildren<WeaponVisualEffects>(true);
        }

        NotifyWeaponChanged();
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
        if (currentWeapon == null)
        {
            TryUseUnarmedMelee();
            return;
        }

        if (Time.timeScale <= 0f || Time.time < nextFireTime)
        {
            return;
        }

        if (currentAmmo <= 0)
        {
            nextFireTime = Time.time + Mathf.Max(0.1f, currentWeapon.FireRate);
            EmptyWeaponUsed?.Invoke(currentWeapon);
            return;
        }

        if (!CanUseWeapon())
        {
            return;
        }

        currentAmmo--;
        nextFireTime = Time.time + currentWeapon.FireRate;
        NotifyAmmoChanged();
        WeaponFired?.Invoke(currentWeapon, currentAmmo);
        PlayMuzzleFlash();

        switch (currentWeapon.Mode)
        {
            case WeaponMode.Spread:
                FireSpread();
                break;

            case WeaponMode.Melee:
                FireMelee(currentWeapon.Damage, currentWeapon.Range, currentWeapon.ImpactEffectPrefab, "Melee", false);
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

    private void TryUseUnarmedMelee()
    {
        if (Time.timeScale <= 0f || Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + unarmedMeleeCooldown;
        UnarmedMeleeUsed?.Invoke();
        FireMelee(unarmedMeleeDamage, unarmedMeleeHitboxSize, unarmedMeleeImpactEffectPrefab, "Unarmed melee", true);
    }

    private void FireRaycast(float damage)
    {
        Vector3 origin = GetFireOriginPosition();
        Vector3 direction = GetShootDirection();
        Ray ray = new Ray(origin, direction);

        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.Range, raycastMask, QueryTriggerInteraction.Ignore))
        {
            SpawnBulletTrail(origin, hit.point);
            LogWeaponDamage($"Normal hit {GetColliderInfo(hit.collider)} for {damage:0.##} damage.");
            ApplyDamage(hit.collider, damage, hit.point, direction, true);
        }
        else
        {
            SpawnBulletTrail(origin, origin + (direction * currentWeapon.Range));
            LogWeaponDamage($"Normal missed. Range: {currentWeapon.Range:0.##}.");
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

        spreadHits.Clear();
        LogWeaponDamage($"Spread fired. Pellets: {pelletCount}. Damage per pellet: {damagePerPellet:0.##}. Range: {currentWeapon.Range:0.##}.");

        for (int i = 0; i < pelletCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector3 pelletDirection = Quaternion.AngleAxis(angle, Vector3.up) * baseDirection;
            Ray ray = new Ray(origin, pelletDirection);

            if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.Range, raycastMask, QueryTriggerInteraction.Ignore))
            {
                SpawnSpreadBulletTrail(origin, hit.point);

                IDamageable damageable = GetDamageable(hit.collider);
                if (damageable == null)
                {
                    LogWeaponDamage($"Spread pellet {i} hit {GetColliderInfo(hit.collider)}, but no IDamageable was found in parents.");
                    continue;
                }

                LogWeaponDamage($"Spread pellet {i} hit {GetDamageableInfo(damageable)} through {GetColliderInfo(hit.collider)} for {damagePerPellet:0.##} damage.");
                AccumulateSpreadDamage(damageable, damagePerPellet, hit.point, pelletDirection);
                SpawnImpactEffect(hit.point, pelletDirection);
            }
            else
            {
                SpawnSpreadBulletTrail(origin, origin + (pelletDirection * currentWeapon.Range));
                LogWeaponDamage($"Spread pellet {i} missed.");
            }
        }

        ApplySpreadDamage();
    }

    private void FireMelee(float damage, float hitboxSize, GameObject impactEffectPrefab, string logLabel, bool notifyUnarmedImpact)
    {
        Vector3 direction = GetShootDirection();
        float radius = Mathf.Max(0f, hitboxSize);
        Vector3 center = GetFireOriginPosition() + (direction * radius);
        Collider[] hits = Physics.OverlapSphere(center, radius, meleeMask, QueryTriggerInteraction.Ignore);

        meleeHits.Clear();
        bool impactNotified = false;

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable == null || meleeHits.Contains(damageable))
            {
                continue;
            }

            meleeHits.Add(damageable);
            Vector3 hitPoint = hits[i].ClosestPoint(center);
            Vector3 effectDirection = GetDirectionFromHitPointToPlayer(hitPoint);

            LogWeaponDamage($"{logLabel} hit {GetDamageableInfo(damageable)} through {GetColliderInfo(hits[i])} for {damage:0.##} damage.");
            damageable.TakeDamage(damage, hitPoint, direction);
            SpawnImpactEffect(impactEffectPrefab, hitPoint, effectDirection);

            if (notifyUnarmedImpact && !impactNotified)
            {
                impactNotified = true;
                UnarmedMeleeImpact?.Invoke(hitPoint);
            }
        }
    }

    private void ApplyDamage(Collider targetCollider, float damage, Vector3 hitPoint, Vector3 hitDirection, bool spawnImpactEffect)
    {
        IDamageable damageable = GetDamageable(targetCollider);
        if (damageable != null)
        {
            LogWeaponDamage($"Applying {damage:0.##} damage to {GetDamageableInfo(damageable)}.");
            damageable.TakeDamage(damage, hitPoint, hitDirection);

            if (spawnImpactEffect)
            {
                SpawnImpactEffect(hitPoint, hitDirection);
            }
        }
        else
        {
            LogWeaponDamage($"Hit {GetColliderInfo(targetCollider)}, but no IDamageable was found in parents.");
        }
    }

    private IDamageable GetDamageable(Collider targetCollider)
    {
        return targetCollider != null ? targetCollider.GetComponentInParent<IDamageable>() : null;
    }

    private void AccumulateSpreadDamage(IDamageable damageable, float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (!spreadHits.TryGetValue(damageable, out SpreadDamageHit hitData))
        {
            hitData.HitPoint = hitPoint;
            hitData.HitDirection = hitDirection;
        }

        hitData.Damage += damage;
        spreadHits[damageable] = hitData;
        LogWeaponDamage($"Spread accumulated {hitData.Damage:0.##} total damage for {GetDamageableInfo(damageable)}.");
    }

    private void ApplySpreadDamage()
    {
        if (spreadHits.Count == 0)
        {
            LogWeaponDamage("Spread applied no damage.");
            return;
        }

        foreach (KeyValuePair<IDamageable, SpreadDamageHit> spreadHit in spreadHits)
        {
            SpreadDamageHit hitData = spreadHit.Value;
            LogWeaponDamage($"Spread applying {hitData.Damage:0.##} total damage to {GetDamageableInfo(spreadHit.Key)}.");
            spreadHit.Key.TakeDamage(hitData.Damage, hitData.HitPoint, hitData.HitDirection);
        }

        spreadHits.Clear();
    }

    private void LogWeaponDamage(string message)
    {
        if (logWeaponDamage)
        {
            Debug.Log($"[WeaponDamage] {message}", this);
        }
    }

    private string GetDamageableInfo(IDamageable damageable)
    {
        if (damageable is Component component)
        {
            return component.name;
        }

        return damageable != null ? damageable.ToString() : "None";
    }

    private string GetColliderInfo(Collider targetCollider)
    {
        if (targetCollider == null)
        {
            return "None";
        }

        string layerName = LayerMask.LayerToName(targetCollider.gameObject.layer);
        return $"{targetCollider.name} (Layer: {layerName})";
    }

    private void PlayMuzzleFlash()
    {
        if (currentWeaponVisualEffects != null)
        {
            currentWeaponVisualEffects.PlayMuzzleFlash();
        }
    }

    private void SpawnImpactEffect(Vector3 position, Vector3 direction)
    {
        SpawnImpactEffect(currentWeapon != null ? currentWeapon.ImpactEffectPrefab : null, position, direction);
    }

    private void SpawnImpactEffect(GameObject impactEffectPrefab, Vector3 position, Vector3 direction)
    {
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, position, GetEffectRotation(direction));
        }
    }

    private void SpawnBulletTrail(Vector3 start, Vector3 end)
    {
        if (bulletTrailPrefab == null)
        {
            return;
        }

        BulletTrailEffect trail = Instantiate(bulletTrailPrefab, start, Quaternion.identity);
        trail.Play(start, end);
    }

    private void SpawnSpreadBulletTrail(Vector3 start, Vector3 end)
    {
        if (spawnTrailsForSpread)
        {
            SpawnBulletTrail(start, end);
        }
    }

    private Quaternion GetEffectRotation(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = transform.forward;
        }

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private Vector3 GetDirectionFromHitPointToPlayer(Vector3 hitPoint)
    {
        Vector3 direction = transform.position - hitPoint;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return -GetShootDirection();
        }

        return direction.normalized;
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
        currentWeaponVisualEffects = null;
        ClearEquippedModel();
        NotifyWeaponChanged();
    }

    private void ClearEquippedModel()
    {
        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
        }

        currentWeaponVisualEffects = null;
    }

    private void NotifyWeaponChanged()
    {
        WeaponChanged?.Invoke(currentWeapon, currentAmmo);
        NotifyAmmoChanged();
    }

    private void NotifyAmmoChanged()
    {
        AmmoChanged?.Invoke(currentAmmo);
    }

    private void OnDrawGizmos()
    {
        if (!drawWeaponUsePreview)
        {
            return;
        }

        Gizmos.color = Color.red;

        if (currentWeapon == null)
        {
            DrawMeleePreview(unarmedMeleeHitboxSize);
            return;
        }

        switch (currentWeapon.Mode)
        {
            case WeaponMode.Spread:
                DrawSpreadPreview();
                break;

            case WeaponMode.Melee:
                DrawMeleePreview(currentWeapon.Range);
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

    private void DrawMeleePreview(float hitboxSize)
    {
        Vector3 direction = GetShootDirection();
        float radius = Mathf.Max(0f, hitboxSize);
        Vector3 center = GetFireOriginPosition() + (direction * radius);
        Gizmos.DrawWireSphere(center, radius);
    }
}
