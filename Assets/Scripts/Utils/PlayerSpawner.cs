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

    public void SetSelectedConfiguration(PlayerSO config)
    {
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        playerSelections[clientId] = config; // Simplified to always set the chosen config
        Debug.Log($"Stored selected configuration for client {clientId}: {config.name}");
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

                Debug.Log($"Assigned {chosenConfig.name} configuration to player {clientId}");
            }
        }
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
