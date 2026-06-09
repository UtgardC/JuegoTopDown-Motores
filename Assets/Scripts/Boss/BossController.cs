using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HealthTarget))]
[RequireComponent(typeof(Rigidbody))]
public class BossController : MonoBehaviour
{
    private enum BossAction
    {
        Melee,
        Dash,
        Spin
    }

    private enum BossState
    {
        Idle,
        MeleeChase,
        MeleeWindup,
        MeleeRecovery,
        DashWindup,
        DashMove,
        DashRecovery,
        SpinWindup,
        SpinMove,
        SpinRecovery,
        Dead
    }

    [Header("References")]
    [SerializeField] private HealthTarget healthTarget;
    [SerializeField] private Health playerHealth;
    [SerializeField] private BossActivationGroup activationGroup;
    [SerializeField] private Rigidbody bossRigidbody;
    [SerializeField] private Transform meleeOrigin;
    [SerializeField] private GameObject dashDamageObject;
    [SerializeField] private GameObject spinDamageObject;

    [Header("Masks")]
    [SerializeField] private LayerMask targetHitMask = ~0;
    [SerializeField] private LayerMask bounceLayers = ~0;

    [Header("Action Selection")]
    [SerializeField] private BossAction[] behaviorPattern =
    {
        BossAction.Melee,
        BossAction.Dash,
        BossAction.Melee,
        BossAction.Dash,
        BossAction.Melee,
        BossAction.Spin
    };
    [SerializeField, Range(0f, 1f)] private float preferredActionWeight = 0.6f;
    [SerializeField, Range(0f, 1f)] private float lowHealthDashChainThreshold = 0.3f;

    [Header("Movement")]
    [SerializeField] private float meleeMoveSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 14f;

    [Header("Melee")]
    [SerializeField] private float meleeDamage = 25f;
    [SerializeField] private float meleeAttackRange = 1.8f;
    [SerializeField] private float meleeAttackRadius = 1.1f;
    [SerializeField] private float meleeAttackDelay = 0.45f;
    [SerializeField] private float meleeRecoveryDuration = 0.35f;
    [SerializeField] private float meleeOriginHeight = 1f;
    [SerializeField] private float meleeTargetAimHeight = 1f;
    [SerializeField] private GameObject meleeImpactEffectPrefab;

    [Header("Dash")]
    [SerializeField] private float dashWindupDuration = 0.8f;
    [SerializeField] private float dashDistance = 9f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashRecoveryDuration = 0.6f;

    [Header("Spin")]
    [SerializeField] private float spinWindupDuration = 0.6f;
    [SerializeField] private float spinMoveSpeed = 8f;
    [SerializeField] private float spinDuration = 4f;
    [SerializeField] private float spinRecoveryDuration = 0.75f;
    [SerializeField, Range(0.5f, 1f)] private float axisDominanceThreshold = 0.9f;
    [SerializeField, Range(0.5f, 0.95f)] private float redistributedDominantAxisShare = 0.6f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color meleeRangeGizmoColor = Color.red;
    [SerializeField] private Color meleeHitboxGizmoColor = Color.magenta;

    private readonly HashSet<IDamageable> meleeHits = new HashSet<IDamageable>();

    private Transform target;
    private BossState state;
    private BossAction lastCompletedAction = BossAction.Melee;
    private int patternIndex;
    private float stateTimer;
    private Vector3 lockedMeleeDirection;
    private Vector3 lockedDashDirection;
    private Vector3 spinDirection;
    private bool activated;
    private bool died;

    public HealthTarget BossHealth => healthTarget;
    public bool IsActivated => activated;
    public bool IsDead => died || healthTarget == null || healthTarget.IsDead;
    public bool IsDashing => state == BossState.DashMove;
    public bool IsSpinning => state == BossState.SpinMove;

    public event Action<BossController> Activated;
    public event Action<BossController> MeleeAttackStarted;
    public event Action<BossController> DashStarted;
    public event Action<BossController> DashWallHit;
    public event Action<BossController> DashRecovered;
    public event Action<BossController> SpinStarted;
    public event Action<BossController> SpinBounced;
    public event Action<BossController> SpinStopped;
    public event Action<BossController> Died;

    private void Awake()
    {
        if (healthTarget == null)
        {
            healthTarget = GetComponent<HealthTarget>();
        }

        if (bossRigidbody == null)
        {
            bossRigidbody = GetComponent<Rigidbody>();
        }

        AcquirePlayer();
        SetDamageObjectsActive(false, false);
    }

    private void OnEnable()
    {
        if (activationGroup != null)
        {
            activationGroup.RegisterBoss(this);
        }

        if (healthTarget != null)
        {
            healthTarget.Damaged += HandleDamaged;
            healthTarget.Died += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (healthTarget != null)
        {
            healthTarget.Damaged -= HandleDamaged;
            healthTarget.Died -= HandleDied;
        }
    }

    private void Reset()
    {
        healthTarget = GetComponent<HealthTarget>();
        bossRigidbody = GetComponent<Rigidbody>();
        activationGroup = GetComponentInParent<BossActivationGroup>();

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
            targetHitMask = 1 << playerLayer;
        }
    }

    private void OnValidate()
    {
        meleeMoveSpeed = Mathf.Max(0f, meleeMoveSpeed);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        meleeDamage = Mathf.Max(0f, meleeDamage);
        meleeAttackRange = Mathf.Max(0f, meleeAttackRange);
        meleeAttackRadius = Mathf.Max(0f, meleeAttackRadius);
        meleeAttackDelay = Mathf.Max(0f, meleeAttackDelay);
        meleeRecoveryDuration = Mathf.Max(0f, meleeRecoveryDuration);
        meleeOriginHeight = Mathf.Max(0f, meleeOriginHeight);
        meleeTargetAimHeight = Mathf.Max(0f, meleeTargetAimHeight);
        dashWindupDuration = Mathf.Max(0f, dashWindupDuration);
        dashDistance = Mathf.Max(0f, dashDistance);
        dashDuration = Mathf.Max(0.01f, dashDuration);
        dashRecoveryDuration = Mathf.Max(0f, dashRecoveryDuration);
        spinWindupDuration = Mathf.Max(0f, spinWindupDuration);
        spinMoveSpeed = Mathf.Max(0f, spinMoveSpeed);
        spinDuration = Mathf.Max(0f, spinDuration);
        spinRecoveryDuration = Mathf.Max(0f, spinRecoveryDuration);
    }

    private void Update()
    {
        if (!activated || IsDead || Time.timeScale <= 0f)
        {
            return;
        }

        if (playerHealth == null || playerHealth.IsDead)
        {
            AcquirePlayer();
        }

        if (target == null || playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        TickState(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (!activated || IsDead || Time.timeScale <= 0f)
        {
            SetHorizontalVelocity(Vector3.zero);
            return;
        }

        Vector3 desiredVelocity = Vector3.zero;

        if (state == BossState.MeleeChase)
        {
            desiredVelocity = GetFlatDirectionToTarget() * meleeMoveSpeed;
        }
        else if (state == BossState.DashMove)
        {
            desiredVelocity = lockedDashDirection * (dashDistance / dashDuration);
        }
        else if (state == BossState.SpinMove)
        {
            desiredVelocity = spinDirection * spinMoveSpeed;
        }

        SetHorizontalVelocity(desiredVelocity);
    }

    public void Activate(Health targetHealth)
    {
        if (targetHealth != null)
        {
            playerHealth = targetHealth;
            target = targetHealth.transform;
        }
        else
        {
            AcquirePlayer();
        }

        if (playerHealth == null || playerHealth.IsDead || IsDead)
        {
            return;
        }

        if (!activated)
        {
            activated = true;
            Activated?.Invoke(this);
            StartNextAction();
        }
    }

    public void SetActivationGroup(BossActivationGroup group)
    {
        activationGroup = group;
    }

    private void TickState(float deltaTime)
    {
        switch (state)
        {
            case BossState.MeleeChase:
                TickMeleeChase();
                break;

            case BossState.MeleeWindup:
                TickMeleeWindup(deltaTime);
                break;

            case BossState.MeleeRecovery:
                TickTimedRecovery(deltaTime, meleeRecoveryDuration, BossAction.Melee);
                break;

            case BossState.DashWindup:
                TickDashWindup(deltaTime);
                break;

            case BossState.DashMove:
                TickDashMove(deltaTime);
                break;

            case BossState.DashRecovery:
                TickTimedRecovery(deltaTime, GetCurrentDashRecoveryDuration(), BossAction.Dash);
                break;

            case BossState.SpinWindup:
                TickSpinWindup(deltaTime);
                break;

            case BossState.SpinMove:
                TickSpinMove(deltaTime);
                break;

            case BossState.SpinRecovery:
                TickTimedRecovery(deltaTime, spinRecoveryDuration, BossAction.Spin);
                break;
        }
    }

    private void StartNextAction()
    {
        SetDamageObjectsActive(false, false);

        BossAction nextAction = ChooseNextAction();
        switch (nextAction)
        {
            case BossAction.Dash:
                StartDashWindup();
                break;

            case BossAction.Spin:
                StartSpinWindup();
                break;

            case BossAction.Melee:
            default:
                StartMeleeChase();
                break;
        }
    }

    private BossAction ChooseNextAction()
    {
        if (IsLowHealthDashChainActive())
        {
            return BossAction.Dash;
        }

        BossAction preferredAction = GetPreferredPatternAction();
        float roll = UnityEngine.Random.value;

        if (roll <= preferredActionWeight)
        {
            return preferredAction;
        }

        BossAction[] alternatives = GetAlternativeActions(preferredAction);
        if (alternatives.Length == 0)
        {
            return preferredAction;
        }

        float normalizedAlternativeRoll = Mathf.InverseLerp(preferredActionWeight, 1f, roll);
        int index = normalizedAlternativeRoll < 0.5f ? 0 : Mathf.Min(1, alternatives.Length - 1);
        return alternatives[index];
    }

    private BossAction GetPreferredPatternAction()
    {
        if (behaviorPattern == null || behaviorPattern.Length == 0)
        {
            return BossAction.Melee;
        }

        BossAction action = behaviorPattern[patternIndex % behaviorPattern.Length];
        patternIndex++;
        return action;
    }

    private BossAction[] GetAlternativeActions(BossAction preferredAction)
    {
        if (preferredAction == BossAction.Melee)
        {
            return new[] { BossAction.Dash, BossAction.Spin };
        }

        if (preferredAction == BossAction.Dash)
        {
            return new[] { BossAction.Melee, BossAction.Spin };
        }

        return new[] { BossAction.Melee, BossAction.Dash };
    }

    private bool IsLowHealthDashChainActive()
    {
        return healthTarget != null
            && healthTarget.HealthNormalized <= lowHealthDashChainThreshold
            && lastCompletedAction == BossAction.Dash;
    }

    private void StartMeleeChase()
    {
        state = BossState.MeleeChase;
        stateTimer = 0f;
    }

    private void TickMeleeChase()
    {
        RotateTowardsDirection(GetFlatDirectionToTarget());

        if (GetFlatDistanceToTarget() <= meleeAttackRange)
        {
            StartMeleeWindup();
        }
    }

    private void StartMeleeWindup()
    {
        state = BossState.MeleeWindup;
        stateTimer = 0f;
        lockedMeleeDirection = GetAimDirectionToTarget(GetMeleeOriginPosition(), meleeTargetAimHeight);
        MeleeAttackStarted?.Invoke(this);
    }

    private void TickMeleeWindup(float deltaTime)
    {
        stateTimer += deltaTime;
        RotateTowardsDirection(GetFlatDirection(lockedMeleeDirection));

        if (stateTimer < meleeAttackDelay)
        {
            return;
        }

        ExecuteMeleeAttack();
        state = BossState.MeleeRecovery;
        stateTimer = 0f;
    }

    private void ExecuteMeleeAttack()
    {
        Vector3 origin = GetMeleeOriginPosition();
        Vector3 flatDirection = GetFlatDirection(lockedMeleeDirection);
        Vector3 center = origin + (flatDirection * meleeAttackRange);
        Collider[] hits = Physics.OverlapSphere(center, meleeAttackRadius, targetHitMask, QueryTriggerInteraction.Ignore);

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
            damageable.TakeDamage(meleeDamage, hitPoint, lockedMeleeDirection);
            SpawnImpactEffect(hitPoint, lockedMeleeDirection);
        }
    }

    private void StartDashWindup()
    {
        state = BossState.DashWindup;
        stateTimer = 0f;
        lockedDashDirection = GetFlatDirectionToTarget();
    }

    private void TickDashWindup(float deltaTime)
    {
        stateTimer += deltaTime;
        lockedDashDirection = GetFlatDirectionToTarget();
        RotateTowardsDirection(lockedDashDirection);

        if (stateTimer >= dashWindupDuration)
        {
            StartDashMove();
        }
    }

    private void StartDashMove()
    {
        state = BossState.DashMove;
        stateTimer = 0f;
        lockedDashDirection = GetValidFlatDirection(lockedDashDirection, transform.forward);
        SetDamageObjectsActive(true, false);
        DashStarted?.Invoke(this);
    }

    private void TickDashMove(float deltaTime)
    {
        stateTimer += deltaTime;
        RotateTowardsDirection(lockedDashDirection);

        if (stateTimer >= dashDuration)
        {
            StartDashRecovery();
        }
    }

    private void StartDashRecovery()
    {
        if (state != BossState.DashMove && state != BossState.DashWindup)
        {
            return;
        }

        state = BossState.DashRecovery;
        stateTimer = 0f;
        SetDamageObjectsActive(false, false);
        SetHorizontalVelocity(Vector3.zero);
        DashRecovered?.Invoke(this);
    }

    private float GetCurrentDashRecoveryDuration()
    {
        return healthTarget != null && healthTarget.HealthNormalized <= lowHealthDashChainThreshold
            ? 0f
            : dashRecoveryDuration;
    }

    private void StartSpinWindup()
    {
        state = BossState.SpinWindup;
        stateTimer = 0f;
        spinDirection = GetFlatDirectionToTarget();
    }

    private void TickSpinWindup(float deltaTime)
    {
        stateTimer += deltaTime;
        spinDirection = GetFlatDirectionToTarget();
        RotateTowardsDirection(spinDirection);

        if (stateTimer >= spinWindupDuration)
        {
            StartSpinMove();
        }
    }

    private void StartSpinMove()
    {
        state = BossState.SpinMove;
        stateTimer = 0f;
        spinDirection = GetValidFlatDirection(spinDirection, transform.forward);
        SetDamageObjectsActive(false, true);
        SpinStarted?.Invoke(this);
    }

    private void TickSpinMove(float deltaTime)
    {
        stateTimer += deltaTime;
        RotateTowardsDirection(spinDirection);

        if (stateTimer >= spinDuration)
        {
            StartSpinRecovery();
        }
    }

    private void StartSpinRecovery()
    {
        if (state != BossState.SpinMove && state != BossState.SpinWindup)
        {
            return;
        }

        state = BossState.SpinRecovery;
        stateTimer = 0f;
        SetDamageObjectsActive(false, false);
        SetHorizontalVelocity(Vector3.zero);
        SpinStopped?.Invoke(this);
    }

    private void TickTimedRecovery(float deltaTime, float duration, BossAction completedAction)
    {
        stateTimer += deltaTime;
        if (stateTimer < duration)
        {
            return;
        }

        lastCompletedAction = completedAction;
        StartNextAction();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider == null || !IsInLayerMask(collision.collider.gameObject, bounceLayers))
        {
            return;
        }

        if (state == BossState.DashMove)
        {
            DashWallHit?.Invoke(this);
            StartDashRecovery();
            return;
        }

        if (state == BossState.SpinMove)
        {
            ReflectSpinDirection(collision);
            SpinBounced?.Invoke(this);
        }
    }

    private void ReflectSpinDirection(Collision collision)
    {
        Vector3 normal = collision.contactCount > 0 ? collision.GetContact(0).normal : -spinDirection;
        normal.y = 0f;

        if (normal.sqrMagnitude <= 0.001f)
        {
            normal = -spinDirection;
        }

        spinDirection = Vector3.Reflect(spinDirection, normal.normalized);
        spinDirection.y = 0f;
        spinDirection = RedistributeIfAxisDominant(GetValidFlatDirection(spinDirection, transform.forward));
        SetHorizontalVelocity(spinDirection * spinMoveSpeed);
    }

    private Vector3 RedistributeIfAxisDominant(Vector3 direction)
    {
        float absX = Mathf.Abs(direction.x);
        float absZ = Mathf.Abs(direction.z);
        float total = absX + absZ;

        if (total <= 0.001f)
        {
            return Vector3.right;
        }

        float xShare = absX / total;
        float zShare = absZ / total;
        if (xShare < axisDominanceThreshold && zShare < axisDominanceThreshold)
        {
            return direction.normalized;
        }

        bool xDominant = xShare >= zShare;
        float dominant = redistributedDominantAxisShare;
        float secondary = 1f - redistributedDominantAxisShare;
        float xSign = Mathf.Abs(direction.x) > 0.001f ? Mathf.Sign(direction.x) : 1f;
        float zSign = Mathf.Abs(direction.z) > 0.001f ? Mathf.Sign(direction.z) : 1f;

        Vector3 redistributed = xDominant
            ? new Vector3(xSign * dominant, 0f, zSign * secondary)
            : new Vector3(xSign * secondary, 0f, zSign * dominant);

        return redistributed.normalized;
    }

    private void HandleDamaged(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (activated || IsDead)
        {
            return;
        }

        AcquirePlayer();
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
        died = true;
        state = BossState.Dead;
        SetDamageObjectsActive(false, false);
        SetHorizontalVelocity(Vector3.zero);
        Died?.Invoke(this);
    }

    private void AcquirePlayer()
    {
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<Health>();
        }

        target = playerHealth != null ? playerHealth.transform : null;
    }

    private float GetFlatDistanceToTarget()
    {
        if (target == null)
        {
            return float.PositiveInfinity;
        }

        return Vector3.Distance(GetFlatPosition(transform.position), GetFlatPosition(target.position));
    }

    private Vector3 GetFlatDirectionToTarget()
    {
        if (target == null)
        {
            return transform.forward;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        return GetValidFlatDirection(direction, transform.forward);
    }

    private Vector3 GetAimDirectionToTarget(Vector3 origin, float targetHeight)
    {
        if (target == null)
        {
            return transform.forward;
        }

        Vector3 targetPoint = target.position + (Vector3.up * targetHeight);
        Vector3 direction = targetPoint - origin;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
    }

    private Vector3 GetMeleeOriginPosition()
    {
        return meleeOrigin != null ? meleeOrigin.position : transform.position + (Vector3.up * meleeOriginHeight);
    }

    private void RotateTowardsDirection(Vector3 direction)
    {
        Vector3 flatDirection = GetFlatDirection(direction);
        if (flatDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void SetHorizontalVelocity(Vector3 horizontalVelocity)
    {
        if (bossRigidbody == null)
        {
            return;
        }

        Vector3 currentVelocity = bossRigidbody.linearVelocity;
        bossRigidbody.linearVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
    }

    private void SetDamageObjectsActive(bool dashActive, bool spinActive)
    {
        if (dashDamageObject != null)
        {
            dashDamageObject.SetActive(dashActive);
        }

        if (spinDamageObject != null)
        {
            spinDamageObject.SetActive(spinActive);
        }
    }

    private void SpawnImpactEffect(Vector3 position, Vector3 direction)
    {
        if (meleeImpactEffectPrefab == null)
        {
            return;
        }

        Vector3 flatDirection = GetFlatDirection(direction);
        if (flatDirection.sqrMagnitude <= 0.001f)
        {
            flatDirection = transform.forward;
        }

        Instantiate(meleeImpactEffectPrefab, position, Quaternion.LookRotation(flatDirection.normalized, Vector3.up));
    }

    private static Vector3 GetFlatDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction;
    }

    private static Vector3 GetFlatPosition(Vector3 position)
    {
        position.y = 0f;
        return position;
    }

    private static Vector3 GetValidFlatDirection(Vector3 direction, Vector3 fallback)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            return direction.normalized;
        }

        fallback.y = 0f;
        return fallback.sqrMagnitude > 0.001f ? fallback.normalized : Vector3.forward;
    }

    private static bool IsInLayerMask(GameObject targetObject, LayerMask layerMask)
    {
        return targetObject != null && (layerMask.value & (1 << targetObject.layer)) != 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = meleeRangeGizmoColor;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);

        Vector3 direction = Application.isPlaying ? GetValidFlatDirection(lockedMeleeDirection, transform.forward) : transform.forward;
        Gizmos.color = meleeHitboxGizmoColor;
        Gizmos.DrawWireSphere(GetMeleeOriginPosition() + direction * meleeAttackRange, meleeAttackRadius);
    }
}
