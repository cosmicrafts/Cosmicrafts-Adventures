using UnityEngine;
using TMPro; // Import TextMeshPro namespace
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    public GameObject bulletImpactPrefab; // Reference to impact prefab
    public GameObject damageTextPrefab; // Reference to damage text prefab for visual confirmation
    public float impactDestroyDelay = 0.5f; // Time to destroy the impact effect
    public int damage = 1; // Damage the bullet deals
    public float lifespan = 12f; // Time before the bullet is automatically destroyed

    private void Start()
    {
        // Destroy the bullet after its lifespan
        Destroy(gameObject, lifespan);

        // Ignore collision between bullet and the player that fired it
        if (IsOwner)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Collider2D bulletCollider = GetComponent<Collider2D>();
                Collider2D playerCollider = player.GetComponent<Collider2D>();

                if (bulletCollider != null && playerCollider != null)
                {
                    Physics2D.IgnoreCollision(bulletCollider, playerCollider);
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return; // Only handle collision logic on the server

        if (collision.gameObject.CompareTag("Player"))
        {
            return; // Do nothing if it hits the player
        }

        // Handle collisions with asteroids or other objects
        if (collision.gameObject.CompareTag("Asteroid"))
        {
            Health health = collision.gameObject.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
                ShowDamageTextClientRpc(collision.transform.position, damage);
            }

            // Instantiate the impact effect
            GameObject impact = Instantiate(bulletImpactPrefab, transform.position, Quaternion.identity);
            NetworkObject networkImpact = impact.GetComponent<NetworkObject>();
            if (networkImpact != null)
            {
                networkImpact.Spawn();
            }
            Destroy(impact, impactDestroyDelay);
        }

        // Destroy the bullet on impact
        Destroy(gameObject);
    }

    // Method to instantiate and animate the damage text on all clients
    [ClientRpc]
    void ShowDamageTextClientRpc(Vector3 position, int damage)
    {
        GameObject damageText = Instantiate(damageTextPrefab, position, Quaternion.identity);
        TMP_Text textMesh = damageText.GetComponent<TMP_Text>();

        if (textMesh != null)
        {
            textMesh.text = damage.ToString(); // Display the damage amount
        }
        else
        {
            Debug.LogError("Missing TMP_Text component on damage text prefab!");
        }

        Destroy(damageText, .25f); // Destroy the damage text after 0.25 seconds
    }
}
