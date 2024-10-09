using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections.Generic;

public class ObjectLoader : NetworkBehaviour
{
    [SerializeField]
    public ObjectSO objectConfiguration;
    private NetworkVariable<int> selectedConfigIndex = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[ObjectLoader] OnNetworkSpawn called for {gameObject.name} - IsServer: {IsServer} - IsClient: {IsClient}");

        if (IsClient)
        {
            selectedConfigIndex.OnValueChanged += OnConfigurationIndexChanged;

            if (!IsServer)
            {
                RequestCurrentConfigurationServerRpc();
            }
        }

        if (IsServer && objectConfiguration != null)
        {
            Debug.Log($"[ObjectLoader] Applying default configuration: {objectConfiguration.name} for {gameObject.name}");
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

    private void ChangeOwnershipToClient()
    {
        if (IsServer && NetworkManager.Singleton.ConnectedClientsList.Count > 0)
        {
            ulong clientId = NetworkManager.Singleton.LocalClientId;  // Modify based on the desired owner
            NetworkObject.ChangeOwnership(clientId);
            Debug.Log($"Ownership of {gameObject.name} transferred to client with ID: {clientId}");
        }
    }

    // Handles configuration changes when the index changes
    private void OnConfigurationIndexChanged(int previousIndex, int newIndex)
    {
        if (newIndex >= 0 && newIndex < PlayerSelectorUI.Instance.availableConfigurations.Length)
        {
            objectConfiguration = PlayerSelectorUI.Instance.availableConfigurations[newIndex];
            Debug.Log($"[ObjectLoader] OnConfigurationIndexChanged called for {gameObject.name} - Applying configuration: {objectConfiguration.name}");
            ApplyConfiguration();
        }
    }

    public void ApplyConfiguration()
    {
        if (objectConfiguration == null)
        {
            Debug.LogWarning($"{gameObject.name} [ObjectLoader] Configuration is not assigned.");
            return;
        }

        // Apply SpriteRenderer configuration
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && objectConfiguration.objectSprite != null)
        {
            spriteRenderer.sprite = objectConfiguration.objectSprite;
        }

        // Apply CircleCollider2D configuration
        var collider = GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            collider.radius = objectConfiguration.colliderRadius;
            collider.isTrigger = objectConfiguration.isTrigger;
        }

        // Apply Rigidbody2D configuration
        var rigidbody2D = GetComponent<Rigidbody2D>();
        if (rigidbody2D != null)
        {
            rigidbody2D.bodyType = objectConfiguration.isKinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
            rigidbody2D.sharedMaterial = objectConfiguration.physicsMaterial;
        }

        // Apply MovementComponent configuration
        var movementComponent = GetComponent<MovementComponent>();
        if (movementComponent != null)
        {
            movementComponent.enabled = objectConfiguration.hasMovement;
            if (movementComponent.enabled)
            {
                movementComponent.ApplyConfiguration(objectConfiguration);
            }
        }

        // Apply RotationComponent configuration
        var rotationComponent = GetComponent<RotationComponent>();
        if (rotationComponent != null)
        {
            rotationComponent.enabled = objectConfiguration.hasRotation;
            if (rotationComponent.enabled)
            {
                rotationComponent.ApplyConfiguration(objectConfiguration);
            }
        }

        // Apply ShootingComponent configuration
        var shootingComponent = GetComponent<ShootingComponent>();
        if (shootingComponent != null)
        {
            shootingComponent.enabled = objectConfiguration.hasShooting;
            if (shootingComponent.enabled)
            {
                shootingComponent.ApplyConfiguration(objectConfiguration);
            }
        }

        // Apply HealthComponent configuration
        var healthComponent = GetComponent<HealthComponent>();
        if (healthComponent != null)
        {
            healthComponent.ApplyConfiguration(objectConfiguration);
        }

        // Apply TeamComponent configuration
        var teamComponent = GetComponent<TeamComponent>();
        if (teamComponent != null)
        {
            teamComponent.ApplyConfiguration(objectConfiguration);
        }

        // Apply optional abilities (Dash/Blink)
        var dashComponent = GetComponent<DashComponent>();
        if (dashComponent != null)
        {
            dashComponent.enabled = objectConfiguration.hasDashAbility;
            if (dashComponent.enabled)
            {
                dashComponent.ApplyConfiguration(objectConfiguration);
            }
        }

        var blinkComponent = GetComponent<BlinkComponent>();
        if (blinkComponent != null)
        {
            blinkComponent.enabled = objectConfiguration.hasBlinkAbility;
            if (blinkComponent.enabled)
            {
                blinkComponent.ApplyConfiguration(objectConfiguration);
            }
        }
    }

    // ServerRpc for setting configuration index
    [ServerRpc(RequireOwnership = false)]
    public void SetConfigurationIndexServerRpc(int index, ulong clientId)
    {
        Debug.Log($"[ObjectLoader] Received configuration index {index} from client {clientId}");
        selectedConfigIndex.Value = index;
    }

    // ServerRpc to request current configuration from the server
    [ServerRpc(RequireOwnership = false)]
    private void RequestCurrentConfigurationServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong requestingClientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[ObjectLoader] RequestCurrentConfigurationServerRpc called - Client ID: {requestingClientId}");

        SendCurrentConfigurationClientRpc(selectedConfigIndex.Value, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new List<ulong> { requestingClientId }
            }
        });
    }

    // ClientRpc to send current configuration index to the client
    [ClientRpc]
    private void SendCurrentConfigurationClientRpc(int configIndex, ClientRpcParams clientRpcParams = default)
    {
        if (configIndex >= 0 && configIndex < PlayerSelectorUI.Instance.availableConfigurations.Length)
        {
            objectConfiguration = PlayerSelectorUI.Instance.availableConfigurations[configIndex];
            Debug.Log($"[ObjectLoader] SendCurrentConfigurationClientRpc called for {gameObject.name} - Applying configuration: {objectConfiguration.name}");
            ApplyConfiguration();
        }
    }
}
