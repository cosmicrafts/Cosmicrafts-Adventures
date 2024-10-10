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

    public GameObject explosionPrefab; // Reference to explosion prefab
    public float explosionDuration = 1f; // Explosion duration, tweakable in the Inspector

    private ObjectLoader objectLoader; // Reference to ObjectLoader for getting pool index

    // Unified ApplyConfiguration method
    public void ApplyConfiguration(ObjectSO config)
    {
        maxHealth = config.maxHealth;
    }

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

        // Reference to ObjectLoader for pooling purposes
        objectLoader = GetComponent<ObjectLoader>();
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
        // Check if the network object is initialized
        if (IsSpawned)
        {
            ApplyDamage(amount);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} [HealthComponent] Tried to apply damage but object is not fully spawned.");
        }
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
        // Trigger explosion if the prefab is set
        if (explosionPrefab != null)
        {
            GameObject explosionInstance = Instantiate(explosionPrefab, transform.position, transform.rotation);
            
            // Use the explosionDuration to destroy the explosion instance after the set time
            Destroy(explosionInstance, explosionDuration); 
        }
        else
        {
            Debug.LogWarning("Explosion prefab not assigned.");
        }

        // Instead of destroying, return the object to the pool
        if (objectLoader != null)
        {
            int poolIndex = objectLoader.GetPoolIndex(); // Get pool index from the ObjectLoader
            ObjectPooler.Instance.ReturnToPool(gameObject, poolIndex); // Return object to pool
        }
        else
        {
            Debug.LogWarning("ObjectLoader is not found on this object.");
            Destroy(gameObject); // Fallback to destroying if no pooling is available
        }

        // Optionally, disable health slider and any other visuals related to the object before pooling
        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Bullet bullet = collision.gameObject.GetComponent<Bullet>();
            if (bullet != null)
            {
                // Client prediction
                predictedHealth -= bullet.bulletDamage;
                if (predictedHealth < 0)
                    predictedHealth = 0;

                UpdateHealthUI(predictedHealth);

                // Request server to apply damage (remove IsOwner check here)
                TakeDamageServerRpc(bullet.bulletDamage);  // Always request server to apply damage
            }
        }
        else if (IsServer)
        {
            // Handle other types of collisions server-side
            ApplyDamage(collisionDamage);
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
