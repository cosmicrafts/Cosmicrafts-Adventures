using UnityEngine;
using Unity.Netcode;

public class InputComponent : NetworkBehaviour
{
    private MovementComponent movementComponent;
    private RotationComponent rotationComponent;
    private DashComponent dashComponent;
    private ShootingComponent shootingComponent;

    [Header("Camera Settings")]
    public Camera playerCamera; // Assign the correct player camera from the prefab in the editor

    [Header("Camera Zoom Settings")]
    public float zoomSmoothSpeed = 5f; // Smooth transition speed between zoom levels
    private readonly float[] zoomLevels = { 8f, 16f, 24f, 36f }; // Predefined zoom levels
    private int currentZoomIndex; // The current zoom level index
    private float zoomInput;

    private void Start()
    {
        movementComponent = GetComponent<MovementComponent>();
        rotationComponent = GetComponent<RotationComponent>();
        dashComponent = GetComponent<DashComponent>();
        shootingComponent = GetComponent<ShootingComponent>();

        if (IsOwner && playerCamera != null)
        {
            // Initialize the zoom level to the closest matching level to the current orthographic size
            currentZoomIndex = GetClosestZoomIndex(playerCamera.orthographicSize);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Handle Movement Input
        Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        movementComponent.SetMoveInput(moveInput);

        // Handle Rotation Input
        Vector3 mouseWorldPosition = playerCamera.ScreenToWorldPoint(new Vector3(
            Input.mousePosition.x, Input.mousePosition.y, -playerCamera.transform.position.z));
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

        // Handle Zoom Input (Mouse Scroll Wheel)
        HandleCameraZoomInput();

        // Handle Camera Zoom (Smooth Transition)
        HandleCameraZoom();
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

        // Reset zoom input after processing to prevent continuous zoom
        zoomInput = 0;
    }

    private void HandleCameraZoom()
    {
        if (playerCamera == null) return;

        // Smoothly interpolate the camera's orthographic size to the target zoom level
        float targetZoom = zoomLevels[currentZoomIndex];
        playerCamera.orthographicSize = Mathf.Lerp(playerCamera.orthographicSize, targetZoom, Time.deltaTime * zoomSmoothSpeed);
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
