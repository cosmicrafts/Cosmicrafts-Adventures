using System.Collections.Generic;
using UnityEngine;

public class HPBar : MonoBehaviour
{
    private Quaternion initialRotation;
    private static List<HPBar> allHPBars = new List<HPBar>(); // Static list to track all HPBars

    void Start()
    {
        // Store the initial world rotation of the health bar
        initialRotation = transform.rotation;

        // Register this instance in the static list
        allHPBars.Add(this);
    }

    void OnDestroy()
    {
        // Unregister this instance from the static list when destroyed
        allHPBars.Remove(this);
    }

    // Static method to reset the rotation of all registered HPBars
    public static void BatchLateUpdate()
    {
        foreach (HPBar hpBar in allHPBars)
        {
            if (hpBar != null)
            {
                // Reset the rotation to the initial rotation
                hpBar.transform.rotation = hpBar.initialRotation;
            }
        }
    }

    // Instead of LateUpdate for individual instances, we'll call BatchLateUpdate once per frame
    void LateUpdate()
    {
        // Call the static batch method from the class itself, not an instance
        if (allHPBars.Count > 0)
        {
            HPBar.BatchLateUpdate(); // Static method call using the class name
        }
    }
}
