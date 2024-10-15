using UnityEngine;

public class FullmapZoom : MonoBehaviour
{
    [SerializeField] private Camera fullmapCamera;  // Reference to the fullmap camera
    private float currentZoomLevel = 128f;           // Default starting size
    private const float minZoomLevel = 36f;         // fullmum zoom level
    private const float maxZoomLevel = 256f;         // Maximum zoom level
    private const float zoomStep = 24f;              // Amount to increase/decrease per click

    private void Start()
    {
        // Initialize the camera size to the default value
        if (fullmapCamera != null)
        {
            fullmapCamera.orthographicSize = currentZoomLevel;
        }
        else
        {
            Debug.LogWarning("fullmap camera is not assigned!");
        }
    }

    // Call this method to zoom in
    public void ZoomIn()
    {
        if (fullmapCamera != null && currentZoomLevel > minZoomLevel)
        {
            currentZoomLevel -= zoomStep;
            currentZoomLevel = Mathf.Clamp(currentZoomLevel, minZoomLevel, maxZoomLevel);
            fullmapCamera.orthographicSize = currentZoomLevel;
        }
    }

    // Call this method to zoom out
    public void ZoomOut()
    {
        if (fullmapCamera != null && currentZoomLevel < maxZoomLevel)
        {
            currentZoomLevel += zoomStep;
            currentZoomLevel = Mathf.Clamp(currentZoomLevel, minZoomLevel, maxZoomLevel);
            fullmapCamera.orthographicSize = currentZoomLevel;
        }
    }
}
