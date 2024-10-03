using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class MovementComponent : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float moveSmoothTime = 0.1f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 smoothMoveVelocity;

    private bool isLocalOnly = false; // Flag to differentiate between networked and local-only player
    private bool canMove = true; // Indicates if the player can move

    // Expose moveInput for external access (required by RotationComponent)
    public Vector2 MoveInput => moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Determine if this instance should be client-only or server-authoritative
        if (IsOwner && !IsServer)
        {
            SetLocalOnly();
        }
    }

    public void SetLocalOnly()
    {
        // Mark this player as local-only and disable network components
        isLocalOnly = true;

        // Remove or disable network components to prevent server synchronization
        if (TryGetComponent<NetworkObject>(out var netObj))
        {
            Destroy(netObj);
        }

        if (TryGetComponent<NetworkRigidbody2D>(out var netRigidbody))
        {
            Destroy(netRigidbody);
        }
    }

    public void SetCanMove(bool value)
    {
        // Allow or disallow player movement
        canMove = value;
    }

    private void Update()
    {
        if (IsOwner && canMove)
        {
            // Handle input for movement
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            SetMoveInput(input);

            // Apply immediate movement locally if it's the client-only object
            if (isLocalOnly)
            {
                ClientMovePrediction();
            }
            else
            {
                // If it's the server-authoritative version, send movement input to the server
                RequestMovementServerRpc(moveInput);
            }
        }
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    /// <summary>
    /// Handles the immediate client-side prediction for the movement.
    /// </summary>
    private void ClientMovePrediction()
    {
        if (!canMove) return;

        Vector3 targetPosition = transform.position + new Vector3(moveInput.x, moveInput.y, 0f) * moveSpeed * Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, targetPosition, 0.5f); // Adjust lerp factor as needed
    }


    [ServerRpc]
    private void RequestMovementServerRpc(Vector2 input, ServerRpcParams serverRpcParams = default)
    {
        moveInput = input;

        // Apply server-authoritative movement
        UpdateServerMovement();
    }

    private void FixedUpdate()
    {
        // Only run the server-authoritative update if this is the server version
        if (IsServer && !isLocalOnly && canMove)
        {
            UpdateServerMovement();
        }
    }

    /// <summary>
    /// Updates the server-authoritative movement of the player.
    /// </summary>
    private void UpdateServerMovement()
    {
        if (!canMove) return;

        // Calculate the target velocity based on server input
        Vector2 targetVelocity = moveInput * moveSpeed;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref smoothMoveVelocity, moveSmoothTime);
    }

    [ClientRpc]
    private void HideForAllClientsClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!IsServer)
        {
            // Hide this server-authoritative version for the owning client
            SetVisibility(false);
        }
    }

    private void SetVisibility(bool visible)
    {
        // Hide or show the renderer based on visibility flag
        if (TryGetComponent<Renderer>(out var renderer))
        {
            renderer.enabled = visible;
        }

        if (TryGetComponent<Collider2D>(out var collider))
        {
            collider.enabled = visible;
        }
    }
}
