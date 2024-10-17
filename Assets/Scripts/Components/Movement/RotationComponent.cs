using UnityEngine;
using Unity.Netcode;

public class RotationComponent : NetworkBehaviour
{
    private MovementComponent movementComponent;
    private Vector3 mousePosition;

    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float rotationSpeed;
    private bool usePointerRotation;

    public void ApplyConfiguration(ObjectSO config)
    {
        rotationSpeed = config.rotationSpeed;
        usePointerRotation = config.usePointerRotation;
    }

    private void Start()
    {
        movementComponent = GetComponent<MovementComponent>();
        networkRotation.OnValueChanged += OnRotationChanged;
    }

    private void Update()
    {
        if (usePointerRotation)
        {
            if (IsOwner)
            {
                HandlePointerRotation();
            }
            else
            {
                // Sync rotation for non-owner clients
                transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation.Value, Time.deltaTime * 10f);
            }
        }
        else
        {
            HandleAutomaticRotation();
        }
    }

    private void HandlePointerRotation()
    {
        Quaternion newRotation = CalculateRotation(mousePosition);
        float rotationSmoothingSpeed = 12f;
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * rotationSmoothingSpeed);

        if (!IsServer)
        {
            UpdateRotationOnServerRpc(transform.rotation);
        }
    }

    private void HandleAutomaticRotation()
    {
        // Only update rotation using transform, avoid Rigidbody interference
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
