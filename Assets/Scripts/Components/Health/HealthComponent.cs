using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HealthComponent : NetworkBehaviour
{
    public float maxHealth = 100f;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public Slider healthSlider;
    public float collisionDamage = 4f;

    private float predictedHealth; // For client-side prediction
    public GameObject explosionPrefab; // Reference to explosion prefab
    public float explosionDuration = 1f;

    private ObjectLoader objectLoader; // Reference to ObjectLoader for pooling purposes

    public void ApplyConfiguration(ObjectSO config)
    {
        maxHealth = config.maxHealth;
    }

    private void Start()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        predictedHealth = currentHealth.Value;
        currentHealth.OnValueChanged += OnHealthChanged;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth.Value;
        }

        objectLoader = GetComponent<ObjectLoader>();
    }

    private new void OnDestroy()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        base.OnDestroy();
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
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
    }

    private void ApplyDamage(float amount)
    {
        if (this == null || !this.gameObject.activeInHierarchy) return;

        float newHealth = currentHealth.Value - amount;
        currentHealth.Value = Mathf.Max(newHealth, 0);

        if (currentHealth.Value <= 0)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        if (explosionPrefab != null)
        {
            GameObject explosionInstance = Instantiate(explosionPrefab, transform.position, transform.rotation);
            Destroy(explosionInstance, explosionDuration);
        }

        if (objectLoader != null)
        {
            NetworkObject netObject = GetComponent<NetworkObject>();
            if (netObject != null && netObject.IsSpawned)
            {
                // Despawn for all clients before returning to the pool
                netObject.Despawn();
            }

            NetworkObjectPool.Singleton.ReturnNetworkObject(netObject, objectLoader.GetOriginalPrefab());
        }
        else
        {
            Destroy(gameObject); // Fallback
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
        if (this == null || !this.gameObject.activeInHierarchy) return;

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
