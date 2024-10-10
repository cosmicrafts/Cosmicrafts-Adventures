using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;
    private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();

    private void Awake()
    {
        Instance = this;
    }

    // Initialize a new pool
    public void CreatePool(GameObject prefab, int poolSize, int objectIndex)
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

    // Retrieve an object from the pool
    public GameObject GetObjectFromPool(int objectIndex, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(objectIndex))
        {
            Debug.LogError("Pool with index " + objectIndex + " doesn't exist.");
            return null;
        }

        GameObject objectToSpawn = poolDictionary[objectIndex].Dequeue();

        // Reset object properties
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Handle networked object spawning
        NetworkObject networkObject = objectToSpawn.GetComponent<NetworkObject>();
        if (networkObject != null && !networkObject.IsSpawned)
        {
            networkObject.Spawn();  // Spawn the networked object
        }

        poolDictionary[objectIndex].Enqueue(objectToSpawn); // Re-enqueue for future reuse

        return objectToSpawn;
    }

    // Deactivate and return object to pool
    public void ReturnToPool(GameObject obj, int objectIndex)
    {
        // Handle networked object despawning before returning it to the pool
        NetworkObject networkObject = obj.GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn();  // Despawn the object in a networked environment
        }

        obj.SetActive(false); // Deactivate the object
    }
}
