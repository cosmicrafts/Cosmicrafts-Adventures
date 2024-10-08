using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System;

public class WorldGenerator : NetworkBehaviour
{
    public static WorldGenerator Instance; // Singleton instance for easy access
    public int sectorSize = 10; // Size of each sector
    private Dictionary<Vector2Int, Sector> sectors = new Dictionary<Vector2Int, Sector>(); // Dictionary to store generated sectors

    [Header("Asteroid Settings")]
    public GameObject baseObjectPrefab; // Single base prefab for both asteroids and enemies
    public ObjectSO[] asteroidConfigurations;
    public int asteroidsPerSector = 5;

    [Header("Enemy Settings")]
    public ObjectSO[] enemyConfigurations;
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

    // Generates initial 9 sectors (3x3 grid)
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

    // Generates a new sector at specified coordinates (server-only)
    public void GenerateSector(Vector2Int coordinates)
    {
        if (sectors.ContainsKey(coordinates))
            return;

        // Create a new sector from the template (no visual representation on the server)
        Sector newSector = ScriptableObject.CreateInstance<Sector>();
        newSector.sectorName = $"Sector ({coordinates.x}, {coordinates.y})";
        newSector.isGenerated = true;

        // Store the sector in the dictionary
        sectors.Add(coordinates, newSector);

        // Generate asteroids and enemies in the sector
        GenerateAsteroids(coordinates);
        GenerateEnemies(coordinates);
    }

    private void GenerateAsteroids(Vector2Int sectorCoords)
    {
        Sector sector = sectors[sectorCoords];

        for (int i = 0; i < asteroidsPerSector; i++)
        {
            Vector3 asteroidPosition = GetRandomPositionInSector(sectorCoords);
            if (asteroidPosition != Vector3.zero)
            {
                sector.asteroidPositions.Add(asteroidPosition);
                int configIndex = UnityEngine.Random.Range(0, asteroidConfigurations.Length);
                SpawnObject(asteroidPosition, asteroidConfigurations[configIndex], true);
            }
        }
    }

    private void GenerateEnemies(Vector2Int sectorCoords)
    {
        Sector sector = sectors[sectorCoords];

        for (int i = 0; i < enemiesPerSector; i++)
        {
            Vector3 enemyPosition = GetRandomPositionInSector(sectorCoords, enemySpawnDistance);
            if (enemyPosition != Vector3.zero)
            {
                sector.enemyPositions.Add(enemyPosition);
                int configIndex = UnityEngine.Random.Range(0, enemyConfigurations.Length);
                SpawnObject(enemyPosition, enemyConfigurations[configIndex], true);
            }
        }
    }

    private Vector3 GetRandomPositionInSector(Vector2Int sectorCoords, float minDistance = 0)
    {
        float xOffset = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);
        float yOffset = UnityEngine.Random.Range(-sectorSize / 2, sectorSize / 2);

        Vector3 potentialPosition = new Vector3(
            sectorCoords.x * sectorSize + xOffset,
            sectorCoords.y * sectorSize + yOffset,
            0f
        );

        // Only return positions that are far enough away, if specified
        if (minDistance > 0 && Vector3.Distance(Vector3.zero, potentialPosition) < minDistance)
        {
            return Vector3.zero;
        }

        return potentialPosition;
    }

private void SpawnObject(Vector3 position, ObjectSO configuration, bool isServerSpawn)
{
    // Only the server should spawn network objects
    if (IsServer)
    {
        GameObject obj = Instantiate(baseObjectPrefab, position, Quaternion.identity);
        NetworkObject networkObject = obj.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.Spawn(); // Network-spawn the object on the server only
        }

        ApplyConfigurationToObject(obj, configuration); // Apply config on both server and clients

        // Notify clients to apply the configuration via ClientRpc
        if (isServerSpawn)
        {
            ObjectSpawnedClientRpc(position, configuration.teamTag == TeamComponent.TeamTag.Enemy);
        }
    }
}

[ClientRpc]
private void ObjectSpawnedClientRpc(Vector3 position, bool isEnemy, ClientRpcParams clientRpcParams = default)
{
    if (!IsServer)
    {
        // Clients instantiate only visual objects for local representation
        ObjectSO configuration = isEnemy
            ? enemyConfigurations[UnityEngine.Random.Range(0, enemyConfigurations.Length)]
            : asteroidConfigurations[UnityEngine.Random.Range(0, asteroidConfigurations.Length)];

        GameObject obj = Instantiate(baseObjectPrefab, position, Quaternion.identity);
        ApplyConfigurationToObject(obj, configuration);  // Apply the configuration
    }
}

    private void ApplyConfigurationToObject(GameObject obj, ObjectSO configuration)
    {
        // Health Component
        var healthComponent = obj.GetComponent<HealthComponent>();
        if (healthComponent != null)
        {
            healthComponent.ApplyConfiguration(configuration);
        }

        // Conditionally add movement
        if (configuration.hasMovement)
        {
            var movementComponent = obj.GetComponent<MovementComponent>() ?? obj.AddComponent<MovementComponent>();
            movementComponent.ApplyConfiguration(configuration);
        }
        else
        {
            var movementComponent = obj.GetComponent<MovementComponent>();
            if (movementComponent != null)
            {
                Destroy(movementComponent);
            }
        }

        // Conditionally add shooting
        if (configuration.hasShooting)
        {
            var shootingComponent = obj.GetComponent<ShootingComponent>() ?? obj.AddComponent<ShootingComponent>();
            shootingComponent.ApplyConfiguration(configuration);
        }
        else
        {
            var shootingComponent = obj.GetComponent<ShootingComponent>();
            if (shootingComponent != null)
            {
                Destroy(shootingComponent);
            }
        }

        // Conditionally add rotation
        if (configuration.hasRotation)
        {
            var rotationComponent = obj.GetComponent<RotationComponent>() ?? obj.AddComponent<RotationComponent>();
            rotationComponent.ApplyConfiguration(configuration);
        }
        else
        {
            var rotationComponent = obj.GetComponent<RotationComponent>();
            if (rotationComponent != null)
            {
                Destroy(rotationComponent);
            }
        }

        // Conditionally add or remove input
        if (configuration.hasInput)
        {
            var inputComponent = obj.GetComponent<InputComponent>() ?? obj.AddComponent<InputComponent>();
        }
        else
        {
            var inputComponent = obj.GetComponent<InputComponent>();
            if (inputComponent != null)
            {
                Destroy(inputComponent);
            }
        }

        // Apply the sprite
        var spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && configuration.objectSprite != null)
        {
            spriteRenderer.sprite = configuration.objectSprite;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void GetAllSectorsServerRpc(ServerRpcParams serverRpcParams = default)
    {
        foreach (var sector in sectors)
        {
            var asteroidPositions = sector.Value.asteroidPositions;
            var enemyPositions = sector.Value.enemyPositions;

            float[] asteroidX = new float[asteroidPositions.Count];
            float[] asteroidY = new float[asteroidPositions.Count];
            float[] asteroidZ = new float[asteroidPositions.Count];

            for (int i = 0; i < asteroidPositions.Count; i++)
            {
                asteroidX[i] = asteroidPositions[i].x;
                asteroidY[i] = asteroidPositions[i].y;
                asteroidZ[i] = asteroidPositions[i].z;
            }

            float[] enemyX = new float[enemyPositions.Count];
            float[] enemyY = new float[enemyPositions.Count];
            float[] enemyZ = new float[enemyPositions.Count];

            for (int i = 0; i < enemyPositions.Count; i++)
            {
                enemyX[i] = enemyPositions[i].x;
                enemyY[i] = enemyPositions[i].y;
                enemyZ[i] = enemyPositions[i].z;
            }

            SendSectorToClientRpc(sector.Key, sector.Value.sectorName, asteroidX, asteroidY, asteroidZ, enemyX, enemyY, enemyZ, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }
                }
            });
        }
    }

    // Sends sector data  in batch to the specific client
    [ClientRpc]
    private void SendSectorToClientRpc(Vector2Int sectorCoords, string sectorName, float[] asteroidX, float[] asteroidY, float[] asteroidZ, float[] enemyX, float[] enemyY, float[] enemyZ, ClientRpcParams clientRpcParams = default)
    {
        // Update minimap or other UI
        MinimapController.Instance?.AddSectorData(sectorCoords, sectorName);

        // Spawn all asteroids in one batch
        for (int i = 0; i < asteroidX.Length; i++)
        {
            Vector3 position = new Vector3(asteroidX[i], asteroidY[i], asteroidZ[i]);
            SpawnObject(position, asteroidConfigurations[UnityEngine.Random.Range(0, asteroidConfigurations.Length)], false);
        }

        // Spawn all enemies in one batch
        for (int i = 0; i < enemyX.Length; i++)
        {
            Vector3 position = new Vector3(enemyX[i], enemyY[i], enemyZ[i]);
            SpawnObject(position, enemyConfigurations[UnityEngine.Random.Range(0, enemyConfigurations.Length)], false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestAllWorldDataFromServerRpc(ServerRpcParams rpcParams = default)
    {
        foreach (var sector in sectors)
        {
            var asteroidPositions = sector.Value.asteroidPositions;
            var enemyPositions = sector.Value.enemyPositions;

            float[] asteroidX = new float[asteroidPositions.Count];
            float[] asteroidY = new float[asteroidPositions.Count];
            float[] asteroidZ = new float[asteroidPositions.Count];

            for (int i = 0; i < asteroidPositions.Count; i++)
            {
                asteroidX[i] = asteroidPositions[i].x;
                asteroidY[i] = asteroidPositions[i].y;
                asteroidZ[i] = asteroidPositions[i].z;
            }

            float[] enemyX = new float[enemyPositions.Count];
            float[] enemyY = new float[enemyPositions.Count];
            float[] enemyZ = new float[enemyPositions.Count];

            for (int i = 0; i < enemyPositions.Count; i++)
            {
                enemyX[i] = enemyPositions[i].x;
                enemyY[i] = enemyPositions[i].y;
                enemyZ[i] = enemyPositions[i].z;
            }

            SendSectorToClientRpc(sector.Key, sector.Value.sectorName, asteroidX, asteroidY, asteroidZ, enemyX, enemyY, enemyZ, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { rpcParams.Receive.SenderClientId }
                }
            });
        }
    }

    private void Log(string message)
    {
        if (debugLoggingEnabled)
        {
            Debug.Log(message);
        }
    }

    private bool debugLoggingEnabled = false;
}
