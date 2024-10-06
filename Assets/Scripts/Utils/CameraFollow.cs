using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Reference to the player transform to follow
    public float smoothSpeed = 0.125f; // Smooth speed for following
    public Vector3 offset; // Offset to maintain relative to the player

    private Vector3 velocity = Vector3.zero;

    public float delay = 0.25f;

    private void LateUpdate()
    {
        if (target == null) return;
        StartCoroutine(SmoothFollow());
    }

    IEnumerator SmoothFollow()
    {
        yield return new WaitForSeconds(delay);

        // Check if the target is still valid before accessing it
        if (target == null) yield break;

        // Calculate the new target position based on the player's position
        Vector3 targetPosition = target.position + offset;

        // Ensure the camera's Z position remains fixed at -10
        targetPosition.z = -10f;

        // Smoothly interpolate the camera's position towards the target position
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed);

        // Update the camera position
        transform.position = smoothedPosition;
    }
}
