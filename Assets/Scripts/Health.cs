using UnityEngine;
using UnityEngine.UI; // For the health bar
using Unity.Netcode;

public class Health : NetworkBehaviour
{
    public int maxHealth = 10; // Default health (can be different for asteroids)
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        writePerm: NetworkVariableWritePermission.Server
    );

    public GameObject healthBarUI; // Reference to the health bar UI
    public Slider healthSlider; // Slider component for the health bar

    private void Start()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth; // Initialize health only on the server
        }

        // Register callback to update the health bar whenever health changes
        currentHealth.OnValueChanged += OnHealthChanged;
        
        // Set UI elements to initial state
        UpdateHealthUI(currentHealth.Value);
    }

    // Method to take damage
    public void TakeDamage(int damage)
    {
        if (IsServer)
        {
            currentHealth.Value -= damage;
            if (currentHealth.Value <= 0)
            {
                Die();
            }
        }
    }

    // Method to destroy the object
    private void Die()
    {
        if (gameObject.CompareTag("Player"))
        {
            // Implement game over logic here (e.g., disable controls, show game over screen)
            Debug.Log("Player has died!");
        }
        else
        {
            // Destroy the asteroid on the server
            Destroy(gameObject);
        }
    }

    // Callback to update the health bar whenever the health value changes
    private void OnHealthChanged(int previousValue, int newValue)
    {
        UpdateHealthUI(newValue);
    }

    private void UpdateHealthUI(int healthValue)
    {
        healthSlider.value = healthValue;

        // Show the health bar when the entity takes damage
        if (healthValue < maxHealth && !healthBarUI.activeSelf)
        {
            healthBarUI.SetActive(true);
        }
        // Hide the health bar when health is full
        else if (healthValue == maxHealth && healthBarUI.activeSelf)
        {
            healthBarUI.SetActive(false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return; // Ensure collision handling happens only on the server

        if (collision.gameObject.CompareTag("Asteroid"))
        {
            TakeDamage(1);
        }
    }
}
