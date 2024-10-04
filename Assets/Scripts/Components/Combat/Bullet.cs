using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    public float lifespan = 5f;
    public GameObject impactEffectPrefab; // Impact effect prefab for collision effect
    public float bulletDamage = 16f;

    private void Start()
    {
        // Destroy bullet after lifespan (server should handle despawning)
        if (IsServer)
        {
            Invoke(nameof(DespawnBullet), lifespan);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HealthComponent healthComponent = collision.gameObject.GetComponent<HealthComponent>();

        if (healthComponent != null)
        {
            if (IsOwner)
            {
                // Only the owner can request to apply damage
                healthComponent.TakeDamageServerRpc(bulletDamage);
                RequestBulletDespawnServerRpc();
            }
        }
        else if (IsOwner)
        {
            // Handle collision effects if bullet collides with something else
            HandleCollision(collision.contacts[0].point);
            RequestBulletDespawnServerRpc();
        }
    }

    [ServerRpc]
    private void RequestBulletDespawnServerRpc()
    {
        // Server handles bullet despawn
        DespawnBullet();
    }

    private void DespawnBullet()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    private void HandleCollision(Vector2 impactPoint)
    {
        if (impactEffectPrefab != null)
        {
            GameObject impactEffect = Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);
            Destroy(impactEffect, 0.1f);
        }
    }
}
