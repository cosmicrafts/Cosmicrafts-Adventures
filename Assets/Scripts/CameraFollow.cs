using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Reference to the player transform to follow
    public float smoothSpeed = 0.125f; // Smooth speed for following
    public Vector3 offset; // Offset to maintain relative to the player

    private Vector3 velocity = Vector3.zero;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Ensure the Rigidbody rotation constraints are set
        if (rb != null)
        {
            rb.freezeRotation = true; // Locks all rotations
        }
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        // Calculate the new target position based on the player's position
        Vector3 targetPosition = target.position + offset;

        // Smoothly interpolate the camera's position towards the target position
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed);

        // Ensure the camera's Z position remains fixed at -10
        smoothedPosition.z = -10f;

        // Update the camera position
        rb.MovePosition(smoothedPosition);
    }
}
