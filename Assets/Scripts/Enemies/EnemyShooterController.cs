using System;
using UnityEngine;

public abstract class EnemyShooterController : EnemyController
{
    [Header("Shooting")]
    [SerializeField] private float bulletDamage = 10f;
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float fireCooldown = 1f;
    [SerializeField] private float aimDelay = 0.5f;
    [SerializeField] private float shotOriginHeight = 1f;
    [SerializeField] private float targetAimHeight = 1f;
    [SerializeField] private bool canAttackWhileRetreating = true;
    [SerializeField] private GameObject impactEffectPrefab;

    [Header("Aim Delay Scaling")]
    [SerializeField] private float safeRangeAimDelayMultiplier = 0.5f;
    [SerializeField] private float closeRangeAimDelayMultiplier = 0.25f;

    [Header("Bullet Trails")]
    [SerializeField] private BulletTrailEffect bulletTrailPrefab;
    [SerializeField] private bool spawnTrailsForSpread = true;

    [Header("Gizmos")]
    [SerializeField] private NamedGizmoColor attackRangeGizmo = new NamedGizmoColor("Attack Range", Color.magenta);

    [Header("Shot Debug")]
    [SerializeField] private bool drawShotDebugLaser;
    [SerializeField] private float shotDebugLaserDuration = 0.25f;
    [SerializeField] private Color shotDebugHitColor = Color.red;
    [SerializeField] private Color shotDebugMissColor = Color.yellow;

    private bool aiming;
    private float aimTimer;
    private float nextFireTime;
    private Vector3 lockedAimDirection;

    protected float BulletDamage => bulletDamage;
    protected float AttackRange => attackRange;
    protected float AimDelay => GetScaledAimDelay(GetFlatDistanceToTarget());
    protected Vector3 LockedAimDirection => lockedAimDirection;
    protected bool IsAiming => aiming;
    protected GameObject ImpactEffectPrefab => impactEffectPrefab;
    protected bool SpawnTrailsForSpread => spawnTrailsForSpread;

    public event Action<EnemyShooterController> ShotFired;

    protected override void OnValidate()
    {
        base.OnValidate();
        bulletDamage = Mathf.Max(0f, bulletDamage);
        attackRange = Mathf.Max(0f, attackRange);
        fireCooldown = Mathf.Max(0f, fireCooldown);
        aimDelay = Mathf.Max(0f, aimDelay);
        shotOriginHeight = Mathf.Max(0f, shotOriginHeight);
        targetAimHeight = Mathf.Max(0f, targetAimHeight);
        safeRangeAimDelayMultiplier = Mathf.Max(0f, safeRangeAimDelayMultiplier);
        closeRangeAimDelayMultiplier = Mathf.Max(0f, closeRangeAimDelayMultiplier);
        shotDebugLaserDuration = Mathf.Max(0f, shotDebugLaserDuration);
    }

    protected override void TickEnemy()
    {
        float distanceToTarget = GetFlatDistanceToTarget();
        bool retreating = ShouldRetreatFromTarget(distanceToTarget);

        if (retreating)
        {
            MoveAwayFromTarget();

            if (!canAttackWhileRetreating)
            {
                CancelAiming();
                RotateTowardsTarget();
                return;
            }
        }
        else if (distanceToTarget > attackRange)
        {
            CancelAiming();
            MoveTowardsTarget();
            RotateTowardsTarget();
            return;
        }
        else
        {
            HandleInRangeMovement(distanceToTarget);
        }

        RotateTowardsDirection(aiming ? lockedAimDirection : GetFlatDirectionToTarget());

        if (distanceToTarget <= attackRange)
        {
            TickAttackWindup(distanceToTarget);
        }
    }

    protected virtual void HandleInRangeMovement(float distanceToTarget)
    {
        StopMovement();
    }

    protected void CancelAiming()
    {
        aiming = false;
        aimTimer = 0f;
    }

    protected Vector3 GetShotOriginPosition()
    {
        return transform.position + (Vector3.up * shotOriginHeight);
    }

    protected Vector3 GetCurrentAimDirection()
    {
        return GetAimDirectionToTarget(GetShotOriginPosition(), targetAimHeight);
    }

    protected void FireSingleRay(Vector3 direction, float damage)
    {
        Vector3 origin = GetShotOriginPosition();
        Vector3 shotDirection = GetValidShotDirection(direction);

        if (RaycastTarget(origin, shotDirection, attackRange, out RaycastHit hit))
        {
            DrawShotDebugLaser(origin, hit.point, true);
            SpawnBulletTrail(origin, hit.point);
            ApplyDamageToHit(hit, damage, shotDirection, impactEffectPrefab);
        }
        else
        {
            Vector3 end = GetMissEndPoint(origin, shotDirection);
            DrawShotDebugLaser(origin, end, false);
            SpawnBulletTrail(origin, end);
        }
    }

    protected void SpawnBulletTrail(Vector3 start, Vector3 end)
    {
        if (bulletTrailPrefab == null)
        {
            return;
        }

        BulletTrailEffect trail = Instantiate(bulletTrailPrefab, start, Quaternion.identity);
        trail.Play(start, end);
    }

    protected Vector3 GetMissEndPoint(Vector3 origin, Vector3 direction)
    {
        return origin + (GetValidShotDirection(direction) * attackRange);
    }

    protected Vector3 GetValidShotDirection(Vector3 direction)
    {
        return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
    }

    protected void DrawShotDebugLaser(Vector3 start, Vector3 end, bool hit)
    {
        if (!drawShotDebugLaser)
        {
            return;
        }

        Debug.DrawLine(start, end, hit ? shotDebugHitColor : shotDebugMissColor, shotDebugLaserDuration);
    }

    private void TickAttackWindup(float distanceToTarget)
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        if (!aiming)
        {
            aiming = true;
            aimTimer = 0f;
            lockedAimDirection = GetCurrentAimDirection();
        }

        if (distanceToTarget > attackRange)
        {
            CancelAiming();
            return;
        }

        aimTimer += Time.deltaTime;
        if (aimTimer < GetScaledAimDelay(distanceToTarget))
        {
            return;
        }

        ExecuteLockedAttack(lockedAimDirection);
        NotifyShotFired();
        nextFireTime = Time.time + fireCooldown;
        CancelAiming();
    }

    protected abstract void ExecuteLockedAttack(Vector3 direction);

    protected void NotifyShotFired()
    {
        ShotFired?.Invoke(this);
    }

    private float GetScaledAimDelay(float distanceToTarget)
    {
        if (distanceToTarget <= CloseRange)
        {
            return aimDelay * closeRangeAimDelayMultiplier;
        }

        if (distanceToTarget <= SafeRange)
        {
            return aimDelay * safeRangeAimDelayMultiplier;
        }

        return aimDelay;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = attackRangeGizmo.Color;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
