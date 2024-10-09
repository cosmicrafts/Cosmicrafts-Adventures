using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class WorldGenerator : NetworkBehaviour
{
    public static WorldGenerator Instance;
    public int sectorSize = 10;
    private Dictionary<Vector2Int, Sector> sectors = new Dictionary<Vector2Int, Sector>();

    [Header("Settings")]
    public GameObject baseObjectPrefab;
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

        GenerateObjectsInSector(coordinates, asteroidsPerSector, false);
        GenerateObjectsInSector(coordinates, enemiesPerSector, true);
    }

    private void GenerateObjectsInSector(Vector2Int sectorCoords, int objectCount, bool isEnemy)
    {
        for (int i = 0; i < objectCount; i++)
        {
            Vector3 position = GetRandomPositionInSector(sectorCoords, isEnemy ? enemySpawnDistance : 0);
            if (position != Vector3.zero)
            {
                // Get index from ObjectManager
                int configIndex = UnityEngine.Random.Range(0, ObjectManager.Instance.allConfigurations.Length);

                // Spawn with the correct index
                SpawnObject(position, configIndex, isEnemy);
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

    private void SpawnObject(Vector3 position, int configIndex, bool isEnemy)
    {
        if (IsServer)
        {
            GameObject obj = Instantiate(baseObjectPrefab, position, Quaternion.identity);
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            ObjectSO configuration = ObjectManager.Instance.GetObjectSOByIndex(configIndex);
            var objectLoader = obj.GetComponent<ObjectLoader>();
            objectLoader?.SetConfigurationFromWorldGenerator(configuration, configIndex);

            ObjectSpawnedClientRpc(position, configIndex, isEnemy);
        }
    }

    [ClientRpc]
    private void ObjectSpawnedClientRpc(Vector3 position, int configIndex, bool isEnemy)
    {
        if (!IsServer)
        {
            ObjectSO configuration = ObjectManager.Instance.GetObjectSOByIndex(configIndex);
            GameObject obj = Instantiate(baseObjectPrefab, position, Quaternion.identity);
            var loader = obj.GetComponent<ObjectLoader>();
            loader?.SetConfigurationFromWorldGenerator(configuration, configIndex);
        }
    }
    
    [ClientRpc]
    private void SendSectorToClientRpc(Vector2Int sectorCoords, string sectorName, ulong clientId)
    {
        // Update the minimap or any other UI with sector data
        MinimapController.Instance?.AddSectorData(sectorCoords, sectorName);
    }

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
}
