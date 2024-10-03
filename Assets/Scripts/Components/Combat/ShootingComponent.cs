using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class ShootingComponent : NetworkBehaviour
{
    public List<Transform> shootPoints;
    public GameObject bulletPrefab; // Bullet prefab with NetworkObject component
    public float bulletSpeed = 20f;
    public float shootingCooldown = 0.1f;
    public GameObject muzzleFlashPrefab;

    private float shootingCooldownTimer;

    public void ApplyConfiguration(PlayerSO config)
    {
        bulletSpeed = config.bulletSpeed;
        shootingCooldown = config.shootingCooldown;

        // Bullet lifespan and damage can be applied to each bullet when instantiated
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (shootingCooldownTimer > 0f)
        {
            shootingCooldownTimer -= Time.deltaTime;
        }

        if (Input.GetMouseButton(0))
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

        // Broadcast the shooting event to all clients except the shooter
        BroadcastShootClientRpc(shooterClientId);

        foreach (Transform shootPoint in shootPoints)
        {
            // Instantiate the bullet on the server
            GameObject bulletObject = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);

            // Initialize the bullet with the shooter's client ID
            Bullet bulletScript = bulletObject.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(shooterClientId);
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
