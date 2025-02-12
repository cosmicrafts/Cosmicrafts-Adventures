using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ShootingComponent : NetworkBehaviour
{
    public List<Transform> shootPoints;
    public GameObject bulletPrefab; // Bullet prefab with NetworkObject component
    public float bulletSpeed = 20f;
    public float shootingCooldown = 0.1f;
    public GameObject muzzleFlashPrefab;

    private float shootingCooldownTimer;
    private TeamComponent teamComponent;

    // Reference to the input controls
    private Controls controls;

    private void Awake()
    {
        controls = new Controls();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Start()
    {
        // Get the TeamComponent attached to this player
        teamComponent = GetComponent<TeamComponent>();
    }

    public void ApplyConfiguration(ObjectSO config)
    {
        bulletSpeed = config.bulletSpeed;
        shootingCooldown = config.shootingCooldown;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (shootingCooldownTimer > 0f)
        {
            shootingCooldownTimer -= Time.deltaTime;
        }

        // Replace legacy Input with the new Input System
        if (controls.Shoot.Shoot.ReadValue<float>() > 0 && shootingCooldownTimer <= 0f)
        {
            RequestShoot();
        }
    }

    /// <summary>
    /// Handles the shoot request from the client.
    /// </summary>
    public void RequestShoot()
    {
        if (shootingCooldownTimer <= 0f)
        {
            ClientShootPrediction(); // Immediate client-side prediction
            ShootServerRpc(); // Send to server for authoritative handling
            shootingCooldownTimer = shootingCooldown;
        }
    }

    /// <summary>
    /// Instantiates a local-only bullet for immediate visual feedback.
    /// </summary>
    private void ClientShootPrediction()
    {
        TeamComponent.TeamTag shooterTeamTag = teamComponent != null ? teamComponent.GetTeam() : TeamComponent.TeamTag.Neutral;

        foreach (Transform shootPoint in shootPoints)
        {
            // Spawn muzzle flash immediately on the client
            if (muzzleFlashPrefab != null)
            {
                GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, shootPoint.position, shootPoint.rotation, shootPoint);
                Destroy(muzzleFlash, 0.1f);
            }

            // Instantiate a local-only bullet for visual feedback
            GameObject clientBullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);

            // Set the bullet to be local-only and remove network components
            Bullet bulletScript = clientBullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(NetworkManager.Singleton.LocalClientId, shooterTeamTag);
                bulletScript.SetLocalOnly();
                Destroy(clientBullet, bulletScript.lifespan); // Destroy after the defined lifespan
            }

            Rigidbody2D bulletRb = clientBullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.bodyType = RigidbodyType2D.Dynamic;
                bulletRb.gravityScale = 0;
                bulletRb.linearVelocity = shootPoint.up * bulletSpeed;
            }
        }
    }

    [ServerRpc]
    private void ShootServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong shooterClientId = rpcParams.Receive.SenderClientId;
        TeamComponent.TeamTag shooterTeamTag = teamComponent != null ? teamComponent.GetTeam() : TeamComponent.TeamTag.Neutral;

        // Broadcast the shooting event to all clients except the shooter
        BroadcastShootClientRpc(shooterClientId);

        foreach (Transform shootPoint in shootPoints)
        {
            // Instantiate the bullet on the server
            GameObject bulletObject = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);

            // Initialize the bullet with the shooter's client ID and team tag
            Bullet bulletScript = bulletObject.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(shooterClientId, shooterTeamTag);
            }

            // Set bullet velocity
            Rigidbody2D bulletRb = bulletObject.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = shootPoint.up * bulletSpeed;
            }

            // Spawn the bullet over the network
            NetworkObject bulletNetworkObject = bulletObject.GetComponent<NetworkObject>();
            if (bulletNetworkObject != null)
            {
                bulletNetworkObject.Spawn();

                // Hide the bullet for all clients
                bulletScript.HideForAllClientsClientRpc();
            }
        }
    }

    [ClientRpc]
    private void BroadcastShootClientRpc(ulong shooterClientId, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId == shooterClientId) return;

        // Handle the shooting locally for other clients
        ClientShootPrediction();
    }
}
