using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook : MonoBehaviour
{
    public Transform player;
    public float mouseSensitivity = 100f;

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Vector2 mouseInput = Mouse.current.delta.ReadValue();

        // Horizontal/Vertical sensitivity are the player's settings from the Controls page (1 = default).
        float mouseX = mouseInput.x * mouseSensitivity * ControlSettings.HorizontalSensitivity * Time.deltaTime;
        float mouseY = mouseInput.y * mouseSensitivity * ControlSettings.VerticalSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 70f); // limit vertical rotation 70 degrees down and 90 degrees up

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        player.Rotate(Vector3.up * mouseX);
    }
}