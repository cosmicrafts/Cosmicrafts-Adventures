using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class MovementComponent : NetworkBehaviour
{
    public float moveSpeed = 5f;  // Movement speed
    public float moveSmoothTime = 0.1f;  // Smoothing time for the movement

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 smoothMoveVelocity;
    private bool canMove = true;

    // Reference to the input controls
    private Controls controls;

    private void Awake()
    {
        controls = new Controls();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    public void ApplyConfiguration(ObjectSO config)
    {
        moveSpeed = config.moveSpeed;
        moveSmoothTime = config.moveSmoothTime;
    }

    public Vector2 MoveInput => moveInput;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;  // Prevent any rotation caused by physics
    }

    private void Update()
    {
        if (!IsOwner || !canMove) return;

        // Get input from the new input system (replacing the old Input.GetAxis)
        moveInput = controls.Move.Moveaction.ReadValue<Vector2>();

        ClientMove();
    }

    private void FixedUpdate()
    {
        if (IsOwner && canMove)
        {
            UpdateMovement();  // Apply movement in FixedUpdate for physics consistency
        }
    }

    private void ClientMove()
    {
        // Move only if there's input
        if (moveInput != Vector2.zero)
        {
            Vector2 targetVelocity = moveInput.normalized * moveSpeed;  // Normalize input and scale by speed
            rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref smoothMoveVelocity, moveSmoothTime);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;  // Stop movement when there's no input
        }
    }

    private void UpdateMovement()
    {
        Vector2 targetVelocity = moveInput.normalized * moveSpeed;  // Normalize and apply speed
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref smoothMoveVelocity, moveSmoothTime);
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input; // Store the received input

        // If the input is non-zero, apply movement speed
        if (moveInput != Vector2.zero)
        {
            Vector2 normalizedMoveInput = moveInput.normalized;
            Vector2 targetVelocity = normalizedMoveInput * moveSpeed;
            rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref smoothMoveVelocity, moveSmoothTime);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
    }
}
