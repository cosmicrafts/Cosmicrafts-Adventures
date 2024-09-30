using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    public float lifespan = 5f;

    private void Start()
    {
        if (IsServer)
        {
            // Destroy the bullet after its lifespan
            Invoke(nameof(DespawnBullet), lifespan);
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
