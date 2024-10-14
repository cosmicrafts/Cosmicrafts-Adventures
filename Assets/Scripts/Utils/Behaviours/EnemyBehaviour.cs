using UnityEngine;

[CreateAssetMenu(fileName = "EnemyBehavior", menuName = "Behaviors/Enemy")]
public class EnemyBehaviorSO : BehaviorSO
{
    public enum MovementType
    {
        Patrol,
        Chase,
        Stationary
    }

    [Header("Movement Settings")]
    public MovementType movementType = MovementType.Patrol;  // Choose movement type
    public float moveSpeed = 3f;

    [Header("Attack Settings")]
    public float attackRange = 10f;
    public float shootingCooldown = 1f;  // Cooldown between attacks

    [Header("Aggro Settings")]
    public float aggroRange = 15f;

    public override void ApplyBehavior(GameObject enemy)
    {
        // Register the enemy with the manager for batch processing
        EnemyManager.Instance.RegisterEnemy(enemy, this);
    }
}
