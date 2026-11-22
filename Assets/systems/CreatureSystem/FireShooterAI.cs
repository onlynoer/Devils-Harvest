
using UnityEngine;

public class FireShooterAI : MonoBehaviour
{
    [Header("References")]
    public Player player;
    public Animator animator;
    public CharacterController controller;

    [Header("Detection")]
    public float detectionRange = 12f;
    public float loseTargetRange = 18f;
    public float shootingRange = 10f;
    [Tooltip("Guard this position and break pursuit beyond the posted radius. Zero allows roaming.")]
    public float guardRadius;
    private Vector3 home;

    [Header("Fireball")]
    public GameObject fireballPrefab;
    public Transform fireballSpawn;
    public float fireballSpeed = 8f;
    public float fireballDamage = 10f;
    public float shootingCooldown = 2f;

    public GameObject castingCue;
    [Min(.1f)] public float windup = .65f;
    private static float nextGroupCast;
    private float releaseTime, nextShotTime;
    private bool casting;
    private int strafeSide = 1;
    private Vector3 markedTarget;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCastSpacing() => nextGroupCast = 0f;

    private void SetCasting(bool active)
    {
        casting = active;
        if (castingCue != null) castingCue.SetActive(active);
    }

    [Header("Movement")]
    public float walkSpeed = 1.5f;
    public float rotationSpeed = 360f;

    [Header("Wandering")]
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;
    public float minWalkTime = 3f;
    public float maxWalkTime = 7f;

    private bool isWalking;
    private bool isAggressive;

    private float stateTimer;
    private float verticalVelocity;

    private Vector3 wanderDirection;

    private void OnDisable() => SetCasting(false);

    void Start()
    {
        if (player == null) player = FindAnyObjectByType<Player>();
        home = transform.position;
        SetCasting(false);
        StartIdle();
    }

    void Update()
    {
        if (player == null || player.IsDead || animator == null || controller == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            player.transform.position
        );

        var toPost = Vector3.ProjectOnPlane(home - transform.position, Vector3.up);
        if (guardRadius > 0f && (toPost.magnitude > guardRadius || Vector3.Distance(home, player.transform.position) > guardRadius + detectionRange))
        {
            isAggressive = false;
            SetCasting(false);
            if (toPost.sqrMagnitude > 1f) Move(toPost.normalized, walkSpeed);
            else StopMoving();
            return;
        }

        if (!isAggressive && distance <= detectionRange)
        {
            isAggressive = true;
        }
        else if (isAggressive && distance > loseTargetRange)
        {
            isAggressive = false;
            SetCasting(false);
            StartIdle();
        }

        if (isAggressive)
        {
            if (casting)
            {
                FacePlayer();
                StopMoving();
                if (castingCue != null) castingCue.transform.localScale = Vector3.one * Mathf.Lerp(.16f, .42f, 1f - (releaseTime - Time.time) / windup);
                if (Time.time >= releaseTime)
                {
                    SetCasting(false);
                    ShootFireball();
                    strafeSide = -strafeSide;
                }
                return;
            }
            var toward = Vector3.ProjectOnPlane(player.transform.position - transform.position, Vector3.up).normalized;
            Vector3 travel = distance > shootingRange ? toward : distance < 5f ? -toward : Vector3.Cross(Vector3.up,toward) * strafeSide;
            Move(travel, walkSpeed);
            animator.SetFloat("Speed", .65f);
            if (distance <= shootingRange && Time.time >= nextShotTime && Time.time >= nextGroupCast && fireballSpawn != null)
            {
                var capsule = player.GetComponent<CharacterController>();
                markedTarget = capsule != null ? capsule.bounds.center : player.transform.position;
                if (Physics.Linecast(fireballSpawn.position, markedTarget, out var hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                    hit.collider.GetComponentInParent<Player>() != player) return;
                SetCasting(true);
                releaseTime = Time.time + windup;
                nextShotTime = Time.time + shootingCooldown;
                nextGroupCast = Time.time + .85f;
            }
        }
        else
        {
            Wander();
        }
    }

    void Wander()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            if (isWalking)
                StartIdle();
            else
                StartWalking();
        }

        if (isWalking)
        {
            var back = Vector3.ProjectOnPlane(home - transform.position, Vector3.up);
            if (back.sqrMagnitude > 25f) wanderDirection = back.normalized;
            Move(wanderDirection, walkSpeed);
            animator.SetFloat("Speed", 0.5f);
        }
        else
        {
            StopMoving();
        }
    }

    void StartIdle()
    {
        isWalking = false;
        stateTimer = Random.Range(minIdleTime, maxIdleTime);
    }

    void StartWalking()
    {
        isWalking = true;
        stateTimer = Random.Range(minWalkTime, maxWalkTime);

        float angle = Random.Range(0f, 360f);

        wanderDirection =
            Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
    }

    void Move(Vector3 direction, float speed)
    {
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        Vector3 movement = direction * speed;
        movement.y = verticalVelocity;

        controller.Move(movement * Time.deltaTime);

        UpdateGravity();
    }

    void StopMoving()
    {
        animator.SetFloat("Speed", 0f);

        controller.Move(
            Vector3.up * verticalVelocity * Time.deltaTime
        );

        UpdateGravity();
    }

    void UpdateGravity()
    {
        if (controller.isGrounded)
            verticalVelocity = -2f;
        else
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
    }

    void FacePlayer()
    {
        Vector3 direction =
            player.transform.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
    void ShootFireball()
    {
        if (fireballPrefab == null || fireballSpawn == null)
            return;

        Vector3 direction = (markedTarget - fireballSpawn.position).normalized;

        GameObject fireball = Instantiate(
            fireballPrefab,
            fireballSpawn.position,
            Quaternion.LookRotation(direction)
        );

        var hitbox = fireball.GetComponent<Collider>();
        if (hitbox != null && controller != null) Physics.IgnoreCollision(hitbox, controller);

        Projectile projectile = fireball.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.SetDamage(fireballDamage);
        }

        Rigidbody rb = fireball.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = direction * fireballSpeed;
        }

    }
}


