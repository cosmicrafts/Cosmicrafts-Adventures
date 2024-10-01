using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HealthComponent : NetworkBehaviour
{
    public float maxHealth = 100f;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f);
    public Slider healthSlider;

    private void Start()
    {
        // Initialize health and setup slider
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

        Debug.Log($"{gameObject.name} HealthComponent initialized with {currentHealth.Value} health");
    }

    private new void OnDestroy()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        base.OnDestroy();
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        Debug.Log($"{gameObject.name} health changed from {oldHealth} to {newHealth}");

        if (healthSlider != null)
        {
            healthSlider.value = newHealth;
        }
        else
        {
            Debug.LogWarning("Health slider reference is missing on the client side.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount)
    {
        if (!IsServer)
        {
            Debug.LogWarning("TakeDamageServerRpc called, but not on the server.");
            return;
        }

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
        // Handle death logic here, like respawning or disabling the player
    }
}
