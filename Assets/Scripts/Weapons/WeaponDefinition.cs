using UnityEngine;

[CreateAssetMenu(menuName = "TopDown/Weapons/Weapon Definition", fileName = "WeaponDefinition")]
public class WeaponDefinition : ScriptableObject
{
    [SerializeField] private string weaponName = "Weapon";
    [SerializeField] private WeaponMode mode = WeaponMode.Normal;
    [SerializeField] private int ammo = 10;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float range = 30f;
    [SerializeField] private float fireRate = 0.25f;
    [SerializeField] private bool automatic;

    [Header("Spread")]
    [SerializeField] private int pelletCount = 6;
    [SerializeField] private float spreadAngle = 30f;

    [Header("Visual")]
    [SerializeField] private GameObject model;

    [Header("Pickup")]
    [SerializeField] private WeaponPickup pickupPrefab;

    public string WeaponName => weaponName;
    public WeaponMode Mode => mode;
    public int Ammo => Mathf.Max(0, ammo);
    public float Damage => Mathf.Max(0f, damage);
    public float Range => Mathf.Max(0f, range);
    public float FireRate => Mathf.Max(0f, fireRate);
    public bool Automatic => automatic;
    public int PelletCount => Mathf.Max(1, pelletCount);
    public float SpreadAngle => Mathf.Max(0f, spreadAngle);
    public GameObject Model => model;
    public WeaponPickup PickupPrefab => pickupPrefab;
    public float DamagePerPellet => Mode == WeaponMode.Spread ? Damage / PelletCount : Damage;

    private void OnValidate()
    {
        ammo = Mathf.Max(0, ammo);
        damage = Mathf.Max(0f, damage);
        range = Mathf.Max(0f, range);
        fireRate = Mathf.Max(0f, fireRate);
        pelletCount = Mathf.Max(1, pelletCount);
        spreadAngle = Mathf.Max(0f, spreadAngle);
    }
}
