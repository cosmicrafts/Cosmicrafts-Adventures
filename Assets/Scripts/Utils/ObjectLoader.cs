using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class ObjectLoader : NetworkBehaviour
{
    [SerializeField]
    public ObjectSO objectConfiguration;

    public override void OnNetworkSpawn()
    {
        if (IsServer && objectConfiguration != null)
        {
            ApplyConfiguration();
        }
    }

    public void ApplyConfiguration()
    {
        if (objectConfiguration == null)
        {
            Debug.LogWarning($"{gameObject.name} [ObjectLoader] Configuration is not assigned.");
            return;
        }

        // Apply SpriteRenderer configuration
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && objectConfiguration.objectSprite != null)
        {
            spriteRenderer.sprite = objectConfiguration.objectSprite;
        }

        // Apply NetworkObject ownership configuration
        var networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && objectConfiguration.hasNetworkOwnership)
        {
            networkObject.ChangeOwnership(NetworkManager.Singleton.LocalClientId);
        }

        // Apply CircleCollider2D configuration
        var collider = GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            collider.radius = objectConfiguration.colliderRadius;
            collider.isTrigger = objectConfiguration.isTrigger;
        }

        // Apply NetworkTransform configuration
        var networkTransform = GetComponent<NetworkTransform>();
        if (networkTransform != null)
        {
            networkTransform.Interpolate = objectConfiguration.interpolateTransform;
        }

        // Apply Rigidbody2D configuration
        var rigidbody2D = GetComponent<Rigidbody2D>();
        if (rigidbody2D != null)
        {
            rigidbody2D.bodyType = objectConfiguration.isKinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
            rigidbody2D.sharedMaterial = objectConfiguration.physicsMaterial;
        }

        // Apply MovementComponent configuration
        var movementComponent = GetComponent<MovementComponent>();
        if (movementComponent != null)
        {
            movementComponent.enabled = objectConfiguration.hasMovement;
            if (movementComponent.enabled)
            {
                movementComponent.ApplyConfiguration(objectConfiguration);
            }
        }

        // Apply RotationComponent configuration
        var rotationComponent = GetComponent<RotationComponent>();
        if (rotationComponent != null)
        {
            rotationComponent.enabled = objectConfiguration.hasRotation;
            if (rotationComponent.enabled)
            {
                rotationComponent.ApplyConfiguration(objectConfiguration);
            }
        }

        // Apply ShootingComponent configuration
        var shootingComponent = GetComponent<ShootingComponent>();
        if (shootingComponent != null)
        {
            shootingComponent.enabled = objectConfiguration.hasShooting;
            if (shootingComponent.enabled)
            {
                shootingComponent.ApplyConfiguration(objectConfiguration);
            }
        }

        // Apply HealthComponent configuration
        var healthComponent = GetComponent<HealthComponent>();
        if (healthComponent != null)
        {
            healthComponent.ApplyConfiguration(objectConfiguration);
        }

        // Apply TeamComponent configuration
        var teamComponent = GetComponent<TeamComponent>();
        if (teamComponent != null)
        {
            teamComponent.ApplyConfiguration(objectConfiguration);
        }

        // Apply optional abilities (Dash/Blink)
        var dashComponent = GetComponent<DashComponent>();
        if (dashComponent != null)
        {
            dashComponent.enabled = objectConfiguration.hasDashAbility;
            if (dashComponent.enabled)
            {
                dashComponent.ApplyConfiguration(objectConfiguration);
            }
        }

        var blinkComponent = GetComponent<BlinkComponent>();
        if (blinkComponent != null)
        {
            blinkComponent.enabled = objectConfiguration.hasBlinkAbility;
            if (blinkComponent.enabled)
            {
                blinkComponent.ApplyConfiguration(objectConfiguration);
            }
        }
    }
}
