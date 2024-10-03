using UnityEngine;
using Unity.Netcode;

public class RotationComponent : NetworkBehaviour
{
    private Rigidbody2D rb;
    private MovementComponent movementComponent;

    public float maxTiltAngleX = 8f;
    public float maxTiltAngleY = 8f;

    private Vector3 mousePosition;

    // Network variable to synchronize the rotation across clients
    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        movementComponent = GetComponent<MovementComponent>();

        // Subscribe to the network variable change to update non-owner clients
        networkRotation.OnValueChanged += OnRotationChanged;
    }

    private void Update()
    {
        if (IsOwner)
        {
            Vector2 moveInput = movementComponent.MoveInput;

            // Perform local rotation
            Quaternion newRotation = CalculateCombinedRotation(mousePosition, moveInput);
            transform.rotation = newRotation;

            // Send rotation to the server for synchronization if not the server
            if (!IsServer)
            {
                UpdateRotationOnServerRpc(newRotation);
            }
        }
        else
        {
            // For non-owner clients, use the synchronized network variable for rotation
            transform.rotation = networkRotation.Value;
        }
    }

    public void SetMousePosition(Vector3 mousePosition)
    {
        this.mousePosition = mousePosition;
    }

    private Quaternion CalculateCombinedRotation(Vector3 mousePosition, Vector2 moveInput)
    {
        // Calculate direction towards the mouse
        Vector2 direction = (mousePosition - transform.position).normalized;
        float rotationZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        // Calculate tilt angles based on movement input and mouse direction
        float mouseTiltX = Mathf.Clamp(direction.y * maxTiltAngleX, -maxTiltAngleX, maxTiltAngleX);
        float mouseTiltY = Mathf.Clamp(-direction.x * maxTiltAngleY, -maxTiltAngleY, maxTiltAngleY);

        float tiltX = -moveInput.y * maxTiltAngleX + mouseTiltX;
        float tiltY = moveInput.x * maxTiltAngleY + mouseTiltY;

        // Return the combined tilt and rotation as a quaternion
        return Quaternion.Euler(tiltX, tiltY, rotationZ);
    }

    [ServerRpc]
    private void UpdateRotationOnServerRpc(Quaternion rotation)
    {
        // Update the server-side rotation and synchronize with other clients
        networkRotation.Value = rotation;
    }

    private void OnRotationChanged(Quaternion oldRotation, Quaternion newRotation)
    {
        // Update rotation for non-owner clients when the network variable changes
        if (!IsOwner)
        {
            transform.rotation = newRotation;
        }
    }
}
