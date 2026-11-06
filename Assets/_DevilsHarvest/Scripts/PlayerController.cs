using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 9f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 200f;

    [Header("Jumping & Gravity")]
    public float jumpHeight = 2.5f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Camera playerCamera;

    private float verticalVelocity;
    private float xRotation;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
    }

    void HandleMovement()
    {
        // Get WASD input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Determine whether player is walking or sprinting
        float currentSpeed = Input.GetKey(KeyCode.LeftShift)
            ? sprintSpeed
            : moveSpeed;

        // Move relative to the direction the player is facing
        Vector3 movement =
            transform.right * horizontal +
            transform.forward * vertical;

        movement *= currentSpeed;

        // Keep the player grounded
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // Jump
        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            verticalVelocity =
                Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;

        // Add vertical movement
        movement.y = verticalVelocity;

        // Actually move the CharacterController
        controller.Move(movement * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        float mouseX =
            Input.GetAxis("Mouse X") *
            mouseSensitivity *
            Time.deltaTime;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            mouseSensitivity *
            Time.deltaTime;

        // Look up/down
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation =
            Quaternion.Euler(xRotation, 0f, 0f);

        // Look left/right
        transform.Rotate(Vector3.up * mouseX);
    }
}