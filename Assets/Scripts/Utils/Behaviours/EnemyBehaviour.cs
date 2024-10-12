using UnityEngine;

[CreateAssetMenu(fileName = "EnemyBehavior", menuName = "Behaviors/Enemy")]
public class EnemyBehaviorSO : BehaviorSO
{
    public float enemySpeed = 3f;
    public float detectionRange = 10f;

    public override void ApplyBehavior(GameObject enemy)
    {
        MovementComponent movement = enemy.GetComponent<MovementComponent>();
        Transform player = GameObject.FindWithTag("Player")?.transform;

        if (movement != null && player != null)
        {
            enemy.GetComponent<MonoBehaviour>().StartCoroutine(MoveTowardPlayer(enemy, player, movement));
        }
    }

    private System.Collections.IEnumerator MoveTowardPlayer(GameObject enemy, Transform player, MovementComponent movement)
    {
        while (enemy != null && player != null)
        {
            Vector2 direction = (player.position - enemy.transform.position).normalized;
            float distance = Vector2.Distance(enemy.transform.position, player.position);

            // Move toward the player if within detection range
            if (distance <= detectionRange)
            {
                movement.SetMoveInput(direction);  // Move toward the player
            }
            else
            {
                movement.SetMoveInput(Vector2.zero);  // Stop movement if out of range
            }

            yield return new WaitForSeconds(0.1f);  // Update movement every 0.1 seconds
        }
    }
}
