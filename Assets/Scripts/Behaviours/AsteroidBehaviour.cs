using UnityEngine;

[CreateAssetMenu(fileName = "AsteroidBehavior", menuName = "Behaviors/Asteroid")]
public class AsteroidBehaviorSO : BehaviorSO
{
    public float rotationSpeed = 50f; // Example of asteroid-specific behavior

    public override void ApplyBehavior(GameObject asteroid)
    {
        // Apply rotation behavior
        Rigidbody2D rb = asteroid.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.angularVelocity = rotationSpeed;
        }
    }
}
