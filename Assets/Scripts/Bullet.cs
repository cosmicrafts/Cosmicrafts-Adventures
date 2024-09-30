using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    public float lifespan = 5f;
    private ulong shooterClientId;

    /// <summary>
    /// Initializes the bullet with the shooter's client ID.
    /// </summary>
    /// <param name="shooterId">The Client ID of the shooter.</param>
    public void Initialize(ulong shooterId)
    {
        shooterClientId = shooterId;
    }

    private void Start()
    {
        if (IsServer)
        {
            // Destroy the bullet after its lifespan on the server
            Invoke(nameof(DespawnBullet), lifespan);
        }

        // On clients, check if this bullet was shot by the local client
        if (IsClient && NetworkManager.Singleton.LocalClientId == shooterClientId)
        {
            // Destroy the bullet on the shooter client to avoid duplication
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        // Handle collision logic here (e.g., apply damage)

        // Destroy the bullet upon collision
        DespawnBullet();
    }

    private void DespawnBullet()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true); // Despawn and destroy the bullet
        }
    }
}
