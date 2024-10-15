using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HealthComponent : NetworkBehaviour
{
    public float maxHealth = 100f;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public Slider healthSlider;
    public float collisionDamage = 4f;
    public Animation animationComponent;
    public string damageAnimationName = "Damage";

    private float predictedHealth;
    public GameObject explosionPrefab;
    public float explosionDuration = 1f;
    private bool isDead = false;

    public void ApplyConfiguration(ObjectSO config)
    {
        maxHealth = config.maxHealth;
    }
    
    private void OnEnable()
    {
        isDead = false;
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
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

        if (newHealth < oldHealth && !isDead)
        {
            PlayDamageAnimation();
        }

        if (newHealth <= 0 && !isDead)
        {
            HandleDeath();
        }
    }

    private void PlayDamageAnimation()
    {
        if (animationComponent != null && animationComponent.GetClip(damageAnimationName) != null)
        {
            animationComponent.Play(damageAnimationName);
        }
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
        if (isDead || currentHealth.Value <= 0) return;

        currentHealth.Value = Mathf.Max(currentHealth.Value - amount, 0);

        if (currentHealth.Value <= 0)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        if (explosionPrefab != null)
        {
            GameObject explosionInstance = Instantiate(explosionPrefab, transform.position, transform.rotation);
            Destroy(explosionInstance, explosionDuration);
        }

        if (IsServer)
        {
            NetworkObject netObject = GetComponent<NetworkObject>();
            if (netObject != null && netObject.IsSpawned)
            {
                netObject.Despawn(); // Properly despawn the object from the network
                Destroy(gameObject); // Fully destroy the object
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

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
