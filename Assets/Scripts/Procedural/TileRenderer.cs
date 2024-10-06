using System.Collections.Generic;
using UnityEngine;

public class SpaceProceduralGenerator : MonoBehaviour
{
    [Header("Space Tile Settings")]
    public Sprite spaceTileSpriteForeground; // Foreground sprite
    public Sprite spaceTileSpriteBackground; // Background sprite
    public float tileWorldSize = 16f; // Size of each space tile
    public int renderDistance = 4; // How far the camera should render chunks

    [Header("Foreground Movement Settings")]
    public Vector2 foregroundMovementDirection = new Vector2(0.1f, 0.1f); // Diagonal movement speed
    public float minForegroundMovementSpeed = 0.05f; // Minimum speed at which the foreground moves
    public float maxForegroundMovementSpeed = 0.2f; // Maximum speed at which the foreground moves

    [Header("Material Settings")]
    public Material additiveMaterialForeground; // Additive material for the foreground

    [Header("Customization Options")]
    public Color foregroundColor = Color.white; // Tint color for the foreground
    public Color backgroundColor = Color.white; // Tint color for the background
    [Range(0.5f, 2f)] public float foregroundScaleFactor = 1f; // Scale factor for foreground
    [Range(0.5f, 2f)] public float backgroundScaleFactor = 1f; // Scale factor for background

    private Transform player;
    private Vector2 playerPosition;
    private Dictionary<Vector2, GameObject> foregroundChunks = new Dictionary<Vector2, GameObject>(); // Store foreground chunks
    private Dictionary<Vector2, GameObject> backgroundChunks = new Dictionary<Vector2, GameObject>(); // Store background chunks

    // Dictionary to store random speeds for each foreground chunk
    private Dictionary<Vector2, float> foregroundChunkSpeeds = new Dictionary<Vector2, float>();

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

        // Move the foreground layer in the specified diagonal direction
        MoveForegroundChunks();

        // Continuously generate new foreground chunks over time
        GenerateNewForegroundChunks();
    }

    void GenerateInitialChunks()
    {
        // Calculate the player's initial chunk position
        playerPosition = new Vector2(Mathf.FloorToInt(player.position.x / tileWorldSize), Mathf.FloorToInt(player.position.y / tileWorldSize));

        // Generate chunks within the render distance around the player for both layers
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
        // Check if the chunk has already been generated
        if (chunksDict.ContainsKey(chunkCoord)) return;

        // Create a new GameObject with a SpriteRenderer to use the given sprite
        GameObject newChunk = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}_Layer");
        newChunk.transform.position = new Vector3(chunkCoord.x * tileWorldSize, chunkCoord.y * tileWorldSize, 0); // Generate at the same initial position for overlap
        newChunk.layer = 6; // Assign directly to Layer 6 (Space tiles layer)

        // Add a SpriteRenderer component and set the sprite
        SpriteRenderer spriteRenderer = newChunk.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;

        // Set the size of the sprite to match the tileWorldSize and apply the scale factor
        newChunk.transform.localScale = new Vector3(scaleFactor * tileWorldSize / sprite.bounds.size.x, scaleFactor * tileWorldSize / sprite.bounds.size.y, 1);

        // Set a random rotation of 0, 90, 180, or 270 degrees
        int randomRotation = Random.Range(0, 4) * 90;
        newChunk.transform.rotation = Quaternion.Euler(0, 0, randomRotation);

        // Set the material and color
        if (material != null)
        {
            spriteRenderer.material = material;
        }
        spriteRenderer.color = color;

        // Add the new chunk to the dictionary
        chunksDict.Add(chunkCoord, newChunk);

        // If this is a foreground chunk, assign it a random speed
        if (isForeground)
        {
            float randomSpeed = Random.Range(minForegroundMovementSpeed, maxForegroundMovementSpeed);
            foregroundChunkSpeeds[chunkCoord] = randomSpeed;
        }
    }

    void UpdateChunksAroundPlayer()
    {
        // Calculate the player's current chunk position
        Vector2 newPlayerPos = new Vector2(Mathf.FloorToInt(player.position.x / tileWorldSize), Mathf.FloorToInt(player.position.y / tileWorldSize));

        // Only update if the player has moved to a new chunk
        if (newPlayerPos != playerPosition)
        {
            playerPosition = newPlayerPos;

            // Generate new chunks within the render distance for the background layer only
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

    void GenerateNewForegroundChunks()
    {
        // Generate new foreground chunks continuously within a larger distance to keep creating fresh ones
        for (int x = -renderDistance - 1; x <= renderDistance + 1; x++)
        {
            for (int y = -renderDistance - 1; y <= renderDistance + 1; y++)
            {
                Vector2 chunkCoord = new Vector2(playerPosition.x + x, playerPosition.y + y);
                if (!foregroundChunks.ContainsKey(chunkCoord))
                {
                    GenerateChunk(chunkCoord, spaceTileSpriteForeground, foregroundChunks, additiveMaterialForeground, foregroundColor, foregroundScaleFactor, true);
                }
            }
        }
    }

    void MoveForegroundChunks()
    {
        // Move foreground chunks in a diagonal direction over time with their respective random speeds
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

        // Find foreground chunks that are beyond the render distance and mark them for removal
        foreach (var chunk in foregroundChunks)
        {
            float distanceToPlayer = Vector2.Distance(playerPosition, chunk.Key);
            if (distanceToPlayer > renderDistance + 2) // Use a larger threshold to keep fresh chunks
            {
                chunksToRemove.Add(chunk.Key);
            }
        }

        // Remove and destroy distant foreground chunks
        foreach (var chunkCoord in chunksToRemove)
        {
            Destroy(foregroundChunks[chunkCoord]);
            foregroundChunks.Remove(chunkCoord);
            foregroundChunkSpeeds.Remove(chunkCoord);
        }

        chunksToRemove.Clear();

        // Find background chunks that are beyond the render distance and mark them for removal
        foreach (var chunk in backgroundChunks)
        {
            float distanceToPlayer = Vector2.Distance(playerPosition, chunk.Key);
            if (distanceToPlayer > renderDistance + 1)
            {
                chunksToRemove.Add(chunk.Key);
            }
        }

        // Remove and destroy distant background chunks
        foreach (var chunkCoord in chunksToRemove)
        {
            Destroy(backgroundChunks[chunkCoord]);
            backgroundChunks.Remove(chunkCoord);
        }
    }
}
