using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Player player;
    public float jumpForce = 7f;
    public float gravity = -20f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;

    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // -------------------------
        // Movement input
        // -------------------------

        float x = 0f;
        float z = 0f;

        if (Keyboard.current.aKey.isPressed) x = -1f;
        if (Keyboard.current.dKey.isPressed) x = 1f;
        if (Keyboard.current.wKey.isPressed) z = 1f;
        if (Keyboard.current.sKey.isPressed) z = -1f;

        bool isWalking = x != 0f || z != 0f;
        animator.SetBool("isWalking", isWalking);

        Vector3 movement = transform.right * x + transform.forward * z;


        // -------------------------
        // Gravity
        // -------------------------

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;


        // -------------------------
        // Jump
        // -------------------------

        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            velocity.y = jumpForce;
            animator.SetBool("isJumping", true);
        }


        // -------------------------
        // Move
        // -------------------------

        Vector3 finalMovement =
            movement * player.playerData.speed + Vector3.up * velocity.y;

        CollisionFlags flags =
            controller.Move(finalMovement * Time.deltaTime);


        // -------------------------
        // Ground detection
        // -------------------------

        isGrounded = (flags & CollisionFlags.Below) != 0;


        // -------------------------
        // Landing
        // -------------------------

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            animator.SetBool("isJumping", false);
        }

    }
}