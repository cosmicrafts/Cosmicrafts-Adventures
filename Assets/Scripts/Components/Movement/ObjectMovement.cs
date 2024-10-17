using UnityEngine;
using Unity.Netcode;

public class BallComponent : NetworkBehaviour
{
    private Rigidbody2D rb;

    // Network variable to track the ball's position on the server
    private NetworkVariable<Vector2> ballPosition = new NetworkVariable<Vector2>();

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // Only the server has authority over the ball's physics
        if (IsServer)
        {
            ballPosition.Value = rb.position;  // Update the network variable with the ball's current position
        }
        else
        {
            // Non-authoritative clients update their local position to match the server's position
            rb.position = Vector2.Lerp(rb.position, ballPosition.Value, Time.fixedDeltaTime * 10f);
        }
    }

    // Server-side method to apply force when a player dashes into the ball
    [ServerRpc]
    public void ApplyDashForceServerRpc(Vector2 force)
    {
        rb.AddForce(force, ForceMode2D.Impulse);  // Apply force to the ball
    }
}
