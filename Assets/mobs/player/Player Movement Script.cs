
using UnityEngine;

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
    private PlayerPowerups powerups;
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
        powerups = PlayerPowerups.For(gameObject);
    }

    void Update()
    {
        // Movement input
        float x = 0f;
        float z = 0f;

        // Keys come from the Controls settings (GameInput), so players can rebind them.
        if (GameInput.IsHeld(GameAction.MoveLeft)) x = -1f;
        if (GameInput.IsHeld(GameAction.MoveRight)) x = 1f;
        if (GameInput.IsHeld(GameAction.MoveForward)) z = 1f;
        if (GameInput.IsHeld(GameAction.MoveBack)) z = -1f;

        bool isWalking = x != 0f || z != 0f;
        animator.SetBool("isWalking", isWalking);

        Vector3 movement = transform.right * x + transform.forward * z;
        movement = Vector3.ClampMagnitude(movement, 1f);

        // Sprint
        bool isSprinting = GameInput.IsHeld(GameAction.Sprint) && isWalking;

        float currentSpeed = player.playerData.speed;

        if (IsSprinting)
        {
            currentSpeed *= sprintMultiplier;
        }

        currentSpeed *= powerups.GetMultiplier(PowerupStat.MoveSpeed);

        // Gravity
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;

        // Jump
        if (GameInput.WasPressed(GameAction.Jump) && isGrounded)
        {
            // Jump height grows with the square of take-off speed, so a x2 Jump Height powerup uses sqrt(2) here.
            velocity.y = jumpForce * Mathf.Sqrt(powerups.GetMultiplier(PowerupStat.JumpHeight));
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
