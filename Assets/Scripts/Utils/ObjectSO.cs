using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "ObjectConfiguration", menuName = "Objects/Object Configuration")]
public class ObjectSO : ScriptableObject
{
    public Sprite objectSprite;

    // Collider settings
    public float colliderRadius = 1f;
    public bool isTrigger = false;
    
    // Rigidbody2D settings
    public RigidbodyType2D bodyType = RigidbodyType2D.Dynamic;
    public PhysicsMaterial2D physicsMaterial;
    public bool isKinematic = false;

    // NetworkTransform ownership authority settings
    public enum AuthorityType
    {
        Server,
        Owner
    }

    // MovementComponent settings
    public bool hasMovement = true;
    public float moveSpeed = 5f;
    public float moveSmoothTime = 0.1f;

    // RotationComponent settings
    public bool hasRotation = false;
    public float rotationSpeed = 120f; // Optional, if rotation is enabled

    // ShootingComponent settings
    public bool hasShooting = true;
    public float bulletSpeed = 20f;
    public float shootingCooldown = 0.1f;
    public float bulletDamage = 1f;
    public float bulletLifespan = 1f;

    // HealthComponent settings
    public float maxHealth = 100f;

    // InputComponent settings (for players)
    public bool hasInput = true;

    // TeamComponent settings
    public TeamComponent.TeamTag teamTag = TeamComponent.TeamTag.Neutral;  // Select between Friend, Neutral, and Enemy

    // Optional abilities
    public bool hasDashAbility = false;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    public bool hasBlinkAbility = false;
    public float blinkDistance = 10f;
    public float blinkCooldown = 2f;

    // New field for behavior selection in the Inspector
    public BehaviorSO behavior;
}
