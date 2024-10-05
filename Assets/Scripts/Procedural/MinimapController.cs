using UnityEngine;
using TMPro;
using Unity.Netcode;

public class MinimapController : MonoBehaviour
{
    public TextMeshProUGUI sectorNameText; // Reference to TMP UI to display the sector name
    private Transform playerTransform;

    private void Start()
    {
        // Find the player transform after spawning
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            // Get the player's NetworkObject associated with the local client
            var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            return;
        }

        // Calculate the current sector based on the player's position
        Vector2Int currentSector = new Vector2Int(
            Mathf.FloorToInt(playerTransform.position.x / WorldGenerator.Instance.sectorSize),
            Mathf.FloorToInt(playerTransform.position.y / WorldGenerator.Instance.sectorSize)
        );

        // Get the current sector data from the WorldGenerator
        Sector currentSectorData = WorldGenerator.Instance.GetSector(currentSector);
        if (currentSectorData != null)
        {
            sectorNameText.text = currentSectorData.sectorName;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}
