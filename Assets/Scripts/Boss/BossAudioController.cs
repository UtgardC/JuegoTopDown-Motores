using System.Collections;
using UnityEngine;

public class BossAudioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossController boss;
    [SerializeField] private HealthTarget healthTarget;

    [Header("Health")]
    [SerializeField] private AudioCue damagedCue;
    [SerializeField] private AudioCue deathCue;

    [Header("Melee")]
    [SerializeField] private AudioCue meleeAttackCue;

    [Header("Dash")]
    [SerializeField] private AudioCue dashStartCue;
    [SerializeField] private AudioCue dashLoopCue;
    [SerializeField] private float dashLoopInterval = 0.12f;
    [SerializeField] private AudioCue dashWallImpactCue;
    [SerializeField] private AudioCue dashRecoverCue;

    [Header("Spin")]
    [SerializeField] private AudioCue spinStartCue;
    [SerializeField] private AudioCue spinBounceCue;
    [SerializeField] private AudioCue spinStopCue;

    private Coroutine dashLoopCoroutine;

    private void Awake()
    {
        if (boss == null)
        {
            boss = GetComponent<BossController>();
        }

        if (healthTarget == null)
        {
            healthTarget = GetComponent<HealthTarget>();
        }
    }

    private void OnValidate()
    {
        dashLoopInterval = Mathf.Max(0.01f, dashLoopInterval);
    }

    private void OnEnable()
    {
        if (healthTarget != null)
        {
            healthTarget.Damaged += HandleDamaged;
            healthTarget.Died += HandleDied;
        }

        if (boss != null)
        {
            boss.MeleeAttackStarted += HandleMeleeAttackStarted;
            boss.DashStarted += HandleDashStarted;
            boss.DashWallHit += HandleDashWallHit;
            boss.DashRecovered += HandleDashRecovered;
            boss.SpinStarted += HandleSpinStarted;
            boss.SpinBounced += HandleSpinBounced;
            boss.SpinStopped += HandleSpinStopped;
        }
    }

    private void OnDisable()
    {
        StopDashLoop();

        if (healthTarget != null)
        {
            healthTarget.Damaged -= HandleDamaged;
            healthTarget.Died -= HandleDied;
        }

        if (boss != null)
        {
            boss.MeleeAttackStarted -= HandleMeleeAttackStarted;
            boss.DashStarted -= HandleDashStarted;
            boss.DashWallHit -= HandleDashWallHit;
            boss.DashRecovered -= HandleDashRecovered;
            boss.SpinStarted -= HandleSpinStarted;
            boss.SpinBounced -= HandleSpinBounced;
            boss.SpinStopped -= HandleSpinStopped;
        }
    }

    private void HandleDamaged(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (healthTarget != null && healthTarget.IsDead)
        {
            return;
        }

        AudioManager.PlaySfx(damagedCue, hitPoint);
    }

    private void HandleDied(HealthTarget deadHealth)
    {
        StopDashLoop();
        AudioManager.PlaySfx(deathCue, transform.position);
    }

    private void HandleMeleeAttackStarted(BossController source)
    {
        AudioManager.PlaySfx(meleeAttackCue, transform.position);
    }

    private void HandleDashStarted(BossController source)
    {
        AudioManager.PlaySfx(dashStartCue, transform.position);
        StopDashLoop();
        dashLoopCoroutine = StartCoroutine(PlayDashLoopRoutine());
    }

    private void HandleDashWallHit(BossController source)
    {
        AudioManager.PlaySfx(dashWallImpactCue, transform.position);
    }

    private void HandleDashRecovered(BossController source)
    {
        StopDashLoop();
        AudioManager.PlaySfx(dashRecoverCue, transform.position);
    }

    private void HandleSpinStarted(BossController source)
    {
        AudioManager.PlaySfx(spinStartCue, transform.position);
    }

    private void HandleSpinBounced(BossController source)
    {
        AudioManager.PlaySfx(spinBounceCue, transform.position);
    }

    private void HandleSpinStopped(BossController source)
    {
        AudioManager.PlaySfx(spinStopCue, transform.position);
    }

    private IEnumerator PlayDashLoopRoutine()
    {
        while (boss != null && boss.IsDashing)
        {
            AudioManager.PlaySfx(dashLoopCue, transform.position);
            yield return new WaitForSeconds(dashLoopInterval);
        }

        dashLoopCoroutine = null;
    }

    private void StopDashLoop()
    {
        if (dashLoopCoroutine == null)
        {
            return;
        }

        StopCoroutine(dashLoopCoroutine);
        dashLoopCoroutine = null;
    }
}
