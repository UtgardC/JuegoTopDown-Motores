using System.Collections.Generic;
using UnityEngine;

public class ShotgunEnemyController : EnemyShooterController
{
    [Header("Shotgun")]
    [SerializeField] private int pelletCount = 6;
    [SerializeField] private float spreadAngle = 30f;

    private readonly Dictionary<IDamageable, SpreadDamageHit> spreadHits = new Dictionary<IDamageable, SpreadDamageHit>();

    private struct SpreadDamageHit
    {
        public float Damage;
        public Vector3 HitPoint;
        public Vector3 HitDirection;
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        pelletCount = Mathf.Max(1, pelletCount);
        spreadAngle = Mathf.Max(0f, spreadAngle);
    }

    protected override void ExecuteLockedAttack(Vector3 direction)
    {
        float angleStep = pelletCount > 1 ? spreadAngle / (pelletCount - 1) : 0f;
        float startAngle = spreadAngle * -0.5f;
        Vector3 origin = GetShotOriginPosition();

        spreadHits.Clear();

        for (int i = 0; i < pelletCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector3 pelletDirection = GetValidShotDirection(Quaternion.AngleAxis(angle, Vector3.up) * direction);

            if (!RaycastTarget(origin, pelletDirection, AttackRange, out RaycastHit hit))
            {
                Vector3 end = GetMissEndPoint(origin, pelletDirection);
                DrawShotDebugLaser(origin, end, false);
                SpawnSpreadBulletTrail(origin, end);
                continue;
            }

            DrawShotDebugLaser(origin, hit.point, true);
            SpawnSpreadBulletTrail(origin, hit.point);

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                continue;
            }

            AccumulateDamage(damageable, BulletDamage, hit.point, pelletDirection);
            SpawnImpactEffect(ImpactEffectPrefab, hit.point, pelletDirection);
        }

        ApplySpreadDamage();
    }

    private void SpawnSpreadBulletTrail(Vector3 start, Vector3 end)
    {
        if (SpawnTrailsForSpread)
        {
            SpawnBulletTrail(start, end);
        }
    }

    private void AccumulateDamage(IDamageable damageable, float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (!spreadHits.TryGetValue(damageable, out SpreadDamageHit hitData))
        {
            hitData.HitPoint = hitPoint;
            hitData.HitDirection = hitDirection;
        }

        hitData.Damage += damage;
        spreadHits[damageable] = hitData;
    }

    private void ApplySpreadDamage()
    {
        foreach (KeyValuePair<IDamageable, SpreadDamageHit> spreadHit in spreadHits)
        {
            SpreadDamageHit hitData = spreadHit.Value;
            spreadHit.Key.TakeDamage(hitData.Damage, hitData.HitPoint, hitData.HitDirection);
        }

        spreadHits.Clear();
    }
}
