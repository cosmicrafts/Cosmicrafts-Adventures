using UnityEngine;

[CreateAssetMenu(fileName = "AsteroidBehavior", menuName = "Behaviors/Asteroid")]
public class AsteroidBehaviorSO : BehaviorSO
{
    public enum MovementType
    {
        Linear,
        Circular,
        Spiral,
        RandomJitter,
        ZigZag
    }

    [Header("Movement Settings")]
    public MovementType movementType = MovementType.Linear;  // Choose movement type

    [Header("Linear Movement Settings")]
    public float minDriftSpeed = 0.5f;
    public float maxDriftSpeed = 2f;

    [Header("Circular/Gravitational Movement Settings")]
    public float minCircularSpeed = 0.5f;
    public float maxCircularSpeed = 2f;
    public float orbitRadius = 5f;

    [Header("Spiral Movement Settings")]
    public float minSpiralSpeed = 0.5f;
    public float maxSpiralSpeed = 2f;
    public float spiralGrowthRate = 0.1f;

    [Header("Zig-Zag Movement Settings")]
    public float minZigZagSpeed = 1f;
    public float maxZigZagSpeed = 2f;
    public float zigZagFrequency = 1f;

    [Header("Rotation Settings")]
    public float minRotationSpeed = 10f;
    public float maxRotationSpeed = 100f;

    [Header("Randomization")]
    public float randomStartDelay = 0.5f;  // Random delay between asteroids' movements

    public override void ApplyBehavior(GameObject asteroid)
    {
        // Add random delay to movement start to avoid synchronized movement
        float delay = Random.Range(0f, randomStartDelay);
        asteroid.GetComponent<MonoBehaviour>().StartCoroutine(StartMovementAfterDelay(asteroid, delay));

        // Apply random rotation between min and max values and randomize direction
        Rigidbody2D rb = asteroid.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float randomRotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
            int randomDirection = Random.value > 0.5f ? 1 : -1; // Randomize clockwise or counterclockwise
            rb.angularVelocity = randomRotationSpeed * randomDirection; // Set angular velocity for rotation
        }
    }

    // Coroutine to start movement after a random delay
    private System.Collections.IEnumerator StartMovementAfterDelay(GameObject asteroid, float delay)
    {
        yield return new WaitForSeconds(delay);

        switch (movementType)
        {
            case MovementType.Linear:
                ApplyLinearMovement(asteroid);
                break;
            case MovementType.Circular:
                ApplyCircularMovement(asteroid);
                break;
            case MovementType.Spiral:
                ApplySpiralMovement(asteroid);
                break;
            case MovementType.RandomJitter:
                ApplyRandomJitterMovement(asteroid);
                break;
            case MovementType.ZigZag:
                ApplyZigZagMovement(asteroid);
                break;
        }
    }

    // Linear movement
    private void ApplyLinearMovement(GameObject asteroid)
    {
        Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        float randomDriftSpeed = Random.Range(minDriftSpeed, maxDriftSpeed);
        AsteroidManager.Instance.RegisterAsteroid(asteroid, randomDirection, randomDriftSpeed);
    }

    // Circular movement
    private void ApplyCircularMovement(GameObject asteroid)
    {
        Vector2 center = asteroid.transform.position; // Center of the circular orbit
        float randomCircularSpeed = Random.Range(minCircularSpeed, maxCircularSpeed); // Random circular speed
        AsteroidManager.Instance.RegisterAsteroidForCircularMovement(asteroid, center, randomCircularSpeed, orbitRadius);
    }

    // Spiral movement
    private void ApplySpiralMovement(GameObject asteroid)
    {
        Vector2 initialDirection = Vector2.up; // Initial direction of spiral
        float randomSpiralSpeed = Random.Range(minSpiralSpeed, maxSpiralSpeed); // Random spiral speed
        AsteroidManager.Instance.RegisterAsteroidForSpiralMovement(asteroid, initialDirection, randomSpiralSpeed, spiralGrowthRate);
    }

    // Random jitter movement
    private void ApplyRandomJitterMovement(GameObject asteroid)
    {
        Vector2 randomDirection = Vector2.zero; // Jitter moves randomly every frame
        float jitterSpeed = Random.Range(minDriftSpeed, maxDriftSpeed);
        AsteroidManager.Instance.RegisterAsteroidForRandomJitter(asteroid, randomDirection, jitterSpeed);
    }

    // Zig-zag movement
    private void ApplyZigZagMovement(GameObject asteroid)
    {
        Vector2 initialDirection = Vector2.right; // Zig-Zag starts moving right
        float randomZigZagSpeed = Random.Range(minZigZagSpeed, maxZigZagSpeed);
        AsteroidManager.Instance.RegisterAsteroidForZigZag(asteroid, initialDirection, randomZigZagSpeed, zigZagFrequency);
    }
}
