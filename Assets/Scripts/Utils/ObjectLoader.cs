using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections.Generic;

public class ObjectLoader : NetworkBehaviour
{
    private NetworkVariable<int> selectedConfigIndex = new NetworkVariable<int>(0);
    private bool isConfigFromWorldGenerator = false;  // Flag to prevent overrides

    public ObjectSO objectConfiguration;

    private int poolIndex;

    // Method to set the pool index when this object is spawned from the pool
    public void SetPoolIndex(int index)
    {
        poolIndex = index;
    }

    // Method to get the pool index when returning the object to the pool
    public int GetPoolIndex()
    {
        return poolIndex;
    }

    public int GetConfigIndex()
    {
        return selectedConfigIndex.Value;
    }

    private GameObject originalPrefab; // Store the original prefab reference

    // New method to set the original prefab reference
    public void SetOriginalPrefab(GameObject prefab)
    {
        originalPrefab = prefab;
    }

    public GameObject GetOriginalPrefab()
    {
        return originalPrefab;
    }

    public void SetConfigurationFromWorldGenerator(ObjectSO configuration, int configIndex)
    {
        objectConfiguration = configuration;
        selectedConfigIndex.Value = configIndex;
        isConfigFromWorldGenerator = true;

        //Debug.Log($"[ObjectLoader] Setting configuration from WorldGenerator with index {configIndex} and configuration {configuration.name}");
        ConfigurationApplier.ApplyConfiguration(gameObject, objectConfiguration);
    }

    public override void OnNetworkSpawn()
    {
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
            ConfigurationApplier.ApplyConfiguration(gameObject, objectConfiguration);
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

    [ServerRpc(RequireOwnership = false)]
    public void SetConfigurationIndexServerRpc(int index, ulong clientId)
    {
        if (!isConfigFromWorldGenerator || IsPlayerObject())
        {
            selectedConfigIndex.Value = index;

            ObjectSO config = ObjectManager.Instance.GetObjectSOByIndex(index);
            if (config != null)
            {
                objectConfiguration = config;
                //Debug.Log($"[ObjectLoader] Applying configuration index {index}: {config.name}");
                ConfigurationApplier.ApplyConfiguration(gameObject, objectConfiguration);
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
            ObjectSO config = ObjectManager.Instance.GetObjectSOByIndex(newIndex);
            if (config != null)
            {
                objectConfiguration = config;
                ConfigurationApplier.ApplyConfiguration(gameObject, objectConfiguration);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestCurrentConfigurationServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong requestingClientId = rpcParams.Receive.SenderClientId;
        SendCurrentConfigurationClientRpc(selectedConfigIndex.Value, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { requestingClientId } }
        });
    }

    [ClientRpc]
    private void SendCurrentConfigurationClientRpc(int configIndex, ClientRpcParams clientRpcParams = default)
    {
        ObjectSO config = ObjectManager.Instance.GetObjectSOByIndex(configIndex);
        if (config != null)
        {
            objectConfiguration = config;
            ConfigurationApplier.ApplyConfiguration(gameObject, objectConfiguration);
        }
    }
}
