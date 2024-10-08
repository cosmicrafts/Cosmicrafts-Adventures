using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfiguration", menuName = "Players/Player Configuration")]
public class PlayerSO : ScriptableObject
{
    // TeamComponent settings
    public TeamComponent.TeamTag teamTag = TeamComponent.TeamTag.Neutral;
    public bool isEnemy = false;

    // Base attributes
    public float moveSpeed = 5f;
    public float moveSmoothTime = 0.1f;
    public float maxHealth = 100f;

    // Optional components
    public bool hasMovement = true;        // Toggle movement
    public bool hasRotation = false;       // Toggle rotation
    public bool hasShooting = true;        // Toggle shooting
    public bool hasDashAbility = false;    // Optional dash
    public bool hasBlinkAbility = false;   // Optional blink
    public bool hasInput = true;           // Optional input

    // Shooting attributes
    public float bulletSpeed = 20f;
    public float bulletDamage = 1f;
    public float bulletLifespan = 1f;
    public float shootingCooldown = 0.1f;

    // Dash attributes
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    // Blink attributes
    public float blinkDistance = 10f;
    public float blinkCooldown = 2f;

    // Sprite for visual representation
    public Sprite playerSprite;
}
