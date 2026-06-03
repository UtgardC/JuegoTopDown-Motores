using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerHazardContactReporter : MonoBehaviour
{
    [SerializeField] private Health health;

    private void Awake()
    {
        if (health == null)
        {
            health = GetComponent<Health>();
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (health == null || hit.collider == null)
        {
            return;
        }

        Hazard hazard = hit.collider.GetComponentInParent<Hazard>();
        if (hazard == null)
        {
            return;
        }

        Vector3 hitDirection = transform.position - hazard.transform.position;
        hitDirection.y = 0f;

        if (hitDirection.sqrMagnitude <= 0.001f)
        {
            hitDirection = -hit.normal;
        }

        hazard.ApplyToHealth(health, hit.point, hitDirection.normalized, Time.deltaTime);
    }
}
