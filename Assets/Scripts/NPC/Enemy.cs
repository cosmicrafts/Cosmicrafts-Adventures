using UnityEngine;
using Unity.Netcode;

public class EnemyAIBehavior : NetworkBehaviour
{
    private HealthComponent healthComponent;
    private MovementComponent movementComponent;
    private ShootingComponent shootingComponent;
    private Transform player;

    private void Start()
    {
        // Find the player object by tag
        player = GameObject.FindWithTag("Player")?.transform;

        // Get the necessary components
        healthComponent = GetComponent<HealthComponent>();
        movementComponent = GetComponent<MovementComponent>();
        shootingComponent = GetComponent<ShootingComponent>();

        // Get the ObjectLoader to apply configuration from ObjectSO
        ObjectLoader objectLoader = GetComponent<ObjectLoader>();

        if (healthComponent != null && objectLoader != null)
        {
            healthComponent.ApplyConfiguration(objectLoader.objectConfiguration);
            if (IsServer)
            {
                healthComponent.currentHealth.Value = healthComponent.maxHealth;
            }
        }

        if (movementComponent != null && objectLoader != null)
        {
            movementComponent.ApplyConfiguration(objectLoader.objectConfiguration);
        }

        if (shootingComponent != null && objectLoader != null)
        {
            shootingComponent.ApplyConfiguration(objectLoader.objectConfiguration);
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer || player == null) return;

        // Use the MoveInput for AI movement
        if (movementComponent != null)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            movementComponent.SetMoveInput(direction);  // Set movement input directly to the MovementComponent
        }

        // Check for shooting range and call RequestShoot if in range
        if (shootingComponent != null)
        {
            // For AI, you can define a fixed shooting range and check against player distance
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= 10f)  // Define your AI shooting range here
            {
                shootingComponent.RequestShoot();  // Use the existing RequestShoot method in ShootingComponent
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage)
    {
        if (IsServer && healthComponent != null)
        {
            healthComponent.TakeDamageServerRpc(damage);  // Call the existing TakeDamage logic in HealthComponent
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Bullet bullet = collision.gameObject.GetComponent<Bullet>();
            if (bullet != null)
            {
                TakeDamageServerRpc(bullet.bulletDamage);  // Apply bullet damage
                Destroy(collision.gameObject);  // Destroy the bullet
            }
        }
    }
}
