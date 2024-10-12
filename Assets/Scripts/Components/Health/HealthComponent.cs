using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HealthComponent : NetworkBehaviour
{
    public float maxHealth = 100f;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public Slider healthSlider;
    public float collisionDamage = 4f;

    private float predictedHealth;
    public GameObject explosionPrefab;
    public float explosionDuration = 1f;
    private bool isDead = false;
    private ObjectLoader objectLoader;

    public void ApplyConfiguration(ObjectSO config)
    {
        maxHealth = config.maxHealth;
    }

    // OnEnable to reset object when reused from pool
    private void OnEnable()
    {
        isDead = false; // Reset isDead flag when object is re-enabled
        if (IsServer)
        {
            currentHealth.Value = maxHealth; // Reset health on server
        }
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
        if (IsSpawned && !isDead)
        {
            ApplyDamage(amount);
        }
    }

    private void ApplyDamage(float amount)
    {
        if (isDead || currentHealth.Value <= 0) return;  // Simplified conditions

        currentHealth.Value = Mathf.Max(currentHealth.Value - amount, 0);

        if (currentHealth.Value <= 0)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        if (isDead) return; // Simplified multiple death check

        isDead = true;

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
                NetworkObjectPool.Singleton.ReturnNetworkObject(netObject, objectLoader.GetOriginalPrefab());
                DeactivateClientRpc();
            }
        }
    }

    [ClientRpc]
    private void DeactivateClientRpc()
    {
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return; // Only check if already dead

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
