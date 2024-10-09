using UnityEngine;

public static class ConfigurationApplier
{
    public static void ApplyConfiguration(GameObject gameObject, ObjectSO objectConfiguration)
    {
        if (objectConfiguration == null)
        {
            Debug.LogWarning($"{gameObject.name} [ConfigurationApplier] Configuration is not assigned.");
            return;
        }

        // Apply SpriteRenderer configuration
        var spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && objectConfiguration.objectSprite != null)
        {
            spriteRenderer.sprite = objectConfiguration.objectSprite;
        }

        // Apply CircleCollider2D configuration
        var collider = gameObject.GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            collider.radius = objectConfiguration.colliderRadius;
            collider.isTrigger = objectConfiguration.isTrigger;
        }

        // Apply Rigidbody2D configuration
        var rigidbody2D = gameObject.GetComponent<Rigidbody2D>();
        if (rigidbody2D != null)
        {
            rigidbody2D.bodyType = objectConfiguration.isKinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
            rigidbody2D.sharedMaterial = objectConfiguration.physicsMaterial;
        }

        // Apply MovementComponent configuration
        var movementComponent = gameObject.GetComponent<MovementComponent>();
        if (movementComponent != null)
        {
            movementComponent.enabled = objectConfiguration.hasMovement;
            if (movementComponent.enabled)
            {
                movementComponent.ApplyConfiguration(objectConfiguration);
            }
        }

        // Apply RotationComponent configuration
        var rotationComponent = gameObject.GetComponent<RotationComponent>();
        if (rotationComponent != null)
        {
            rotationComponent.enabled = objectConfiguration.hasRotation;
            if (rotationComponent.enabled)
            {
                rotationComponent.ApplyConfiguration(objectConfiguration);
            }
        }

        // Apply ShootingComponent configuration
        var shootingComponent = gameObject.GetComponent<ShootingComponent>();
        if (shootingComponent != null)
        {
            shootingComponent.enabled = objectConfiguration.hasShooting;
            if (shootingComponent.enabled)
            {
                shootingComponent.ApplyConfiguration(objectConfiguration);
            }
        }

        // Apply HealthComponent configuration
        var healthComponent = gameObject.GetComponent<HealthComponent>();
        if (healthComponent != null)
        {
            healthComponent.ApplyConfiguration(objectConfiguration);
        }

        // Apply TeamComponent configuration
        var teamComponent = gameObject.GetComponent<TeamComponent>();
        if (teamComponent != null)
        {
            teamComponent.ApplyConfiguration(objectConfiguration);
        }

        // Apply optional abilities (Dash/Blink)
        var dashComponent = gameObject.GetComponent<DashComponent>();
        if (dashComponent != null)
        {
            dashComponent.enabled = objectConfiguration.hasDashAbility;
            if (dashComponent.enabled)
            {
                dashComponent.ApplyConfiguration(objectConfiguration);
            }
        }

        var blinkComponent = gameObject.GetComponent<BlinkComponent>();
        if (blinkComponent != null)
        {
            blinkComponent.enabled = objectConfiguration.hasBlinkAbility;
            if (blinkComponent.enabled)
            {
                blinkComponent.ApplyConfiguration(objectConfiguration);
            }
        }

        // Apply behavior if assigned
        if (objectConfiguration.behavior != null)
        {
            objectConfiguration.behavior.ApplyBehavior(gameObject);
        }
    }
}
