using UnityEngine;
using Unity.Netcode;

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

    private float initialPinchDistance;
    private float previousPinchDistance;

    public void ApplyConfiguration(ObjectSO config)
    {
        // default settings
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

        // Handle Zoom Input and Zoom Update
        HandleCameraZoomInput();
        HandleCameraZoom();
    }

    private void UpdateMovementAndRotation()
    {
        // Get input from either joystick or WASD
        Vector2 joystickInput = Vector2.zero;
        if (JoystickController.Instance != null)
        {
            joystickInput = JoystickController.Instance.GetJoystickDirection();
        }

        Vector2 wasdInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // Combine joystick and WASD inputs
        Vector2 moveInput = joystickInput + wasdInput;
        moveInput = Vector2.ClampMagnitude(moveInput, 1f); // Ensure the combined input does not exceed 1

        // Handle Movement Input
        movementComponent.SetMoveInput(moveInput);

        // Handle Rotation Input based on the joystick direction
        if (joystickInput != Vector2.zero)
        {
            rotationComponent.SetJoystickDirection(joystickInput);
        }
        else
        {
            // Handle Rotation Input based on the current mouse position
            Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(
                Input.mousePosition.x, Input.mousePosition.y, -mainCamera.transform.position.z));
            rotationComponent.SetMousePosition(mouseWorldPosition);
        }
    }

    private void HandleCameraZoomInput()
    {
        // Read mouse scroll wheel input for zooming
        zoomInput = Input.GetAxis("Mouse ScrollWheel");

        // Detect if there's a scroll input
        if (zoomInput > 0 && currentZoomIndex > 0)
        {
            // Zoom in, decrease the zoom index
            currentZoomIndex--;
        }
        else if (zoomInput < 0 && currentZoomIndex < zoomLevels.Length - 1)
        {
            // Zoom out, increase the zoom index
            currentZoomIndex++;
        }

        // Handle pinch gesture for mobile devices
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            if (touchZero.phase == TouchPhase.Began || touchOne.phase == TouchPhase.Began)
            {
                initialPinchDistance = Vector2.Distance(touchZero.position, touchOne.position);
                previousPinchDistance = initialPinchDistance;
            }
            else if (touchZero.phase == TouchPhase.Moved || touchOne.phase == TouchPhase.Moved)
            {
                float currentPinchDistance = Vector2.Distance(touchZero.position, touchOne.position);
                float pinchDelta = currentPinchDistance - previousPinchDistance;

                // Adjust zoom based on pinch delta
                zoomInput = pinchDelta * 0.01f; // Adjust sensitivity as needed

                previousPinchDistance = currentPinchDistance;
            }
        }

        // Reset zoom input after processing to prevent continuous zoom
        zoomInput = 0;
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