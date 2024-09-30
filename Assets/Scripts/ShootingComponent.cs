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
    private void ShootServerRpc()
    {
        foreach (Transform shootPoint in shootPoints)
        {
            GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);
            bullet.GetComponent<Rigidbody2D>().linearVelocity = shootPoint.up * bulletSpeed;
            bullet.GetComponent<NetworkObject>().Spawn();

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
            Destroy(muzzleFlash, 0.1f);
        }
    }
}
