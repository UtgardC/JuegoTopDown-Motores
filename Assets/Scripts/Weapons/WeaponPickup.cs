using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponDefinition definition;
    [SerializeField] private int remainingAmmo = -1;
    [SerializeField] private GameObject pickupRoot;

    [Header("Empty Cleanup")]
    [SerializeField] private bool destroyWhenEmptyAndOffCamera = true;
    [SerializeField] private Camera visibilityCamera;
    [SerializeField] private float offCameraDestroyDelay = 0.5f;
    [SerializeField] private float viewportMargin = 0.05f;

    private float pickupEnabledTime;
    private float offCameraTime;

    public WeaponDefinition Definition => definition;
    public int RemainingAmmo => remainingAmmo;

    private void Awake()
    {
        if (pickupRoot == null)
        {
            pickupRoot = gameObject;
        }

        if (definition != null && remainingAmmo < 0)
        {
            remainingAmmo = definition.Ammo;
        }
    }

    private void Update()
    {
        CleanupEmptyPickup();
    }

    private void OnValidate()
    {
        offCameraDestroyDelay = Mathf.Max(0f, offCameraDestroyDelay);
        viewportMargin = Mathf.Max(0f, viewportMargin);
    }

    public void SetRemainingAmmo(int ammo, float pickupLockoutSeconds)
    {
        remainingAmmo = Mathf.Max(0, ammo);
        pickupEnabledTime = Time.time + Mathf.Max(0f, pickupLockoutSeconds);
    }

    public void TryGiveWeaponTo(Collider other)
    {
        if (Time.time < pickupEnabledTime || definition == null || other == null)
        {
            return;
        }

        PlayerWeaponController playerWeaponController = other.GetComponentInParent<PlayerWeaponController>();
        if (playerWeaponController == null)
        {
            return;
        }

        if (playerWeaponController.TryPickupWeapon(this))
        {
            Destroy(pickupRoot != null ? pickupRoot : gameObject);
        }
    }

    private void CleanupEmptyPickup()
    {
        if (!destroyWhenEmptyAndOffCamera || remainingAmmo > 0)
        {
            offCameraTime = 0f;
            return;
        }

        Camera cameraToUse = GetVisibilityCamera();
        if (cameraToUse == null)
        {
            return;
        }

        if (IsVisible(cameraToUse))
        {
            offCameraTime = 0f;
            return;
        }

        offCameraTime += Time.deltaTime;
        if (offCameraTime >= offCameraDestroyDelay)
        {
            Destroy(pickupRoot != null ? pickupRoot : gameObject);
        }
    }

    private Camera GetVisibilityCamera()
    {
        if (visibilityCamera == null)
        {
            visibilityCamera = Camera.main;
        }

        return visibilityCamera;
    }

    private bool IsVisible(Camera cameraToUse)
    {
        Vector3 viewportPosition = cameraToUse.WorldToViewportPoint(transform.position);
        return viewportPosition.z > 0f
            && viewportPosition.x >= -viewportMargin
            && viewportPosition.x <= 1f + viewportMargin
            && viewportPosition.y >= -viewportMargin
            && viewportPosition.y <= 1f + viewportMargin;
    }
}
