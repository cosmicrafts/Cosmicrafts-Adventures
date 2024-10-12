using UnityEngine;

public class MinimapZoom : MonoBehaviour
{
    [SerializeField] private Camera minimapCamera;  // Reference to the minimap camera
    private float currentZoomLevel = 36f;           // Default starting size
    private const float minZoomLevel = 24f;         // Minimum zoom level
    private const float maxZoomLevel = 64f;         // Maximum zoom level
    private const float zoomStep = 2f;              // Amount to increase/decrease per click

    private void Start()
    {
        // Initialize the camera size to the default value
        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = currentZoomLevel;
        }
        else
        {
            Debug.LogWarning("Minimap camera is not assigned!");
        }
    }

    // Call this method to zoom in
    public void ZoomIn()
    {
        if (minimapCamera != null && currentZoomLevel > minZoomLevel)
        {
            currentZoomLevel -= zoomStep;
            currentZoomLevel = Mathf.Clamp(currentZoomLevel, minZoomLevel, maxZoomLevel);
            minimapCamera.orthographicSize = currentZoomLevel;
        }
    }

    // Call this method to zoom out
    public void ZoomOut()
    {
        if (minimapCamera != null && currentZoomLevel < maxZoomLevel)
        {
            currentZoomLevel += zoomStep;
            currentZoomLevel = Mathf.Clamp(currentZoomLevel, minZoomLevel, maxZoomLevel);
            minimapCamera.orthographicSize = currentZoomLevel;
        }
    }
}
