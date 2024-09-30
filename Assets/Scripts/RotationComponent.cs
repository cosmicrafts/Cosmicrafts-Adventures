using UnityEngine;
using Unity.Netcode;

public class RotationComponent : NetworkBehaviour
{
    private Camera mainCamera;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (IsOwner)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, -mainCamera.transform.position.z));

        SendRotationInputServerRpc(mouseWorldPosition);
    }

    [ServerRpc]
    private void SendRotationInputServerRpc(Vector3 mousePosition)
    {
        HandleRotation(mousePosition);
        UpdateRotationClientRpc(mousePosition);
    }

    [ClientRpc]
    private void UpdateRotationClientRpc(Vector3 mousePosition)
    {
        if (!IsOwner)
        {
            HandleRotation(mousePosition);
        }
    }

    private void HandleRotation(Vector3 mousePosition)
    {
        Vector2 direction = (mousePosition - transform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        rb.SetRotation(targetAngle);
    }
}
