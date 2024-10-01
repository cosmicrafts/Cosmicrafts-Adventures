using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HealthComponent : NetworkBehaviour
{
    public float maxHealth = 100f;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f);

    public Slider healthSlider; // Reference to the health bar UI slider

    private void Start()
    {
        Debug.Log($"{gameObject.name} HealthComponent initialized");

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        currentHealth.OnValueChanged += OnHealthChanged;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth.Value;
        }
        else
        {
            Debug.LogWarning("Health slider is not assigned.");
        }
    }

    private new void OnDestroy()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        base.OnDestroy();
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        Debug.Log($"Health updated from {oldHealth} to {newHealth}");
        if (healthSlider != null)
        {
            healthSlider.value = newHealth;
        }
        else
        {
            Debug.LogWarning("Health slider reference is missing on the client side.");
        }
    }

    /// <summary>
    /// Server-side method to reduce health.
    /// </summary>
    /// <param name="amount">The amount of health to reduce.</param>
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount)
    {
        Debug.Log($"TakeDamageServerRpc called on {gameObject.name} with damage: {amount}");

        if (!IsServer)
        {
            Debug.Log("TakeDamageServerRpc called, but not on the server. Exiting.");
            return;
        }

        Debug.Log($"Reducing health for {gameObject.name} by {amount}");
        currentHealth.Value -= amount;

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            Debug.Log($"{gameObject.name} has died. Handling death.");
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        Debug.Log($"{gameObject.name} has died!");
        // Handle death logic here (e.g., respawn, disable player, etc.)
    }
}
