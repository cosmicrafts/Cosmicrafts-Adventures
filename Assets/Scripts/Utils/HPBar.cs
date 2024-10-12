using UnityEngine;

public class HPBar : MonoBehaviour
{
    private Quaternion initialRotation;

    void Start()
    {
        // Store the initial world rotation of the health bar
        initialRotation = transform.rotation;
    }

    void LateUpdate()
    {
        // Reset the rotation to the initial rotation every frame
        transform.rotation = initialRotation;
    }
}