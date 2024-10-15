using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
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
    public List<ObjectSO> secondaryConfigurations = new List<ObjectSO>();

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
        GenerateObjectsInSector(coordinates, objectsPerSector, noiseGrid, configurations);
    }

    private float[,] PrecalculatePerlinNoiseGrid(Vector2Int sectorCoords)
    {
        int gridResolution = objectsPerSector;
        float[,] noiseGrid = new float[gridResolution, 2];

        for (int i = 0; i < gridResolution; i++)
        {
            float xOffset = Random.Range(-sectorSize / 2, sectorSize / 2);
            float yOffset = Random.Range(-sectorSize / 2, sectorSize / 2);

            float xNoise = Mathf.PerlinNoise((sectorCoords.x + xOffset + randomSeed) / noiseScale, (sectorCoords.y + yOffset + randomSeed) / noiseScale);
            float yNoise = Mathf.PerlinNoise((sectorCoords.y + yOffset + randomSeed) / noiseScale, (sectorCoords.x + xOffset + randomSeed) / noiseScale);

            noiseGrid[i, 0] = xNoise;
            noiseGrid[i, 1] = yNoise;
        }

        return noiseGrid;
    }

    private void GenerateObjectsInSector(Vector2Int sectorCoords, int objectCount, float[,] noiseGrid, List<ObjectSO> configList)
    {
        for (int i = 0; i < objectCount; i++)
        {
            float adjustedMinDistance = spawnDistance * 1.2f;
            Vector3 position = GetPositionFromNoiseGrid(sectorCoords, adjustedMinDistance, noiseGrid, i);
            if (position != Vector3.zero)
            {
                InstantiateObject(position, configList[Random.Range(0, configList.Count)]);
            }
        }
    }

    private Vector3 GetPositionFromNoiseGrid(Vector2Int sectorCoords, float minDistance, float[,] noiseGrid, int index)
    {
        float xNoise = noiseGrid[index, 0];
        float yNoise = noiseGrid[index, 1];
        float noiseFactor = sectorSize * 1.5f;

        float randomOffsetX = Random.Range(-sectorSize * 0.5f, sectorSize * 0.5f);
        float randomOffsetY = Random.Range(-sectorSize * 0.5f, sectorSize * 0.5f);

        Vector3 position = new Vector3(
            sectorCoords.x * sectorSize + (xNoise * noiseFactor) + randomOffsetX,
            sectorCoords.y * sectorSize + (yNoise * noiseFactor) + randomOffsetY,
            0f
        );

        return (minDistance > 0 && Vector3.Distance(Vector3.zero, position) < minDistance) ? Vector3.zero : position;
    }

    private void InstantiateObject(Vector3 position, ObjectSO configuration)
    {
        if (!IsServer) return;

        // Instantiate the object
        GameObject objInstance = Instantiate(objectPrefab, position, Quaternion.identity);

        NetworkObject netObj = objInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(); // Spawn the object on the network

            var objectLoader = netObj.GetComponent<ObjectLoader>();
            if (objectLoader != null)
            {
                objectLoader.SetConfigurationFromWorldGenerator(configuration, ObjectManager.Instance.GetObjectSOIndex(configuration));
            }
        }
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
                    RegenerateSectorObjects(sector);
                }
            }
        }
    }

    private void RegenerateSectorObjects(Vector2Int sectorCoords)
    {
        for (int i = 0; i < objectsPerSector; i++)
        {
            Vector3 position = GetRandomPositionInSector(sectorCoords);
            InstantiateObject(position, configurations[Random.Range(0, configurations.Count)]);
        }
    }

    private Vector3 GetRandomPositionInSector(Vector2Int sectorCoords)
    {
        float xNoise = Random.Range(-sectorSize / 2, sectorSize / 2);
        float yNoise = Random.Range(-sectorSize / 2, sectorSize / 2);
        return new Vector3(sectorCoords.x * sectorSize + xNoise, sectorCoords.y * sectorSize + yNoise, 0f);
    }

    // Request Regeneration from Client
    public void RequestRegeneration()
    {
        if (IsClient)
        {
            RequestRegenerationServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRegenerationServerRpc()
    {
        foreach (var sector in sectors.Keys)
        {
            if (gameObject.activeInHierarchy)
            {
                RegenerateSectorObjects(sector);
            }
        }
    }

    
}
