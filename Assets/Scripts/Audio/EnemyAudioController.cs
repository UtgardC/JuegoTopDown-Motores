using UnityEngine;

public class EnemyAudioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HealthTarget healthTarget;
    [SerializeField] private EnemyShooterController shooter;
    [SerializeField] private MeleeEnemyController meleeEnemy;

    [Header("Sounds")]
    [SerializeField] private AudioCue enemyShotCue;
    [SerializeField] private AudioCue meleeVoiceCue;
    [SerializeField] private AudioCue meleeAttackCue;
    [SerializeField] private AudioCue deathCue;

    private void Awake()
    {
        if (healthTarget == null)
        {
            healthTarget = GetComponent<HealthTarget>();
        }

        if (shooter == null)
        {
            shooter = GetComponent<EnemyShooterController>();
        }

        if (meleeEnemy == null)
        {
            meleeEnemy = GetComponent<MeleeEnemyController>();
        }
    }

    private void OnEnable()
    {
        if (healthTarget != null)
        {
            healthTarget.Died += HandleDied;
        }

        if (shooter != null)
        {
            shooter.ShotFired += HandleShotFired;
        }

        if (meleeEnemy != null)
        {
            meleeEnemy.AttackStarted += HandleMeleeAttackStarted;
            meleeEnemy.AttackExecuted += HandleMeleeAttackExecuted;
        }
    }

    private void OnDisable()
    {
        if (healthTarget != null)
        {
            healthTarget.Died -= HandleDied;
        }

        if (shooter != null)
        {
            shooter.ShotFired -= HandleShotFired;
        }

        if (meleeEnemy != null)
        {
            meleeEnemy.AttackStarted -= HandleMeleeAttackStarted;
            meleeEnemy.AttackExecuted -= HandleMeleeAttackExecuted;
        }
    }

    private void HandleShotFired(EnemyShooterController source)
    {
        AudioManager.PlaySfx(enemyShotCue, transform.position);
    }

    private void HandleMeleeAttackStarted(MeleeEnemyController source)
    {
        AudioManager.PlaySfx(meleeVoiceCue, transform.position);
    }

    private void HandleMeleeAttackExecuted(MeleeEnemyController source)
    {
        AudioManager.PlaySfx(meleeAttackCue, transform.position);
    }

    private void HandleDied(HealthTarget deadHealth)
    {
        AudioManager.PlaySfx(deathCue, transform.position);
    }
}
