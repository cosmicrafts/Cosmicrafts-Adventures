using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class Bullet : NetworkBehaviour
{
    public float lifespan = 5f;
    public GameObject impactEffectPrefab; // Impact effect prefab for collision effect
    public float bulletDamage = 16f; // Amount of damage each bullet deals
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
    }

    public void Initialize(ulong shooterId, TeamComponent.TeamTag teamTag)
    {
        shooterClientId = shooterId;
        shooterTeamTag = teamTag;

        // Set the layer for the bullet based on the team
        switch (shooterTeamTag)
        {
            case TeamComponent.TeamTag.Friend:
                gameObject.layer = LayerMask.NameToLayer("Friend");
                break;
            case TeamComponent.TeamTag.Enemy:
                gameObject.layer = LayerMask.NameToLayer("Enemy");
                break;
            case TeamComponent.TeamTag.Neutral:
                gameObject.layer = LayerMask.NameToLayer("Neutral");
                break;
        }

        // Ignore collision between bullet and friendly team players
        foreach (var player in FindObjectsByType<TeamComponent>(FindObjectsSortMode.None))
        {
            if (player.GetTeam() == shooterTeamTag && shooterTeamTag == TeamComponent.TeamTag.Friend)
            {
                Collider2D playerCollider = player.GetComponent<Collider2D>();
                if (playerCollider != null && bulletCollider != null)
                {
                    Physics2D.IgnoreCollision(bulletCollider, playerCollider);
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
            Destroy(gameObject, lifespan);
        }
        else if (IsServer)
        {
            Invoke(nameof(DespawnBullet), lifespan);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isLocalOnly)
        {
            HandleLocalCollision(collision);
        }
        else if (IsServer)
        {
            HandleServerCollision(collision);
        }
    }

    private void HandleLocalCollision(Collision2D collision)
    {
        TeamComponent targetTeamComponent = collision.gameObject.GetComponent<TeamComponent>();

        // Ignore collision if it's a friend
        if (targetTeamComponent != null && targetTeamComponent.GetTeam() == shooterTeamTag && shooterTeamTag == TeamComponent.TeamTag.Friend)
        {
            return;
        }

        HandleCollisionEffect(collision.contacts[0].point);
        Destroy(gameObject);
    }

    private void HandleServerCollision(Collision2D collision)
    {
        TeamComponent targetTeamComponent = collision.gameObject.GetComponent<TeamComponent>();

        // Ignore collision if the target is a friend of the shooter
        if (targetTeamComponent != null && targetTeamComponent.GetTeam() == shooterTeamTag && shooterTeamTag == TeamComponent.TeamTag.Friend)
        {
            return;
        }

        HealthComponent healthComponent = collision.gameObject.GetComponent<HealthComponent>();
        if (healthComponent != null)
        {
            healthComponent.TakeDamageServerRpc(bulletDamage);
        }

        HandleCollisionEffect(collision.contacts[0].point);
        DespawnBullet();
    }

    private void HandleCollisionEffect(Vector2 impactPoint)
    {
        if (impactEffectPrefab != null)
        {
            GameObject impactEffect = Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);
            Destroy(impactEffect, 0.1f);
        }
    }

    private void DespawnBullet()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
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
