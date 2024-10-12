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

    public void RegisterAsteroid(GameObject asteroid, Vector2 direction, float driftSpeed)
    {
        asteroids.Add(new AsteroidData(asteroid, direction, driftSpeed, MovementType.Linear));
    }

    public void RegisterAsteroidForCircularMovement(GameObject asteroid, Vector2 center, float speed, float radius)
    {
        asteroids.Add(new AsteroidData(asteroid, Vector2.zero, speed, MovementType.Circular, center, radius));
    }

    public void RegisterAsteroidForSpiralMovement(GameObject asteroid, Vector2 initialDirection, float speed, float growthRate)
    {
        asteroids.Add(new AsteroidData(asteroid, initialDirection, speed, MovementType.Spiral, Vector2.zero, growthRate));
    }

    public void RegisterAsteroidForRandomJitter(GameObject asteroid, Vector2 direction, float speed)
    {
        asteroids.Add(new AsteroidData(asteroid, direction, speed, MovementType.RandomJitter));
    }

    public void RegisterAsteroidForZigZag(GameObject asteroid, Vector2 initialDirection, float speed, float frequency)
    {
        asteroids.Add(new AsteroidData(asteroid, initialDirection, speed, MovementType.ZigZag, Vector2.zero, frequency));
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        foreach (var asteroid in asteroids)
        {
            switch (asteroid.movementType)
            {
                case MovementType.Linear:
                    asteroid.asteroid.transform.Translate(asteroid.direction * asteroid.driftSpeed * deltaTime);
                    break;

                case MovementType.Circular:
                    asteroid.angle += asteroid.driftSpeed * deltaTime; // Circular movement
                    asteroid.asteroid.transform.position = asteroid.center + new Vector2(Mathf.Cos(asteroid.angle), Mathf.Sin(asteroid.angle)) * asteroid.radius;
                    break;

                case MovementType.Spiral:
                    asteroid.angle += asteroid.driftSpeed * deltaTime; // Spiral movement
                    asteroid.radius += asteroid.growthRate * deltaTime; // Spiral outwards
                    asteroid.asteroid.transform.position += new Vector3(Mathf.Cos(asteroid.angle), Mathf.Sin(asteroid.angle), 0) * asteroid.radius * deltaTime;
                    break;

                case MovementType.RandomJitter:
                    asteroid.direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
                    asteroid.asteroid.transform.Translate(asteroid.direction * asteroid.driftSpeed * deltaTime);
                    break;

                case MovementType.ZigZag:
                    asteroid.angle += asteroid.driftSpeed * asteroid.frequency * deltaTime;
                    float zigZagX = Mathf.Sin(asteroid.angle);
                    asteroid.asteroid.transform.Translate(new Vector3(zigZagX, asteroid.driftSpeed * deltaTime, 0));
                    break;
            }
        }
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

        // Linear or Jitter
        public AsteroidData(GameObject asteroid, Vector2 direction, float driftSpeed, MovementType type)
        {
            this.asteroid = asteroid;
            this.direction = direction;
            this.driftSpeed = driftSpeed;
            this.movementType = type;
        }

        // Circular, Spiral, or ZigZag
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
