using UnityEngine;
using Unity.Netcode;

public class PlayerLoader : NetworkBehaviour
{
    [SerializeField]
    public PlayerSO playerConfiguration;

    public override void OnNetworkSpawn()
    {
        // Do not automatically apply the default configuration from the prefab
        if (IsOwner && playerConfiguration != null)
        {
            ApplyConfiguration();
        }
    }

    public void SetPlayerConfiguration(PlayerSO config)
    {
        playerConfiguration = config;
    }

    public void ApplyConfiguration()
    {
        if (playerConfiguration == null)
        {
            Debug.LogWarning($"{gameObject.name} [PlayerLoader] Player configuration is not assigned.");
            return;
        }

        Debug.Log($"Applying configuration: {playerConfiguration.name} to {gameObject.name}");

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
            Debug.Log($"Set sprite for {gameObject.name} to {playerConfiguration.playerSprite.name}");
        }
    }
}
