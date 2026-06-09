using UnityEngine;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(HealthTarget))]
public abstract class EnemyController : MonoBehaviour
{
    [System.Serializable]
    protected struct NamedGizmoColor
    {
        [SerializeField] private string name;
        [SerializeField] private Color color;

        public Color Color => color;

        public NamedGizmoColor(string name, Color color)
        {
            this.name = name;
            this.color = color;
        }
    }

    [Header("References")]
    [SerializeField] private HealthTarget healthTarget;
    [SerializeField] private Health playerHealth;
    [SerializeField] private EnemyActivationGroup activationGroup;

    [Header("Combat Target")]
    [SerializeField] private LayerMask targetHitMask = ~0;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 14f;
    [SerializeField] private float closeRange = 2f;
    [SerializeField] private float safeRange = 3.25f;

    [Header("Drops")]
    [SerializeField] private WeaponPickup dropPickupPrefab;
    [SerializeField] private int dropAmmo = -1;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float dropImpulse = 4f;
    [SerializeField] private float dropUpwardImpulse = 1f;
    [SerializeField] private float dropPickupLockout = 0.6f;

    [Header("Gizmos")]
    [SerializeField] private bool drawCombatRangeGizmos = true;
    [SerializeField] private NamedGizmoColor closeRangeGizmo = new NamedGizmoColor("Close Range", Color.red);
    [SerializeField] private NamedGizmoColor safeRangeGizmo = new NamedGizmoColor("Safe Range", Color.cyan);

    private NavMeshAgent navMeshAgent;
    private Transform target;
    private bool activated;
    private bool retreating;
    private bool droppedWeapon;

    protected HealthTarget EnemyHealth => healthTarget;
    protected Transform Target => target;
    protected Health TargetHealth => playerHealth;
    protected LayerMask TargetHitMask => targetHitMask;
    protected float MoveSpeed => moveSpeed;
    protected float RotationSpeed => rotationSpeed;
    protected float CloseRange => closeRange;
    protected float SafeRange => Mathf.Max(closeRange, safeRange);
    protected bool IsActivated => activated;
    protected bool IsRetreating => retreating;
    protected bool HasTarget => target != null && playerHealth != null && !playerHealth.IsDead;
    public bool IsDead => healthTarget == null || healthTarget.IsDead;

    public event Action<EnemyController> Died;

    protected virtual void Awake()
    {
        if (healthTarget == null)
        {
            healthTarget = GetComponent<HealthTarget>();
        }

        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.updateRotation = false;
        }

        AcquirePlayer();
    }

    protected virtual void OnEnable()
    {
        if (activationGroup != null)
        {
            activationGroup.RegisterEnemy(this);
        }

        if (healthTarget != null)
        {
            healthTarget.Damaged += HandleDamaged;
            healthTarget.Died += HandleDied;
        }
    }

    protected virtual void OnDisable()
    {
        if (healthTarget != null)
        {
            healthTarget.Damaged -= HandleDamaged;
            healthTarget.Died -= HandleDied;
        }
    }

    protected virtual void Reset()
    {
        healthTarget = GetComponent<HealthTarget>();

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
            targetHitMask = 1 << playerLayer;
        }
    }

    protected virtual void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        closeRange = Mathf.Max(0f, closeRange);
        safeRange = Mathf.Max(closeRange, safeRange);
        dropAmmo = Mathf.Max(-1, dropAmmo);
        dropImpulse = Mathf.Max(0f, dropImpulse);
        dropUpwardImpulse = Mathf.Max(0f, dropUpwardImpulse);
        dropPickupLockout = Mathf.Max(0f, dropPickupLockout);
    }

    protected virtual void Update()
    {
        if (healthTarget == null || healthTarget.IsDead || Time.timeScale <= 0f)
        {
            StopMovement();
            return;
        }

        if (!activated)
        {
            StopMovement();
            return;
        }

        if (playerHealth == null || playerHealth.IsDead)
        {
            AcquirePlayer();
        }

        if (!HasTarget)
        {
            StopMovement();
            return;
        }

        TickEnemy();
    }

    protected abstract void TickEnemy();

    public void Activate(Health targetHealth)
    {
        if (targetHealth == null)
        {
            AcquirePlayer();
            targetHealth = playerHealth;
        }

        if (targetHealth == null || targetHealth.IsDead)
        {
            return;
        }

        playerHealth = targetHealth;
        target = targetHealth.transform;
        activated = true;
    }

    public void SetActivationGroup(EnemyActivationGroup group)
    {
        activationGroup = group;
    }

    protected float GetFlatDistanceToTarget()
    {
        if (target == null)
        {
            return float.PositiveInfinity;
        }

        return Vector3.Distance(GetFlatPosition(transform.position), GetFlatPosition(target.position));
    }

    protected Vector3 GetFlatDirectionToTarget()
    {
        if (target == null)
        {
            return transform.forward;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
    }

    protected Vector3 GetAimDirectionToTarget(Vector3 origin, float targetHeight = 1f)
    {
        if (target == null)
        {
            return transform.forward;
        }

        Vector3 targetPoint = target.position + (Vector3.up * targetHeight);
        Vector3 direction = targetPoint - origin;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
    }

    protected bool ShouldRetreatFromTarget(float distanceToTarget)
    {
        if (closeRange <= 0f)
        {
            retreating = false;
            return false;
        }

        if (retreating)
        {
            retreating = distanceToTarget < SafeRange;
        }
        else
        {
            retreating = distanceToTarget < closeRange;
        }

        return retreating;
    }

    protected void MoveTowardsTarget(float speed = -1f)
    {
        if (target == null)
        {
            StopMovement();
            return;
        }

        MoveTowardsPosition(target.position, speed);
    }

    protected void MoveAwayFromTarget(float speed = -1f)
    {
        MoveInDirection(-GetFlatDirectionToTarget(), speed);
    }

    protected void MoveTowardsPosition(Vector3 position, float speed = -1f)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;
        MoveInDirection(direction, speed);
    }

    protected void MoveInDirection(Vector3 direction, float speed = -1f)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            StopMovement();
            return;
        }

        float finalSpeed = speed >= 0f ? speed : moveSpeed;
        Vector3 normalizedDirection = direction.normalized;

        if (CanUseNavMeshAgent())
        {
            navMeshAgent.speed = finalSpeed;
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(transform.position + normalizedDirection * 2f);
        }
        else
        {
            transform.position += normalizedDirection * finalSpeed * Time.deltaTime;
        }
    }

    protected void StopMovement()
    {
        if (CanUseNavMeshAgent())
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
        }
    }

    protected void RotateTowardsTarget()
    {
        RotateTowardsDirection(GetFlatDirectionToTarget());
    }

    protected void RotateTowardsDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    protected bool RaycastTarget(Vector3 origin, Vector3 direction, float range, out RaycastHit hit)
    {
        return Physics.Raycast(origin, direction, out hit, range, targetHitMask, QueryTriggerInteraction.Ignore);
    }

    protected void ApplyDamageToHit(RaycastHit hit, float damage, Vector3 hitDirection, GameObject impactEffectPrefab)
    {
        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            return;
        }

        damageable.TakeDamage(damage, hit.point, hitDirection);
        SpawnImpactEffect(impactEffectPrefab, hit.point, hitDirection);
    }

    protected void SpawnImpactEffect(GameObject impactEffectPrefab, Vector3 position, Vector3 direction)
    {
        if (impactEffectPrefab == null)
        {
            return;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = transform.forward;
        }

        Instantiate(impactEffectPrefab, position, Quaternion.LookRotation(direction.normalized, Vector3.up));
    }

    private void AcquirePlayer()
    {
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<Health>();
        }

        target = playerHealth != null ? playerHealth.transform : null;
    }

    private void HandleDamaged(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        AcquirePlayer();

        if (playerHealth == null)
        {
            return;
        }

        if (activationGroup != null)
        {
            activationGroup.Activate(playerHealth);
        }
        else
        {
            Activate(playerHealth);
        }
    }

    private void HandleDied(HealthTarget deadHealth)
    {
        StopMovement();
        DropWeapon();
        Died?.Invoke(this);
    }

    private void DropWeapon()
    {
        if (droppedWeapon || dropPickupPrefab == null)
        {
            return;
        }

        droppedWeapon = true;

        Transform source = dropPoint != null ? dropPoint : transform;
        WeaponPickup pickup = Instantiate(dropPickupPrefab, source.position, source.rotation);
        if (dropAmmo >= 0)
        {
            pickup.SetRemainingAmmo(dropAmmo, dropPickupLockout);
        }
        else
        {
            pickup.SetPickupLockout(dropPickupLockout);
        }

        Rigidbody pickupRigidbody = pickup.GetComponentInParent<Rigidbody>();
        if (pickupRigidbody != null)
        {
            Vector3 impulse = (transform.forward * dropImpulse) + (Vector3.up * dropUpwardImpulse);
            pickupRigidbody.AddForce(impulse, ForceMode.Impulse);
        }
    }

    private bool CanUseNavMeshAgent()
    {
        return navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh;
    }

    private static Vector3 GetFlatPosition(Vector3 position)
    {
        position.y = 0f;
        return position;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!drawCombatRangeGizmos)
        {
            return;
        }

        Gizmos.color = closeRangeGizmo.Color;
        Gizmos.DrawWireSphere(transform.position, closeRange);

        Gizmos.color = safeRangeGizmo.Color;
        Gizmos.DrawWireSphere(transform.position, safeRange);
    }
}
