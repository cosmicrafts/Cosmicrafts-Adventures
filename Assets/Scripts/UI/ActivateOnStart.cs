using UnityEngine;

public class ActivateOnStart : MonoBehaviour
{
    // Reference to the object you want to activate
    public GameObject objectToActivate;

    private void Start()
    {
        // Activate the object if it's not already active
        if (objectToActivate != null && !objectToActivate.activeInHierarchy)
        {
            objectToActivate.SetActive(true);
        }
    }
}
