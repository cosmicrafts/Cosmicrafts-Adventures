using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class ShootingComponent : NetworkBehaviour
{
    public List<Transform> shootPoints;
    public GameObject bulletPrefab; // Networked bullet prefab
    public float bulletSpeed = 20f;
    public float shootingCooldown = 0.1f;
    public GameObject muzzleFlashPrefab;

    private float shootingCooldownTimer;

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

    public void RequestShoot()
    {
        if (shootingCooldownTimer <= 0f)
        {
            ClientShootPrediction(); // Immediate client-side prediction
            ShootServerRpc(NetworkManager.LocalClientId); // Send to server for authoritative handling
            shootingCooldownTimer = shootingCooldown;
        }
    }

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

            // Instantiate a local-only bullet for visual feedback (no NetworkObject component)
            GameObject clientBullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);
            Rigidbody2D bulletRb = clientBullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = shootPoint.up * bulletSpeed;
            }
            Destroy(clientBullet, 2f); // Destroy after a short time to avoid clutter
        }
    }

    [ServerRpc]
    private void ShootServerRpc(ulong shooterClientId, ServerRpcParams rpcParams = default)
    {
        foreach (Transform shootPoint in shootPoints)
        {
            // Instantiate the bullet on the server
            GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);

            // Set bullet velocity
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = shootPoint.up * bulletSpeed;
            }

            // Spawn the bullet over the network
            NetworkObject bulletNetworkObject = bullet.GetComponent<NetworkObject>();
            if (bulletNetworkObject != null)
            {
                bulletNetworkObject.Spawn();
            }

            // Notify other clients to display the bullet (excluding the shooter)
            SpawnBulletClientRpc(shootPoint.position, shootPoint.rotation, shooterClientId);
        }
    }

    [ClientRpc]
    private void SpawnBulletClientRpc(Vector3 position, Quaternion rotation, ulong shooterClientId, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.LocalClientId == shooterClientId) return; // Shooter already has its own bullet

        // Instantiate the bullet for all other clients
        GameObject bullet = Instantiate(bulletPrefab, position, rotation);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = transform.up * bulletSpeed;
        }
        Destroy(bullet, 2f); // Destroy after a short time
    }
}
