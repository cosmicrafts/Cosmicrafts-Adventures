using UnityEngine;
using Unity.Netcode;

public class AsteroidBehavior : NetworkBehaviour
{
    public GameObject explosionPrefab; // Reference to explosion prefab
    public float explosionDestroyDelay = 0.5f; // Time to destroy the explosion effect
    private HealthComponent healthComponent;
    private MovementComponent movementComponent;
    private RotationComponent rotationComponent;

    private int currentHealth;

    private void Start()
    {
        healthComponent = GetComponent<HealthComponent>();
        movementComponent = GetComponent<MovementComponent>();
        rotationComponent = GetComponent<RotationComponent>();

        // Get ObjectLoader to apply the ObjectSO configuration
        ObjectLoader objectLoader = GetComponent<ObjectLoader>();

        if (healthComponent != null && objectLoader != null)
        {
            // Apply health configuration from ObjectSO
            healthComponent.ApplyConfiguration(objectLoader.objectConfiguration);
            if (IsServer)
            {
                currentHealth = (int)healthComponent.maxHealth;
            }
        }

        // Apply movement and rotation configuration from ObjectSO
        if (movementComponent != null && objectLoader != null)
        {
            movementComponent.ApplyConfiguration(objectLoader.objectConfiguration);
        }

        if (rotationComponent != null && objectLoader != null)
        {
            rotationComponent.ApplyConfiguration(objectLoader.objectConfiguration);
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            // Optional: You can implement asteroid movement here if required for AI-like behavior
            Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            movementComponent.SetMoveInput(randomDirection);  // Use the movement component to apply random movement
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Explode();
        }
    }

    private void Explode()
    {
        // Instantiate the explosion effect and destroy it after a fixed delay
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, explosionDestroyDelay); // Destroy the explosion effect after delay
        }

        Destroy(gameObject); // Destroy the asteroid
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TakeDamage(1); // Take 1 damage if hit by player
        }
    }
}
