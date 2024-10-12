using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class AsteroidManager : MonoBehaviour
{
    public static AsteroidManager Instance;
    private List<AsteroidData> asteroids = new List<AsteroidData>();
    private NativeArray<Vector3> positions;
    private NativeArray<Vector2> directions;
    private NativeArray<float> speeds;
    private bool jobsInitialized = false;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        // Dispose native arrays when the object is destroyed
        if (jobsInitialized)
        {
            positions.Dispose();
            directions.Dispose();
            speeds.Dispose();
        }
    }

    // Add an asteroid to the list to be batched
    public void RegisterAsteroid(GameObject asteroid, Vector2 direction, float driftSpeed)
    {
        asteroids.Add(new AsteroidData(asteroid, direction, driftSpeed));
        InitializeNativeArrays();
    }

    // Remove asteroid when no longer active
    public void UnregisterAsteroid(GameObject asteroid)
    {
        asteroids.RemoveAll(a => a.asteroid == asteroid);
        InitializeNativeArrays();
    }

    // Reinitialize native arrays if the asteroid list changes
    private void InitializeNativeArrays()
    {
        // Dispose old arrays if they exist
        if (jobsInitialized)
        {
            positions.Dispose();
            directions.Dispose();
            speeds.Dispose();
        }

        // Create new native arrays with current asteroid data size
        int count = asteroids.Count;
        positions = new NativeArray<Vector3>(count, Allocator.Persistent);
        directions = new NativeArray<Vector2>(count, Allocator.Persistent);
        speeds = new NativeArray<float>(count, Allocator.Persistent);

        jobsInitialized = true;
    }

    private void Update()
    {
        if (asteroids.Count == 0 || !jobsInitialized) return;

        // Populate the native arrays with current asteroid data
        for (int i = 0; i < asteroids.Count; i++)
        {
            positions[i] = asteroids[i].asteroid.transform.position;
            directions[i] = asteroids[i].direction;
            speeds[i] = asteroids[i].driftSpeed;
        }

        // Schedule the job to run
        AsteroidMovementJob movementJob = new AsteroidMovementJob
        {
            positions = positions,
            directions = directions,
            speeds = speeds,
            deltaTime = Time.deltaTime
        };

        // Schedule the job for execution
        JobHandle jobHandle = movementJob.Schedule(asteroids.Count, 64);
        jobHandle.Complete(); // Ensure the job completes before accessing the results

        // Apply the updated positions back to the asteroids
        for (int i = 0; i < asteroids.Count; i++)
        {
            asteroids[i].asteroid.transform.position = positions[i];
        }
    }

    // Data structure to hold asteroid details
    private class AsteroidData
    {
        public GameObject asteroid;
        public Vector2 direction;
        public float driftSpeed;

        public AsteroidData(GameObject asteroid, Vector2 direction, float driftSpeed)
        {
            this.asteroid = asteroid;
            this.direction = direction;
            this.driftSpeed = driftSpeed;
        }
    }

    [BurstCompile]
    private struct AsteroidMovementJob : IJobParallelFor
    {
        public NativeArray<Vector3> positions;
        [ReadOnly] public NativeArray<Vector2> directions;
        [ReadOnly] public NativeArray<float> speeds;
        public float deltaTime;

        public void Execute(int index)
        {
            // Move the position based on direction, speed, and deltaTime
            positions[index] += (Vector3)(directions[index] * speeds[index] * deltaTime);
        }
    }
}
