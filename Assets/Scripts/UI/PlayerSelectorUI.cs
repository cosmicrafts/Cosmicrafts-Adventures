using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerSelectorUI : MonoBehaviour
{
    public static PlayerSelectorUI Instance;
    public Button[] selectionButtons;
    public ObjectSO[] buttonConfigurations;  // Add this to assign ObjectSO to each button via the inspector

    private ObjectSO selectedConfiguration;

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
        // Assign specific ObjectSO for this button from the inspector
        selectedConfiguration = buttonConfigurations[index];
        int configIndex = ObjectManager.Instance.GetObjectSOIndex(selectedConfiguration);  // Get the index of the selected ObjectSO
        Debug.Log($"[PlayerSelectorUI] Player selected configuration: {selectedConfiguration.name}");

        if (NetworkManager.Singleton.LocalClient != null)
        {
            ulong clientId = NetworkManager.Singleton.LocalClient.ClientId;
            SubmitSelectionServerRpc(configIndex, clientId);
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

        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
        {
            GameObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
            ObjectLoader objectLoader = playerObject.GetComponent<ObjectLoader>();

            if (objectLoader != null)
            {
                objectLoader.SetConfigurationIndexServerRpc(selectionIndex, clientId);
            }
            else
            {
                Debug.LogWarning($"[PlayerSelectorUI] ObjectLoader not found on player object for client {clientId}");
            }
        }
    }
}
