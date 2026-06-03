using System.Collections.Generic;
using UnityEngine;

public class Hazard : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private HazardDamageMode damageMode = HazardDamageMode.DamagePerSecond;
    [SerializeField] private LayerMask targetLayers = ~0;

    private readonly HashSet<Health> damagedThisFrame = new HashSet<Health>();
    private int damageFrame = -1;

    private void Reset()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
            targetLayers = 1 << playerLayer;
        }
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0f, damage);
    }

    private void OnTriggerStay(Collider other)
    {
        ApplyToCollider(other, Time.deltaTime);
    }

    private void OnCollisionStay(Collision collision)
    {
        Vector3 hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : collision.collider.ClosestPoint(transform.position);
        ApplyToCollider(collision.collider, Time.fixedDeltaTime, hitPoint);
    }

    public void ApplyToHealth(Health targetHealth, Vector3 hitPoint, Vector3 hitDirection, float deltaTime)
    {
        if (targetHealth == null || !IsInTargetLayer(targetHealth.gameObject))
        {
            return;
        }

        ApplyDamage(targetHealth, hitPoint, hitDirection, deltaTime);
    }

    private void ApplyToCollider(Collider targetCollider, float deltaTime)
    {
        Vector3 hitPoint = targetCollider != null ? targetCollider.ClosestPoint(transform.position) : transform.position;
        ApplyToCollider(targetCollider, deltaTime, hitPoint);
    }

    private void ApplyToCollider(Collider targetCollider, float deltaTime, Vector3 hitPoint)
    {
        if (targetCollider == null)
        {
            return;
        }

        Health targetHealth = targetCollider.GetComponentInParent<Health>();
        if (targetHealth == null)
        {
            return;
        }

        if (!IsInTargetLayer(targetCollider.gameObject) && !IsInTargetLayer(targetHealth.gameObject))
        {
            return;
        }

        Vector3 hitDirection = targetHealth.transform.position - transform.position;
        hitDirection.y = 0f;

        if (hitDirection.sqrMagnitude <= 0.001f)
        {
            hitDirection = Vector3.forward;
        }

        ApplyDamage(targetHealth, hitPoint, hitDirection.normalized, deltaTime);
    }

    private void ApplyDamage(Health targetHealth, Vector3 hitPoint, Vector3 hitDirection, float deltaTime)
    {
        RefreshFrameCache();

        if (damagedThisFrame.Contains(targetHealth))
        {
            return;
        }

        float appliedDamage = damageMode == HazardDamageMode.DamagePerSecond
            ? damage * Mathf.Max(0f, deltaTime)
            : damage;

        if (appliedDamage <= 0f)
        {
            return;
        }

        targetHealth.TakeDamage(appliedDamage, hitPoint, hitDirection);
        damagedThisFrame.Add(targetHealth);
    }

    private void RefreshFrameCache()
    {
        if (damageFrame == Time.frameCount)
        {
            return;
        }

        damageFrame = Time.frameCount;
        damagedThisFrame.Clear();
    }

    private bool IsInTargetLayer(GameObject target)
    {
        return target != null && (targetLayers.value & (1 << target.layer)) != 0;
    }
}
