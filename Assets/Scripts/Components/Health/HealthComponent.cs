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

        if (newHealth < 0)
        {
            newHealth = 0;
        }

        currentHealth.Value = newHealth;

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
            Destroy(explosionInstance, explosionDuration); 
        }

        // Return the object to the pool using the original prefab
        if (objectLoader != null)
        {
            NetworkObjectPool.Singleton.ReturnNetworkObject(GetComponent<NetworkObject>(), objectLoader.GetOriginalPrefab()); // Use the original prefab here
        }
        else
        {
            Destroy(gameObject); // Fallback to destroying if no pooling is available
        }

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
                predictedHealth -= bullet.bulletDamage;
                if (predictedHealth < 0)
                    predictedHealth = 0;

                UpdateHealthUI(predictedHealth);

                TakeDamageServerRpc(bullet.bulletDamage);
            }
        }
        else if (IsServer)
        {
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
            predictedHealth = Mathf.Lerp(predictedHealth, currentHealth.Value, Time.deltaTime * 10f);
            UpdateHealthUI(predictedHealth);
        }
    }
}
