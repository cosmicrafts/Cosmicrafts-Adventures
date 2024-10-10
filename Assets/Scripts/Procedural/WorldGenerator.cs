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

        GenerateObjectsInSector(coordinates, asteroidsPerSector, false, asteroidConfiguration);
        GenerateObjectsInSector(coordinates, enemiesPerSector, true, enemyConfiguration);
    }

    private void GenerateObjectsInSector(Vector2Int sectorCoords, int objectCount, bool isEnemy, ObjectSO configuration)
    {
        int configIndex = ObjectManager.Instance.GetObjectSOIndex(configuration);

        for (int i = 0; i < objectCount; i++)
        {
            Vector3 position = GetPositionUsingPerlinNoise(sectorCoords, isEnemy ? enemySpawnDistance : 0, randomSeed);
            if (position != Vector3.zero)
            {
                SpawnObject(position, configIndex);
            }
        }
    }

    // New method to generate a position using Perlin noise for chunk-like placement with a seed
    private Vector3 GetPositionUsingPerlinNoise(Vector2Int sectorCoords, float minDistance = 0, int seed = 0)
    {
        float xOffset = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);
        float yOffset = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);

        // Modify the seed for Perlin noise
        float xNoise = Mathf.PerlinNoise((sectorCoords.x + xOffset + seed) / noiseScale, (sectorCoords.y + yOffset + seed) / noiseScale);
        float yNoise = Mathf.PerlinNoise((sectorCoords.y + yOffset + seed) / noiseScale, (sectorCoords.x + xOffset + seed) / noiseScale);

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
            RegenerateObjectsInSector(sector, asteroidsPerSector, false, asteroidConfiguration);
            RegenerateObjectsInSector(sector, enemiesPerSector, true, enemyConfiguration);
        }
    }
}

private void RegenerateObjectsInSector(Vector2Int sectorCoords, int objectCount, bool isEnemy, ObjectSO configuration)
{
    int configIndex = ObjectManager.Instance.GetObjectSOIndex(configuration);

    for (int i = 0; i < objectCount; i++)
    {
        Vector3 position = GetPositionUsingPerlinNoise(sectorCoords, isEnemy ? enemySpawnDistance : 0, randomSeed);
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
