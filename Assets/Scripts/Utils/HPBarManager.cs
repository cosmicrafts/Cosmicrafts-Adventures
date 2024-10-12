using System.Collections.Generic;
using UnityEngine;

public class HealthBarManager : MonoBehaviour
{
    public static HealthBarManager Instance { get; private set; }

    // Struct to store the health bar transform and its initial local rotation
    private struct HealthBarData
    {
        public Transform healthBar;       // Reference to the health bar's Transform
        public Quaternion initialLocalRotation; // Initial local rotation of the health bar
    }

    private List<HealthBarData> healthBars = new List<HealthBarData>(); // List to track all health bars

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Ensure the manager persists across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Register a health bar with its initial local rotation
    public void RegisterHealthBar(Transform healthBar)
    {
        if (healthBar == null) return;

        foreach (var hb in healthBars)
        {
            if (hb.healthBar == healthBar)
                return; // Prevent duplicates
        }

        // Store the health bar's initial local rotation (relative to parent)
        HealthBarData newHealthBar = new HealthBarData
        {
            healthBar = healthBar,
            initialLocalRotation = healthBar.localRotation // Store local rotation to lock it relative to parent
        };

        healthBars.Add(newHealthBar);
    }

    // Unregister a health bar
    public void UnregisterHealthBar(Transform healthBar)
    {
        if (healthBar == null) return;

        healthBars.RemoveAll(hb => hb.healthBar == healthBar);
    }

    // LateUpdate to reset local rotations of all registered health bars
    void LateUpdate()
    {
        foreach (var hb in healthBars)
        {
            if (hb.healthBar != null)
            {
                // Reset the health bar's local rotation to keep it fixed in world space
                hb.healthBar.localRotation = hb.initialLocalRotation;
            }
        }
    }
}
