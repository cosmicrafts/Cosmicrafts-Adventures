using UnityEngine;
using Unity.Netcode;

public class PlayerLoader : NetworkBehaviour
{
    [SerializeField]
    public PlayerSO playerConfiguration;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerLoader] OnNetworkSpawn called for {gameObject.name} - IsServer: {IsServer} - IsClient: {IsClient}");
        if (playerConfiguration != null)
        {
            Debug.Log($"[PlayerLoader] Applying default configuration: {playerConfiguration.name} for {gameObject.name}");
            ApplyConfiguration();
        }
    }

    public void SetPlayerConfiguration(PlayerSO config)
    {
        playerConfiguration = config;
        Debug.Log($"[PlayerLoader] SetPlayerConfiguration called for {gameObject.name} - New Configuration: {config.name}");
        ApplyConfiguration(); // Apply the configuration as soon as it's set
    }

    public void ApplyConfiguration()
    {
        if (playerConfiguration == null)
        {
            Debug.LogWarning($"{gameObject.name} [PlayerLoader] Player configuration is not assigned.");
            return;
        }

        Debug.Log($"[PlayerLoader] Applying configuration: {playerConfiguration.name} to {gameObject.name}");

        // Apply configuration to each relevant component
        var movementComponent = GetComponent<MovementComponent>();
        if (movementComponent != null)
        {
            Debug.Log($"[PlayerLoader] Applying movement configuration for {gameObject.name}");
            movementComponent.ApplyConfiguration(playerConfiguration);
        }

        var healthComponent = GetComponent<HealthComponent>();
        if (healthComponent != null)
        {
            Debug.Log($"[PlayerLoader] Applying health configuration for {gameObject.name}");
            healthComponent.ApplyConfiguration(playerConfiguration);
        }

        var shootingComponent = GetComponent<ShootingComponent>();
        if (shootingComponent != null)
        {
            Debug.Log($"[PlayerLoader] Applying shooting configuration for {gameObject.name}");
            shootingComponent.ApplyConfiguration(playerConfiguration);
        }

        // Set the player sprite from the PlayerSO
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && playerConfiguration.playerSprite != null)
        {
            spriteRenderer.sprite = playerConfiguration.playerSprite;
            Debug.Log($"[PlayerLoader] Set sprite for {gameObject.name} to {playerConfiguration.playerSprite.name}");
        }
    }
}
