using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

public class NetworkObjectPool : NetworkBehaviour
{
    public static NetworkObjectPool Singleton { get; private set; }

    [SerializeField]
    List<PoolConfigObject> PooledPrefabsList;

    HashSet<GameObject> m_Prefabs = new HashSet<GameObject>();

    Dictionary<GameObject, ObjectPool<NetworkObject>> m_PooledObjects = new Dictionary<GameObject, ObjectPool<NetworkObject>>();

    public void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Singleton = this;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (PooledPrefabsList == null || PooledPrefabsList.Count == 0)
        {
           // Debug.LogError("[NetworkObjectPool] No prefabs are registered in PooledPrefabsList. Please ensure prefabs are assigned.");
            return;
        }

        foreach (var configObject in PooledPrefabsList)
        {
            if (configObject.Prefab == null)
            {
               // Debug.LogError("[NetworkObjectPool] A prefab is null during registration. Ensure all prefab references are set.");
                continue;
            }
            RegisterPrefabInternal(configObject.Prefab, configObject.PrewarmCount);
        }
    }


    public override void OnNetworkDespawn()
    {
        // Unregisters all objects in PooledPrefabsList from the cache.
        foreach (var prefab in m_Prefabs)
        {
            // Unregister Netcode Spawn handlers
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefab);
            m_PooledObjects[prefab].Clear();
        }
        m_PooledObjects.Clear();
        m_Prefabs.Clear();
    }

    public void OnValidate()
    {
        for (var i = 0; i < PooledPrefabsList.Count; i++)
        {
            var prefab = PooledPrefabsList[i].Prefab;
            if (prefab != null)
            {
                Assert.IsNotNull(prefab.GetComponent<NetworkObject>(), $"{nameof(NetworkObjectPool)}: Pooled prefab \"{prefab.name}\" at index {i.ToString()} has no {nameof(NetworkObject)} component.");
            }
        }
    }

    /// <summary>
    /// Gets an instance of the given prefab from the pool. The prefab must be registered to the pool.
    /// </summary>
    /// <remarks>
    /// To spawn a NetworkObject from one of the pools, this must be called on the server, then the instance
    /// returned from it must be spawned on the server. This method will then also be called on the client by the
    /// PooledPrefabInstanceHandler when the client receives a spawn message for a prefab that has been registered
    /// here.
    /// </remarks>
    /// <param name="prefab"></param>
    /// <param name="position">The position to spawn the object at.</param>
    /// <param name="rotation">The rotation to spawn the object with.</param>
    /// <returns></returns>
public NetworkObject GetNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation)
{
    if (!m_PooledObjects.ContainsKey(prefab))
    {
       // Debug.LogError($"[NetworkObjectPool] Prefab '{prefab.name}' is not registered in the pool. Ensure it is included in PooledPrefabsList.");
        return null;
    }

    var networkObject = m_PooledObjects[prefab].Get();

    var noTransform = networkObject.transform;
    noTransform.position = position;
    noTransform.rotation = rotation;

    return networkObject;
}


    /// <summary>
    /// Return an object to the pool (reset objects before returning).
    /// </summary>
    public void ReturnNetworkObject(NetworkObject networkObject, GameObject prefab)
    {
        m_PooledObjects[prefab].Release(networkObject);
    }

    /// <summary>
    /// Builds up the cache for a prefab.
    /// </summary>
    void RegisterPrefabInternal(GameObject prefab, int prewarmCount)
    {
        if (prefab == null || !prefab.GetComponent<NetworkObject>())
        {
           // Debug.LogError($"[NetworkObjectPool] Prefab {prefab.name} is not valid for pooling. It must have a NetworkObject component.");
            return;
        }

        NetworkObject CreateFunc()
        {
            var newObj = Instantiate(prefab).GetComponent<NetworkObject>();
           // Debug.Log($"[NetworkObjectPool] Created new instance of {prefab.name} for the pool.");
            return newObj;
        }

        void ActionOnGet(NetworkObject networkObject)
        {
           // Debug.Log($"[NetworkObjectPool] Activating {networkObject.name} from the pool.");
            networkObject.gameObject.SetActive(true);
        }

        void ActionOnRelease(NetworkObject networkObject)
        {
           // Debug.Log($"[NetworkObjectPool] Deactivating {networkObject.name} and returning it to the pool.");
            networkObject.gameObject.SetActive(false);
        }

        void ActionOnDestroy(NetworkObject networkObject)
        {
           // Debug.Log($"[NetworkObjectPool] Destroying {networkObject.name}.");
            Destroy(networkObject.gameObject);
        }

        m_Prefabs.Add(prefab);
        m_PooledObjects[prefab] = new ObjectPool<NetworkObject>(CreateFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy, defaultCapacity: prewarmCount);

        // Prewarm the pool
        var prewarmNetworkObjects = new List<NetworkObject>();
        for (var i = 0; i < prewarmCount; i++)
        {
            prewarmNetworkObjects.Add(m_PooledObjects[prefab].Get());
        }
        foreach (var networkObject in prewarmNetworkObjects)
        {
            m_PooledObjects[prefab].Release(networkObject);
        }

        // Register Netcode Spawn handlers
        NetworkManager.Singleton.PrefabHandler.AddHandler(prefab, new PooledPrefabInstanceHandler(prefab, this));
    }
}

    [Serializable]
    struct PoolConfigObject
    {
        public GameObject Prefab;
        public int PrewarmCount;
    }

    class PooledPrefabInstanceHandler : INetworkPrefabInstanceHandler
    {
        GameObject m_Prefab;
        NetworkObjectPool m_Pool;

        public PooledPrefabInstanceHandler(GameObject prefab, NetworkObjectPool pool)
        {
            m_Prefab = prefab;
            m_Pool = pool;
        }

        NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            return m_Pool.GetNetworkObject(m_Prefab, position, rotation);
        }

        void INetworkPrefabInstanceHandler.Destroy(NetworkObject networkObject)
        {
            m_Pool.ReturnNetworkObject(networkObject, m_Prefab);
        }
    }