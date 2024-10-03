using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfiguration", menuName = "Players/Player Configuration")]
public class PlayerSO : ScriptableObject
{
    // Base attributes
    public float moveSpeed = 5f;
    public float moveSmoothTime = 0.1f;
    public float maxHealth = 100f;

    // Shooting attributes
    public float bulletSpeed = 20f;
    public float bulletDamage = 1f;
    public float bulletLifespan = 1f;
    public float shootingCooldown = 0.1f;

    // Dash attributes (optional)
    public bool hasDashAbility = false;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    // Blink attributes (optional)
    public bool hasBlinkAbility = false;
    public float blinkDistance = 10f;
    public float blinkCooldown = 2f;

    // Sprite for visual representation
    public Sprite playerSprite;

    // Add more attributes if needed
}
