using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections.Generic;

public class ObjectLoader : NetworkBehaviour
{
    [SerializeField]
    public ObjectSO objectConfiguration;
    private NetworkVariable<int> selectedConfigIndex = new NetworkVariable<int>(0);
    private bool isConfigFromWorldGenerator = false;  // Flag to prevent overrides

    // This method is called by WorldGenerator to apply configuration from the server
    public void SetConfigurationFromWorldGenerator(ObjectSO configuration, int configIndex)
    {
        objectConfiguration = configuration;
        selectedConfigIndex.Value = configIndex;
        isConfigFromWorldGenerator = true;  // Mark that this was set by the WorldGenerator

        Debug.Log($"[ObjectLoader] Setting configuration from WorldGenerator with index {configIndex} and configuration {configuration.name}");
        ConfigurationApplier.ApplyConfiguration(gameObject, objectConfiguration); // Apply configuration using the decoupled class
    }

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
            ConfigurationApplier.ApplyConfiguration(gameObject, objectConfiguration); // Apply configuration using the decoupled class
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

    // ServerRpc for setting configuration index in real-time
    [ServerRpc(RequireOwnership = false)]
    public void SetConfigurationIndexServerRpc(int index, ulong clientId)
    {
        Debug.Log($"[ObjectLoader] Received configuration index {index} from client {clientId}");

        // Prevent overriding configuration set by WorldGenerator unless it's allowed (for players)
        if (!isConfigFromWorldGenerator || IsPlayerObject())
        {
            selectedConfigIndex.Value = index;  // Update the NetworkVariable

            if (index >= 0 && index < PlayerSelectorUI.Instance.availableConfigurations.Length)
            {
                objectConfiguration = PlayerSelectorUI.Instance.availableConfigurations[index];
                Debug.Log($"[ObjectLoader] Applying configuration index {index}: {objectConfiguration.name}");
                ConfigurationApplier.ApplyConfiguration(gameObject, objectConfiguration); // Apply configuration using the decoupled class
            }
        }
    }

    private bool IsPlayerObject()
    {
        return gameObject.CompareTag("Player");
    }

    private void OnConfigurationIndexChanged(int previousIndex, int newIndex)
    {
        if (!IsServer)
        {
            if (newIndex >= 0 && newIndex < PlayerSelectorUI.Instance.availableConfigurations.Length)
            {
                objectConfiguration = PlayerSelectorUI.Instance.availableConfigurations[newIndex];
                Debug.Log($"[ObjectLoader] OnConfigurationIndexChanged called for {gameObject.name} - Applying configuration: {objectConfiguration.name}");
                ConfigurationApplier.ApplyConfiguration(gameObject, objectConfiguration); // Apply configuration using the decoupled class
            }
        }
    }

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

    [ClientRpc]
    private void SendCurrentConfigurationClientRpc(int configIndex, ClientRpcParams clientRpcParams = default)
    {
        if (configIndex >= 0 && configIndex < PlayerSelectorUI.Instance.availableConfigurations.Length)
        {
            objectConfiguration = PlayerSelectorUI.Instance.availableConfigurations[configIndex];
            Debug.Log($"[ObjectLoader] SendCurrentConfigurationClientRpc called for {gameObject.name} - Applying configuration: {objectConfiguration.name}");
            ConfigurationApplier.ApplyConfiguration(gameObject, objectConfiguration); // Apply configuration using the decoupled class
        }
    }
}
