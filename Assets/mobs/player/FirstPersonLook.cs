using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50)]
public class FirstPersonLook : MonoBehaviour
{
    public Transform player;
    public float mouseSensitivity = 100f;

    private float xRotation = 0f;

    private float recoil;
    public void Kick(float degrees) => recoil = Mathf.Min(5f, recoil + degrees);
    public void ClearKick()
    {
        recoil = 0f;
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!GameInput.GameplayEnabled) return;
        Vector2 mouseInput = Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue();
        recoil = Mathf.MoveTowards(recoil, 0f, 10f * Time.deltaTime);

        // Horizontal/Vertical sensitivity are the player's settings from the Controls page (1 = default).
        float mouseX = mouseInput.x * mouseSensitivity * ControlSettings.HorizontalSensitivity * Time.deltaTime;
        float mouseY = mouseInput.y * mouseSensitivity * ControlSettings.VerticalSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 70f); // limit vertical rotation 70 degrees down and 90 degrees up

        transform.localRotation = Quaternion.Euler(Mathf.Clamp(xRotation - recoil, -90f, 70f), 0f, 0f);

        player.Rotate(Vector3.up * mouseX);
    }
}