using UnityEngine;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator Instance; // Singleton instance for easy access
    public List<Sprite> spaceTiles; // List of starry tile sprites to choose from
    public int sectorSize = 100; // Size of each sector
    public Sector sectorTemplate; // Template Sector SO to create new sectors

    private Dictionary<Vector2Int, Sector> sectors = new Dictionary<Vector2Int, Sector>(); // Dictionary to store generated sectors

    private void Awake()
    {
        Instance = this;
        GenerateInitialSectors();
    }

    // Generates initial 9 sectors (3x3 grid)
    private void GenerateInitialSectors()
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                GenerateSector(new Vector2Int(x, y));
            }
        }
    }

    // Generates a new sector at specified coordinates
// Generates a new sector at specified coordinates
public void GenerateSector(Vector2Int coordinates)
{
    if (sectors.ContainsKey(coordinates))
        return;

    Debug.Log($"Generating sector at coordinates: {coordinates}");

    // Create a new sector from the template
    Sector newSector = ScriptableObject.CreateInstance<Sector>();
    newSector.sectorName = $"Sector ({coordinates.x}, {coordinates.y})";
    newSector.isGenerated = true;

    // Add visualization for the sector (using a LineRenderer)
    GameObject sectorPlaceholder = new GameObject($"SectorPlaceholder ({coordinates.x}, {coordinates.y})");
    sectorPlaceholder.transform.position = new Vector3(coordinates.x * sectorSize, coordinates.y * sectorSize, 0);

    // Add a LineRenderer to visualize the sector boundaries
    LineRenderer lineRenderer = sectorPlaceholder.AddComponent<LineRenderer>();
    lineRenderer.positionCount = 5;
    lineRenderer.startWidth = 0.1f; // Set line width
    lineRenderer.endWidth = 0.1f;
    lineRenderer.loop = true;

    // Ensure the line renderer uses world space correctly
    lineRenderer.useWorldSpace = false; // This will make the positions relative to the GameObject (sectorPlaceholder)

    // Set the positions of the line to form a square around the sector (relative to the sectorPlaceholder)
    lineRenderer.SetPosition(0, new Vector3(0, 0, 0));
    lineRenderer.SetPosition(1, new Vector3(sectorSize, 0, 0));
    lineRenderer.SetPosition(2, new Vector3(sectorSize, sectorSize, 0));
    lineRenderer.SetPosition(3, new Vector3(0, sectorSize, 0));
    lineRenderer.SetPosition(4, new Vector3(0, 0, 0));

    // Set the material and color for better visibility
    lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    lineRenderer.startColor = new Color(1, 1, 1, 0.5f); // Light transparent color for visibility
    lineRenderer.endColor = new Color(1, 1, 1, 0.5f);

    // Add sector to the dictionary
    sectors.Add(coordinates, newSector);

    Debug.Log($"Generated new sector: {newSector.sectorName}");
}

    public bool SectorExists(Vector2Int coordinates)
    {
        return sectors.ContainsKey(coordinates);
    }

    public Sector GetSector(Vector2Int coordinates)
    {
        if (sectors.TryGetValue(coordinates, out Sector sector))
        {
            return sector;
        }
        return null;
    }
}
