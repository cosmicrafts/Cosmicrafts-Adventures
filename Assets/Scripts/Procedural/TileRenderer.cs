using System.Collections.Generic;
using UnityEngine;

public class SpaceProceduralGenerator : MonoBehaviour
{
    [Header("Space Tile Settings")]
    public Sprite spaceTileSpriteForeground;
    public Sprite spaceTileSpriteBackground;
    public float tileWorldSize = 16f;
    public int renderDistance = 4;

    [Header("Foreground Movement Settings")]
    public Vector2 foregroundMovementDirection = new Vector2(0.1f, 0.1f);
    public float minForegroundMovementSpeed = 0.05f;
    public float maxForegroundMovementSpeed = 0.2f;

    [Header("Material Settings")]
    public Material additiveMaterialForeground;

    [Header("Customization Options")]
    public Color foregroundColor = Color.white;
    public Color backgroundColor = Color.white;
    [Range(0.5f, 10f)] public float foregroundScaleFactor = 1f;
    [Range(0.5f, 10f)] public float backgroundScaleFactor = 1f;

    private Transform player;
    private Vector2 playerPosition;
    private Dictionary<Vector2, GameObject> foregroundChunks = new Dictionary<Vector2, GameObject>();
    private Dictionary<Vector2, GameObject> backgroundChunks = new Dictionary<Vector2, GameObject>();
    private Dictionary<Vector2, float> foregroundChunkSpeeds = new Dictionary<Vector2, float>();
    
    private Queue<GameObject> chunkPool = new Queue<GameObject>(); // Pool for recycling chunks
    private float updateInterval = 0.2f; // Update chunks every 0.2 seconds
    private float updateTimer = 0f;

    private void Start()
    {
        player = Camera.main.transform;
        if (player == null)
        {
            Debug.LogWarning("Main camera not found.");
            return;
        }

        GenerateInitialChunks();
    }

    private void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            UpdateChunksAroundPlayer();
            RemoveDistantChunks();
            updateTimer = 0f; // Reset timer after update
        }

        MoveForegroundChunks();
    }

    void GenerateInitialChunks()
    {
        playerPosition = new Vector2(Mathf.FloorToInt(player.position.x / tileWorldSize), Mathf.FloorToInt(player.position.y / tileWorldSize));

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                Vector2 chunkCoord = new Vector2(playerPosition.x + x, playerPosition.y + y);
                GenerateChunk(chunkCoord, spaceTileSpriteForeground, foregroundChunks, additiveMaterialForeground, foregroundColor, foregroundScaleFactor, true);
                GenerateChunk(chunkCoord, spaceTileSpriteBackground, backgroundChunks, null, backgroundColor, backgroundScaleFactor, false);
            }
        }
    }

    void GenerateChunk(Vector2 chunkCoord, Sprite sprite, Dictionary<Vector2, GameObject> chunksDict, Material material, Color color, float scaleFactor, bool isForeground)
    {
        if (chunksDict.ContainsKey(chunkCoord)) return;

        GameObject newChunk = GetChunkFromPool(); // Get a chunk from the pool
        newChunk.transform.position = new Vector3(chunkCoord.x * tileWorldSize, chunkCoord.y * tileWorldSize, 0);
        newChunk.layer = 6;

        SpriteRenderer spriteRenderer = newChunk.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = newChunk.AddComponent<SpriteRenderer>();
        }
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;

        if (material != null)
        {
            spriteRenderer.material = material;
        }

        newChunk.transform.localScale = new Vector3(scaleFactor * tileWorldSize / sprite.bounds.size.x, scaleFactor * tileWorldSize / sprite.bounds.size.y, 1);
        int randomRotation = Random.Range(0, 4) * 90;
        newChunk.transform.rotation = Quaternion.Euler(0, 0, randomRotation);

        chunksDict.Add(chunkCoord, newChunk);

        if (isForeground)
        {
            float randomSpeed = Random.Range(minForegroundMovementSpeed, maxForegroundMovementSpeed);
            foregroundChunkSpeeds[chunkCoord] = randomSpeed;
        }
    }

    void UpdateChunksAroundPlayer()
    {
        Vector2 newPlayerPos = new Vector2(Mathf.FloorToInt(player.position.x / tileWorldSize), Mathf.FloorToInt(player.position.y / tileWorldSize));

        if (newPlayerPos != playerPosition)
        {
            playerPosition = newPlayerPos;

            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int y = -renderDistance; y <= renderDistance; y++)
                {
                    Vector2 chunkCoord = new Vector2(playerPosition.x + x, playerPosition.y + y);
                    if (!backgroundChunks.ContainsKey(chunkCoord))
                    {
                        GenerateChunk(chunkCoord, spaceTileSpriteBackground, backgroundChunks, null, backgroundColor, backgroundScaleFactor, false);
                    }
                }
            }
        }
    }

    void MoveForegroundChunks()
    {
        foreach (var chunk in foregroundChunks)
        {
            Vector2 chunkCoord = chunk.Key;
            GameObject chunkObject = chunk.Value;
            if (foregroundChunkSpeeds.TryGetValue(chunkCoord, out float speed))
            {
                Vector3 movement = (Vector3)foregroundMovementDirection * speed * Time.deltaTime;
                chunkObject.transform.position += movement;
            }
        }
    }

    void RemoveDistantChunks()
    {
        List<Vector2> chunksToRemove = new List<Vector2>();

        foreach (var chunk in foregroundChunks)
        {
            float distanceToPlayer = Vector2.Distance(playerPosition, chunk.Key);
            if (distanceToPlayer > renderDistance + 2)
            {
                chunksToRemove.Add(chunk.Key);
            }
        }

        foreach (var chunkCoord in chunksToRemove)
        {
            ReturnChunkToPool(foregroundChunks[chunkCoord]); // Return to pool
            foregroundChunks.Remove(chunkCoord);
            foregroundChunkSpeeds.Remove(chunkCoord);
        }

        chunksToRemove.Clear();

        foreach (var chunk in backgroundChunks)
        {
            float distanceToPlayer = Vector2.Distance(playerPosition, chunk.Key);
            if (distanceToPlayer > renderDistance + 1)
            {
                chunksToRemove.Add(chunk.Key);
            }
        }

        foreach (var chunkCoord in chunksToRemove)
        {
            ReturnChunkToPool(backgroundChunks[chunkCoord]); // Return to pool
            backgroundChunks.Remove(chunkCoord);
        }
    }

    GameObject GetChunkFromPool()
    {
        if (chunkPool.Count > 0)
        {
            GameObject chunk = chunkPool.Dequeue();
            chunk.SetActive(true);
            return chunk;
        }

        return new GameObject("Chunk");
    }

    void ReturnChunkToPool(GameObject chunk)
    {
        chunk.SetActive(false); // Disable the chunk
        chunkPool.Enqueue(chunk); // Return it to the pool
    }
}
