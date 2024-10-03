using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HealthComponent : NetworkBehaviour
{
    public float maxHealth = 100f;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public Slider healthSlider;
    public float collisionDamage = 4f; // Damage taken upon collision
    private float predictedHealth; // For client-side prediction

    private void Start()
    {
        // Initialize health and setup slider
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        predictedHealth = currentHealth.Value; // Initialize prediction to current health

        currentHealth.OnValueChanged += OnHealthChanged;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth.Value;
        }
    }

    private new void OnDestroy()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        base.OnDestroy();
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        // Server-authoritative value received, adjust the predicted health
        predictedHealth = newHealth;
        UpdateHealthUI(predictedHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount)
    {
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

        // Handle death if health is zero
        if (currentHealth.Value <= 0)
        {
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
        if (IsClient)
        {
            // Check if the collision object is a bullet
            if (collision.gameObject.CompareTag("Bullet"))
            {
                Bullet bullet = collision.gameObject.GetComponent<Bullet>();
                if (bullet != null)
                {
                    // Use bullet damage for prediction and server call
                    float bulletDamage = bullet.bulletDamage;

                    Debug.Log($"{gameObject.name} [HealthComponent] collided with {collision.gameObject.name}. Predicted damage: {bulletDamage}");

                    // Client prediction
                    predictedHealth -= bulletDamage;
                    if (predictedHealth < 0)
                        predictedHealth = 0;

                    UpdateHealthUI(predictedHealth);

                    // Request server to apply damage
                    if (IsOwner)
                    {
                        TakeDamageServerRpc(bulletDamage);
                    }
                }
            }
            else
            {
                // Handle non-bullet collisions with default collision damage
                Debug.Log($"{gameObject.name} [HealthComponent] collided with {collision.gameObject.name}. Predicted damage: {collisionDamage}");

                // Client prediction
                predictedHealth -= collisionDamage;
                if (predictedHealth < 0)
                    predictedHealth = 0;

                UpdateHealthUI(predictedHealth);

                // Request server to apply damage
                if (IsOwner)
                {
                    TakeDamageServerRpc(collisionDamage);
                }
            }
        }
    }

    private void UpdateHealthUI(float healthValue)
    {
        if (healthSlider != null)
        {
            healthSlider.value = healthValue;
        }
    }

    private void Update()
    {
        if (IsClient)
        {
            // Smooth correction towards server-authoritative health
            predictedHealth = Mathf.Lerp(predictedHealth, currentHealth.Value, Time.deltaTime * 10f);
            UpdateHealthUI(predictedHealth);
        }
    }
}
