using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections;

public class WorldGenerator : NetworkBehaviour
{
    public static WorldGenerator Instance;
    public int sectorSize = 10;
    private Dictionary<Vector2Int, Sector> sectors = new Dictionary<Vector2Int, Sector>();

    [Header("Settings")]
    public GameObject objectPrefab; // The single object prefab to be used
    public int objectsPerSector = 5;
    public float spawnDistance = 10;
    public float noiseScale = 5f;

    [Header("Regeneration")]
    public float regenerationInterval = 10f;
    public int objectsPerFrame = 4; // Number of objects to spawn per frame

    [Header("Configurations")]
    public List<ObjectSO> configurations = new List<ObjectSO>(); // Dynamic list of configurations

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
        GenerateObjectsInSector(coordinates, objectsPerSector, noiseGrid);
    }

    private float[,] PrecalculatePerlinNoiseGrid(Vector2Int sectorCoords)
    {
        int gridResolution = objectsPerSector;
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

    private void GenerateObjectsInSector(Vector2Int sectorCoords, int objectCount, float[,] noiseGrid)
    {
        for (int i = 0; i < objectCount; i++)
        {
            Vector3 position = GetPositionFromNoiseGrid(sectorCoords, spawnDistance, noiseGrid, i);
            if (position != Vector3.zero)
            {
                SpawnObject(position, configurations[Random.Range(0, configurations.Count)]);
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

    private void SpawnObject(Vector3 position, ObjectSO configuration)
    {
        if (!IsServer) return; // Ensure only server executes this code

        // Get object from the NetworkObjectPool
        NetworkObject netObj = NetworkObjectPool.Singleton.GetNetworkObject(objectPrefab, position, Quaternion.identity);

        if (netObj != null)
        {
            if (!netObj.IsSpawned)
            {
                netObj.Spawn(true); // Ensure the object is spawned by the server and synchronized to clients
            }

            var objectLoader = netObj.GetComponent<ObjectLoader>();
            if (objectLoader != null)
            {
                // Apply the configuration dynamically from the ObjectSO
                objectLoader.SetConfigurationFromWorldGenerator(configuration, ObjectManager.Instance.GetObjectSOIndex(configuration));
                objectLoader.SetOriginalPrefab(objectPrefab); // Ensure proper pooling behavior
            }

            // Inform clients about the spawned object
            InformClientAboutSpawnedObjectClientRpc(position, ObjectManager.Instance.GetObjectSOIndex(configuration), true);
        }
        else
        {
            Debug.LogError($"[WorldGenerator] Failed to get object from the pool.");
        }
    }

    [ClientRpc]
    public void InformClientAboutSpawnedObjectClientRpc(Vector3 position, int configIndex, bool isActive)
    {
        if (IsServer) return; // This logic is for clients only

        ObjectSO configuration = ObjectManager.Instance.GetObjectSOByIndex(configIndex);
        GameObject obj = FindInactiveObject();

        if (obj != null)
        {
            if (isActive)
            {
                obj.transform.position = position;
                obj.SetActive(true);

                var loader = obj.GetComponent<ObjectLoader>();
                loader?.SetConfigurationFromWorldGenerator(configuration, configIndex);
            }
            else
            {
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

    private IEnumerator RegenerateObjectsCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(regenerationInterval);

            foreach (var sector in sectors.Keys)
            {
                if (gameObject.activeInHierarchy)
                {
                    ReactivateObjectsInSector(sector, objectsPerSector);
                }
            }
        }
    }

    private void ReactivateObjectsInSector(Vector2Int sectorCoords, int objectCount)
    {
        for (int i = 0; i < objectCount; i++)
        {
            NetworkObject netObj = NetworkObjectPool.Singleton.GetNetworkObject(objectPrefab, GetRandomPositionInSector(sectorCoords), Quaternion.identity);
            if (netObj != null && !netObj.IsSpawned)
            {
                netObj.Spawn(true);
                var objectLoader = netObj.GetComponent<ObjectLoader>();
                if (objectLoader != null)
                {
                    objectLoader.SetConfigurationFromWorldGenerator(configurations[Random.Range(0, configurations.Count)], ObjectManager.Instance.GetObjectSOIndex(configurations[Random.Range(0, configurations.Count)]));
                }
            }
        }
    }
//
    private Vector3 GetRandomPositionInSector(Vector2Int sectorCoords)
    {
        float xNoise = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);
        float yNoise = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);
        return new Vector3(sectorCoords.x * sectorSize + xNoise, sectorCoords.y * sectorSize + yNoise, 0f);
    }
}
