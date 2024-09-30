using UnityEngine;
using Unity.Netcode;

public class DashComponent : NetworkBehaviour
{
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private MovementComponent movementComponent;
    private Rigidbody2D rb;

    private NetworkVariable<bool> isDashing = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float serverDashCooldownTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        movementComponent = GetComponent<MovementComponent>();
    }

    private void Update()
    {
        if (IsServer)
        {
            HandleServerCooldown();
        }

        if (IsOwner && Input.GetKeyDown(KeyCode.Space))
        {
            RequestDash(); // Update to use the new public method
        }
    }

    private void HandleServerCooldown()
    {
        if (serverDashCooldownTimer > 0f)
        {
            serverDashCooldownTimer -= Time.deltaTime;
        }
    }

    // Add this public method to be called by InputComponent
    public void RequestDash()
    {
        if (!isDashing.Value && serverDashCooldownTimer <= 0f)
        {
            RequestDashServerRpc();
        }
    }

    [ServerRpc]
    private void RequestDashServerRpc()
    {
        if (!isDashing.Value && serverDashCooldownTimer <= 0f)
        {
            isDashing.Value = true;
            serverDashCooldownTimer = dashCooldown;
            StartDashClientRpc();
            Debug.Log($"[Server] Player {OwnerClientId} started dashing.");
        }
        else
        {
            Debug.Log($"[Server] Player {OwnerClientId} cannot dash yet. Cooldown: {serverDashCooldownTimer}");
        }
    }

    [ClientRpc]
    private void StartDashClientRpc()
    {
        StartDash();
    }

    private void StartDash()
    {
        rb.linearVelocity = transform.up * dashSpeed;
        movementComponent.SetCanMove(false);
        Invoke(nameof(EndDash), dashDuration);
    }

    private void EndDash()
    {
        movementComponent.SetCanMove(true);
        NotifyDashEndedServerRpc();
    }

    [ServerRpc]
    private void NotifyDashEndedServerRpc()
    {
        isDashing.Value = false;
        Debug.Log($"[Server] Player {OwnerClientId} ended dashing.");
    }
}
