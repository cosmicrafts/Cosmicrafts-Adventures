using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class InputComponent : NetworkBehaviour
{
    private MovementComponent movementComponent;
    private RotationComponent rotationComponent;
    private DashComponent dashComponent;
    private ShootingComponent shootingComponent;

    private Camera mainCamera;
    private CameraFollow cameraFollow;

    [Header("Camera Zoom Settings")]
    public float zoomSmoothSpeed = 5f; // Smooth transition speed between zoom levels
    private readonly float[] zoomLevels = { 8f, 12f, 18f, 24f, 28f, 32f, 36f }; // Predefined zoom levels
    private int currentZoomIndex; // The current zoom level index
    private float zoomInput;

    private Controls controls; // Input action reference

    private void Awake()
    {
        controls = new Controls(); // Initialize Input Actions
    }

    private void OnEnable()
    {
        controls.Enable(); // Enable the input actions when component is enabled
    }

    private void OnDisable()
    {
        controls.Disable(); // Disable the input actions when component is disabled
    }

    private void Start()
    {
        movementComponent = GetComponent<MovementComponent>();
        rotationComponent = GetComponent<RotationComponent>();
        dashComponent = GetComponent<DashComponent>();
        shootingComponent = GetComponent<ShootingComponent>();

        if (IsOwner)
        {
            mainCamera = Camera.main;
            cameraFollow = mainCamera.GetComponent<CameraFollow>();

            // Set the target of the camera to this player's transform
            if (cameraFollow != null)
            {
                cameraFollow.target = transform; // Assign this player's transform as the target
            }

            // Initialize the zoom level to the closest matching level to the current orthographic size
            currentZoomIndex = GetClosestZoomIndex(mainCamera.orthographicSize);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Handle Movement and Rotation together
        UpdateMovementAndRotation();

        // Handle Dash Input (using the new Input System)
        if (controls.Dash.Dash.triggered)
        {
            dashComponent.RequestDash();
        }

        // Handle Shooting Input
        if (controls.Shoot.Shoot.ReadValue<float>() > 0)
        {
            shootingComponent.RequestShoot();
        }

        // Handle Zoom Input and Zoom Update
        HandleCameraZoomInput();
        HandleCameraZoom();
    }

    private void UpdateMovementAndRotation()
    {
        // Get input from either joystick or WASD
        Vector2 moveInput = controls.Move.Moveaction.ReadValue<Vector2>(); // New Input System for Movement

        // Handle Movement Input
        movementComponent.SetMoveInput(moveInput);

        // Handle Rotation Input based on the current mouse position
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(
            Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, -mainCamera.transform.position.z));
        rotationComponent.SetMousePosition(mouseWorldPosition);
    }

    private void HandleCameraZoomInput()
    {
        // Handle mouse scroll for zooming (desktop)
        float zoomScrollInput = controls.Zoom.Zoom.ReadValue<Vector2>().y; // Use y axis for zoom

        // Detect if there's a scroll input
        if (zoomScrollInput > 0 && currentZoomIndex > 0)
        {
            currentZoomIndex--;
        }
        else if (zoomScrollInput < 0 && currentZoomIndex < zoomLevels.Length - 1)
        {
            currentZoomIndex++;
        }
    }

    private void HandleCameraZoom()
    {
        // Smoothly interpolate the camera's orthographic size to the target zoom level
        float targetZoom = zoomLevels[currentZoomIndex];
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetZoom, Time.deltaTime * zoomSmoothSpeed);
    }

    private int GetClosestZoomIndex(float currentZoom)
    {
        int closestIndex = 0;
        float closestDifference = Mathf.Abs(currentZoom - zoomLevels[0]);

        for (int i = 1; i < zoomLevels.Length; i++)
        {
            float difference = Mathf.Abs(currentZoom - zoomLevels[i]);
            if (difference < closestDifference)
            {
                closestDifference = difference;
                closestIndex = i;
            }
        }

        return closestIndex;
    }
}
