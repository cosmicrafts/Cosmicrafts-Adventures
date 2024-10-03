using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerSpawner : NetworkBehaviour
{
    public PlayerSO[] availableConfigurations;
    private Dictionary<ulong, PlayerSO> playerSelections = new Dictionary<ulong, PlayerSO>();

    public static PlayerSpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    public void SetSelectedConfigurationServer(ulong clientId, PlayerSO chosenConfig)
    {
        playerSelections[clientId] = chosenConfig;
        Debug.Log($"[Server] Stored selected configuration for client {clientId}: {chosenConfig.name}");
    }

    private void OnClientConnected(ulong clientId)
    {
        // Assign the chosen configuration to the player once they connect
        if (playerSelections.TryGetValue(clientId, out PlayerSO chosenConfig))
        {
            GameObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;

            PlayerLoader playerLoader = playerObject.GetComponent<PlayerLoader>();
            if (playerLoader != null)
            {
                playerLoader.SetPlayerConfiguration(chosenConfig);
                playerLoader.ApplyConfiguration(); // Apply the configuration immediately after setting

                // Broadcast the configuration to all clients
                ApplyConfigurationClientRpc(playerObject.GetComponent<NetworkObject>().NetworkObjectId, chosenConfig.name);
                Debug.Log($"Assigned {chosenConfig.name} configuration to player {clientId}");
            }
        }
        else
        {
            Debug.LogWarning($"No configuration selected for player {clientId}. Using default.");
        }
    }

    [ClientRpc]
    private void ApplyConfigurationClientRpc(ulong networkObjectId, string configName)
    {
        PlayerSO chosenConfig = FindPlayerSOByName(configName);
        if (chosenConfig != null)
        {
            NetworkObject networkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
            if (networkObject != null)
            {
                PlayerLoader playerLoader = networkObject.GetComponent<PlayerLoader>();
                if (playerLoader != null)
                {
                    playerLoader.SetPlayerConfiguration(chosenConfig);
                    playerLoader.ApplyConfiguration();
                }
            }
        }
    }

    public PlayerSO FindPlayerSOByName(string name)
    {
        foreach (var config in availableConfigurations)
        {
            if (config.name == name)
            {
                return config;
            }
        }
        return null;
    }

    protected new void OnDestroy()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
        base.OnDestroy();
    }
}
