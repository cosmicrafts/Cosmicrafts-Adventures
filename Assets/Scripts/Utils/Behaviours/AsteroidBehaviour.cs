using UnityEngine;

[CreateAssetMenu(fileName = "AsteroidBehavior", menuName = "Behaviors/Asteroid")]
public class AsteroidBehaviorSO : BehaviorSO
{
    [Header("Drift Settings")]
    public float minDriftSpeed = 0.5f;
    public float maxDriftSpeed = 2f;

    [Header("Rotation Settings")]
    public float minRotationSpeed = 10f;
    public float maxRotationSpeed = 100f;

    public override void ApplyBehavior(GameObject asteroid)
    {
        // Set random drift direction
        Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

        // Set random drift speed between min and max
        float randomDriftSpeed = Random.Range(minDriftSpeed, maxDriftSpeed);

        // Register the asteroid with the manager to batch its movement
        AsteroidManager.Instance.RegisterAsteroid(asteroid, randomDirection, randomDriftSpeed);

        // Apply random rotation if Rigidbody2D is present
        Rigidbody2D rb = asteroid.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float randomRotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
            rb.angularVelocity = randomRotationSpeed; // Set angular velocity for rotation
        }
    }
}
