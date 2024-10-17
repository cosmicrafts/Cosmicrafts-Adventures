using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Unity.Netcode.Components;

public class BlinkComponent : NetworkBehaviour
{
    public float blinkDistance = 10f;
    public float blinkCooldown = 2f;

    private float blinkCooldownTimer;
    private NetworkTransform networkTransform; // Reference to the NetworkTransform component
    private Controls controls;

    public void ApplyConfiguration(ObjectSO config)
    {
        blinkDistance = config.blinkDistance;
        blinkCooldown = config.blinkCooldown;
    }

    private void Awake()
    {
        controls = new Controls();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Start()
    {
        networkTransform = GetComponent<NetworkTransform>(); // Get the NetworkTransform component
    }

    private void Update()
    {
        if (IsOwner && blinkCooldownTimer > 0f)
        {
            blinkCooldownTimer -= Time.deltaTime;
        }

        // Check if Blink is triggered (as Button action)
        if (IsOwner && controls.Blink.Blink.triggered && blinkCooldownTimer <= 0f)
        {
            PerformBlinkClient();
            blinkCooldownTimer = blinkCooldown;
        }
    }

    // This method handles the blink for the client and syncs it with the server
    private void PerformBlinkClient()
    {
        Vector3 blinkDirection = transform.up * blinkDistance;
        Vector3 newPosition = transform.position + blinkDirection;

        if (networkTransform != null)
        {
            // Use NetworkTransform's Teleport method for client-owned movement
            networkTransform.Teleport(newPosition, transform.rotation, transform.localScale);
        }

        // Notify the server about the blink action
        PerformBlinkServerRpc(newPosition);
    }

    [ServerRpc(RequireOwnership = true)]
    private void PerformBlinkServerRpc(Vector3 newPosition)
    {
        // Sync the new position with all clients, including the owner
        PerformBlinkClientRpc(newPosition);
    }

    [ClientRpc]
    private void PerformBlinkClientRpc(Vector3 newPosition)
    {
        // Apply the blink for all clients (including the owner to ensure sync)
        if (!IsOwner) // Skip if it's the owner since it already performed the blink
        {
            networkTransform.Teleport(newPosition, transform.rotation, transform.localScale);
        }
    }
}
