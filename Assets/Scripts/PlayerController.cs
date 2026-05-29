using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float rotationSpeed = 20f;

    [Header("Camera Reference")]
    [SerializeField]
    [Tooltip("Camara usada para convertir el input de movimiento a espacio de pantalla. Si queda vacia usa Main Camera.")]
    private Camera gameplayCamera;

    [Header("Aiming References")]
    [SerializeField]
    [Tooltip("Objeto vacío en el mundo que marca dónde está apuntando el mouse.")]
    private Transform aimTarget;

    private CharacterController characterController;
    private Vector2 moveInput;
    private Vector3 lookTarget;
    private float verticalVelocity;
    private bool isJumping;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && characterController.isGrounded)
        {
            verticalVelocity = jumpForce;
            isJumping = true;
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        Camera cameraToUse = GetGameplayCamera();
        if (cameraToUse == null) return;

        Vector2 mouseScreenPosition = context.ReadValue<Vector2>();

        Ray ray = cameraToUse.ScreenPointToRay(mouseScreenPosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float enter))
        {
            lookTarget = ray.GetPoint(enter);

            // Movemos el objeto objetivo a la posición del mouse en el mundo 3D
            if (aimTarget != null)
            {
                aimTarget.position = lookTarget;
            }
        }
    }

    private void Update()
    {
        ApplyGravity();
        MovePlayer();
        RotateTowardsMouse();
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded)
        {
            if (!isJumping) verticalVelocity = -1f;
            isJumping = false;
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

    private void RotateTowardsMouse()
    {
        Vector3 lookDirection = lookTarget - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude <= 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}