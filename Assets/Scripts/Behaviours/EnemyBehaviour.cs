using UnityEngine;

[CreateAssetMenu(fileName = "EnemyBehavior", menuName = "Behaviors/Enemy")]
public class EnemyBehaviorSO : BehaviorSO
{
    public float enemySpeed = 3f;

    public override void ApplyBehavior(GameObject enemy)
    {
        // Apply enemy-specific behavior like movement speed
        MovementComponent movement = enemy.GetComponent<MovementComponent>();
        if (movement != null)
        {
            movement.moveSpeed = enemySpeed;
        }
    }
}
