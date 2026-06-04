using System.Collections.Generic;
using UnityEngine;

public class EnemyActivationGroup : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private bool autoRegisterChildEnemies = true;
    [SerializeField] private List<EnemyController> enemies = new List<EnemyController>();

    private bool activated;

    public bool IsActivated => activated;

    private void Awake()
    {
        RegisterConfiguredEnemies();
        AcquirePlayer();
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
        RegisterConfiguredEnemies();

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
            {
                enemies[i].Activate(playerHealth);
            }
        }
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
                enemies[i].SetActivationGroup(this);
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
}
