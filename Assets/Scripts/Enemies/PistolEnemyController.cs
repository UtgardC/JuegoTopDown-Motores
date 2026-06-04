using UnityEngine;

public class PistolEnemyController : EnemyShooterController
{
    [Header("Pistol Movement")]
    [SerializeField] private bool orbitWhileInRange = true;
    [SerializeField] private float orbitSpeed = 3f;
    [SerializeField] private float preferredRange = 6f;
    [SerializeField] private float rangeCorrectionWeight = 0.6f;
    [SerializeField] private int orbitDirection = 1;

    protected override void OnValidate()
    {
        base.OnValidate();
        orbitSpeed = Mathf.Max(0f, orbitSpeed);
        preferredRange = Mathf.Max(0f, preferredRange);
        rangeCorrectionWeight = Mathf.Max(0f, rangeCorrectionWeight);
        orbitDirection = orbitDirection < 0 ? -1 : 1;
    }

    protected override void HandleInRangeMovement(float distanceToTarget)
    {
        if (!orbitWhileInRange)
        {
            base.HandleInRangeMovement(distanceToTarget);
            return;
        }

        Vector3 toTarget = GetFlatDirectionToTarget();
        Vector3 orbitDirectionVector = Vector3.Cross(Vector3.up, toTarget) * orbitDirection;
        Vector3 rangeCorrection = Vector3.zero;

        if (distanceToTarget < preferredRange)
        {
            rangeCorrection = -toTarget * rangeCorrectionWeight;
        }
        else if (distanceToTarget > preferredRange)
        {
            rangeCorrection = toTarget * rangeCorrectionWeight;
        }

        MoveInDirection(orbitDirectionVector + rangeCorrection, orbitSpeed);
    }

    protected override void ExecuteLockedAttack(Vector3 direction)
    {
        FireSingleRay(direction, BulletDamage);
    }
}
