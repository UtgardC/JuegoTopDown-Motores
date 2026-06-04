using UnityEngine;

public class SubmachineGunEnemyController : EnemyShooterController
{
    [Header("SMG Burst")]
    [SerializeField] private int burstShotCount = 5;
    [SerializeField] private float burstCadence = 0.08f;
    [SerializeField] private float burstCooldown = 0.8f;
    [SerializeField] private float aimFollowSpeed = 5f;

    private bool preparingBurst;
    private bool bursting;
    private int shotsRemaining;
    private float prepareTimer;
    private float nextShotTime;
    private float nextBurstReadyTime;
    private Vector3 currentBurstDirection;

    protected override void OnValidate()
    {
        base.OnValidate();
        burstShotCount = Mathf.Max(1, burstShotCount);
        burstCadence = Mathf.Max(0.01f, burstCadence);
        burstCooldown = Mathf.Max(0f, burstCooldown);
        aimFollowSpeed = Mathf.Max(0f, aimFollowSpeed);
    }

    protected override void TickEnemy()
    {
        float distanceToTarget = GetFlatDistanceToTarget();
        bool retreating = ShouldRetreatFromTarget(distanceToTarget);

        if (retreating)
        {
            CancelBurst();
            MoveAwayFromTarget();
            RotateTowardsTarget();
            return;
        }

        if (distanceToTarget > AttackRange)
        {
            CancelBurst();
            MoveTowardsTarget();
            RotateTowardsTarget();
            return;
        }

        StopMovement();
        TickBurst();
    }

    protected override void ExecuteLockedAttack(Vector3 direction)
    {
        FireSingleRay(direction, BulletDamage);
    }

    private void TickBurst()
    {
        Vector3 targetDirection = GetCurrentAimDirection();

        if (!preparingBurst && !bursting)
        {
            if (Time.time < nextBurstReadyTime)
            {
                RotateTowardsDirection(targetDirection);
                return;
            }

            preparingBurst = true;
            prepareTimer = 0f;
            currentBurstDirection = targetDirection;
        }

        currentBurstDirection = Vector3.Slerp(currentBurstDirection, targetDirection, aimFollowSpeed * Time.deltaTime);
        RotateTowardsDirection(currentBurstDirection);

        if (preparingBurst)
        {
            prepareTimer += Time.deltaTime;
            if (prepareTimer < AimDelay)
            {
                return;
            }

            preparingBurst = false;
            bursting = true;
            shotsRemaining = burstShotCount;
            nextShotTime = Time.time;
        }

        if (!bursting || Time.time < nextShotTime)
        {
            return;
        }

        ExecuteLockedAttack(currentBurstDirection.normalized);
        shotsRemaining--;
        nextShotTime = Time.time + burstCadence;

        if (shotsRemaining <= 0)
        {
            bursting = false;
            nextBurstReadyTime = Time.time + burstCooldown;
        }
    }

    private void CancelBurst()
    {
        preparingBurst = false;
        bursting = false;
        shotsRemaining = 0;
    }
}
