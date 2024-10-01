using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class Bullet : NetworkBehaviour
{
    public float lifespan = 5f;
    public GameObject impactEffectPrefab; // Impact effect prefab for collision effect
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

    /// <summary>
    /// Initializes the bullet with the shooter's client ID.
    /// </summary>
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
            // Destroy the bullet locally after its lifespan on the clients
            Destroy(gameObject, lifespan);
        }
        else if (IsServer)
        {
            // Destroy the bullet after its lifespan on the server
            Invoke(nameof(DespawnBullet), lifespan);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            // Handle bullet-to-bullet collision for both server and clients
            if (isLocalOnly)
            {
                HandleLocalCollision(collision.contacts[0].point);
            }
            else if (IsServer)
            {
                DespawnBullet();
            }
        }
        else
        {
            // Handle collisions with other objects (e.g., walls, targets)
            if (isLocalOnly)
            {
                HandleLocalCollision(collision.contacts[0].point);
            }
            else if (IsServer)
            {
                // Handle server-side collision logic here (e.g., apply damage)
                DespawnBullet();
            }
        }
    }

    /// <summary>
    /// Handles bullet despawning on the server.
    /// </summary>
    private void DespawnBullet()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            // Ensure that only the server despawns the network object
            NetworkObject.Despawn(true);
        }
    }

    /// <summary>
    /// Handles the local impact effect and bullet destruction.
    /// </summary>
    /// <param name="impactPoint">The point of impact where the effect should be instantiated.</param>
    private void HandleLocalCollision(Vector2 impactPoint)
    {
        // Instantiate impact effect locally
        if (impactEffectPrefab != null)
        {
            GameObject impactEffect = Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);
            Destroy(impactEffect, 0.1f); // Destroy the impact effect after a short delay
        }

        // Destroy the bullet locally
        Destroy(gameObject);
    }

    /// <summary>
    /// Hides the bullet for all clients except the server.
    /// </summary>
    [ClientRpc]
    public void HideForAllClientsClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!IsServer)
        {
            // Disable the renderer and collider to hide the bullet instead of destroying it
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
