using UnityEngine;
using Unity.Netcode;

public class RotationComponent : NetworkBehaviour
{
    private Rigidbody2D rb;
    private MovementComponent movementComponent;

    private Vector3 mousePosition;

    // Network variable to synchronize the rotation across clients
    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void ApplyConfiguration(PlayerSO config)
    {
        // If your PlayerSO ever includes rotation-specific settings, apply them here.
        // For now, we'll leave it simple and use default settings.

        // Example: If you had a rotation speed in PlayerSO, you could apply it here:
        // this.rotationSmoothingSpeed = config.rotationSmoothingSpeed;
    }

        public void ApplyConfiguration(ObjectSO config)
    {
        // If your PlayerSO ever includes rotation-specific settings, apply them here.
        // For now, we'll leave it simple and use default settings.

        // Example: If you had a rotation speed in PlayerSO, you could apply it here:
        // this.rotationSmoothingSpeed = config.rotationSmoothingSpeed;
    }
    
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
            // Perform local rotation
            Quaternion newRotation = CalculateRotation(mousePosition);
            
            // Smoothly interpolate towards the new rotation
            float rotationSmoothingSpeed = 12f; // Adjust as needed for smoothness
            transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * rotationSmoothingSpeed);

            // Send rotation to the server for synchronization if not the server
            if (!IsServer)
            {
                UpdateRotationOnServerRpc(transform.rotation);
            }
        }
        else
        {
            // For non-owner clients, use the synchronized network variable for rotation
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation.Value, Time.deltaTime * 10f);
        }
    }


    public void SetMousePosition(Vector3 mousePosition)
    {
        this.mousePosition = mousePosition;
    }

    private Quaternion CalculateRotation(Vector3 mousePosition)
    {
        // Calculate direction towards the mouse
        Vector2 direction = (mousePosition - transform.position);
        
        // Define a minimum threshold distance to avoid jitter when the mouse is too close
        float minDistance = 0.25f; // Adjust as needed to find a balance that prevents jitter
        
        if (direction.magnitude < minDistance)
        {
            // If the mouse is too close, return the current rotation to prevent jitter
            return transform.rotation;
        }

        // Normalize the direction vector to calculate the new rotation
        direction.Normalize();
        float rotationZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        // Return the rotation around the Z-axis as a quaternion
        return Quaternion.Euler(0f, 0f, rotationZ);
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