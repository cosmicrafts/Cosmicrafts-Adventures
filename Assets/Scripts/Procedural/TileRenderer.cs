using UnityEngine;
using System.Collections.Generic;

public class TileRenderer : MonoBehaviour
{
    public Camera playerCamera;
    public float renderDistance = 2000f; // Render tiles within this distance from the camera
    private Dictionary<Vector2Int, Sector> renderedSectors = new Dictionary<Vector2Int, Sector>();

    private void Update()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("PlayerCamera is not assigned in TileRenderer.");
            return;
        }

        // Calculate the camera's current sector
        Vector2Int currentSector = new Vector2Int(
            Mathf.FloorToInt(playerCamera.transform.position.x / WorldGenerator.Instance.sectorSize),
            Mathf.FloorToInt(playerCamera.transform.position.y / WorldGenerator.Instance.sectorSize)
        );

        // Render tiles from sectors within range
        RenderTilesInRange(currentSector);
    }

    private void RenderTilesInRange(Vector2Int currentSector)
    {
        for (int x = currentSector.x - 1; x <= currentSector.x + 1; x++)
        {
            for (int y = currentSector.y - 1; y <= currentSector.y + 1; y++)
            {
                Vector2Int sectorCoords = new Vector2Int(x, y);
                
                if (WorldGenerator.Instance.SectorExists(sectorCoords))
                {
                    Sector sector = WorldGenerator.Instance.GetSector(sectorCoords);
                    if (!renderedSectors.ContainsKey(sectorCoords))
                    {
                        Debug.Log($"Rendering sector: {sector.sectorName}");
                        RenderSector(sector);
                        renderedSectors[sectorCoords] = sector;
                    }
                }
            }
        }
    }

    private void RenderSector(Sector sector)
    {
        foreach (var tileData in sector.tiles)
        {
            GameObject tile = new GameObject($"Tile_{tileData.position}");
            tile.transform.position = new Vector3(tileData.position.x, tileData.position.y, 0);
            tile.transform.rotation = tileData.rotation;

            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = tileData.sprite;

            // Set parent to this object for better organization
            tile.transform.SetParent(transform);

            Debug.Log($"Rendered tile at {tile.transform.position} with sprite {tileData.sprite.name}");
        }
    }
}
