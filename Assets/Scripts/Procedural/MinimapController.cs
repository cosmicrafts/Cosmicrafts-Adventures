// Minimap Script
using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

public class MinimapController : MonoBehaviour
{
    public static MinimapController Instance; // Singleton for easy access
    public TextMeshProUGUI sectorNameText; // Reference to TMP UI to display the sector name
    private Transform playerTransform;
    private ulong localClientId;
    private Dictionary<Vector2Int, string> sectors = new Dictionary<Vector2Int, string>(); // Store sector data locally
    private Vector2Int currentSector;

    private void Awake()
    {
        Instance = this;
    }

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
           // Debug.Log($"Client {clientId} connected.");
            localClientId = clientId;

            // Get the player's NetworkObject associated with the local client
            var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
                Debug.Log($"Client {clientId} found player object and requesting all sectors.");

                // Request all sector information from the server
                RequestAllSectorsFromServer();
            }
            else
            {
                Debug.LogWarning($"Client {clientId} could not find player object.");
            }
        }
    }

    private void Update()
    {
        if (playerTransform != null)
        {
            // Continuously track the player's movement to detect sector changes
            Vector2Int newSector = GetCurrentSector(playerTransform.position);

            if (newSector != currentSector && sectors.ContainsKey(newSector))
            {
                currentSector = newSector;
                UpdateSectorUI(sectors[newSector]);
            }
        }
    }

    private void RequestAllSectorsFromServer()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log($"Client {localClientId} requesting all sector data from server.");
            WorldGenerator.Instance.GetAllSectorsServerRpc();
        }
    }

    public void AddSectorData(Vector2Int sectorCoords, string sectorName)
    {
        if (!sectors.ContainsKey(sectorCoords))
        {
            sectors.Add(sectorCoords, sectorName);
            //Debug.Log($"Client added sector data: {sectorName} at {sectorCoords}");

            // Update the UI if the player is in the newly received sector
            if (playerTransform != null)
            {
                Vector2Int newSector = GetCurrentSector(playerTransform.position);
                if (newSector == sectorCoords)
                {
                    currentSector = newSector;
                    UpdateSectorUI(sectorName);
                }
            }
        }
    }

    private Vector2Int GetCurrentSector(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / WorldGenerator.Instance.sectorSize),
            Mathf.FloorToInt(position.y / WorldGenerator.Instance.sectorSize)
        );
    }

    private void UpdateSectorUI(string sectorName)
    {
        //Debug.Log($"Updating minimap for client {localClientId} with sector: {sectorName}");
        // Update the sector name UI on the client
        sectorNameText.text = sectorName;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}