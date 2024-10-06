using UnityEngine;

public class Asteroid : MonoBehaviour
{
    public GameObject explosionPrefab; // Reference to explosion prefab
    public float explosionDestroyDelay = 0.5f; // Time to destroy the explosion effect
    public int maxHealth = 4; // Maximum health of the asteroid
    private int currentHealth;
    
    public GameObject healthBarUI; // Health bar UI
    public UnityEngine.UI.Slider healthSlider; // Health bar slider

    private void Start()
    {
        // Set a random health between 1 and 4
        currentHealth = Random.Range(1, maxHealth + 1);
        healthSlider.maxValue = currentHealth;
        healthSlider.value = currentHealth;
        healthBarUI.SetActive(false); // Hide health bar initially
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            // Instantiate the explosion effect and destroy it after a fixed delay
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, explosionDestroyDelay); // Destroy the explosion effect after delay
            Destroy(gameObject); // Destroy the asteroid
        }
        else
        {
            healthBarUI.SetActive(true); // Show health bar if damaged
            healthSlider.value = currentHealth; // Update health bar
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TakeDamage(1); // Take 1 damage if hit by player
        }
    }
}
