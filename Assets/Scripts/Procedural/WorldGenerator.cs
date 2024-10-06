using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System;

public class WorldGenerator : NetworkBehaviour
{
    public static WorldGenerator Instance; // Singleton instance for easy access
    public int sectorSize = 10; // Size of each sector
    private Dictionary<Vector2Int, Sector> sectors = new Dictionary<Vector2Int, Sector>(); // Dictionary to store generated sectors

    // Event to notify when a player enters a new sector
    public event Action<ulong, Vector2Int> OnPlayerEnteredNewSector;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GenerateInitialSectors();
        }
    }

    // Generates initial 9 sectors (3x3 grid)
    private void GenerateInitialSectors()
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                GenerateSector(new Vector2Int(x, y));
            }
        }
    }

    // Generates a new sector at specified coordinates (server-only)
    public void GenerateSector(Vector2Int coordinates)
    {
        if (sectors.ContainsKey(coordinates))
            return;

        // Create a new sector from the template (no visual representation on the server)
        Sector newSector = ScriptableObject.CreateInstance<Sector>();
        newSector.sectorName = $"Sector ({coordinates.x}, {coordinates.y})";
        newSector.isGenerated = true;

        // Store the sector in the dictionary
        sectors.Add(coordinates, newSector);

        Debug.Log($"Generated new sector: {newSector.sectorName}");
    }

    public bool SectorExists(Vector2Int coordinates)
    {
        return sectors.ContainsKey(coordinates);
    }

    public Sector GetSector(Vector2Int coordinates)
    {
        sectors.TryGetValue(coordinates, out Sector sector);
        return sector;
    }

    // Call this method to track player movement and detect sector changes
    public void TrackPlayerMovement(ulong clientId, Vector3 playerPosition)
    {
        Vector2Int newSector = new Vector2Int(
            Mathf.FloorToInt(playerPosition.x / sectorSize),
            Mathf.FloorToInt(playerPosition.y / sectorSize)
        );

        if (SectorExists(newSector))
        {
            // Fire the event if the player has entered a new sector
            OnPlayerEnteredNewSector?.Invoke(clientId, newSector);
        }
    }
}
