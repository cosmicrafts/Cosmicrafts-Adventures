using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;
    private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Prevent duplicate singletons
        }
    }

    // Initialize a new pool
    public void CreatePool(GameObject prefab, int poolSize, int objectIndex)
    {
        if (!poolDictionary.ContainsKey(objectIndex))
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);

                // Ensure networked object is despawned if needed
                NetworkObject networkObject = obj.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn();
                }

                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(objectIndex, objectPool);
        }
    }

    // Dynamically create a pool if it doesn't exist and expand it if needed
    private void EnsurePoolExists(int objectIndex, GameObject prefab, int expandSize = 5)
    {
        if (!poolDictionary.ContainsKey(objectIndex))
        {
            Debug.Log($"Pool with index {objectIndex} doesn't exist. Creating new pool.");
            CreatePool(prefab, expandSize, objectIndex);
        }
    }

    // Retrieve an object from the pool, expand the pool if needed
    public GameObject GetObjectFromPool(int objectIndex, Vector3 position, Quaternion rotation, GameObject prefab, int expandPoolSize = 5)
    {
        EnsurePoolExists(objectIndex, prefab, expandPoolSize);

        Queue<GameObject> objectPool = poolDictionary[objectIndex];

        // If pool is empty, instantiate new objects
        if (objectPool.Count == 0)
        {
            Debug.Log($"Pool with index {objectIndex} is empty, expanding pool.");
            for (int i = 0; i < expandPoolSize; i++)
            {
                GameObject newObj = Instantiate(prefab);
                newObj.SetActive(false);
                objectPool.Enqueue(newObj);
            }
        }

        GameObject objectToSpawn = objectPool.Dequeue();

        // Reset object properties
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Handle networked object spawning
        NetworkObject networkObject = objectToSpawn.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            if (!networkObject.IsSpawned)
            {
                networkObject.Spawn();  // Spawn the networked object only if it's not already spawned
            }
        }

        poolDictionary[objectIndex].Enqueue(objectToSpawn); // Re-enqueue for future reuse

        return objectToSpawn;
    }

    // Deactivate and return object to pool
    public void ReturnToPool(GameObject obj, int objectIndex)
    {
        NetworkObject networkObject = obj.GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn(true);  // Ensure proper despawn
        }

        obj.SetActive(false); // Deactivate the object
    }
}
