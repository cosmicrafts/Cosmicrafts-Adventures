using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class ShootingComponent : NetworkBehaviour
{
    public List<Transform> shootPoints;
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;
    public float shootingCooldown = 0.1f;
    public GameObject muzzleFlashPrefab;

    private float shootingCooldownTimer;

    public void RequestShoot()
    {
        if (shootingCooldownTimer <= 0f)
        {
            ShootServerRpc();
            shootingCooldownTimer = shootingCooldown;
        }
    }

    private void Update()
    {
        if (shootingCooldownTimer > 0f)
        {
            shootingCooldownTimer -= Time.deltaTime;
        }
    }

    [ServerRpc]
    private void ShootServerRpc(ServerRpcParams rpcParams = default)
    {
        foreach (Transform shootPoint in shootPoints)
        {
            // Instantiate the bullet
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

            // Spawn muzzle flash if it exists
            if (muzzleFlashPrefab != null)
            {
                SpawnMuzzleFlashClientRpc(shootPoint.position, shootPoint.rotation);
            }
        }
    }

    [ClientRpc]
    private void SpawnMuzzleFlashClientRpc(Vector3 position, Quaternion rotation)
    {
        if (muzzleFlashPrefab != null)
        {
            GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, position, rotation);
            muzzleFlash.transform.SetParent(transform); // Set the parent to keep it in sync with player movement
            Destroy(muzzleFlash, 0.1f);
        }
    }
}
