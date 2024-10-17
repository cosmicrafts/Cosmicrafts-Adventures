using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoystickController : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public static JoystickController Instance { get; private set; }

    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private Button shootButton; // Reference to the shooting button

    private Vector2 joystickDirection;
    private Vector2 joystickPosition;
    private bool isDragging;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Add a listener to the shooting button
        if (shootButton != null)
        {
            shootButton.onClick.AddListener(OnShootButtonPressed);
        }
    }

    private void Start()
    {
        if (background == null || handle == null)
        {
            Debug.LogError("Joystick background or handle is not assigned.");
            return;
        }

        joystickPosition = background.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null)
        {
            Debug.LogError("Joystick background or handle is not assigned.");
            return;
        }

        Vector2 touchPosition = eventData.position;
        Vector2 direction = (touchPosition - joystickPosition) / (background.sizeDelta.x / 2);

        // Clamp the direction vector to the radius of the background
        direction = Vector2.ClampMagnitude(direction, 1f);

        // Move the handle
        handle.position = joystickPosition + direction * (background.sizeDelta.x / 2);

        // Update the joystick direction
        joystickDirection = direction;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        handle.position = joystickPosition; // Reset handle position
        joystickDirection = Vector2.zero; // Reset joystick direction
    }

    public Vector2 GetJoystickDirection()
    {
        return joystickDirection;
    }

    // Method to handle the shooting button press
    private void OnShootButtonPressed()
    {
        InputComponent.Instance.OnShootButtonPressed();
    }
}