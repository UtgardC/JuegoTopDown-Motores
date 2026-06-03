using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float rotationSpeed = 20f;

    [Header("Dash")]
    [SerializeField]
    [Tooltip("Distance covered by a dash.")]
    private float dashDistance = 5f;

    [SerializeField]
    [Tooltip("Dash duration in seconds. Speed is calculated from distance / duration.")]
    private float dashDuration = 0.15f;

    [SerializeField]
    [Tooltip("Maximum dash charges available to the player.")]
    private int maxDashCharges = 3;

    [SerializeField]
    [Tooltip("Points consumed by one dash charge.")]
    private float dashPointsPerCharge = 100f;

    [SerializeField]
    [Tooltip("Seconds needed to recover one full dash charge.")]
    private float dashRechargeSecondsPerCharge = 3f;

    [SerializeField]
    [Tooltip("Minimum input magnitude needed to dash.")]
    private float dashInputDeadzone = 0.1f;

    [SerializeField]
    [Tooltip("If active, the player starts with all dash charges available.")]
    private bool startWithFullDashCharges = true;

    [Header("Camera Reference")]
    [SerializeField]
    [Tooltip("Camera used to convert movement input to screen-relative world movement. If empty, Main Camera is used.")]
    private Camera gameplayCamera;

    [Header("Aiming References")]
    [SerializeField]
    [Tooltip("Empty world object that marks where the mouse is aiming.")]
    private Transform aimTarget;

    [SerializeField]
    [Tooltip("Weapon controller used to read the equipped weapon range. If empty, the component on this GameObject is used.")]
    private PlayerWeaponController weaponController;

    [SerializeField]
    [Tooltip("Aim range used when the player has no weapon equipped.")]
    private float defaultAimRange = 20f;

    [SerializeField]
    [Tooltip("Global multiplier applied only to the visual sight cursor range. It does not change weapon damage range.")]
    private float sightRangeVisualMultiplier = 1f;

    [SerializeField]
    [Tooltip("Extra multiplier by weapon range for the visual sight cursor. X = weapon range, Y = visual range multiplier.")]
    private AnimationCurve sightRangeVisualMultiplierByRange = AnimationCurve.Linear(0f, 1f, 30f, 1f);

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("How much the unclamped auxiliary cursor contributes to the camera aim target.")]
    private float auxAimTargetWeight;

    private CharacterController characterController;
    private Vector2 moveInput;
    private Vector2 rawMouseScreenPosition;
    private Vector3 lookTarget;
    private Vector3 rawAimWorldPoint;
    private Vector3 sightAimWorldPoint;
    private Vector3 dashDirection;
    private float verticalVelocity;
    private float dashPoints;
    private float dashElapsed;
    private bool hasLookInput;
    private bool isDashing;

    public float CurrentDashPoints => dashPoints;
    public float DashPointsPerCharge => SafeDashPointsPerCharge;
    public float MaxDashPoints => SafeDashPointsPerCharge * SafeMaxDashCharges;
    public int MaxDashCharges => SafeMaxDashCharges;
    public int AvailableDashCharges => Mathf.Clamp(Mathf.FloorToInt(dashPoints / SafeDashPointsPerCharge), 0, SafeMaxDashCharges);
    public bool IsDashing => isDashing;
    public Vector2 RawMouseScreenPosition => rawMouseScreenPosition;
    public Vector3 RawAimWorldPoint => rawAimWorldPoint;
    public Vector3 SightAimWorldPoint => sightAimWorldPoint;
    public float CurrentAimRange => GetCurrentSightAimRange();
    public float CurrentWeaponAimRange => GetCurrentWeaponAimRange();

    private int SafeMaxDashCharges => Mathf.Max(1, maxDashCharges);
    private float SafeDashPointsPerCharge => Mathf.Max(1f, dashPointsPerCharge);
    private float SafeDashRechargeSecondsPerCharge => Mathf.Max(0.01f, dashRechargeSecondsPerCharge);
    private float DashRechargeRate => SafeDashPointsPerCharge / SafeDashRechargeSecondsPerCharge;
    private float SafeDashDuration => Mathf.Max(0.01f, dashDuration);

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<PlayerWeaponController>();
        }

        rawMouseScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        hasLookInput = true;
        RefreshAimFromScreenPosition();

        dashPoints = startWithFullDashCharges ? MaxDashPoints : 0f;
    }

    private void OnValidate()
    {
        dashDistance = Mathf.Max(0f, dashDistance);
        dashDuration = Mathf.Max(0.01f, dashDuration);
        maxDashCharges = Mathf.Max(1, maxDashCharges);
        dashPointsPerCharge = Mathf.Max(1f, dashPointsPerCharge);
        dashRechargeSecondsPerCharge = Mathf.Max(0.01f, dashRechargeSecondsPerCharge);
        dashInputDeadzone = Mathf.Max(0f, dashInputDeadzone);
        defaultAimRange = Mathf.Max(0f, defaultAimRange);
        sightRangeVisualMultiplier = Mathf.Max(0f, sightRangeVisualMultiplier);

        if (sightRangeVisualMultiplierByRange == null)
        {
            sightRangeVisualMultiplierByRange = AnimationCurve.Linear(0f, 1f, 30f, 1f);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TryStartDash();
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        rawMouseScreenPosition = context.ReadValue<Vector2>();
        hasLookInput = true;
        RefreshAimFromScreenPosition();
    }

    private void Update()
    {
        if (hasLookInput && Time.timeScale > 0f)
        {
            RefreshAimFromScreenPosition();
        }

        RechargeDash();
        ApplyGravity();

        if (isDashing)
        {
            MoveDash();
        }
        else
        {
            MovePlayer();
        }

        RotateTowardsMouse();
    }

    private void RechargeDash()
    {
        if (dashPoints >= MaxDashPoints)
        {
            dashPoints = MaxDashPoints;
            return;
        }

        dashPoints = Mathf.Min(MaxDashPoints, dashPoints + (DashRechargeRate * Time.deltaTime));
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = -1f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void MovePlayer()
    {
        Vector3 movement = GetCameraRelativeMoveDirection() * moveSpeed;
        movement.y = verticalVelocity;
        characterController.Move(movement * Time.deltaTime);
    }

    private void TryStartDash()
    {
        if (isDashing || Time.timeScale <= 0f || dashPoints < SafeDashPointsPerCharge)
        {
            return;
        }

        Vector3 requestedDashDirection = GetCameraRelativeMoveDirection();
        if (requestedDashDirection.sqrMagnitude <= dashInputDeadzone * dashInputDeadzone)
        {
            return;
        }

        dashPoints = Mathf.Max(0f, dashPoints - SafeDashPointsPerCharge);
        dashDirection = requestedDashDirection.normalized;
        dashElapsed = 0f;
        isDashing = true;
    }

    private void MoveDash()
    {
        float remainingDuration = SafeDashDuration - dashElapsed;
        float frameDuration = Mathf.Min(Time.deltaTime, remainingDuration);
        float dashSpeed = dashDistance / SafeDashDuration;

        Vector3 movement = dashDirection * dashSpeed * frameDuration;
        movement.y = verticalVelocity * Time.deltaTime;
        characterController.Move(movement);

        dashElapsed += frameDuration;
        if (dashElapsed >= SafeDashDuration)
        {
            isDashing = false;
        }
    }

    private Vector3 GetCameraRelativeMoveDirection()
    {
        Camera cameraToUse = GetGameplayCamera();

        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraToUse != null)
        {
            forward = cameraToUse.transform.forward;
            right = cameraToUse.transform.right;

            forward.y = 0f;
            right.y = 0f;

            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            right = right.sqrMagnitude > 0.001f ? right.normalized : Vector3.right;
        }

        Vector3 moveDirection = (right * moveInput.x) + (forward * moveInput.y);
        return Vector3.ClampMagnitude(moveDirection, 1f);
    }

    private Camera GetGameplayCamera()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        return gameplayCamera;
    }

    private void RefreshAimFromScreenPosition()
    {
        Camera cameraToUse = GetGameplayCamera();
        if (cameraToUse == null)
        {
            return;
        }

        Ray ray = cameraToUse.ScreenPointToRay(rawMouseScreenPosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (!groundPlane.Raycast(ray, out float enter))
        {
            return;
        }

        rawAimWorldPoint = ray.GetPoint(enter);
        sightAimWorldPoint = ClampAimToCurrentRange(rawAimWorldPoint);
        lookTarget = sightAimWorldPoint;

        if (aimTarget != null)
        {
            aimTarget.position = Vector3.Lerp(sightAimWorldPoint, rawAimWorldPoint, auxAimTargetWeight);
        }
    }

    private Vector3 ClampAimToCurrentRange(Vector3 targetPoint)
    {
        float range = CurrentAimRange;
        Vector3 origin = transform.position;
        origin.y = targetPoint.y;

        Vector3 offset = targetPoint - origin;
        if (offset.magnitude <= range)
        {
            return targetPoint;
        }

        return origin + (offset.normalized * range);
    }

    private float GetCurrentWeaponAimRange()
    {
        if (weaponController == null)
        {
            weaponController = GetComponent<PlayerWeaponController>();
        }

        if (weaponController != null && weaponController.CurrentWeapon != null)
        {
            return Mathf.Max(0f, weaponController.CurrentWeapon.Range);
        }

        return Mathf.Max(0f, defaultAimRange);
    }

    private float GetCurrentSightAimRange()
    {
        float weaponRange = GetCurrentWeaponAimRange();
        float curveMultiplier = sightRangeVisualMultiplierByRange != null
            ? sightRangeVisualMultiplierByRange.Evaluate(weaponRange)
            : 1f;

        return Mathf.Max(0f, weaponRange * sightRangeVisualMultiplier * Mathf.Max(0f, curveMultiplier));
    }

    private void RotateTowardsMouse()
    {
        Vector3 lookDirection = lookTarget - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude <= 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
