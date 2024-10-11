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
    public GameObject objectPrefab;
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
            int asteroidIndex = ObjectManager.Instance.GetObjectSOIndex(asteroidConfiguration);
            int enemyIndex = ObjectManager.Instance.GetObjectSOIndex(enemyConfiguration);

            ObjectPooler.Instance.CreatePool(objectPrefab, asteroidsPerSector, asteroidIndex);
            ObjectPooler.Instance.CreatePool(objectPrefab, enemiesPerSector, enemyIndex);

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
        int configIndex = ObjectManager.Instance.GetObjectSOIndex(configuration);
        for (int i = 0; i < objectCount; i++)
        {
            Vector3 position = GetPositionFromNoiseGrid(sectorCoords, isEnemy ? enemySpawnDistance : 0, noiseGrid, i);
            if (position != Vector3.zero)
            {
                SpawnObject(position, configIndex);
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

    private void SpawnObject(Vector3 position, int configIndex)
    {
        if (IsServer)
        {
            GameObject obj = FindInactiveObject(objectPrefab);
            if (obj == null)
            {
                // No inactive object found, expand pool and create new one
                obj = ObjectPooler.Instance.GetObjectFromPool(configIndex, position, Quaternion.identity, objectPrefab);
            }
            else
            {
                // Reuse the inactive object
                obj.transform.position = position;
                obj.SetActive(true);
            }

            NetworkObject netObj = obj.GetComponent<NetworkObject>();
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
        }
    }

    // Method to search for inactive objects in the pool by their tag (as per the old implementation)
    private GameObject FindInactiveObject(GameObject prefab)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(prefab.tag);
        foreach (GameObject obj in objects)
        {
            if (!obj.activeInHierarchy)
            {
                return obj;
            }
        }
        return null;
    }

    private IEnumerator RegenerateObjectsCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(regenerationInterval);

            randomSeed = Random.Range(0, 10000);

            foreach (var sector in sectors.Keys)
            {
                float[,] noiseGrid = PrecalculatePerlinNoiseGrid(sector);

                JobHandle handle = RegenerateSectorInBatches(sector, asteroidsPerSector, false, asteroidConfiguration, noiseGrid);
                handle.Complete();

                handle = RegenerateSectorInBatches(sector, enemiesPerSector, true, enemyConfiguration, noiseGrid);
                handle.Complete();
            }

            yield return null;
        }
    }
private JobHandle RegenerateSectorInBatches(Vector2Int sectorCoords, int objectCount, bool isEnemy, ObjectSO configuration, float[,] noiseGrid)
{
    // Create NativeArrays for noiseGrid and positions
    NativeArray<float> noiseGridNative = new NativeArray<float>(objectCount * 2, Allocator.TempJob);
    NativeArray<Vector3> positionsNative = new NativeArray<Vector3>(objectCount, Allocator.TempJob);

    // Fill NativeArray with noise grid data
    for (int i = 0; i < objectCount; i++)
    {
        noiseGridNative[i * 2] = noiseGrid[i, 0];
        noiseGridNative[i * 2 + 1] = noiseGrid[i, 1];
    }

    // Set up the job
    RegenerateObjectsJob job = new RegenerateObjectsJob
    {
        sectorCoords = sectorCoords,
        objectCount = objectCount,
        isEnemy = isEnemy,
        noiseGrid = noiseGridNative,
        positions = positionsNative,
        sectorSize = sectorSize
    };

    // Schedule the job and get the JobHandle
    JobHandle handle = job.Schedule();

    // Start the coroutine to process the objects after the job is completed
    StartCoroutine(SpawnObjectsAfterJob(sectorCoords, objectCount, configuration, positionsNative, handle));

    return handle;
}

private IEnumerator SpawnObjectsAfterJob(Vector2Int sectorCoords, int objectCount, ObjectSO configuration, NativeArray<Vector3> positions, JobHandle handle)
{
    yield return new WaitUntil(() => handle.IsCompleted); // Wait until the job is done

    handle.Complete(); // Ensure the job has fully completed

    int configIndex = ObjectManager.Instance.GetObjectSOIndex(configuration);

    // Spawn the objects in batches over multiple frames
    for (int i = 0; i < objectCount; i++)
    {
        if (i % objectsPerFrame == 0)
        {
            yield return null; // Yield to the next frame every 'objectsPerFrame'
        }

        Vector3 position = positions[i];
        if (position != Vector3.zero)
        {
            SpawnObject(position, configIndex);
        }
    }

    // Dispose the NativeArray after processing
    positions.Dispose();
}

[BurstCompile]
private struct RegenerateObjectsJob : IJob
{
    public Vector2Int sectorCoords;
    public int objectCount;
    public bool isEnemy;
    public int sectorSize;

    // Automatically dispose of noiseGrid after the job is complete
    [DeallocateOnJobCompletion] public NativeArray<float> noiseGrid;

    // Manually dispose of positions after job completion in coroutine
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
