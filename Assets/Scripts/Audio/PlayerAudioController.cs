using System;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAudioController : MonoBehaviour
{
    [Serializable]
    private class WeaponFireAudio
    {
        [SerializeField] private WeaponDefinition weapon;
        [SerializeField] private AudioCue fireCue;
        [SerializeField] private AudioCue delayedCue;
        [SerializeField] private float delayedCueSeconds = 0.12f;
        [SerializeField] private bool playDelayedOnlyIfAmmoRemains = true;

        public WeaponDefinition Weapon => weapon;
        public AudioCue FireCue => fireCue;
        public AudioCue DelayedCue => delayedCue;
        public float DelayedCueSeconds => Mathf.Max(0f, delayedCueSeconds);
        public bool PlayDelayedOnlyIfAmmoRemains => playDelayedOnlyIfAmmoRemains;
    }

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private Health health;
    [SerializeField] private CharacterController characterController;

    [Header("Weapon")]
    [SerializeField] private WeaponFireAudio[] weaponFireAudio;
    [SerializeField] private AudioCue unarmedSwingCue;
    [SerializeField] private AudioCue unarmedImpactCue;
    [SerializeField] private AudioCue equipWeaponCue;
    [SerializeField] private AudioCue emptyWeaponCue;

    [Header("Health")]
    [SerializeField] private AudioCue damagedCue;
    [SerializeField] private AudioCue healedCue;
    [SerializeField] private AudioCue deathCue;

    [Header("Movement")]
    [SerializeField] private AudioCue dashCue;
    [SerializeField] private AudioCue footstepsCue;
    [SerializeField] private float footstepInterval = 0.32f;
    [SerializeField] private float minimumFootstepSpeed = 0.2f;

    private WeaponDefinition lastWeapon;
    private float footstepTimer;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<PlayerWeaponController>();
        }

        if (health == null)
        {
            health = GetComponent<Health>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }
    }

    private void OnValidate()
    {
        footstepInterval = Mathf.Max(0.01f, footstepInterval);
        minimumFootstepSpeed = Mathf.Max(0f, minimumFootstepSpeed);
    }

    private void OnEnable()
    {
        if (weaponController != null)
        {
            weaponController.WeaponChanged += HandleWeaponChanged;
            weaponController.WeaponFired += HandleWeaponFired;
            weaponController.EmptyWeaponUsed += HandleEmptyWeaponUsed;
            weaponController.UnarmedMeleeUsed += HandleUnarmedMeleeUsed;
            weaponController.UnarmedMeleeImpact += HandleUnarmedMeleeImpact;
            lastWeapon = weaponController.CurrentWeapon;
        }

        if (playerController != null)
        {
            playerController.DashStarted += HandleDashStarted;
        }

        if (health != null)
        {
            health.Damaged += HandleDamaged;
            health.Healed += HandleHealed;
            health.Died += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (weaponController != null)
        {
            weaponController.WeaponChanged -= HandleWeaponChanged;
            weaponController.WeaponFired -= HandleWeaponFired;
            weaponController.EmptyWeaponUsed -= HandleEmptyWeaponUsed;
            weaponController.UnarmedMeleeUsed -= HandleUnarmedMeleeUsed;
            weaponController.UnarmedMeleeImpact -= HandleUnarmedMeleeImpact;
        }

        if (playerController != null)
        {
            playerController.DashStarted -= HandleDashStarted;
        }

        if (health != null)
        {
            health.Damaged -= HandleDamaged;
            health.Healed -= HandleHealed;
            health.Died -= HandleDied;
        }
    }

    private void Update()
    {
        TickFootsteps();
    }

    private void HandleWeaponChanged(WeaponDefinition weapon, int ammo)
    {
        if (weapon != null && weapon != lastWeapon)
        {
            AudioManager.PlaySfx(equipWeaponCue, transform.position);
        }

        lastWeapon = weapon;
    }

    private void HandleWeaponFired(WeaponDefinition weapon, int remainingAmmo)
    {
        WeaponFireAudio entry = GetWeaponFireAudio(weapon);
        if (entry == null)
        {
            return;
        }

        AudioManager.PlaySfx(entry.FireCue, transform.position);

        if (entry.DelayedCue != null && (!entry.PlayDelayedOnlyIfAmmoRemains || remainingAmmo > 0))
        {
            InvokeDelayedCue(entry.DelayedCue, entry.DelayedCueSeconds);
        }
    }

    private void HandleEmptyWeaponUsed(WeaponDefinition weapon)
    {
        AudioManager.PlaySfx(emptyWeaponCue, transform.position);
    }

    private void HandleUnarmedMeleeUsed()
    {
        AudioManager.PlaySfx(unarmedSwingCue, transform.position);
    }

    private void HandleUnarmedMeleeImpact(Vector3 hitPoint)
    {
        AudioManager.PlaySfx(unarmedImpactCue, hitPoint);
    }

    private void HandleDamaged(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        AudioManager.PlaySfx(damagedCue, transform.position);
    }

    private void HandleHealed(float amount)
    {
        AudioManager.PlaySfx(healedCue, transform.position);
    }

    private void HandleDied(Health playerHealth)
    {
        AudioManager.PlaySfx(deathCue, transform.position);
    }

    private void HandleDashStarted()
    {
        AudioManager.PlaySfx(dashCue, transform.position);
    }

    private void TickFootsteps()
    {
        if (characterController == null || playerController == null || playerController.IsDashing)
        {
            footstepTimer = 0f;
            return;
        }

        Vector3 horizontalVelocity = characterController.velocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.magnitude < minimumFootstepSpeed || !characterController.isGrounded)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer > 0f)
        {
            return;
        }

        footstepTimer = footstepInterval;
        AudioManager.PlaySfx(footstepsCue, transform.position);
    }

    private void InvokeDelayedCue(AudioCue cue, float delay)
    {
        StartCoroutine(PlayDelayedCueRoutine(cue, delay));
    }

    private System.Collections.IEnumerator PlayDelayedCueRoutine(AudioCue cue, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        AudioManager.PlaySfx(cue, transform.position);
    }

    private WeaponFireAudio GetWeaponFireAudio(WeaponDefinition weapon)
    {
        if (weaponFireAudio == null)
        {
            return null;
        }

        for (int i = 0; i < weaponFireAudio.Length; i++)
        {
            if (weaponFireAudio[i] != null && weaponFireAudio[i].Weapon == weapon)
            {
                return weaponFireAudio[i];
            }
        }

        return null;
    }
}
