using UnityEngine;
using Unity.Netcode;

public class PlayerTracker : MonoBehaviour
{
    public Transform playerTransform;
    private Vector2Int currentSector;

    private void Start()
    {
        Debug.Log("PlayerTracker started.");

        // Find the player transform after spawning
        if (NetworkManager.Singleton != null)
        {
            Debug.Log("NetworkManager found, subscribing to OnClientConnectedCallback.");
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        else
        {
            Debug.LogError("NetworkManager is null. PlayerTracker cannot subscribe to OnClientConnectedCallback.");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"OnClientConnected called with clientId: {clientId}");

        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            Debug.Log("Local client connected, attempting to find the player object.");

            var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
                currentSector = GetCurrentSector();
                Debug.Log($"Player object found. Starting at sector: {currentSector}");
            }
            else
            {
                Debug.LogError("Failed to find the local player object.");
            }
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("PlayerTransform is null, waiting for player object to be assigned.");
            return;
        }

        Vector2Int newSector = GetCurrentSector();

        if (newSector != currentSector)
        {
            currentSector = newSector;
            OnSectorChanged(newSector);
        }
    }

    // Determines the current sector based on the player's position
    private Vector2Int GetCurrentSector()
    {
        if (WorldGenerator.Instance == null)
        {
            Debug.LogError("WorldGenerator instance is null. Cannot calculate the current sector.");
            return Vector2Int.zero;
        }

        int x = Mathf.FloorToInt(playerTransform.position.x / WorldGenerator.Instance.sectorSize);
        int y = Mathf.FloorToInt(playerTransform.position.y / WorldGenerator.Instance.sectorSize);
        return new Vector2Int(x, y);
    }

    // Called when the player enters a new sector
    private void OnSectorChanged(Vector2Int newSector)
    {
       // Debug.Log($"Player entered new sector: ({newSector.x}, {newSector.y})");

        // Generate new sectors around the current one if not already generated
        for (int x = newSector.x - 1; x <= newSector.x + 1; x++)
        {
            for (int y = newSector.y - 1; y <= newSector.y + 1; y++)
            {
                Vector2Int sectorCoords = new Vector2Int(x, y);
                if (!WorldGenerator.Instance.SectorExists(sectorCoords))
                {
                    Debug.Log($"Generating sector at: ({sectorCoords.x}, {sectorCoords.y})");
                    WorldGenerator.Instance.GenerateSector(sectorCoords);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            Debug.Log("Unsubscribing from OnClientConnectedCallback.");
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}
