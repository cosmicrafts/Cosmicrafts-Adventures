using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerLoader : NetworkBehaviour
{
    [SerializeField]
    public PlayerSO playerConfiguration;

    // NetworkVariable to synchronize the selected configuration index, defaulting to 1
    private NetworkVariable<int> selectedConfigIndex = new NetworkVariable<int>(1);

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerLoader] OnNetworkSpawn called for {gameObject.name} - IsServer: {IsServer} - IsClient: {IsClient}");

        if (IsClient)
        {
            selectedConfigIndex.OnValueChanged += OnConfigurationIndexChanged;

            // Request server to send current configuration index if it's a new client joining
            if (!IsServer)
            {
                RequestCurrentConfigurationServerRpc();
            }
        }

        // Apply the initial configuration if available (for the server)
        if (IsServer && playerConfiguration != null)
        {
            Debug.Log($"[PlayerLoader] Applying default configuration: {playerConfiguration.name} for {gameObject.name}");
            ApplyConfiguration();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsClient)
        {
            selectedConfigIndex.OnValueChanged -= OnConfigurationIndexChanged;
        }
    }

    // Called when the selectedConfigIndex NetworkVariable changes
    private void OnConfigurationIndexChanged(int previousIndex, int newIndex)
    {
        if (newIndex >= 0 && newIndex < PlayerSelectorUI.Instance.availableConfigurations.Length)
        {
            playerConfiguration = PlayerSelectorUI.Instance.availableConfigurations[newIndex];
            Debug.Log($"[PlayerLoader] OnConfigurationIndexChanged called for {gameObject.name} - Applying configuration: {playerConfiguration.name}");
            ApplyConfiguration();
        }
    }

    public void SetPlayerConfiguration(PlayerSO config)
    {
        playerConfiguration = config;
        Debug.Log($"[PlayerLoader] SetPlayerConfiguration called for {gameObject.name} - New Configuration: {config.name}");
        ApplyConfiguration();
    }

    public void ApplyConfiguration()
    {
        if (playerConfiguration == null)
        {
            Debug.LogWarning($"{gameObject.name} [PlayerLoader] Player configuration is not assigned.");
            return;
        }

        // Apply configuration to each relevant component
        var movementComponent = GetComponent<MovementComponent>();
        if (movementComponent != null)
        {
            movementComponent.ApplyConfiguration(playerConfiguration);
        }

        var healthComponent = GetComponent<HealthComponent>();
        if (healthComponent != null)
        {
            healthComponent.ApplyConfiguration(playerConfiguration);
        }

        var shootingComponent = GetComponent<ShootingComponent>();
        if (shootingComponent != null)
        {
            shootingComponent.ApplyConfiguration(playerConfiguration);
        }

        // Set the player sprite from the PlayerSO
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && playerConfiguration.playerSprite != null)
        {
            spriteRenderer.sprite = playerConfiguration.playerSprite;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetConfigurationIndexServerRpc(int index, ulong clientId)
    {
        Debug.Log($"[PlayerLoader] Received configuration index {index} from client {clientId}");
        selectedConfigIndex.Value = index; // Update the NetworkVariable
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestCurrentConfigurationServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong requestingClientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[PlayerLoader] RequestCurrentConfigurationServerRpc called - Client ID: {requestingClientId}");

        // Send the current configuration index to the requesting client
        SendCurrentConfigurationClientRpc(selectedConfigIndex.Value, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new List<ulong> { requestingClientId }
            }
        });
    }

    [ClientRpc]
    private void SendCurrentConfigurationClientRpc(int configIndex, ClientRpcParams clientRpcParams = default)
    {
        if (configIndex >= 0 && configIndex < PlayerSelectorUI.Instance.availableConfigurations.Length)
        {
            playerConfiguration = PlayerSelectorUI.Instance.availableConfigurations[configIndex];
            Debug.Log($"[PlayerLoader] SendCurrentConfigurationClientRpc called for {gameObject.name} - Applying configuration: {playerConfiguration.name}");
            ApplyConfiguration();
        }
    }
}
