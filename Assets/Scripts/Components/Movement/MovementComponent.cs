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

    // Expose moveInput for external access
    public Vector2 MoveInput => moveInput;

    // Start is called before the first frame update
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (!IsOwner || !canMove) return;

        // Handle Movement Input
        moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // Apply immediate movement for the owning client
        ClientMove();
    }

    private void FixedUpdate()
    {
        if (IsOwner && canMove)
        {
            UpdateMovement();
        }
    }

    /// <summary>
    /// Immediate local movement for the owning client.
    /// </summary>
    private void ClientMove()
    {
        if (moveInput != Vector2.zero)
        {
            Vector2 targetVelocity = moveInput * moveSpeed;
            rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref smoothMoveVelocity, moveSmoothTime);
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // Stop movement when no input is provided
        }
    }

    private void UpdateMovement()
    {
        // Update movement based on the latest input.
        Vector2 targetVelocity = moveInput * moveSpeed;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref smoothMoveVelocity, moveSmoothTime);
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
    }

    public void ApplyConfiguration(PlayerSO config)
    {
        moveSpeed = config.moveSpeed;
        moveSmoothTime = config.moveSmoothTime;
    }
}
