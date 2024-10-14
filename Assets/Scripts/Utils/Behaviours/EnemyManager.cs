using System.Collections.Generic;
using UnityEngine;



public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;
    public GameObject bulletPrefab;

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
            HandleShooting(enemyData, deltaTime);
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
    GameObject target = FindClosestTarget(enemyData.enemy, enemyData.behavior.attackRange);
    
    if (target != null && enemyData.shootingCooldownTimer <= 0f)
    {
        // Get the ShootingComponent from the enemy
        var shootingComponent = enemyData.enemy.GetComponent<ShootingComponent>();

        if (shootingComponent != null)
        {
            shootingComponent.RequestShoot();  // Enemy shoots via ShootingComponent
        }

        enemyData.shootingCooldownTimer = enemyData.behavior.shootingCooldown;
    }
    else
    {
        enemyData.shootingCooldownTimer -= deltaTime;
    }
}



    // Example patrol movement (move back and forth)
    private void Patrol(GameObject enemy, float speed, float deltaTime)
    {
        // Patrol logic (simplified)
        enemy.transform.Translate(Vector2.left * speed * deltaTime);
    }

    // Example chase movement (move toward closest target)
    private void Chase(EnemyData enemyData, float deltaTime)
    {
        GameObject target = FindClosestTarget(enemyData.enemy, enemyData.behavior.aggroRange);
        if (target != null)
        {
            Vector2 direction = (target.transform.position - enemyData.enemy.transform.position).normalized;
            enemyData.enemy.transform.Translate(direction * enemyData.behavior.moveSpeed * deltaTime);
        }
    }


    // Shooting logic
private void ShootAtTarget(GameObject enemy, GameObject target)
{
    // Handle shooting with the ShootingComponent
    var shootingComponent = enemy.GetComponent<ShootingComponent>();
    if (shootingComponent != null)
    {
        shootingComponent.RequestShoot();  // Use the existing shooting logic
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

        public EnemyData(GameObject enemy, EnemyBehaviorSO behavior)
        {
            this.enemy = enemy;
            this.behavior = behavior;
            this.shootingCooldownTimer = 0f;
        }
    }
}
