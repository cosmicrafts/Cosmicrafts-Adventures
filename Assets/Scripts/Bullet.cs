using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    public float lifespan = 5f; // Time before the bullet is automatically destroyed
    private ulong shooterClientId;

    public void Initialize(ulong shooterClientId)
    {
        this.shooterClientId = shooterClientId;
    }

    private void Start()
    {
        // Destroy the bullet after its lifespan
        Destroy(gameObject, lifespan);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        // Destroy the bullet upon collision
        Destroy(gameObject);
    }
}
