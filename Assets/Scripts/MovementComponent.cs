using UnityEngine;
using Unity.Netcode;

public class MovementComponent : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float moveSmoothTime = 0.1f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 smoothMoveVelocity;

    private bool canMove = true;

    public void SetCanMove(bool value)
    {
        canMove = value;
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;

        if (IsOwner && canMove)
        {
            // Send movement input to the server
            SendMovementInputServerRpc(moveInput);
        }
    }

    // Expose moveInput
    public Vector2 MoveInput => moveInput;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    [ServerRpc]
    private void SendMovementInputServerRpc(Vector2 input)
    {
        moveInput = input;
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            UpdateMovement();
        }
    }

    private void UpdateMovement()
    {
        if (!canMove) return;

        Vector2 targetVelocity = moveInput * moveSpeed;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref smoothMoveVelocity, moveSmoothTime);
    }
}
