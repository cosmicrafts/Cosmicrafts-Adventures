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
    public int poolSizePerType = 20; // Pool size for each object type

    // Assignable ObjectSO fields for custom configurations
    public ObjectSO asteroidConfiguration;
    public ObjectSO enemyConfiguration;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Pre-pool asteroids and enemies
            int asteroidIndex = ObjectManager.Instance.GetObjectSOIndex(asteroidConfiguration);
            int enemyIndex = ObjectManager.Instance.GetObjectSOIndex(enemyConfiguration);
            ObjectPooler.Instance.CreatePool(baseObjectPrefab, poolSizePerType, asteroidIndex);
            ObjectPooler.Instance.CreatePool(baseObjectPrefab, poolSizePerType, enemyIndex);

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

        GenerateObjectsInSector(coordinates, asteroidsPerSector, false, asteroidConfiguration);
        GenerateObjectsInSector(coordinates, enemiesPerSector, true, enemyConfiguration);
    }

    private void GenerateObjectsInSector(Vector2Int sectorCoords, int objectCount, bool isEnemy, ObjectSO configuration)
    {
        for (int i = 0; i < objectCount; i++)
        {
            Vector3 position = GetRandomPositionInSector(sectorCoords, isEnemy ? enemySpawnDistance : 0);
            if (position != Vector3.zero)
            {
                int configIndex = ObjectManager.Instance.GetObjectSOIndex(configuration);
                SpawnPooledObject(position, configIndex, isEnemy);
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

    private void SpawnPooledObject(Vector3 position, int configIndex, bool isEnemy)
    {
        if (IsServer)
        {
            GameObject obj = ObjectPooler.Instance.GetObjectFromPool(configIndex, position, Quaternion.identity);
            NetworkObject netObj = obj.GetComponent<NetworkObject>();

            // Only spawn if it's not already spawned
            if (netObj != null && !netObj.IsSpawned)
            {
                netObj.Spawn();
            }

            ObjectSO configuration = ObjectManager.Instance.GetObjectSOByIndex(configIndex);
            var objectLoader = obj.GetComponent<ObjectLoader>();
            if (objectLoader != null)
            {
                objectLoader.SetConfigurationFromWorldGenerator(configuration, configIndex);
            }

            ObjectSpawnedClientRpc(position, configIndex, isEnemy);
        }
    }


    [ClientRpc]
    private void ObjectSpawnedClientRpc(Vector3 position, int configIndex, bool isEnemy)
    {
        if (!IsServer)
        {
            ObjectSO configuration = ObjectManager.Instance.GetObjectSOByIndex(configIndex);
            GameObject obj = ObjectPooler.Instance.GetObjectFromPool(configIndex, position, Quaternion.identity);
            var loader = obj.GetComponent<ObjectLoader>();
            loader?.SetConfigurationFromWorldGenerator(configuration, configIndex);
        }
    }
}
