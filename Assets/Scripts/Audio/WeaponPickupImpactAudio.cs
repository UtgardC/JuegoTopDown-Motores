using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WeaponPickupImpactAudio : MonoBehaviour
{
    [SerializeField] private AudioCue impactCue;
    [SerializeField] private float minimumImpactSpeed = 1.5f;
    [SerializeField] private float maximumImpactSpeed = 8f;
    [SerializeField] private float minimumSecondsBetweenSounds = 0.15f;
    [SerializeField] private AnimationCurve volumeBySpeed = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private float nextAllowedSoundTime;

    private void OnValidate()
    {
        minimumImpactSpeed = Mathf.Max(0f, minimumImpactSpeed);
        maximumImpactSpeed = Mathf.Max(minimumImpactSpeed, maximumImpactSpeed);
        minimumSecondsBetweenSounds = Mathf.Max(0f, minimumSecondsBetweenSounds);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 contactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        TryPlayImpact(collision.relativeVelocity.magnitude, contactPoint);
    }

    private void TryPlayImpact(float speed, Vector3 position)
    {
        if (Time.time < nextAllowedSoundTime || speed < minimumImpactSpeed)
        {
            return;
        }

        float normalizedSpeed = Mathf.InverseLerp(minimumImpactSpeed, maximumImpactSpeed, speed);
        float volumeMultiplier = volumeBySpeed != null ? volumeBySpeed.Evaluate(normalizedSpeed) : normalizedSpeed;
        AudioManager.PlaySfx(impactCue, position, volumeMultiplier);
        nextAllowedSoundTime = Time.time + minimumSecondsBetweenSounds;
    }
}
