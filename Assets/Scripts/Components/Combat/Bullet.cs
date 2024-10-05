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

        // Set the bullet tag (optional, but removed tag comparison logic)
        gameObject.tag = "Bullet";
    }

    public void Initialize(ulong shooterId, TeamComponent.TeamTag teamTag)
    {
        shooterClientId = shooterId;
        shooterTeamTag = teamTag;

        // Ignore collision between bullet and friendly team players
        foreach (var player in FindObjectsOfType<TeamComponent>())
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

        HealthComponent healthComponent = collision.gameObject.GetComponent<HealthComponent>();
        if (healthComponent != null)
        {

            if (IsServer)
            {
                if (healthComponent.NetworkObject != null)
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