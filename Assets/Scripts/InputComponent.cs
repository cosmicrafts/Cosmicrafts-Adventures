using UnityEngine;
using Unity.Netcode;

public class InputComponent : NetworkBehaviour
{
    private MovementComponent movementComponent;
    private RotationComponent rotationComponent;
    private DashComponent dashComponent;
    private ShootingComponent shootingComponent; // Assuming you have this

    private Camera mainCamera;

    private void Start()
    {
        movementComponent = GetComponent<MovementComponent>();
        rotationComponent = GetComponent<RotationComponent>();
        dashComponent = GetComponent<DashComponent>();
        shootingComponent = GetComponent<ShootingComponent>();

        if (IsOwner)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Handle Movement Input
        Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        movementComponent.SetMoveInput(moveInput);

        // Handle Rotation Input
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(
            Input.mousePosition.x, Input.mousePosition.y, -mainCamera.transform.position.z));
        rotationComponent.SetMousePosition(mouseWorldPosition);

        // Handle Dash Input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            dashComponent.RequestDash();
        }

        // Handle Shooting Input
        if (Input.GetMouseButton(0))
        {
            shootingComponent.RequestShoot();
        }
    }
}