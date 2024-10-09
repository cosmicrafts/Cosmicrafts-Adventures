using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class WorldGenerator : NetworkBehaviour
{
    public static WorldGenerator Instance;
    public int sectorSize = 10;
    private Dictionary<Vector2Int, Sector> sectors = new Dictionary<Vector2Int, Sector>();

    [Header("Asteroid and Enemy Settings")]
    public GameObject baseObjectPrefab;
    public ObjectSO[] asteroidConfigurations;
    public ObjectSO[] enemyConfigurations;
    public int asteroidsPerSector = 5;
    public int enemiesPerSector = 2;
    public float enemySpawnDistance = 10;

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

    private void GenerateSector(Vector2Int coordinates)
    {
        if (sectors.ContainsKey(coordinates)) return;

        Sector newSector = ScriptableObject.CreateInstance<Sector>();
        newSector.sectorName = $"Sector ({coordinates.x}, {coordinates.y})";
        sectors.Add(coordinates, newSector);

        GenerateObjectsInSector(coordinates, asteroidsPerSector, asteroidConfigurations, false);
        GenerateObjectsInSector(coordinates, enemiesPerSector, enemyConfigurations, true);
    }

    private void GenerateObjectsInSector(Vector2Int sectorCoords, int objectCount, ObjectSO[] configurations, bool isEnemy)
    {
        for (int i = 0; i < objectCount; i++)
        {
            Vector3 position = GetRandomPositionInSector(sectorCoords, isEnemy ? enemySpawnDistance : 0);
            if (position != Vector3.zero)
            {
                int configIndex = UnityEngine.Random.Range(0, configurations.Length);
                SpawnObject(position, configurations[configIndex], true, configIndex);
            }
        }
    }

    private Vector3 GetRandomPositionInSector(Vector2Int sectorCoords, float minDistance = 0)
    {
        float xOffset = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);
        float yOffset = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);
        Vector3 position = new Vector3(sectorCoords.x * sectorSize + xOffset, sectorCoords.y * sectorSize + yOffset, 0f);

        return (minDistance > 0 && Vector3.Distance(Vector3.zero, position) < minDistance) ? Vector3.zero : position;
    }

    private void SpawnObject(Vector3 position, ObjectSO configuration, bool isServerSpawn, int configIndex)
    {
        if (IsServer)
        {
            GameObject obj = Instantiate(baseObjectPrefab, position, Quaternion.identity);
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            var objectLoader = obj.GetComponent<ObjectLoader>();
            objectLoader?.SetConfigurationFromWorldGenerator(configuration, configIndex);

            if (isServerSpawn)
            {
                ObjectSpawnedClientRpc(position, configIndex, configuration.teamTag == TeamComponent.TeamTag.Enemy);
            }
        }
    }

    [ClientRpc]
    private void ObjectSpawnedClientRpc(Vector3 position, int configIndex, bool isEnemy)
    {
        if (!IsServer)
        {
            ObjectSO config = isEnemy ? enemyConfigurations[configIndex] : asteroidConfigurations[configIndex];
            GameObject obj = Instantiate(baseObjectPrefab, position, Quaternion.identity);
            var loader = obj.GetComponent<ObjectLoader>();
            loader?.SetConfigurationFromWorldGenerator(config, configIndex);
        }
    }

    // Add this method to handle sector data requests from clients
    [ServerRpc(RequireOwnership = false)]
    public void GetAllSectorsServerRpc(ServerRpcParams rpcParams = default)
    {
        foreach (var sector in sectors)
        {
            var sectorCoords = sector.Key;
            var sectorName = sector.Value.sectorName;

            // Send the sector data to the requesting client
            SendSectorToClientRpc(sectorCoords, sectorName, rpcParams.Receive.SenderClientId);
        }
    }

    [ClientRpc]
    private void SendSectorToClientRpc(Vector2Int sectorCoords, string sectorName, ulong clientId)
    {
        // Update the minimap or any other UI with sector data
        MinimapController.Instance?.AddSectorData(sectorCoords, sectorName);
    }
}
