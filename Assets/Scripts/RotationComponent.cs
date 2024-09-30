using UnityEngine;
using Unity.Netcode;

public class RotationComponent : NetworkBehaviour
{
    private Rigidbody2D rb;
    private MovementComponent movementComponent;

    // Maximum tilt angles for X and Y axes
    public float maxTiltAngleX = 8f;
    public float maxTiltAngleY = 8f;

    private Vector3 mousePosition; // Variable to store mouse position

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        movementComponent = GetComponent<MovementComponent>();
    }

    // Method to receive mouse position from InputComponent
    public void SetMousePosition(Vector3 mousePosition)
    {
        this.mousePosition = mousePosition;
    }

    private void Update()
    {
        if (IsOwner)
        {
            // Get the movement input from the MovementComponent
            Vector2 moveInput = movementComponent.MoveInput;

            // Send the move input and mouse position to the server for rotation calculations
            SendRotationInputServerRpc(mousePosition, moveInput);
        }
    }

    [ServerRpc]
    private void SendRotationInputServerRpc(Vector3 mousePosition, Vector2 moveInput)
    {
        HandleCombinedRotation(mousePosition, moveInput);
        UpdateRotationClientRpc(mousePosition, moveInput);
    }

    [ClientRpc]
    private void UpdateRotationClientRpc(Vector3 mousePosition, Vector2 moveInput)
    {
        if (!IsOwner)
        {
            HandleCombinedRotation(mousePosition, moveInput);
        }
    }

    private void HandleCombinedRotation(Vector3 mousePosition, Vector2 moveInput)
    {
        // Calculate rotation towards the mouse cursor (Z-axis rotation)
        Vector2 direction = (mousePosition - transform.position).normalized;
        float rotationZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        // Invert the X and Y tilt based on the mouse direction
        float mouseTiltX = Mathf.Clamp(direction.y * maxTiltAngleX, -maxTiltAngleX, maxTiltAngleX);
        float mouseTiltY = Mathf.Clamp(-direction.x * maxTiltAngleY, -maxTiltAngleY, maxTiltAngleY);

        // Calculate tilt angles based on movement input
        float tiltX = -moveInput.y * maxTiltAngleX + mouseTiltX; // Combine movement and inverted mouse tilt for X-axis
        float tiltY = moveInput.x * maxTiltAngleY + mouseTiltY;  // Combine movement and inverted mouse tilt for Y-axis

        // Create the combined rotation with tilt and pointer direction
        Quaternion tiltRotation = Quaternion.Euler(tiltX, tiltY, rotationZ);

        // Apply the combined rotation to the transform
        transform.rotation = tiltRotation;
    }
}
