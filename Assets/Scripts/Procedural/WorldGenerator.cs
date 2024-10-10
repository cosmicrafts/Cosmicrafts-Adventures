using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System.Collections;

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
    public float noiseScale = 5f; // Scale of the Perlin noise

    [Header("Regeneration")]
    public float regenerationInterval = 10f; // Set from Inspector for regeneration time

    // Assignable ObjectSO fields for custom configurations
    public ObjectSO asteroidConfiguration;
    public ObjectSO enemyConfiguration;

    private int randomSeed; // Seed for Perlin noise

    private void Awake()
    {
        Instance = this;
        randomSeed = Random.Range(0, 10000); // Initialize random seed once
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Pre-create pools for asteroids and enemies
            int asteroidIndex = ObjectManager.Instance.GetObjectSOIndex(asteroidConfiguration);
            int enemyIndex = ObjectManager.Instance.GetObjectSOIndex(enemyConfiguration);

            // Create a pool for asteroids
            ObjectPooler.Instance.CreatePool(objectPrefab, asteroidsPerSector, asteroidIndex);

            // Create a pool for enemies
            ObjectPooler.Instance.CreatePool(objectPrefab, enemiesPerSector, enemyIndex);

            // Now proceed with sector generation and object spawning
            GenerateInitialSectors();

            // Start object regeneration coroutine
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

        // Pre-calculate Perlin noise grid for this sector
        float[,] noiseGrid = PrecalculatePerlinNoiseGrid(coordinates);

        GenerateObjectsInSector(coordinates, asteroidsPerSector, false, asteroidConfiguration, noiseGrid);
        GenerateObjectsInSector(coordinates, enemiesPerSector, true, enemyConfiguration, noiseGrid);
    }

    // Precalculate a grid of Perlin noise values for the sector
    private float[,] PrecalculatePerlinNoiseGrid(Vector2Int sectorCoords)
    {
        int gridResolution = Mathf.Max(asteroidsPerSector, enemiesPerSector); // Adjust grid resolution based on object count
        float[,] noiseGrid = new float[gridResolution, 2]; // Each object will have an x and y noise value

        for (int i = 0; i < gridResolution; i++)
        {
            // Random offsets for the noise
            float xOffset = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);
            float yOffset = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);

            // Generate Perlin noise values
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

    // Generate object position from the pre-calculated Perlin noise grid
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
            // Get object from the pool instead of instantiating new
            GameObject obj = ObjectPooler.Instance.GetObjectFromPool(configIndex, position, Quaternion.identity, objectPrefab);

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

            // Notify clients about the spawned object without instantiating on client
            InformClientAboutSpawnedObjectClientRpc(position, configIndex);
        }
    }

    private IEnumerator RegenerateObjectsCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(regenerationInterval); // Expose to Inspector

            randomSeed = Random.Range(0, 10000); // Update seed for each regeneration

            foreach (var sector in sectors.Keys)
            {
                float[,] noiseGrid = PrecalculatePerlinNoiseGrid(sector);
                RegenerateObjectsInSector(sector, asteroidsPerSector, false, asteroidConfiguration, noiseGrid);
                RegenerateObjectsInSector(sector, enemiesPerSector, true, enemyConfiguration, noiseGrid);
            }
        }
    }

    private void RegenerateObjectsInSector(Vector2Int sectorCoords, int objectCount, bool isEnemy, ObjectSO configuration, float[,] noiseGrid)
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

    [ClientRpc]
    private void InformClientAboutSpawnedObjectClientRpc(Vector3 position, int configIndex)
    {
        if (!IsServer)
        {
            ObjectSO configuration = ObjectManager.Instance.GetObjectSOByIndex(configIndex);

            // Find a pre-existing inactive object instead of instantiating a new one
            GameObject obj = FindInactiveObject(objectPrefab); // Custom method to find an inactive object
            if (obj != null)
            {
                obj.SetActive(true); // Activate the object
                var loader = obj.GetComponent<ObjectLoader>();
                loader?.SetConfigurationFromWorldGenerator(configuration, configIndex);
            }
        }
    }

    private GameObject FindInactiveObject(GameObject prefab)
    {
        // This method searches for an inactive object of the given prefab in the scene
        GameObject[] objects = GameObject.FindGameObjectsWithTag(prefab.tag); // Adjust the tag accordingly
        foreach (GameObject obj in objects)
        {
            if (!obj.activeInHierarchy)
            {
                return obj;
            }
        }
        return null;
    }
}
