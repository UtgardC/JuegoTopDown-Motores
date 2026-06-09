using System;
using UnityEngine;

public class BossActivationGroup : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private BossController boss;
    [SerializeField] private bool autoFindChildBoss = true;

    private bool activated;
    private bool cleared;

    public bool IsActivated => activated;
    public bool IsCleared => cleared;
    public BossController Boss => boss;
    public HealthTarget BossHealth => boss != null ? boss.BossHealth : null;

    public event Action<BossActivationGroup> Activated;
    public event Action<BossActivationGroup> Cleared;

    private void Awake()
    {
        AcquirePlayer();
    }

    private void OnEnable()
    {
        RegisterBoss();
    }

    private void OnDisable()
    {
        if (boss != null)
        {
            boss.Died -= HandleBossDied;
        }
    }

    public void Activate(Health targetHealth = null)
    {
        if (activated)
        {
            return;
        }

        if (targetHealth != null)
        {
            playerHealth = targetHealth;
        }
        else
        {
            AcquirePlayer();
        }

        RegisterBoss();

        if (playerHealth == null || playerHealth.IsDead || boss == null || boss.IsDead)
        {
            return;
        }

        activated = true;
        cleared = false;

        boss.Activate(playerHealth);
        Activated?.Invoke(this);
        AudioManager.NotifyCombatGroupActivated(this);
        CheckCleared();
    }

    public void RegisterBoss(BossController newBoss)
    {
        if (newBoss == null)
        {
            return;
        }

        if (boss != null)
        {
            boss.Died -= HandleBossDied;
        }

        boss = newBoss;
        boss.Died -= HandleBossDied;
        boss.Died += HandleBossDied;
        boss.SetActivationGroup(this);
    }

    private void RegisterBoss()
    {
        if (autoFindChildBoss)
        {
            BossController childBoss = GetComponentInChildren<BossController>(true);
            if (childBoss != null)
            {
                RegisterBoss(childBoss);
                return;
            }
        }

        if (boss != null)
        {
            RegisterBoss(boss);
        }
    }

    private void AcquirePlayer()
    {
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<Health>();
        }
    }

    private void HandleBossDied(BossController deadBoss)
    {
        CheckCleared();
    }

    private void CheckCleared()
    {
        if (!activated || cleared || boss == null || !boss.IsDead)
        {
            return;
        }

        cleared = true;
        Cleared?.Invoke(this);
        AudioManager.NotifyCombatGroupCleared(this);
    }
}
