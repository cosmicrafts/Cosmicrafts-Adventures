using System.Collections.Generic;
using UnityEngine;

public class AsteroidManager : MonoBehaviour
{
    public static AsteroidManager Instance;
    private List<AsteroidData> asteroids = new List<AsteroidData>();

    private void Awake()
    {
        Instance = this;
    }

    public enum MovementType
    {
        Linear,
        Circular,
        Spiral,
        RandomJitter,
        ZigZag
    }

    // Registers an asteroid for circular movement
    public void RegisterAsteroidForCircularMovement(GameObject asteroid, Vector2 center, float speed, float radius)
    {
        int randomDirection = Random.value > 0.5f ? 1 : -1; // Randomize direction
        asteroids.Add(new AsteroidData(asteroid, Vector2.zero, speed * randomDirection, MovementType.Circular, center, radius));
    }

    // Registers an asteroid for spiral movement
    public void RegisterAsteroidForSpiralMovement(GameObject asteroid, Vector2 initialDirection, float speed, float growthRate)
    {
        int randomDirection = Random.value > 0.5f ? 1 : -1; // Randomize direction
        asteroids.Add(new AsteroidData(asteroid, initialDirection, speed * randomDirection, MovementType.Spiral, Vector2.zero, growthRate));
    }

    // Registers a linear moving asteroid
    public void RegisterAsteroid(GameObject asteroid, Vector2 direction, float driftSpeed)
    {
        int randomDirection = Random.value > 0.5f ? 1 : -1; // Randomize direction
        asteroids.Add(new AsteroidData(asteroid, direction * randomDirection, driftSpeed, MovementType.Linear));
    }

    // Registers an asteroid for random jitter movement
    public void RegisterAsteroidForRandomJitter(GameObject asteroid, Vector2 direction, float speed)
    {
        asteroids.Add(new AsteroidData(asteroid, direction, speed, MovementType.RandomJitter));
    }

    // Registers an asteroid for zigzag movement
    public void RegisterAsteroidForZigZag(GameObject asteroid, Vector2 initialDirection, float speed, float frequency)
    {
        asteroids.Add(new AsteroidData(asteroid, initialDirection, speed, MovementType.ZigZag, Vector2.zero, frequency));
    }

    // Update function to move asteroids
    private void Update()
    {
        float deltaTime = Time.deltaTime;

        // Iterate backwards to safely remove items from the list
        for (int i = asteroids.Count - 1; i >= 0; i--)
        {
            var asteroidData = asteroids[i];

            if (asteroidData.asteroid == null)
            {
                // The asteroid has been destroyed; remove it from the list
                asteroids.RemoveAt(i);
                continue;
            }

            // Update asteroid's position based on movement type
            switch (asteroidData.movementType)
            {
                case MovementType.Linear:
                    asteroidData.asteroid.transform.Translate(asteroidData.direction * asteroidData.driftSpeed * deltaTime);
                    break;

                case MovementType.Circular:
                    asteroidData.angle += asteroidData.driftSpeed * deltaTime;
                    asteroidData.asteroid.transform.position = asteroidData.center + new Vector2(Mathf.Cos(asteroidData.angle), Mathf.Sin(asteroidData.angle)) * asteroidData.radius;
                    break;

                case MovementType.Spiral:
                    asteroidData.angle += asteroidData.driftSpeed * deltaTime;
                    asteroidData.radius += asteroidData.growthRate * deltaTime; // Spiral outwards
                    asteroidData.asteroid.transform.position += new Vector3(Mathf.Cos(asteroidData.angle), Mathf.Sin(asteroidData.angle), 0) * asteroidData.radius * deltaTime;
                    break;

                case MovementType.RandomJitter:
                    asteroidData.direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
                    asteroidData.asteroid.transform.Translate(asteroidData.direction * asteroidData.driftSpeed * deltaTime);
                    break;

                case MovementType.ZigZag:
                    asteroidData.angle += asteroidData.driftSpeed * asteroidData.frequency * deltaTime;
                    float zigZagX = Mathf.Sin(asteroidData.angle);
                    asteroidData.asteroid.transform.Translate(new Vector3(zigZagX, asteroidData.driftSpeed * deltaTime, 0));
                    break;
            }
        }
    }

    // Clears all asteroids when needed, ensuring all references are removed
    public void ClearAllAsteroids()
    {
        foreach (var asteroidData in asteroids)
        {
            if (asteroidData.asteroid != null)
            {
                Destroy(asteroidData.asteroid); // Properly destroy all active asteroids
            }
        }
        asteroids.Clear(); // Clear the list to remove any lingering references
    }

    private class AsteroidData
    {
        public GameObject asteroid;
        public Vector2 direction;
        public float driftSpeed;
        public MovementType movementType;
        public float angle;
        public Vector2 center;
        public float radius;
        public float growthRate;
        public float frequency;

        // Constructor for Linear or RandomJitter movement types
        public AsteroidData(GameObject asteroid, Vector2 direction, float driftSpeed, MovementType type)
        {
            this.asteroid = asteroid;
            this.direction = direction;
            this.driftSpeed = driftSpeed;
            this.movementType = type;
        }

        // Constructor for Circular, Spiral, or ZigZag movement types
        public AsteroidData(GameObject asteroid, Vector2 direction, float driftSpeed, MovementType type, Vector2 center, float param)
        {
            this.asteroid = asteroid;
            this.direction = direction;
            this.driftSpeed = driftSpeed;
            this.movementType = type;
            this.center = center;
            this.radius = param; // Can be radius for circular, growth rate for spiral, or frequency for zigzag
        }
    }
}
