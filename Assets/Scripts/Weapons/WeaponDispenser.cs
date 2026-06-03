using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponDispenser : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private WeaponDefinition weaponDefinition;
    [SerializeField] private Transform spawnPoint;

    [Header("Launch")]
    [SerializeField] private Vector3 localLaunchDirection = Vector3.forward;
    [SerializeField] private float launchForce = 8f;
    [SerializeField] private float pickupLockoutSeconds = 0.5f;

    [Header("Timing")]
    [SerializeField] private float spawnDelay = 1f;

    private readonly HashSet<PlayerWeaponController> playersInside = new HashSet<PlayerWeaponController>();

    private bool waitingToSpawn;
    private bool mustExitBeforeNextSpawn;

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnValidate()
    {
        launchForce = Mathf.Max(0f, launchForce);
        pickupLockoutSeconds = Mathf.Max(0f, pickupLockoutSeconds);
        spawnDelay = Mathf.Max(0f, spawnDelay);

        if (localLaunchDirection.sqrMagnitude <= 0.001f)
        {
            localLaunchDirection = Vector3.forward;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerWeaponController player = other.GetComponentInParent<PlayerWeaponController>();
        if (player == null)
        {
            return;
        }

        playersInside.Add(player);

        if (!waitingToSpawn && !mustExitBeforeNextSpawn)
        {
            StartCoroutine(SpawnAfterDelay());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerWeaponController player = other.GetComponentInParent<PlayerWeaponController>();
        if (player == null)
        {
            return;
        }

        playersInside.Remove(player);

        if (playersInside.Count == 0)
        {
            mustExitBeforeNextSpawn = false;
        }
    }

    private IEnumerator SpawnAfterDelay()
    {
        waitingToSpawn = true;

        if (spawnDelay > 0f)
        {
            yield return new WaitForSeconds(spawnDelay);
        }

        waitingToSpawn = false;

        if (playersInside.Count == 0)
        {
            yield break;
        }

        SpawnWeapon();
        mustExitBeforeNextSpawn = true;
    }

    private void SpawnWeapon()
    {
        if (weaponDefinition == null || weaponDefinition.PickupPrefab == null)
        {
            Debug.LogWarning($"{name} cannot dispense a weapon because its definition or pickup prefab is missing.", this);
            return;
        }

        Transform source = spawnPoint != null ? spawnPoint : transform;
        Vector3 direction = source.TransformDirection(localLaunchDirection.normalized);
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        WeaponPickup pickup = Instantiate(weaponDefinition.PickupPrefab, source.position, rotation);
        pickup.SetRemainingAmmo(weaponDefinition.Ammo, pickupLockoutSeconds);

        Rigidbody pickupRigidbody = pickup.GetComponentInParent<Rigidbody>();
        if (pickupRigidbody != null)
        {
            pickupRigidbody.AddForce(direction * launchForce, ForceMode.Impulse);
        }
    }
}
