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

    [Header("Gizmos")]
    [SerializeField] private NamedGizmoColor attackRangeGizmo = new NamedGizmoColor("Attack Range", Color.magenta);

    private bool aiming;
    private float aimTimer;
    private float nextFireTime;
    private Vector3 lockedAimDirection;

    protected float BulletDamage => bulletDamage;
    protected float AttackRange => attackRange;
    protected float AimDelay => aimDelay;
    protected Vector3 LockedAimDirection => lockedAimDirection;
    protected bool IsAiming => aiming;
    protected GameObject ImpactEffectPrefab => impactEffectPrefab;

    protected override void OnValidate()
    {
        base.OnValidate();
        bulletDamage = Mathf.Max(0f, bulletDamage);
        attackRange = Mathf.Max(0f, attackRange);
        fireCooldown = Mathf.Max(0f, fireCooldown);
        aimDelay = Mathf.Max(0f, aimDelay);
        shotOriginHeight = Mathf.Max(0f, shotOriginHeight);
        targetAimHeight = Mathf.Max(0f, targetAimHeight);
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
        if (RaycastTarget(GetShotOriginPosition(), direction, attackRange, out RaycastHit hit))
        {
            ApplyDamageToHit(hit, damage, direction, impactEffectPrefab);
        }
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
        if (aimTimer < aimDelay)
        {
            return;
        }

        ExecuteLockedAttack(lockedAimDirection);
        nextFireTime = Time.time + fireCooldown;
        CancelAiming();
    }

    protected abstract void ExecuteLockedAttack(Vector3 direction);

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = attackRangeGizmo.Color;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
