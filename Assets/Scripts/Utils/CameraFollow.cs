using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Reference to the player transform to follow
    public float smoothSpeed = 0.125f; // Smooth speed for following
    public Vector3 offset; // Offset to maintain relative to the player

    private Vector3 velocity = Vector3.zero;
    private float originalSmoothSpeed;

    private void Start()
    {
        // Store the original smooth speed
        originalSmoothSpeed = smoothSpeed;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Calculate the new target position based on the player's position
        Vector3 targetPosition = target.position + offset;

        // Smoothly interpolate the camera's position towards the target position
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed);
        smoothedPosition.z = -10f;

        // Update the camera position
        transform.position = smoothedPosition;
    }

    // Public method to toggle smooth speed on and off
    public void ToggleSmoothSpeed(bool isOn)
    {
        smoothSpeed = isOn ? originalSmoothSpeed : 0f;
    }
}
