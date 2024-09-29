using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerController : NetworkBehaviour
{
    private Camera mainCamera;

    [Header("Player Health Settings")]
    public int maxHealth = 10;
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>();

    public GameObject healthBarUI;
    public Slider healthSlider;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    private Vector2 moveInput;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        if (IsOwner)
        {
            currentHealth.Value = maxHealth;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth.Value;
            healthBarUI.SetActive(false); // Initially hide the health bar
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleInput();

        // Predict movement locally
        Vector2 predictedVelocity = moveInput * moveSpeed;
        rb.linearVelocity = predictedVelocity;

        // Send movement input to the server
        SendMovementInputServerRpc(moveInput);
    }

    [ServerRpc]
    private void SendMovementInputServerRpc(Vector2 input)
    {
        // Only the server should update the rigidbody's velocity
        rb.linearVelocity = input * moveSpeed;

        // Synchronize position with all clients
        UpdatePositionClientRpc(rb.position);
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector2 serverPosition)
    {
        if (IsOwner) return;

        // Correct the client position if it differs from the server's authoritative position
        rb.position = Vector2.Lerp(rb.position, serverPosition, 0.1f);
    }

    private void HandleInput()
    {
        // Get input for movement
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");
    }
}
