using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class WorldGenerator : NetworkBehaviour
{
    public static WorldGenerator Instance;
    public int sectorSize = 10;
    private Dictionary<Vector2Int, Sector> sectors = new Dictionary<Vector2Int, Sector>();

    [Header("Settings")]
    public GameObject objectPrefab; // Single prefab for all objects
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
            // Pre-pool asteroids and enemies with the single prefab
            int asteroidIndex = ObjectManager.Instance.GetObjectSOIndex(asteroidConfiguration);
            int enemyIndex = ObjectManager.Instance.GetObjectSOIndex(enemyConfiguration);

            ObjectPooler.Instance.CreatePool(objectPrefab, poolSizePerType, asteroidIndex);
            ObjectPooler.Instance.CreatePool(objectPrefab, poolSizePerType, enemyIndex);

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

    // Simplified to use one prefab for both asteroids and enemies
    private void GenerateObjectsInSector(Vector2Int sectorCoords, int objectCount, bool isEnemy, ObjectSO configuration)
    {
        int configIndex = ObjectManager.Instance.GetObjectSOIndex(configuration);

        for (int i = 0; i < objectCount; i++)
        {
            Vector3 position = GetRandomPositionInSector(sectorCoords, isEnemy ? enemySpawnDistance : 0);
            if (position != Vector3.zero)
            {
                // Request the object from the pool using the single prefab
                SpawnPooledObject(position, configIndex);
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

    private void SpawnPooledObject(Vector3 position, int configIndex)
    {
        if (IsServer)
        {
            GameObject obj = ObjectPooler.Instance.GetObjectFromPool(configIndex, position, Quaternion.identity, objectPrefab);
            NetworkObject netObj = obj.GetComponent<NetworkObject>();

            // Only spawn if it's not already spawned
            if (netObj != null && !netObj.IsSpawned)
            {
                netObj.Spawn(); // Ensure spawn only happens if not already spawned
            }

            ObjectSO configuration = ObjectManager.Instance.GetObjectSOByIndex(configIndex);
            var objectLoader = obj.GetComponent<ObjectLoader>();
            if (objectLoader != null)
            {
                objectLoader.SetConfigurationFromWorldGenerator(configuration, configIndex);
            }

            ObjectSpawnedClientRpc(position, configIndex);
        }
    }

    [ClientRpc]
    private void ObjectSpawnedClientRpc(Vector3 position, int configIndex)
    {
        if (!IsServer)
        {
            ObjectSO configuration = ObjectManager.Instance.GetObjectSOByIndex(configIndex);

            // Use the single prefab for both asteroids and enemies
            GameObject obj = ObjectPooler.Instance.GetObjectFromPool(configIndex, position, Quaternion.identity, objectPrefab);
            var loader = obj.GetComponent<ObjectLoader>();
            loader?.SetConfigurationFromWorldGenerator(configuration, configIndex);
        }
    }
}
