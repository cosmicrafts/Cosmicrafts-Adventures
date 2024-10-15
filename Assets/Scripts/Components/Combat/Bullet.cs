using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class Bullet : NetworkBehaviour
{
    public float lifespan = 5f;
    public GameObject impactEffectPrefab; // Impact effect prefab for collision
    public float bulletDamage = 16f;
    private ulong shooterClientId;
    private TeamComponent.TeamTag shooterTeamTag;

    // Components to hide
    private Renderer bulletRenderer;
    private Collider2D bulletCollider;
    private bool isLocalOnly = false;

    private void Awake()
    {
        bulletRenderer = GetComponent<Renderer>();
        bulletCollider = GetComponent<Collider2D>();

        // Set the bullet tag
        gameObject.tag = "Bullet";
    }

    public void Initialize(ulong shooterId, TeamComponent.TeamTag teamTag)
    {
        shooterClientId = shooterId;
        shooterTeamTag = teamTag;

        // Ignore collision between bullet and entities on the same team
        foreach (var entity in FindObjectsByType<TeamComponent>(FindObjectsSortMode.None))
        {
            if (entity.GetTeam() == shooterTeamTag)
            {
                Collider2D entityCollider = entity.GetComponent<Collider2D>();
                if (entityCollider != null && bulletCollider != null)
                {
                    Physics2D.IgnoreCollision(bulletCollider, entityCollider);
                }
            }
        }
    }

    public void SetLocalOnly()
    {
        isLocalOnly = true;

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
            Destroy(gameObject, lifespan); // Auto-destroy after lifespan locally
        }
        else if (IsServer)
        {
            Invoke(nameof(DespawnBullet), lifespan); // Auto-despawn after lifespan on the server
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HealthComponent healthComponent = collision.gameObject.GetComponent<HealthComponent>();

        if (healthComponent != null)
        {
            if (IsServer)
            {
                healthComponent.TakeDamageServerRpc(bulletDamage); // Apply damage on the server
                HandleImpact(collision.contacts[0].point);
            }
            else if (isLocalOnly)
            {
                HandleLocalCollision(collision.contacts[0].point); // Local-only collision
            }
        }
        else
        {
            if (isLocalOnly)
            {
                HandleLocalCollision(collision.contacts[0].point); // Local collision
            }
            else if (IsServer)
            {
                HandleImpact(collision.contacts[0].point); // Server-side collision
            }
        }
    }

    private void DespawnBullet()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }

        Destroy(gameObject); // Ensure complete destruction
    }

    private void HandleImpact(Vector2 impactPoint)
    {
        // Spawn impact effect
        if (impactEffectPrefab != null)
        {
            GameObject impactEffect = Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);
            Destroy(impactEffect, 0.25f); // Clean up impact effect
        }

        DespawnBullet(); // Ensure bullet cleanup
    }

    private void HandleLocalCollision(Vector2 impactPoint)
    {
        if (impactEffectPrefab != null)
        {
            GameObject impactEffect = Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);
            Destroy(impactEffect, 0.25f); // Clean up impact effect
        }

        Destroy(gameObject); // Local-only destruction
    }

    [ClientRpc]
    public void HideForAllClientsClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!IsServer || IsHost)
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
