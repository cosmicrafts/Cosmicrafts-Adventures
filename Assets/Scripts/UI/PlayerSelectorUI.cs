using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerSelectorUI : MonoBehaviour
{
    public PlayerSO[] availableConfigurations;
    private PlayerSO selectedConfiguration;

    public Button[] selectionButtons;

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
        Debug.Log($"Player selected configuration: {selectedConfiguration.name}");

        // Send the selection to the server via ServerRpc
        SubmitSelectionServerRpc(index);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitSelectionServerRpc(int selectionIndex, ServerRpcParams serverRpcParams = default)
    {
        ulong clientId = serverRpcParams.Receive.SenderClientId;

        if (selectionIndex >= 0 && selectionIndex < availableConfigurations.Length)
        {
            PlayerSO chosenConfig = availableConfigurations[selectionIndex];
            PlayerSpawner.Instance.SetSelectedConfigurationServer(clientId, chosenConfig);
        }
        else
        {
            Debug.LogWarning($"Invalid selection index {selectionIndex} from client {clientId}");
        }
    }
}
