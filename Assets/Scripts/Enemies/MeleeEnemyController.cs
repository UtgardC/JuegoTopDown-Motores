using System.Collections.Generic;
using System;
using UnityEngine;

public class MeleeEnemyController : EnemyController
{
    [Header("Melee")]
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackRadius = 1.1f;
    [SerializeField] private float attackDelay = 0.5f;
    [SerializeField] private float attackCooldown = 0.8f;
    [SerializeField] private float attackOriginHeight = 1f;
    [SerializeField] private float targetAimHeight = 1f;
    [SerializeField] private GameObject impactEffectPrefab;

    [Header("Gizmos")]
    [SerializeField] private NamedGizmoColor attackRangeGizmo = new NamedGizmoColor("Attack Range", Color.magenta);
    [SerializeField] private NamedGizmoColor hitSphereGizmo = new NamedGizmoColor("Hit Sphere", Color.red);

    private readonly HashSet<IDamageable> meleeHits = new HashSet<IDamageable>();

    private bool preparingAttack;
    private float attackTimer;
    private float nextAttackTime;
    private Vector3 lockedAttackDirection;

    public event Action<MeleeEnemyController> AttackStarted;
    public event Action<MeleeEnemyController> AttackExecuted;

    protected override void OnValidate()
    {
        base.OnValidate();
        attackDamage = Mathf.Max(0f, attackDamage);
        attackRange = Mathf.Max(0f, attackRange);
        attackRadius = Mathf.Max(0f, attackRadius);
        attackDelay = Mathf.Max(0f, attackDelay);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        attackOriginHeight = Mathf.Max(0f, attackOriginHeight);
        targetAimHeight = Mathf.Max(0f, targetAimHeight);
    }

    protected override void TickEnemy()
    {
        float distanceToTarget = GetFlatDistanceToTarget();

        if (distanceToTarget > attackRange)
        {
            CancelAttack();
            MoveTowardsTarget();
            RotateTowardsTarget();
            return;
        }

        StopMovement();

        if (!preparingAttack && Time.time < nextAttackTime)
        {
            RotateTowardsTarget();
            return;
        }

        if (!preparingAttack)
        {
            preparingAttack = true;
            attackTimer = 0f;
            lockedAttackDirection = GetAimDirectionToTarget(GetAttackOriginPosition(), targetAimHeight);
            AttackStarted?.Invoke(this);
        }

        RotateTowardsDirection(lockedAttackDirection);
        attackTimer += Time.deltaTime;

        if (attackTimer < attackDelay)
        {
            return;
        }

        ExecuteAttack();
        AttackExecuted?.Invoke(this);
        nextAttackTime = Time.time + attackCooldown;
        CancelAttack();
    }

    private void ExecuteAttack()
    {
        Vector3 origin = GetAttackOriginPosition();
        Vector3 center = origin + (GetFlatAttackDirection() * attackRange);
        Collider[] hits = Physics.OverlapSphere(center, attackRadius, TargetHitMask, QueryTriggerInteraction.Ignore);

        meleeHits.Clear();

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable == null || meleeHits.Contains(damageable))
            {
                continue;
            }

            meleeHits.Add(damageable);
            Vector3 hitPoint = hits[i].ClosestPoint(center);
            damageable.TakeDamage(attackDamage, hitPoint, lockedAttackDirection);
            SpawnImpactEffect(impactEffectPrefab, hitPoint, lockedAttackDirection);
        }
    }

    private void CancelAttack()
    {
        preparingAttack = false;
        attackTimer = 0f;
    }

    private Vector3 GetAttackOriginPosition()
    {
        return transform.position + (Vector3.up * attackOriginHeight);
    }

    private Vector3 GetFlatAttackDirection()
    {
        Vector3 direction = lockedAttackDirection;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = attackRangeGizmo.Color;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 direction = Application.isPlaying && preparingAttack ? GetFlatAttackDirection() : transform.forward;
        Gizmos.color = hitSphereGizmo.Color;
        Gizmos.DrawWireSphere(GetAttackOriginPosition() + direction * attackRange, attackRadius);
    }
}
