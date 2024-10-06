using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System;

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

        Debug.Log($"Generated new sector: {newSector.sectorName}");
    }

    // ServerRpc to handle client requests for all sector data
    [ServerRpc(RequireOwnership = false)]
    public void GetAllSectorsServerRpc(ServerRpcParams serverRpcParams = default)
    {
        foreach (var sector in sectors)
        {
            SendSectorToClientRpc(sector.Key, sector.Value.sectorName, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }
                }
            });
        }
    }

    // Sends sector data to the specific client
    [ClientRpc]
    private void SendSectorToClientRpc(Vector2Int sectorCoords, string sectorName, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"ClientRpc: Sending sector info for sector '{sectorName}'");
        MinimapController.Instance?.AddSectorData(sectorCoords, sectorName);
    }
}
