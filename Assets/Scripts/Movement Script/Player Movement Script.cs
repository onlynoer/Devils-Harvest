using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerMovement : MonoBehaviour
{
    
    public float speed = 5f;
    public float jumpForce = 7f;
    public float gravity = -20f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update(){
    // WASD movement
    float x = 0f;
    float z = 0f;

    if (Keyboard.current.aKey.isPressed) x = -1f;
    if (Keyboard.current.dKey.isPressed) x = 1f;
    if (Keyboard.current.wKey.isPressed) z = 1f;
    if (Keyboard.current.sKey.isPressed) z = -1f;

    bool isWalking = x != 0f || z != 0f;
    animator.SetBool("isWalking", isWalking);

    Vector3 movement = transform.right * x + transform.forward * z;
    controller.Move(movement * speed * Time.deltaTime);

    // Ground
    if (controller.isGrounded && velocity.y < 0)
    {
        velocity.y = -2f;
    }

    // Jump
    if (Keyboard.current.spaceKey.wasPressedThisFrame)
    {
        velocity.y = jumpForce;
        Debug.Log("JUMP!");
    }

    // Gravity
    velocity.y += gravity * Time.deltaTime;

    // Vertical movement
    controller.Move(velocity * Time.deltaTime);
    }
}