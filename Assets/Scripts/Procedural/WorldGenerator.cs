using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class WorldGenerator : NetworkBehaviour
{
    public static WorldGenerator Instance; // Singleton instance for easy access
    public int sectorSize = 10; // Size of each sector

    private Dictionary<Vector2Int, Sector> sectors = new Dictionary<Vector2Int, Sector>(); // Dictionary to store generated sectors

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

       // Debug.Log($"Generated new sector: {newSector.sectorName}");
    }

    public bool SectorExists(Vector2Int coordinates)
    {
        return sectors.ContainsKey(coordinates);
    }

    public Sector GetSector(Vector2Int coordinates)
    {
        if (sectors.TryGetValue(coordinates, out Sector sector))
        {
            return sector;
        }
        return null;
    }
}
