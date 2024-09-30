using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class Bullet : NetworkBehaviour
{
    public float lifespan = 5f;
    private ulong shooterClientId;

    // Components to hide
    private Renderer bulletRenderer;
    private Collider2D bulletCollider;

    // For interpolation
    private Vector2 previousPosition;
    private Vector2 currentPosition;
    private float interpolationSpeed = 10f; // Adjust this to control the smoothness

    private void Awake()
    {
        bulletRenderer = GetComponent<Renderer>();
        bulletCollider = GetComponent<Collider2D>();
    }

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
    }

    private void Update()
    {
        if (IsClient && !IsOwner)
        {
            // Interpolation logic to smooth the bullet position
            previousPosition = currentPosition;
            currentPosition = transform.position;
            transform.position = Vector2.Lerp(previousPosition, currentPosition, Time.deltaTime * interpolationSpeed);
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

    /// <summary>
    /// Hides the bullet for the shooter client.
    /// </summary>
    /// <param name="shooterClientId">The client ID of the shooter.</param>
    [ClientRpc]
    public void HideForShooterClientRpc(ulong shooterClientId, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId == shooterClientId)
        {
            // Delay the destruction or hiding of the client bullet
            StartCoroutine(DelayedHide(0.2f)); // Delay for 0.2 seconds or any duration you prefer
        }
    }

    private IEnumerator DelayedHide(float delay)
    {
        yield return new WaitForSeconds(delay);

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
