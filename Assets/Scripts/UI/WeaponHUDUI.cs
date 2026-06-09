using System;
using UnityEngine;

public class WeaponHUDUI : MonoBehaviour
{
    [Serializable]
    private class UnarmedHudEntry
    {
        [SerializeField] private string label = "Unarmed";
        [SerializeField] private GameObject visualRoot;

        public void SetActive(bool active)
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(active);
            }
        }
    }

    [Serializable]
    private class WeaponHudEntry
    {
        [SerializeField] private string label = "Weapon";
        [SerializeField] private WeaponDefinition weapon;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private GameObject[] ammoSprites;

        public WeaponDefinition Weapon => weapon;

        public void SetActive(bool active)
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(active);
            }
        }

        public void SetAmmoVisible(int ammo)
        {
            if (ammoSprites == null)
            {
                return;
            }

            int visibleAmmo = Mathf.Max(0, ammo);
            for (int i = 0; i < ammoSprites.Length; i++)
            {
                if (ammoSprites[i] != null)
                {
                    ammoSprites[i].SetActive(i < visibleAmmo);
                }
            }
        }
    }

    [Header("References")]
    [SerializeField] private PlayerWeaponController weaponController;

    [Header("Entries")]
    [SerializeField] private UnarmedHudEntry unarmedEntry = new UnarmedHudEntry();
    [SerializeField] private WeaponHudEntry[] weaponEntries;

    private WeaponHudEntry currentWeaponEntry;

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void SetWeaponController(PlayerWeaponController newWeaponController)
    {
        if (weaponController == newWeaponController)
        {
            return;
        }

        Unsubscribe();
        weaponController = newWeaponController;
        Subscribe();
        Refresh();
    }

    private void Subscribe()
    {
        if (weaponController == null)
        {
            return;
        }

        weaponController.WeaponChanged += HandleWeaponChanged;
        weaponController.AmmoChanged += HandleAmmoChanged;
    }

    private void Unsubscribe()
    {
        if (weaponController == null)
        {
            return;
        }

        weaponController.WeaponChanged -= HandleWeaponChanged;
        weaponController.AmmoChanged -= HandleAmmoChanged;
    }

    private void Refresh()
    {
        ShowWeapon(weaponController != null ? weaponController.CurrentWeapon : null,
            weaponController != null ? weaponController.CurrentAmmo : 0);
    }

    private void HandleWeaponChanged(WeaponDefinition weapon, int ammo)
    {
        ShowWeapon(weapon, ammo);
    }

    private void HandleAmmoChanged(int ammo)
    {
        if (currentWeaponEntry != null)
        {
            currentWeaponEntry.SetAmmoVisible(weaponController != null && weaponController.HasWeapon ? ammo : 0);
        }
    }

    private void ShowWeapon(WeaponDefinition weapon, int ammo)
    {
        HideAllEntries();

        if (weapon == null)
        {
            currentWeaponEntry = null;
            unarmedEntry?.SetActive(true);
            return;
        }

        currentWeaponEntry = GetEntryForWeapon(weapon);
        if (currentWeaponEntry == null)
        {
            return;
        }

        currentWeaponEntry.SetActive(true);
        currentWeaponEntry.SetAmmoVisible(ammo);
    }

    private WeaponHudEntry GetEntryForWeapon(WeaponDefinition weapon)
    {
        if (weapon == null || weaponEntries == null)
        {
            return null;
        }

        for (int i = 0; i < weaponEntries.Length; i++)
        {
            if (weaponEntries[i] != null && weaponEntries[i].Weapon == weapon)
            {
                return weaponEntries[i];
            }
        }

        return null;
    }

    private void HideAllEntries()
    {
        unarmedEntry?.SetActive(false);

        if (weaponEntries == null)
        {
            return;
        }

        for (int i = 0; i < weaponEntries.Length; i++)
        {
            weaponEntries[i]?.SetActive(false);
        }
    }
}
