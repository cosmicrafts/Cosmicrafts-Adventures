using UnityEngine;
using UnityEngine.UI;

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

        // Store the selected configuration and inform the PlayerSpawner
        PlayerSpawner.Instance.SetSelectedConfiguration(selectedConfiguration);
    }
}
