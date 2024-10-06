using UnityEngine;
using System.Collections.Generic;

public class SectorVisualizer : MonoBehaviour
{
    public int sectorSize = 10; // Size of each sector, matching WorldGenerator's sector size
    public Color gridColor = Color.green; // Color of the grid lines
    public float lineWidth = 0.1f; // Width of the grid lines

    private List<LineRenderer> lineRenderers = new List<LineRenderer>();

    private void Start()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("Main camera not found.");
            return;
        }
    }

    private void Update()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("Main camera not found.");
            return;
        }

        // Clear old lines
        ClearOldLines();

        // Get the boundaries of the camera's viewport in world coordinates
        Vector3 bottomLeft = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, -Camera.main.transform.position.z));
        Vector3 topRight = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, -Camera.main.transform.position.z));

        // Calculate sector bounds based on camera's current viewport
        int minX = Mathf.FloorToInt(bottomLeft.x / sectorSize);
        int maxX = Mathf.CeilToInt(topRight.x / sectorSize);
        int minY = Mathf.FloorToInt(bottomLeft.y / sectorSize);
        int maxY = Mathf.CeilToInt(topRight.y / sectorSize);

        // Draw grid for each visible sector
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                DrawSectorGrid(new Vector2Int(x, y));
            }
        }
    }

    private void DrawSectorGrid(Vector2Int sectorCoords)
    {
        // Calculate the world position of the bottom-left corner of the sector
        Vector3 sectorOrigin = new Vector3(sectorCoords.x * sectorSize, sectorCoords.y * sectorSize, 0);

        // Create a line renderer for the grid border
        LineRenderer lineRenderer = new GameObject("SectorGridLine").AddComponent<LineRenderer>();
        lineRenderer.transform.parent = transform; // Set parent to keep the hierarchy clean

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Use a default material
        lineRenderer.startColor = gridColor;
        lineRenderer.endColor = gridColor;
        lineRenderer.positionCount = 5; // Four corners + the closing line

        // Set the positions to draw the square
        lineRenderer.SetPosition(0, sectorOrigin);
        lineRenderer.SetPosition(1, sectorOrigin + new Vector3(sectorSize, 0, 0));
        lineRenderer.SetPosition(2, sectorOrigin + new Vector3(sectorSize, sectorSize, 0));
        lineRenderer.SetPosition(3, sectorOrigin + new Vector3(0, sectorSize, 0));
        lineRenderer.SetPosition(4, sectorOrigin); // Close the square

        lineRenderers.Add(lineRenderer);
    }

    private void ClearOldLines()
    {
        foreach (var lineRenderer in lineRenderers)
        {
            Destroy(lineRenderer.gameObject);
        }
        lineRenderers.Clear();
    }

    // Draws Gizmos for visualization in the editor
    private void OnDrawGizmos()
    {
        if (Camera.main == null)
        {
            return;
        }

        // Get the boundaries of the camera's viewport in world coordinates
        Vector3 bottomLeft = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, -Camera.main.transform.position.z));
        Vector3 topRight = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, -Camera.main.transform.position.z));

        // Calculate sector bounds based on camera's current viewport
        int minX = Mathf.FloorToInt(bottomLeft.x / sectorSize);
        int maxX = Mathf.CeilToInt(topRight.x / sectorSize);
        int minY = Mathf.FloorToInt(bottomLeft.y / sectorSize);
        int maxY = Mathf.CeilToInt(topRight.y / sectorSize);

        Gizmos.color = gridColor;

        // Draw grid for each visible sector
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                DrawGizmoSectorGrid(new Vector2Int(x, y));
            }
        }
    }

    private void DrawGizmoSectorGrid(Vector2Int sectorCoords)
    {
        // Calculate the world position of the bottom-left corner of the sector
        Vector3 sectorOrigin = new Vector3(sectorCoords.x * sectorSize, sectorCoords.y * sectorSize, 0);

        // Draw the sector as a square using Gizmos
        Vector3 bottomLeft = sectorOrigin;
        Vector3 bottomRight = sectorOrigin + new Vector3(sectorSize, 0, 0);
        Vector3 topRight = sectorOrigin + new Vector3(sectorSize, sectorSize, 0);
        Vector3 topLeft = sectorOrigin + new Vector3(0, sectorSize, 0);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
    }
}
