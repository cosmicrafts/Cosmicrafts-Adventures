using UnityEngine;
using Unity.Netcode;

public class RotationComponent : NetworkBehaviour
{
    private Rigidbody2D rb;
    private MovementComponent movementComponent;

    public float maxTiltAngleX = 8f;
    public float maxTiltAngleY = 8f;

    private Vector3 mousePosition;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        movementComponent = GetComponent<MovementComponent>();
    }

    public void SetMousePosition(Vector3 mousePosition)
    {
        this.mousePosition = mousePosition;
    }

    private void Update()
    {
        if (IsOwner)
        {
            Vector2 moveInput = movementComponent.MoveInput;
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
        Vector2 direction = (mousePosition - transform.position).normalized;
        float rotationZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        float mouseTiltX = Mathf.Clamp(direction.y * maxTiltAngleX, -maxTiltAngleX, maxTiltAngleX);
        float mouseTiltY = Mathf.Clamp(-direction.x * maxTiltAngleY, -maxTiltAngleY, maxTiltAngleY);

        float tiltX = -moveInput.y * maxTiltAngleX + mouseTiltX;
        float tiltY = moveInput.x * maxTiltAngleY + mouseTiltY;

        Quaternion tiltRotation = Quaternion.Euler(tiltX, tiltY, rotationZ);
        transform.rotation = tiltRotation;
    }
}
