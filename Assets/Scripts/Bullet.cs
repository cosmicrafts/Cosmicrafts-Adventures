using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
public class Bullet : NetworkBehaviour
{
    public float lifespan = 5f;
    public GameObject impactEffectPrefab; // Impact effect prefab for collision effect
    public float bulletDamage = 1f; // Amount of damage each bullet deals
    private ulong shooterClientId;

    // Components to hide
    private Renderer bulletRenderer;
    private Collider2D bulletCollider;
    private bool isLocalOnly = false; // Flag to differentiate between networked and local-only bullets

    private void Awake()
    {
        bulletRenderer = GetComponent<Renderer>();
        bulletCollider = GetComponent<Collider2D>();

        // Set the bullet tag
        gameObject.tag = "Bullet";
    }

    public void Initialize(ulong shooterId)
    {
        shooterClientId = shooterId;
    }

    public void SetLocalOnly()
    {
        // Mark the bullet as local-only and disable network components
        isLocalOnly = true;

        // Remove or disable network components
        if (TryGetComponent<NetworkObject>(out var netObj))
        {
            Destroy(netObj);
        }

        if (TryGetComponent<NetworkRigidbody2D>(out var netRigidbody))
        {
            Destroy(netRigidbody);
        }
    }

    private void Start()
    {
        if (isLocalOnly)
        {
            Destroy(gameObject, lifespan);
        }
        else if (IsServer)
        {
            Invoke(nameof(DespawnBullet), lifespan);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            if (isLocalOnly)
            {
                HandleLocalCollision(collision.contacts[0].point);
            }
            else if (IsServer)
            {
                DespawnBullet();
            }
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            if (IsServer)
            {
                // Apply damage to the player if the collision happens on the server
                HealthComponent healthComponent = collision.gameObject.GetComponent<HealthComponent>();
                if (healthComponent != null)
                {
                    healthComponent.TakeDamageServerRpc(bulletDamage);
                }

                DespawnBullet();
            }
            else if (isLocalOnly)
            {
                HandleLocalCollision(collision.contacts[0].point);
            }
        }
        else
        {
            if (isLocalOnly)
            {
                HandleLocalCollision(collision.contacts[0].point);
            }
            else if (IsServer)
            {
                DespawnBullet();
            }
        }
    }

    private void DespawnBullet()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    private void HandleLocalCollision(Vector2 impactPoint)
    {
        if (impactEffectPrefab != null)
        {
            GameObject impactEffect = Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);
            Destroy(impactEffect, 0.1f);
        }

        Destroy(gameObject);
    }

    [ClientRpc]
    public void HideForAllClientsClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!IsServer)
        {
            if (bulletRenderer != null)
            {
                bulletRenderer.enabled = false;
            }

            if (bulletCollider != null)
            {
                bulletCollider.enabled = false;
            }
        }
    }
}
