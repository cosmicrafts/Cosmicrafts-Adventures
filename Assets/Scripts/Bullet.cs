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

        // Set the bullet tag (optional, but removed tag comparison logic)
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
        Debug.Log($"[Bullet] OnCollisionEnter2D with {collision.gameObject.name}");

        HealthComponent healthComponent = collision.gameObject.GetComponent<HealthComponent>();
        if (healthComponent != null)
        {
            Debug.Log($"[Bullet] Found HealthComponent on {collision.gameObject.name}. Applying damage...");

            if (IsServer)
            {
                if (healthComponent.NetworkObject != null)
                {
                    Debug.Log($"[Bullet] Calling TakeDamageServerRpc for {collision.gameObject.name} with {bulletDamage} damage.");
                    healthComponent.TakeDamageServerRpc(bulletDamage);
                }
                else
                {
                    Debug.LogWarning($"[Bullet] HealthComponent does not have a NetworkObject on {collision.gameObject.name}.");
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
            Debug.Log($"[Bullet] No HealthComponent found on {collision.gameObject.name}. Handling general collision.");

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
