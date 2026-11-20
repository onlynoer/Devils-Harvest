
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Player player;
    public float jumpForce = 7f;
    public float gravity = -20f;

    [Header("Sprint")]
    public float sprintMultiplier = 1.5f;

    public bool IsSprinting { get; private set; }
    private CharacterController controller;
    [SerializeField] private AudioSource footfall;
    private float stepTravel;
    private Animator animator;
    private Vector3 velocity;

    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;

    public void ResetMotion()
    {
        stepTravel = 0f;
        IsSprinting = false;
        velocity = Vector3.zero;
        isGrounded = false;
        groundNormal = Vector3.up;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y > 0f && Vector3.Angle(Vector3.up, hit.normal) <= controller.slopeLimit)
            groundNormal = hit.normal;

        var rock = hit.collider.GetComponentInParent<DevilsHarvest.RockfallBoulder>();
        if (rock != null) rock.TryImpact(player);
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Movement input
        float x = 0f;
        float z = 0f;

        if (Keyboard.current.aKey.isPressed) x = -1f;
        if (Keyboard.current.dKey.isPressed) x = 1f;
        if (Keyboard.current.wKey.isPressed) z = 1f;
        if (Keyboard.current.sKey.isPressed) z = -1f;

        bool isWalking = x != 0f || z != 0f;
        animator.SetBool("isWalking", isWalking);

        Vector3 movement = transform.right * x + transform.forward * z;
        movement = Vector3.ClampMagnitude(movement, 1f);

        // Sprint
        IsSprinting =
            (Keyboard.current.leftShiftKey.isPressed ||
             Keyboard.current.rightShiftKey.isPressed)
            && isWalking;

        float currentSpeed = player.playerData.speed;

        if (IsSprinting)
        {
            currentSpeed *= sprintMultiplier;
        }

        // Gravity
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;

        // Jump
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            velocity.y = jumpForce;
            animator.SetBool("isJumping", true);
        }

        // Move
        Vector3 finalMovement =
            movement * currentSpeed + Vector3.up * velocity.y;

        // Follow a walkable descent at the same horizontal speed instead of stepping into midair.
        if (isGrounded && velocity.y < 0f)
        {
            float slopeVelocity = -Vector3.Dot(movement * currentSpeed, groundNormal) / groundNormal.y;
            finalMovement.y += Mathf.Min(0f, slopeVelocity);
        }
        groundNormal = Vector3.up;
        CollisionFlags flags =
            controller.Move(finalMovement * Time.deltaTime);

        // Ground detection
        isGrounded = (flags & CollisionFlags.Below) != 0;

        if (footfall != null && isWalking && isGrounded)
        {
            Vector3 groundVelocity = controller.velocity;
            groundVelocity.y = 0f;
            stepTravel += groundVelocity.magnitude * Time.deltaTime;
            if (stepTravel >= player.playerData.speed * .46f)
            {
                stepTravel = 0f;
                footfall.pitch = Random.Range(.90f, 1.10f);
                footfall.Play();
            }
        }
        else stepTravel = 0f;

        // Landing
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            animator.SetBool("isJumping", false);
        }
    }
}
