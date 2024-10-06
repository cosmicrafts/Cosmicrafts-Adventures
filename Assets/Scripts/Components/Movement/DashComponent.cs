using UnityEngine;
using Unity.Netcode;

public class DashComponent : NetworkBehaviour
{
    public float dashSpeed = 40f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private MovementComponent movementComponent;
    private Rigidbody2D rb;

    public void ApplyConfiguration(PlayerSO config)
    {
        if (config.hasDashAbility)
        {
            dashSpeed = config.dashSpeed;
            dashDuration = config.dashDuration;
            dashCooldown = config.dashCooldown;
        }
    }

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
    }

    public void RequestDash()
    {
        if (!isDashing.Value && serverDashCooldownTimer <= 0f)
        {
            RequestDashServerRpc();
        }
    }

    private void HandleServerCooldown()
    {
        if (serverDashCooldownTimer > 0f)
        {
            serverDashCooldownTimer -= Time.deltaTime;
        }
    }

    [ServerRpc]
    private void RequestDashServerRpc()
    {
        if (!isDashing.Value && serverDashCooldownTimer <= 0f)
        {
            isDashing.Value = true;
            serverDashCooldownTimer = dashCooldown;
            StartDash();
            StartDashClientRpc();
        }
    }

    private void StartDash()
    {
        rb.linearVelocity = transform.up * dashSpeed;
        movementComponent.SetCanMove(false);
        Invoke(nameof(EndDash), dashDuration);
    }

    [ClientRpc]
    private void StartDashClientRpc()
    {
        if (!IsServer)
        {
            StartDash();
        }
    }

    private void EndDash()
    {
        movementComponent.SetCanMove(true);
        if (IsServer)
        {
            isDashing.Value = false;
        }
    }
}
