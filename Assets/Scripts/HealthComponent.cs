using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HealthComponent : NetworkBehaviour
{
    public float maxHealth = 100f;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public Slider healthSlider;
    public float collisionDamage = 4f; // Damage taken upon collision

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
            Debug.Log($"{gameObject.name} [HealthComponent] Slider initialized with value {healthSlider.value}");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} [HealthComponent] Health slider is not assigned.");
        }

        Debug.Log($"{gameObject.name} [HealthComponent] initialized with {currentHealth.Value} health");
    }

    private new void OnDestroy()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        base.OnDestroy();
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        Debug.Log($"{gameObject.name} [HealthComponent] health changed from {oldHealth} to {newHealth}");

        if (healthSlider != null)
        {
            healthSlider.value = newHealth;
            Debug.Log($"{gameObject.name} [HealthComponent] Slider updated to value {newHealth}");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} [HealthComponent] Health slider reference is missing on the client side.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount)
    {
        if (!IsServer)
        {
            Debug.LogWarning($"[HealthComponent] TakeDamageServerRpc called, but not on the server. Current server status: {IsServer}");
            return;
        }

        Debug.Log($"{gameObject.name} [HealthComponent] taking {amount} damage. Current health before damage: {currentHealth.Value}");

        ApplyDamage(amount);
    }

    private void ApplyDamage(float amount)
    {
        float newHealth = currentHealth.Value - amount;

        // Prevent health from going below zero
        if (newHealth < 0)
        {
            newHealth = 0;
        }

        // Update the NetworkVariable
        currentHealth.Value = newHealth;

        // Confirm if the value is updated
        if (currentHealth.Value == newHealth)
        {
            Debug.Log($"{gameObject.name} [HealthComponent] current health updated successfully to: {currentHealth.Value}");
        }
        else
        {
            Debug.LogError($"{gameObject.name} [HealthComponent] failed to update current health. Current value: {currentHealth.Value}");
        }

        // Handle death if health is zero
        if (currentHealth.Value <= 0)
        {
            Debug.Log($"{gameObject.name} [HealthComponent] has died. Handling death.");
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        Debug.Log($"{gameObject.name} [HealthComponent] has died!");
        // Handle death logic here, like respawning or disabling the player
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsServer)
        {
            Debug.Log($"{gameObject.name} [HealthComponent] collided with {collision.gameObject.name}. Taking {collisionDamage} damage.");

            // For testing purposes, directly apply damage without using the ServerRpc.
            ApplyDamage(collisionDamage);
        }
    }
}
