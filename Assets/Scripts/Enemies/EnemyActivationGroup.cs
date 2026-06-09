using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyActivationGroup : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private bool autoRegisterChildEnemies = true;
    [SerializeField] private List<EnemyController> enemies = new List<EnemyController>();

    private bool activated;
    private bool cleared;

    public bool IsActivated => activated;
    public bool IsCleared => cleared;

    public event Action<EnemyActivationGroup> Activated;
    public event Action<EnemyActivationGroup> Cleared;

    private void Awake()
    {
        AcquirePlayer();
    }

    private void OnEnable()
    {
        RegisterConfiguredEnemies();
    }

    public void RegisterEnemy(EnemyController enemy)
    {
        if (enemy == null)
        {
            return;
        }

        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }

        enemy.Died -= HandleEnemyDied;
        enemy.Died += HandleEnemyDied;
        enemy.SetActivationGroup(this);
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

        if (playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        activated = true;
        cleared = false;
        RegisterConfiguredEnemies();

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
            {
                enemies[i].Activate(playerHealth);
            }
        }

        Activated?.Invoke(this);
        AudioManager.NotifyCombatGroupActivated(this);
        CheckCleared();
    }

    private void RegisterConfiguredEnemies()
    {
        if (autoRegisterChildEnemies)
        {
            EnemyController[] childEnemies = GetComponentsInChildren<EnemyController>(true);
            for (int i = 0; i < childEnemies.Length; i++)
            {
                RegisterEnemy(childEnemies[i]);
            }
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
            {
                enemies[i].Died -= HandleEnemyDied;
                enemies[i].Died += HandleEnemyDied;
                enemies[i].SetActivationGroup(this);
            }
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
            {
                enemies[i].Died -= HandleEnemyDied;
            }
        }
    }

    private void AcquirePlayer()
    {
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<Health>();
        }
    }

    private void HandleEnemyDied(EnemyController enemy)
    {
        CheckCleared();
    }

    private void CheckCleared()
    {
        if (!activated || cleared)
        {
            return;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null && !enemies[i].IsDead)
            {
                return;
            }
        }

        cleared = true;
        Cleared?.Invoke(this);
        AudioManager.NotifyCombatGroupCleared(this);
    }
}
