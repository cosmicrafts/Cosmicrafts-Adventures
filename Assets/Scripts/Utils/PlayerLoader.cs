using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerLoader : NetworkBehaviour
{
    [SerializeField]
    public PlayerSO playerConfiguration;

    private NetworkVariable<int> selectedConfigIndex = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerLoader] OnNetworkSpawn called for {gameObject.name} - IsServer: {IsServer} - IsClient: {IsClient}");

        if (IsClient)
        {
            selectedConfigIndex.OnValueChanged += OnConfigurationIndexChanged;

            if (!IsServer)
            {
                RequestCurrentConfigurationServerRpc();
            }
        }

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

        var movementComponent = GetComponent<MovementComponent>();
        if (movementComponent != null)
        {
            movementComponent.enabled = playerConfiguration.hasMovement;
            if (movementComponent.enabled)
            {
                movementComponent.ApplyConfiguration(playerConfiguration);
            }
        }

        var healthComponent = GetComponent<HealthComponent>();
        if (healthComponent != null)
        {
            healthComponent.ApplyConfiguration(playerConfiguration);
        }

        var shootingComponent = GetComponent<ShootingComponent>();
        if (shootingComponent != null)
        {
            shootingComponent.enabled = playerConfiguration.hasShooting;
            if (shootingComponent.enabled)
            {
                shootingComponent.ApplyConfiguration(playerConfiguration);
            }
        }

        var dashComponent = GetComponent<DashComponent>();
        if (dashComponent != null)
        {
            dashComponent.enabled = playerConfiguration.hasDashAbility;
            if (dashComponent.enabled)
            {
                dashComponent.ApplyConfiguration(playerConfiguration);
            }
        }

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
        selectedConfigIndex.Value = index;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestCurrentConfigurationServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong requestingClientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[PlayerLoader] RequestCurrentConfigurationServerRpc called - Client ID: {requestingClientId}");

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
