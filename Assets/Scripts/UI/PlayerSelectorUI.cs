using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerSelectorUI : MonoBehaviour
{
    public static PlayerSelectorUI Instance; // Singleton instance for easy access

    public PlayerSO[] availableConfigurations;
    private PlayerSO selectedConfiguration;

    public Button[] selectionButtons;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        for (int i = 0; i < selectionButtons.Length; i++)
        {
            int index = i;
            selectionButtons[i].onClick.AddListener(() => OnConfigurationSelected(index));
        }
    }

    private void OnConfigurationSelected(int index)
    {
        selectedConfiguration = availableConfigurations[index];
        Debug.Log($"[PlayerSelectorUI] Player selected configuration: {selectedConfiguration.name}");

        // Check the local client's ID before calling the ServerRpc
        if (NetworkManager.Singleton.LocalClient != null)
        {
            ulong clientId = NetworkManager.Singleton.LocalClient.ClientId;
            Debug.Log($"[PlayerSelectorUI] Local Client ID before ServerRpc: {clientId}");

            // Send the selection to the server via ServerRpc, passing the client ID explicitly
            SubmitSelectionServerRpc(index, clientId);
        }
        else
        {
            Debug.LogError("[PlayerSelectorUI] LocalClient is null, could not get Client ID.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitSelectionServerRpc(int selectionIndex, ulong clientId)
    {
        Debug.Log($"[PlayerSelectorUI] SubmitSelectionServerRpc called - Client ID: {clientId}, Selection Index: {selectionIndex}");

        if (selectionIndex >= 0 && selectionIndex < availableConfigurations.Length)
        {
            // Apply the chosen configuration directly to the player's object
            if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
            {
                GameObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
                PlayerLoader playerLoader = playerObject.GetComponent<PlayerLoader>();

                if (playerLoader != null)
                {
                    Debug.Log($"[PlayerSelectorUI] Applying configuration index {selectionIndex} to player {clientId}");
                    playerLoader.SetConfigurationIndexServerRpc(selectionIndex, clientId); // Call the ServerRpc on PlayerLoader
                }
                else
                {
                    Debug.LogWarning($"[PlayerSelectorUI] PlayerLoader not found on player object for client {clientId}");
                }
            }
            else
            {
                Debug.LogWarning($"[PlayerSelectorUI] Player object not found for client {clientId}");
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerSelectorUI] Invalid selection index {selectionIndex} from client {clientId}");
        }
    }
}
