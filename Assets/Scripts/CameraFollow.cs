using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    public float smoothSpeed = 0.125f;
    public Vector3 offset;
    public float delay = 0.01f;

    private Vector3 velocity = Vector3.zero;
    private Transform target;

    private void Start()
    {
        // Start coroutine to find the player after it's spawned
        StartCoroutine(FindLocalPlayer());
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Smoothly follow the target
        SmoothFollow();
    }

    private void SmoothFollow()
    {
        if (target == null) return;

        // Keep the camera's Z position unaffected by the target's rotation or movement
        Vector3 targetPosition = new Vector3(target.position.x, target.position.y, transform.position.z) + offset;

        // Smoothly interpolate the camera's position towards the desired position
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed);
        transform.position = smoothedPosition;

        // Ignore X and Y rotation by resetting rotation to default values for the camera
        transform.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z);
    }

    private IEnumerator FindLocalPlayer()
    {
        // Wait until the player object is spawned
        while (target == null)
        {
            // Find the player GameObject with an InputComponent belonging to the local player
            var localPlayerInput = FindFirstObjectByType<InputComponent>();

            if (localPlayerInput != null && localPlayerInput.IsOwner)
            {
                target = localPlayerInput.transform;
                Debug.Log("Local player found: " + target.name);
            }
            else
            {
                Debug.Log("Local player not found yet, retrying...");
            }

            // Wait for a short duration before trying again
            yield return new WaitForSeconds(0.5f);
        }
    }
}
