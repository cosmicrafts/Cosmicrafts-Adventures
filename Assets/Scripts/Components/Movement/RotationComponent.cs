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

    private float rotationSpeed;
    private bool usePointerRotation;  // NEW: Local flag for controlling pointer-based rotation

    public void ApplyConfiguration(ObjectSO config)
    {
        // Apply rotation settings from the ObjectSO
        rotationSpeed = config.rotationSpeed;
        usePointerRotation = config.usePointerRotation;  // Apply the pointer rotation setting
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
        if (usePointerRotation)
        {
            // Handle pointer-based rotation (for players)
            if (IsOwner)
            {
                HandlePointerRotation();
            }
            else
            {
                // Synchronize rotation for non-owner clients
                transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation.Value, Time.deltaTime * 10f);
            }
        }
        else
        {
            // Automatic rotation for non-pointer-based objects (like asteroids)
            HandleAutomaticRotation();
        }
    }

    private void HandlePointerRotation()
    {
        Quaternion newRotation = CalculateRotation(mousePosition);

        // Smoothly interpolate towards the new rotation
        float rotationSmoothingSpeed = 12f;
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * rotationSmoothingSpeed);

        if (!IsServer)
        {
            UpdateRotationOnServerRpc(transform.rotation);
        }
    }

    private void HandleAutomaticRotation()
    {
        // Apply automatic rotation on the Z-axis based on the configured speed
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }

    public void SetMousePosition(Vector3 mousePosition)
    {
        this.mousePosition = mousePosition;
    }

    private Quaternion CalculateRotation(Vector3 mousePosition)
    {
        Vector2 direction = (mousePosition - transform.position);
        if (direction.magnitude < 0.25f)
        {
            return transform.rotation;
        }
        direction.Normalize();
        float rotationZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        return Quaternion.Euler(0f, 0f, rotationZ);
    }

    [ServerRpc]
    private void UpdateRotationOnServerRpc(Quaternion rotation)
    {
        networkRotation.Value = rotation;
    }

    private void OnRotationChanged(Quaternion oldRotation, Quaternion newRotation)
    {
        if (!IsOwner)
        {
            transform.rotation = newRotation;
        }
    }
}
