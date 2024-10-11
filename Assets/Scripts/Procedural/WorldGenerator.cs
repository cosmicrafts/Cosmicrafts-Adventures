using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

public class WorldGenerator : NetworkBehaviour
{
    public static WorldGenerator Instance;
    public int sectorSize = 10;
    private Dictionary<Vector2Int, Sector> sectors = new Dictionary<Vector2Int, Sector>();

    [Header("Settings")]
    public GameObject asteroidPrefab;
    public GameObject enemyPrefab;
    public int asteroidsPerSector = 5;
    public int enemiesPerSector = 2;
    public float enemySpawnDistance = 10;
    public float noiseScale = 5f;

    [Header("Regeneration")]
    public float regenerationInterval = 10f;
    public int objectsPerFrame = 4; // Number of objects to spawn per frame

    // Assignable ObjectSO fields for custom configurations
    public ObjectSO asteroidConfiguration;
    public ObjectSO enemyConfiguration;

    private int randomSeed;

    private void Awake()
    {
        Instance = this;
        randomSeed = Random.Range(0, 10000); // Initialize random seed once
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Server-only object generation
            GenerateInitialSectors();
            StartCoroutine(RegenerateObjectsCoroutine());
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

        float[,] noiseGrid = PrecalculatePerlinNoiseGrid(coordinates);
        GenerateObjectsInSector(coordinates, asteroidsPerSector, false, asteroidConfiguration, noiseGrid);
        GenerateObjectsInSector(coordinates, enemiesPerSector, true, enemyConfiguration, noiseGrid);
    }

    private float[,] PrecalculatePerlinNoiseGrid(Vector2Int sectorCoords)
    {
        int gridResolution = Mathf.Max(asteroidsPerSector, enemiesPerSector);
        float[,] noiseGrid = new float[gridResolution, 2];

        for (int i = 0; i < gridResolution; i++)
        {
            float xOffset = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);
            float yOffset = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);

            float xNoise = Mathf.PerlinNoise((sectorCoords.x + xOffset + randomSeed) / noiseScale, (sectorCoords.y + yOffset + randomSeed) / noiseScale);
            float yNoise = Mathf.PerlinNoise((sectorCoords.y + yOffset + randomSeed) / noiseScale, (sectorCoords.x + xOffset + randomSeed) / noiseScale);

            noiseGrid[i, 0] = xNoise;
            noiseGrid[i, 1] = yNoise;
        }

        return noiseGrid;
    }

    private void GenerateObjectsInSector(Vector2Int sectorCoords, int objectCount, bool isEnemy, ObjectSO configuration, float[,] noiseGrid)
    {
        GameObject prefab = isEnemy ? enemyPrefab : asteroidPrefab;
        if (prefab == null)
        {
          //  Debug.LogError("[WorldGenerator] Prefab is null! Ensure the prefab references are set correctly.");
            return;
        }

        for (int i = 0; i < objectCount; i++)
        {
            Vector3 position = GetPositionFromNoiseGrid(sectorCoords, isEnemy ? enemySpawnDistance : 0, noiseGrid, i);
            if (position != Vector3.zero)
            {
                SpawnObject(position, prefab, configuration);
            }
        }
    }

    private Vector3 GetPositionFromNoiseGrid(Vector2Int sectorCoords, float minDistance, float[,] noiseGrid, int index)
    {
        float xNoise = noiseGrid[index, 0];
        float yNoise = noiseGrid[index, 1];

        Vector3 position = new Vector3(
            sectorCoords.x * sectorSize + (xNoise * sectorSize),
            sectorCoords.y * sectorSize + (yNoise * sectorSize),
            0f
        );

        return (minDistance > 0 && Vector3.Distance(Vector3.zero, position) < minDistance) ? Vector3.zero : position;
    }

private void SpawnObject(Vector3 position, GameObject prefab, ObjectSO configuration)
{
    if (!IsServer) return; // Ensure only server executes this code

    // Get object from the NetworkObjectPool
    NetworkObject netObj = NetworkObjectPool.Singleton.GetNetworkObject(prefab, position, Quaternion.identity);

    if (netObj != null)
    {
        // Only spawn the object if it is not already spawned
        if (!netObj.IsSpawned)
        {
            netObj.Spawn(true); // Ensure the object is spawned by the server and synchronized to clients
            Debug.Log($"[WorldGenerator] {prefab.name} has been spawned on the network.");
        }

        // Handle object configuration and loader
        var objectLoader = netObj.GetComponent<ObjectLoader>();
        if (objectLoader != null)
        {
            // Apply the configuration using ObjectLoader, which sets up the ObjectSO configuration
            objectLoader.SetConfigurationFromWorldGenerator(configuration, ObjectManager.Instance.GetObjectSOIndex(configuration));

            // Set the original prefab for proper pooling behavior when returning objects
            objectLoader.SetOriginalPrefab(prefab); // This ensures the object can be returned to the correct pool
        }

        // Inform clients about the spawned object and set isActive to true since it's being spawned
        InformClientAboutSpawnedObjectClientRpc(position, ObjectManager.Instance.GetObjectSOIndex(configuration), true);
    }
    else
    {
        Debug.LogError($"[WorldGenerator] Failed to get {prefab.name} from the pool.");
    }
}

    private Dictionary<ulong, bool> objectStates = new Dictionary<ulong, bool>(); // Track active/inactive state of each object


[ClientRpc]
public void InformClientAboutSpawnedObjectClientRpc(Vector3 position, int configIndex, bool isActive)
{
    if (IsServer) return; // This logic is for clients only

    ObjectSO configuration = ObjectManager.Instance.GetObjectSOByIndex(configIndex);

    // Find the object using the pooling mechanism or in the scene
    GameObject obj = FindInactiveObject();

    if (obj != null)
    {
        if (isActive)
        {
            // Activate the object, set its position and apply configuration
            obj.transform.position = position;
            obj.SetActive(true);

            var loader = obj.GetComponent<ObjectLoader>();
            loader?.SetConfigurationFromWorldGenerator(configuration, configIndex);
        }
        else
        {
            // Deactivate the object if it's being destroyed
            obj.SetActive(false);
            obj.tag = "Inactive";  // Ensure it's properly tagged as inactive
        }
    }
    else
    {
        Debug.LogError($"[WorldGenerator] Failed to find an object for activation/deactivation.");
    }
}



private GameObject FindInactiveObject()
{
    // This method searches for an inactive object of the given prefab in the scene or pool
    GameObject[] objects = GameObject.FindGameObjectsWithTag("Inactive");
    foreach (GameObject obj in objects)
    {
        if (!obj.activeInHierarchy)
        {
            return obj; // Return the first inactive object found
        }
    }
    return null;
}


private void ReactivateObjectsInSector(Vector2Int sectorCoords, int objectCount, bool isEnemy, ObjectSO configuration)
{
    GameObject prefab = isEnemy ? enemyPrefab : asteroidPrefab;
    if (prefab == null) return;

    for (int i = 0; i < objectCount; i++)
    {
        // Fetch and reactivate objects directly from the pool
        NetworkObject netObj = NetworkObjectPool.Singleton.GetNetworkObject(prefab, GetRandomPositionInSector(sectorCoords), Quaternion.identity);
        if (netObj != null && !netObj.IsSpawned)
        {
            netObj.Spawn(true);
            var objectLoader = netObj.GetComponent<ObjectLoader>();
            if (objectLoader != null)
            {
                objectLoader.SetConfigurationFromWorldGenerator(configuration, ObjectManager.Instance.GetObjectSOIndex(configuration));
            }
        }
    }
}

private IEnumerator RegenerateObjectsCoroutine()
{
    while (true)
    {
        yield return new WaitForSeconds(regenerationInterval);

        // Reactivate objects in each sector by reactivating from the pool
        foreach (var sector in sectors.Keys)
        {
            if (gameObject.activeInHierarchy) // Ensure the WorldGenerator is active
            {
                ReactivateObjectsInSector(sector, asteroidsPerSector, false, asteroidConfiguration);
                ReactivateObjectsInSector(sector, enemiesPerSector, true, enemyConfiguration);
            }
        }
    }
}



private Vector3 GetRandomPositionInSector(Vector2Int sectorCoords)
{
    // Logic to calculate position within the sector
    float xNoise = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);
    float yNoise = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);
    return new Vector3(sectorCoords.x * sectorSize + xNoise, sectorCoords.y * sectorSize + yNoise, 0f);
}


    private JobHandle RegenerateSectorInBatches(Vector2Int sectorCoords, int objectCount, bool isEnemy, ObjectSO configuration, float[,] noiseGrid)
    {
        NativeArray<float> noiseGridNative = new NativeArray<float>(objectCount * 2, Allocator.TempJob);
        NativeArray<Vector3> positionsNative = new NativeArray<Vector3>(objectCount, Allocator.TempJob);

        for (int i = 0; i < objectCount; i++)
        {
            noiseGridNative[i * 2] = noiseGrid[i, 0];
            noiseGridNative[i * 2 + 1] = noiseGrid[i, 1];
        }

        RegenerateObjectsJob job = new RegenerateObjectsJob
        {
            sectorCoords = sectorCoords,
            objectCount = objectCount,
            isEnemy = isEnemy,
            noiseGrid = noiseGridNative,
            positions = positionsNative,
            sectorSize = sectorSize
        };

        JobHandle handle = job.Schedule();
        StartCoroutine(SpawnObjectsAfterJob(sectorCoords, objectCount, configuration, positionsNative, handle));

        return handle;
    }

private IEnumerator SpawnObjectsAfterJob(Vector2Int sectorCoords, int objectCount, ObjectSO configuration, NativeArray<Vector3> positions, JobHandle handle)
{
    yield return new WaitUntil(() => handle.IsCompleted);
    handle.Complete();

    GameObject prefab = (configuration == enemyConfiguration) ? enemyPrefab : asteroidPrefab;

    for (int i = 0; i < objectCount; i++)
    {
        if (i % objectsPerFrame == 0)
        {
            yield return null;
        }

        Vector3 position = positions[i];
        if (position != Vector3.zero)
        {
            // Get an object from the pool and activate it
            NetworkObject netObj = NetworkObjectPool.Singleton.GetNetworkObject(prefab, position, Quaternion.identity);
            if (netObj != null && !netObj.IsSpawned)
            {
                netObj.Spawn(true);
                var objectLoader = netObj.GetComponent<ObjectLoader>();
                if (objectLoader != null)
                {
                    objectLoader.SetConfigurationFromWorldGenerator(configuration, ObjectManager.Instance.GetObjectSOIndex(configuration));
                }
            }
        }
    }

    positions.Dispose();
}

    [BurstCompile]
    private struct RegenerateObjectsJob : IJob
    {
        public Vector2Int sectorCoords;
        public int objectCount;
        public bool isEnemy;
        public int sectorSize;

        [DeallocateOnJobCompletion] public NativeArray<float> noiseGrid;
        public NativeArray<Vector3> positions;

        public void Execute()
        {
            for (int i = 0; i < objectCount; i++)
            {
                positions[i] = GetPositionFromNoiseGrid(sectorCoords, isEnemy ? 10f : 0f, noiseGrid, i);
            }
        }

        private Vector3 GetPositionFromNoiseGrid(Vector2Int sectorCoords, float minDistance, NativeArray<float> noiseGrid, int index)
        {
            float xNoise = noiseGrid[index * 2];
            float yNoise = noiseGrid[index * 2 + 1];

            Vector3 position = new Vector3(
                sectorCoords.x * sectorSize + (xNoise * sectorSize),
                sectorCoords.y * sectorSize + (yNoise * sectorSize),
                0f
            );

            return (minDistance > 0 && Vector3.Distance(Vector3.zero, position) < minDistance) ? Vector3.zero : position;
        }
    }
}
