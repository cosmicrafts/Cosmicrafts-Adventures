using System.Collections.Generic;
using UnityEngine;

public class SpaceProceduralGenerator : MonoBehaviour
{
    [Header("Space Tile Settings")]
    public Sprite spaceTileSprite; // Use a sprite instead of a prefab
    public float tileWorldSize = 16f; // Size of each space tile
    public int renderDistance = 4; // How far the camera should render chunks

    private Transform player;
    private Vector2 playerPosition;
    private Dictionary<Vector2, GameObject> chunks = new Dictionary<Vector2, GameObject>(); // Store generated chunks

    private void Start()
    {
        player = Camera.main.transform; // Use the main camera's transform
        if (player == null)
        {
            Debug.LogWarning("Main camera not found.");
            return;
        }

        GenerateInitialChunks();
    }

    private void Update()
    {
        UpdateChunksAroundPlayer();
        RemoveDistantChunks();
    }

    void GenerateInitialChunks()
    {
        // Calculate the player's initial chunk position
        playerPosition = new Vector2(Mathf.FloorToInt(player.position.x / tileWorldSize), Mathf.FloorToInt(player.position.y / tileWorldSize));

        // Generate chunks within the render distance around the player
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                GenerateChunk(new Vector2(playerPosition.x + x, playerPosition.y + y));
            }
        }
    }

    void GenerateChunk(Vector2 chunkCoord)
    {
        // Check if the chunk has already been generated
        if (chunks.ContainsKey(chunkCoord)) return;

        // Create a new GameObject with a SpriteRenderer to use the given sprite
        GameObject newChunk = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        newChunk.transform.position = new Vector3(chunkCoord.x * tileWorldSize, chunkCoord.y * tileWorldSize, 0);
        newChunk.layer = 6; // Assign directly to Layer 6 (Space tiles layer)

        // Add a SpriteRenderer component and set the sprite
        SpriteRenderer spriteRenderer = newChunk.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = spaceTileSprite;

        // Set the size of the sprite to match the tileWorldSize
        newChunk.transform.localScale = new Vector3(tileWorldSize / spaceTileSprite.bounds.size.x, tileWorldSize / spaceTileSprite.bounds.size.y, 1);

        // Add the new chunk to the dictionary
        chunks.Add(chunkCoord, newChunk);
    }

    void UpdateChunksAroundPlayer()
    {
        // Calculate the player's current chunk position
        Vector2 newPlayerPos = new Vector2(Mathf.FloorToInt(player.position.x / tileWorldSize), Mathf.FloorToInt(player.position.y / tileWorldSize));

        // Only update if the player has moved to a new chunk
        if (newPlayerPos != playerPosition)
        {
            playerPosition = newPlayerPos;

            // Generate new chunks within the render distance
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int y = -renderDistance; y <= renderDistance; y++)
                {
                    Vector2 chunkCoord = new Vector2(playerPosition.x + x, playerPosition.y + y);
                    if (!chunks.ContainsKey(chunkCoord))
                    {
                        GenerateChunk(chunkCoord);
                    }
                }
            }
        }
    }

    void RemoveDistantChunks()
    {
        List<Vector2> chunksToRemove = new List<Vector2>();

        // Find chunks that are beyond the render distance and mark them for removal
        foreach (var chunk in chunks)
        {
            float distanceToPlayer = Vector2.Distance(playerPosition, chunk.Key);
            if (distanceToPlayer > renderDistance + 1)
            {
                chunksToRemove.Add(chunk.Key);
            }
        }

        // Remove and destroy distant chunks
        foreach (var chunkCoord in chunksToRemove)
        {
            Destroy(chunks[chunkCoord]);
            chunks.Remove(chunkCoord);
        }
    }
}
