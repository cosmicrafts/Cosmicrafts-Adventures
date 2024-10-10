using UnityEngine;

[CreateAssetMenu(fileName = "AsteroidBehavior", menuName = "Behaviors/Asteroid")]
public class AsteroidBehaviorSO : BehaviorSO
{
    public float driftSpeed = 1f;
    public float rotationSpeed = 50f;  // Rotation speed

    public override void ApplyBehavior(GameObject asteroid)
    {
        Rigidbody2D rb = asteroid.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Set random drift direction
            Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            asteroid.GetComponent<MonoBehaviour>().StartCoroutine(Drift(asteroid, rb, randomDirection));

            // Set the rotation
            rb.angularVelocity = rotationSpeed;
        }
    }

    private System.Collections.IEnumerator Drift(GameObject asteroid, Rigidbody2D rb, Vector2 direction)
    {
        while (asteroid != null)
        {
            // Apply drift movement continuously
            rb.linearVelocity = direction * driftSpeed;
            yield return null;  // Run every frame
        }
    }
}
