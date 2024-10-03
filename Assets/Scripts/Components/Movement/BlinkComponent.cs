using UnityEngine;
using Unity.Netcode;

public class BlinkComponent : NetworkBehaviour
{
    public float blinkDistance = 10f;
    public float blinkCooldown = 2f;

    private float blinkCooldownTimer;

    private void Update()
    {
        if (IsOwner && blinkCooldownTimer > 0f)
        {
            blinkCooldownTimer -= Time.deltaTime;
        }

        if (IsOwner && Input.GetKeyDown(KeyCode.B) && blinkCooldownTimer <= 0f)
        {
            RequestBlink();
        }
    }

    private void RequestBlink()
    {
        if (IsServer)
        {
            PerformBlink();
        }
        else
        {
            RequestBlinkServerRpc();
        }
        blinkCooldownTimer = blinkCooldown;
    }

    [ServerRpc]
    private void RequestBlinkServerRpc()
    {
        PerformBlink();
    }

    private void PerformBlink()
    {
        Vector3 blinkDirection = transform.up * blinkDistance;
        transform.position += blinkDirection;
    }
}
