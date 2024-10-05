using UnityEngine;
using Unity.Netcode;

public class PlayerLoader : NetworkBehaviour
{
    [SerializeField]
    public PlayerSO playerConfiguration;

    // NetworkVariable to synchronize the selected configuration index
    private NetworkVariable<int> selectedConfigIndex = new NetworkVariable<int>(-1);

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerLoader] OnNetworkSpawn called for {gameObject.name} - IsServer: {IsServer} - IsClient: {IsClient}");

        // If it's a client, subscribe to the NetworkVariable change
        if (IsClient)
        {
            selectedConfigIndex.OnValueChanged += OnConfigurationIndexChanged;
        }

        // Apply the initial configuration if available
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
}
