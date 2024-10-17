using UnityEngine;
using Unity.Netcode;

public class DashComponent : NetworkBehaviour
{
    public float dashSpeed = 40f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private MovementComponent movementComponent;
    private Rigidbody2D rb;

    // Use NetworkVariable to sync dashing state across server and clients
    private NetworkVariable<bool> isDashing = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<float> serverDashCooldownTimer = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void ApplyConfiguration(ObjectSO config)
    {
        if (config.hasDashAbility)
        {
            dashSpeed = config.dashSpeed;
            dashDuration = config.dashDuration;
            dashCooldown = config.dashCooldown;
        }
    }

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
    }

    public void RequestDash()
    {
        if (!isDashing.Value && serverDashCooldownTimer.Value <= 0f)
        {
            // Client-side prediction: simulate the dash immediately for responsiveness
            SimulateDashLocally();

            // Send dash request to the server to confirm and sync with other clients
            RequestDashServerRpc();
        }
    }

    // Simulate dash instantly on the client-side for immediate feedback
    private void SimulateDashLocally()
    {
        if (!isDashing.Value) // Ensure no active dash is occurring
        {
            rb.linearVelocity = transform.up * dashSpeed;  // Apply dash speed
            movementComponent.SetCanMove(false);  // Disable normal movement during dash
            Invoke(nameof(EndLocalDash), dashDuration);  // End dash after duration
        }
    }

    private void EndLocalDash()
    {
        rb.linearVelocity = Vector2.zero; // Clear velocity after dash ends
        movementComponent.SetCanMove(true);  // Re-enable movement after dash ends
    }

    // Server handles cooldowns and authoritative dash logic
    private void HandleServerCooldown()
    {
        if (serverDashCooldownTimer.Value > 0f)
        {
            serverDashCooldownTimer.Value -= Time.deltaTime;
        }
    }

    // Server receives dash request from client via ServerRpc
    [ServerRpc]
    private void RequestDashServerRpc()
    {
        if (!isDashing.Value && serverDashCooldownTimer.Value <= 0f)
        {
            isDashing.Value = true;  // Set dashing state to true on server
            serverDashCooldownTimer.Value = dashCooldown;  // Start cooldown timer

            StartDash();  // Execute dash on server

            StartDashClientRpc();  // Notify all clients to start dashing
        }
    }

    // The server-side logic for initiating a dash
    private void StartDash()
    {
        rb.linearVelocity = transform.up * dashSpeed;  // Apply dash speed
        movementComponent.SetCanMove(false);  // Disable movement
        Invoke(nameof(EndDash), dashDuration);  // End dash after duration
    }

    // This is called on all clients to sync the dash behavior
    [ClientRpc]
    private void StartDashClientRpc()
    {
        if (!IsServer)  // Clients who are not the server will simulate dash locally
        {
            SimulateDashLocally();
        }
    }

    // End dash on the server and re-enable movement
    private void EndDash()
    {
        rb.linearVelocity = Vector2.zero; // Ensure velocity is cleared after dash ends
        if (IsServer)
        {
            isDashing.Value = false;  // Reset the dashing state
        }
        movementComponent.SetCanMove(true);  // Re-enable movement after dash
    }

    // Public method to check if the player is dashing
    public bool IsDashing()
    {
        return isDashing.Value;
    }
}
