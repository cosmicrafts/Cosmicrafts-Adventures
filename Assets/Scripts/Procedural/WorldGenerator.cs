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
            Debug.LogError("[WorldGenerator] Prefab is null! Ensure the prefab references are set correctly.");
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
            Debug.Log($"[WorldGenerator] Retrieved {prefab.name} from the pool and spawning it.");

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
        }
        else
        {
            Debug.LogError($"[WorldGenerator] Failed to get {prefab.name} from the pool.");
        }
    }


    [ClientRpc]
    private void InformClientAboutSpawnedObjectClientRpc(Vector3 position, int configIndex)
    {
        if (IsServer) return;

        // Clients only activate objects, no need to spawn locally
        ObjectSO configuration = ObjectManager.Instance.GetObjectSOByIndex(configIndex);
        GameObject obj = FindInactiveObject(); // Use the prefab tag or custom tag logic if needed

        if (obj != null)
        {
            obj.SetActive(true);
            var loader = obj.GetComponent<ObjectLoader>();
            loader?.SetConfigurationFromWorldGenerator(configuration, configIndex);
        }
    }

    private GameObject FindInactiveObject()
    {
        // Logic for finding inactive object in the scene
        GameObject[] objects = GameObject.FindGameObjectsWithTag("YourTag"); // Replace with the actual tag
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
        }
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
                SpawnObject(position, prefab, configuration);
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
