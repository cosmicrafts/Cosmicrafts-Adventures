using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    private List<EnemyData> enemies = new List<EnemyData>();

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterEnemy(GameObject enemy, EnemyBehaviorSO behavior)
    {
        enemies.Add(new EnemyData(enemy, behavior));
    }

    public void UnregisterEnemy(GameObject enemy)
    {
        enemies.RemoveAll(e => e.enemy == enemy);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        foreach (var enemyData in enemies)
        {
            HandleMovement(enemyData, deltaTime);
            HandleRotation(enemyData, deltaTime);
            HandleShooting(enemyData, deltaTime);  // Call shooting continuously
        }
    }

    private void HandleMovement(EnemyData enemyData, float deltaTime)
    {
        switch (enemyData.behavior.movementType)
        {
            case EnemyBehaviorSO.MovementType.Patrol:
                Patrol(enemyData.enemy, enemyData.behavior.moveSpeed, deltaTime);
                break;

            case EnemyBehaviorSO.MovementType.Chase:
                Chase(enemyData, deltaTime);
                break;

            case EnemyBehaviorSO.MovementType.Stationary:
                // No movement if stationary
                break;
        }
    }

private void HandleShooting(EnemyData enemyData, float deltaTime)
{
    // Find the closest target within the attack range
    GameObject target = FindClosestTarget(enemyData.enemy, enemyData.behavior.attackRange);

    if (target != null)  // Ensure there is a valid target in range
    {
        // Decrease the cooldown timer each frame
        if (enemyData.shootingCooldownTimer > 0f)
        {
            enemyData.shootingCooldownTimer -= deltaTime;  // Count down
        }

        // Shoot when cooldown reaches 0
        if (enemyData.shootingCooldownTimer <= 0f)
        {
            // Get the ShootingComponent from the enemy
            var shootingComponent = enemyData.enemy.GetComponent<ShootingComponent>();

            if (shootingComponent != null)
            {
                // Trigger the shooting and reset the cooldown
                shootingComponent.RequestShoot();  // Enemy shoots via ShootingComponent
                enemyData.shootingCooldownTimer = enemyData.behavior.shootingCooldown;  // Reset cooldown
            }
        }
    }
    else
    {
        // Optionally handle case where target is lost (optional)
        enemyData.shootingCooldownTimer = 0f;  // Reset cooldown to shoot again immediately if target reappears
    }
}


private void Chase(EnemyData enemyData, float deltaTime)
{
    GameObject target = FindClosestTarget(enemyData.enemy, enemyData.behavior.aggroRange);
    if (target != null)
    {
        float distanceToTarget = Vector2.Distance(enemyData.enemy.transform.position, target.transform.position);

        // Only move toward the target if it's outside the attack range
        if (distanceToTarget > enemyData.behavior.attackRange)
        {
            Vector2 direction = (target.transform.position - enemyData.enemy.transform.position).normalized;
            enemyData.enemy.transform.Translate(direction * enemyData.behavior.moveSpeed * deltaTime);
            enemyData.lastMovementDirection = direction;  // Store direction for rotation
        }

        // Handle shooting even if chasing
        HandleShooting(enemyData, deltaTime);  // Ensure continuous shooting logic
    }
    else
    {
        // If no target, stop shooting and cooldown resets (optional)
        enemyData.shootingCooldownTimer = 0f;
    }
}



    // Rotates the enemy towards its movement or shooting direction
    private void HandleRotation(EnemyData enemyData, float deltaTime)
    {
        // For patrol or chase, rotate towards movement direction
        if (enemyData.behavior.movementType != EnemyBehaviorSO.MovementType.Stationary)
        {
            Vector2 movementDirection = enemyData.lastMovementDirection;
            RotateTowardsDirection(enemyData.enemy, movementDirection, deltaTime);
        }

        // If shooting, rotate towards the target
        GameObject target = FindClosestTarget(enemyData.enemy, enemyData.behavior.attackRange);
        if (target != null)
        {
            Vector2 targetDirection = (target.transform.position - enemyData.enemy.transform.position).normalized;
            RotateTowardsDirection(enemyData.enemy, targetDirection, deltaTime);
        }
    }

    // Smooth rotation logic
    private void RotateTowardsDirection(GameObject enemy, Vector2 direction, float deltaTime)
    {
        if (direction == Vector2.zero) return; // Don't rotate if no direction

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

        // Smooth rotation
        enemy.transform.rotation = Quaternion.Lerp(
            enemy.transform.rotation, targetRotation, deltaTime * 5f // You can adjust the rotation speed
        );
    }

    // Example patrol movement (move back and forth)
    private void Patrol(GameObject enemy, float speed, float deltaTime)
    {
        // Patrol logic (simplified)
        Vector2 patrolDirection = Vector2.left; // Example patrol direction
        enemy.transform.Translate(patrolDirection * speed * deltaTime);

        // Set the last movement direction directly on the enemyData
        foreach (var enemyData in enemies)
        {
            if (enemyData.enemy == enemy)
            {
                enemyData.lastMovementDirection = patrolDirection; // Store the last movement direction
                break;
            }
        }
    }


    // Find the closest target in range
    private GameObject FindClosestTarget(GameObject enemy, float range)
    {
        GameObject closestTarget = null;
        float closestDistance = Mathf.Infinity;

        // Iterate through potential targets
        foreach (var target in GameObject.FindGameObjectsWithTag("Player")) // Or another tag
        {
            float distance = Vector2.Distance(enemy.transform.position, target.transform.position);
            if (distance < range && distance < closestDistance)
            {
                closestTarget = target;
                closestDistance = distance;
            }
        }

        return closestTarget;
    }

    private class EnemyData
    {
        public GameObject enemy;
        public EnemyBehaviorSO behavior;
        public float shootingCooldownTimer;
        public Vector2 lastMovementDirection;  // Store the last movement direction for rotation

        public EnemyData(GameObject enemy, EnemyBehaviorSO behavior)
        {
            this.enemy = enemy;
            this.behavior = behavior;
            this.shootingCooldownTimer = 0f;
            this.lastMovementDirection = Vector2.zero;  // Initial direction
        }
    }
}
