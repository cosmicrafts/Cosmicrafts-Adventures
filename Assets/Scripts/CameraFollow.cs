using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class CameraFollow : NetworkBehaviour
{
    public Transform target; // Reference to the target (player) to follow
    public float smoothSpeed = 0.125f; // Smooth speed for following
    public Vector3 offset; // Offset to maintain relative to the player
    public float delay = 0.01f; // Delay before following

    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        // Only execute this script if it belongs to the client who owns the camera.
        if (!IsOwner)
        {
            this.enabled = false;
            return;
        }

        // Assuming the target (player) is this transform's parent (as it is part of the prefab)
        target = transform.parent;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Smoothly follow the target
        StartCoroutine(SmoothFollow());
    }

    IEnumerator SmoothFollow()
    {
        yield return new WaitForSeconds(delay);

        // Calculate the target position with offset, ignoring any rotation from the target
        Vector3 targetPosition = new Vector3(target.position.x, target.position.y, transform.position.z) + offset;

        // Smoothly interpolate the camera's position towards the desired position
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed);
        transform.position = smoothedPosition;

        // Ignore all rotations (X, Y, Z)
        transform.rotation = Quaternion.identity;
    }
}
