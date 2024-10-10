using UnityEngine;

[CreateAssetMenu(fileName = "AsteroidBehavior", menuName = "Behaviors/Asteroid")]
public class AsteroidBehaviorSO : BehaviorSO
{
    [Header("Drift Settings")]
    public float minDriftSpeed = 0.5f;  // Minimum drift speed
    public float maxDriftSpeed = 2f;    // Maximum drift speed

    [Header("Rotation Settings")]
    public float minRotationSpeed = 10f;  // Minimum rotation speed
    public float maxRotationSpeed = 100f; // Maximum rotation speed

    public override void ApplyBehavior(GameObject asteroid)
    {
        Rigidbody2D rb = asteroid.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Set random drift direction
            Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

            // Set random drift speed between min and max
            float randomDriftSpeed = Random.Range(minDriftSpeed, maxDriftSpeed);
            asteroid.GetComponent<MonoBehaviour>().StartCoroutine(Drift(asteroid, rb, randomDirection, randomDriftSpeed));

            // Set random rotation speed between min and max
            float randomRotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
            rb.angularVelocity = randomRotationSpeed;
        }
    }

    private System.Collections.IEnumerator Drift(GameObject asteroid, Rigidbody2D rb, Vector2 direction, float driftSpeed)
    {
        while (asteroid != null)
        {
            // Apply drift movement continuously
            rb.linearVelocity = direction * driftSpeed;
            yield return null;  // Run every frame
        }
    }
}
