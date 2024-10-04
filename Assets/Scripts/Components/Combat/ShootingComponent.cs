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
            // ShootServerRpc() sends the shoot request to the server
            ShootServerRpc();
            shootingCooldownTimer = shootingCooldown;
        }
    }

    [ServerRpc]
    private void ShootServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong shooterClientId = rpcParams.Receive.SenderClientId;

        foreach (Transform shootPoint in shootPoints)
        {
            // Instantiate the bullet on the server
            GameObject bulletObject = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);

            // Set bullet velocity on the server initially
            Rigidbody2D bulletRb = bulletObject.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.bodyType = RigidbodyType2D.Dynamic; // Ensure it's dynamic
                bulletRb.gravityScale = 0; // Ensure gravity does not affect the bullet
                bulletRb.linearVelocity = shootPoint.up * bulletSpeed; // Set the velocity
            }

            // Spawn the bullet over the network with ownership to the client
            NetworkObject bulletNetworkObject = bulletObject.GetComponent<NetworkObject>();
            if (bulletNetworkObject != null)
            {
                bulletNetworkObject.SpawnWithOwnership(shooterClientId);

                // Notify the owning client to ensure bullet velocity is properly set
                SetBulletVelocityClientRpc(bulletNetworkObject.NetworkObjectId, shootPoint.up * bulletSpeed);
            }
        }
    }

    [ClientRpc]
    private void SetBulletVelocityClientRpc(ulong bulletNetworkObjectId, Vector2 velocity)
    {
        // Find the bullet by its NetworkObjectId and set the velocity
        NetworkObject bulletNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[bulletNetworkObjectId];
        if (bulletNetworkObject != null && bulletNetworkObject.TryGetComponent<Rigidbody2D>(out var bulletRb))
        {
            bulletRb.linearVelocity = velocity;
        }
    }
}
