using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerController : NetworkBehaviour
{
    private Camera mainCamera;
    private Health health;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float moveSmoothTime = 0.1f;

    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Shooting Settings")]
    public List<Transform> shootPoints;
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;
    public float shootingCooldown = 0.1f;
    public GameObject muzzleFlashPrefab;

    [Header("Thrusters Settings")]
    public List<GameObject> thrusters;

    [Header("Camera Zoom Settings")]
    public float zoomSmoothSpeed = 5f;
    private readonly float[] zoomLevels = { 8f, 16f, 24f, 36f };
    private int currentZoomIndex;

    private Vector2 moveInput;
    private Vector2 smoothMoveVelocity;
    private float zoomInput;
    private Vector3 mouseWorldPosition;

    private bool isShooting;
    private bool canMove = true;
    private float dashCooldownTimer;
    private float shootingCooldownTimer;
    private Rigidbody2D rb;

    private NetworkVariable<bool> isDashing = new NetworkVariable<bool>
    (false, NetworkVariableReadPermission.Everyone,
     NetworkVariableWritePermission.Server);

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        if (IsOwner)
        {
            currentZoomIndex = GetClosestZoomIndex(mainCamera.orthographicSize);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            mainCamera = Camera.main;
            currentZoomIndex = GetClosestZoomIndex(mainCamera.orthographicSize);
        }

        // Subscribe to changes in isDashing to handle state synchronization
        isDashing.OnValueChanged += (oldValue, newValue) =>
        {
            if (newValue)
            {
                StartDashLocal();
            }
        };
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleInput();
        HandleDashTimer();
        HandleShooting();
        HandleCameraZoom();
    }

    private void FixedUpdate()
    {
        if (IsOwner && canMove)
        {
            SendMovementInputServerRpc(moveInput);
            SendRotationInputServerRpc(mouseWorldPosition);
        }

        if (IsServer && !IsOwner)
        {
            UpdateMovement();
        }
    }

    [ServerRpc]
    private void SendMovementInputServerRpc(Vector2 input)
    {
        moveInput = input;
        UpdateMovement();
    }

    [ServerRpc]
    private void SendRotationInputServerRpc(Vector3 mousePosition)
    {
        HandleRotation(mousePosition);
        UpdateRotationClientRpc(mousePosition);
    }

    [ClientRpc]
    private void UpdateRotationClientRpc(Vector3 mousePosition)
    {
        HandleRotation(mousePosition);
    }

    private void HandleRotation(Vector3 mousePosition)
    {
        if (mousePosition == Vector3.zero) return;

        Vector2 direction = (mousePosition - transform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        rb.SetRotation(targetAngle);
    }

    private void HandleInput()
    {
        // Get input for movement
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");

        // Check for dash input
        if (Input.GetKeyDown(KeyCode.Space) && !isDashing.Value && dashCooldownTimer <= 0f)
        {
            RequestDashServerRpc();
        }

        // Check for shooting input
        isShooting = Input.GetMouseButton(0);

        // Handle mouse position
        mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -mainCamera.transform.position.z));

        // Handle zoom input
        zoomInput = Input.GetAxis("Mouse ScrollWheel");
    }

    private void UpdateMovement()
    {
        if (isDashing.Value) return;

        Vector2 targetVelocity = moveInput * moveSpeed;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref smoothMoveVelocity, moveSmoothTime);
    }

    private void StartDashLocal()
    {
        isDashing.Value = true;
        rb.linearVelocity = transform.up * dashSpeed;
        Invoke(nameof(EndDash), dashDuration);
        dashCooldownTimer = dashCooldown;
        Debug.Log($"[Client] Player {OwnerClientId} started dashing. Cooldown set to {dashCooldownTimer}.");
    }

    private void EndDash()
    {
        isDashing.Value = false;
        canMove = true;
        Debug.Log($"[Client] Player {OwnerClientId} ended dashing. Dash state set to false.");
    }

    private void HandleDashTimer()
    {
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
            Debug.Log($"[Client] Player {OwnerClientId} dash cooldown timer: {dashCooldownTimer}");
        }
        if (dashCooldownTimer < 0f)
        {
            dashCooldownTimer = 0f;
            Debug.Log($"[Client] Player {OwnerClientId} dash cooldown timer reset to 0.");
        }
    }

    [ServerRpc]
    private void RequestDashServerRpc()
    {
        if (!isDashing.Value && dashCooldownTimer <= 0f)
        {
            StartDashLocal();
            
            // Reset cooldown timer on the server
            dashCooldownTimer = dashCooldown;
            Debug.Log($"[Server] Dash cooldown timer set to {dashCooldownTimer} for Player {OwnerClientId}.");

            // Notify the client about the cooldown reset
            UpdateCooldownClientRpc(dashCooldown);
        }
        else
        {
            Debug.Log($"[Server] Player {OwnerClientId} tried to dash but is currently unable to. Cooldown Timer: {dashCooldownTimer}, isDashing: {isDashing.Value}");
        }
    }

    [ClientRpc]
    private void UpdateCooldownClientRpc(float cooldown)
    {
        dashCooldownTimer = cooldown;
        Debug.Log($"[Client] Cooldown timer reset to {dashCooldownTimer} for Player {OwnerClientId}.");
    }

    private void HandleShooting()
    {
        if (isShooting && shootingCooldownTimer <= 0f)
        {
            ShootServerRpc();
            shootingCooldownTimer = shootingCooldown;
        }

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
                GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, shootPoint.position, shootPoint.rotation, shootPoint);
                Destroy(muzzleFlash, 0.1f);
            }
        }
    }

    private void HandleCameraZoom()
    {
        if (zoomInput > 0 && currentZoomIndex > 0)
        {
            currentZoomIndex--;
        }
        else if (zoomInput < 0 && currentZoomIndex < zoomLevels.Length - 1)
        {
            currentZoomIndex++;
        }

        zoomInput = 0;

        float targetZoom = zoomLevels[currentZoomIndex];
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetZoom, Time.deltaTime * zoomSmoothSpeed);
    }

    private int GetClosestZoomIndex(float currentZoom)
    {
        int closestIndex = 0;
        float closestDifference = Mathf.Abs(currentZoom - zoomLevels[0]);

        for (int i = 1; i < zoomLevels.Length; i++)
        {
            float difference = Mathf.Abs(currentZoom - zoomLevels[i]);
            if (difference < closestDifference)
            {
                closestDifference = difference;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Asteroid"))
        {
            health.TakeDamage(1);
        }
    }
}