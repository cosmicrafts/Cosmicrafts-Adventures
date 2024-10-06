using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System;

public class WorldGenerator : NetworkBehaviour
{
    public static WorldGenerator Instance; // Singleton instance for easy access
    public int sectorSize = 10; // Size of each sector
    private Dictionary<Vector2Int, Sector> sectors = new Dictionary<Vector2Int, Sector>(); // Dictionary to store generated sectors

    [Header("Asteroid Settings")]
    public GameObject asteroidPrefab;
    public int asteroidsPerSector = 5;
    public float noiseScale = 0.1f;
    public float asteroidDensity = 0.5f;

    [Header("Enemy Settings")]
    public GameObject enemyPrefab;        // Prefab for enemies
    public int enemiesPerSector = 2;      // Number of enemies per sector
    public float enemySpawnDistance = 10; // Distance from player before spawning enemies

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

        // Generate asteroids and enemies in the sector
        GenerateAsteroids(coordinates);
        GenerateEnemies(coordinates);
    }

    private void GenerateAsteroids(Vector2Int sectorCoords)
    {
        for (int i = 0; i < asteroidsPerSector; i++)
        {
            Vector3 asteroidPosition = GetRandomPositionInSector(sectorCoords);
            if (asteroidPosition != Vector3.zero)
            {
                SpawnAsteroid(asteroidPosition);
            }
        }
    }

    private void GenerateEnemies(Vector2Int sectorCoords)
    {
        for (int i = 0; i < enemiesPerSector; i++)
        {
            Vector3 enemyPosition = GetRandomPositionInSector(sectorCoords, enemySpawnDistance);
            if (enemyPosition != Vector3.zero)
            {
                SpawnEnemy(enemyPosition);
            }
        }
    }

    private Vector3 GetRandomPositionInSector(Vector2Int sectorCoords, float minDistance = 0)
    {
        float xOffset = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);
        float yOffset = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);

        Vector3 potentialPosition = new Vector3(
            sectorCoords.x * sectorSize + xOffset,
            sectorCoords.y * sectorSize + yOffset,
            0f
        );

        // Only return positions that are far enough away, if specified
        if (minDistance > 0 && Vector3.Distance(Vector3.zero, potentialPosition) < minDistance)
        {
            return Vector3.zero;
        }

        return potentialPosition;
    }

    private void SpawnAsteroid(Vector3 position)
    {
        GameObject asteroid = Instantiate(asteroidPrefab, position, Quaternion.identity);
        asteroid.layer = LayerMask.NameToLayer("Neutral");
        NetworkObject networkObject = asteroid.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.Spawn(); // Network-spawn the asteroid
        }

        var teamComponent = asteroid.GetComponent<TeamComponent>();
        if (teamComponent != null)
        {
            teamComponent.SetTeam(TeamComponent.TeamTag.Neutral);
        }
    }

    private void SpawnEnemy(Vector3 position)
    {
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        enemy.layer = LayerMask.NameToLayer("Enemy");
        NetworkObject networkObject = enemy.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.Spawn(); // Network-spawn the enemy
        }

        var teamComponent = enemy.GetComponent<TeamComponent>();
        if (teamComponent != null)
        {
            teamComponent.SetTeam(TeamComponent.TeamTag.Enemy);
        }
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
