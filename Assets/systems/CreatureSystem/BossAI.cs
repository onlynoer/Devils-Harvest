
using UnityEngine;

public class BossAI : MonoBehaviour
{
    [Header("References")]
    public Player player;
    public Animator animator;
    public CharacterController controller;

    [Header("Detection")]
    public float detectionRange = 12f;
    public float loseTargetRange = 18f;
    public float attackRange = 2f;

    [Header("Movement")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 4f;
    public float rotationSpeed = 360f;

    [Header("Wandering")]
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;
    public float minWalkTime = 3f;
    public float maxWalkTime = 7f;

    [Header("Attack")]
    public float attackDamage = 15f;
    public float attackDuration = 1.2f;
    public float attackHitDelay = 0.5f;
    public float attackCooldown = 1.5f;

    [Header("Saddle pursuit")]
    public float pursuitSpeed = 18.5f;
    public float swingStartRange = 10f;
    public float stepSpeed = 13f;
    public bool WindingUp => isAttacking && attackTimer < attackHitDelay;
    public bool Recovering => isAttacking && hasHitPlayer;
    private Vector3 swingDirection;
    private float swingAdvance;
    private Vector3 detourDirection;
    private float detourUntil;

    private Creature creature;
    private bool isWalking;
    private bool isChasing;
    private bool isAttacking;
    public bool IsEngaged => isChasing || isAttacking;
    private bool hasHitPlayer;

    private float stateTimer;
    private float attackTimer;
    private float cooldownTimer;
    private float verticalVelocity;

    private Vector3 wanderDirection;
    private Vector3 home;
    private float woundedUntil;
    public Transform attackVisual;
    private Quaternion restPose;

    public void AlertToHit()
    {
        woundedUntil = Time.time + 5f;
    }

    void Start()
    {
        if (player == null) player = FindAnyObjectByType<Player>();
        creature = GetComponent<Creature>();
        home = transform.position;
        if (attackVisual != null) restPose = attackVisual.localRotation;
        StartIdle();
    }

    void Update()
    {
        if (player == null || player.IsDead || animator == null || controller == null)
            return;

        cooldownTimer -= Time.deltaTime;

        float distance = Vector3.Distance(
            transform.position,
            player.transform.position
        );

        // Check whether the player is nearby.
        if (distance <= detectionRange || Time.time < woundedUntil)
        {
            if (!isChasing && creature != null && creature.Sounds != null)
                creature.Sounds.Signal(creature.creatureData.warning);
            isChasing = true;
        }
        else if (distance > loseTargetRange)
        {
            isChasing = false;
        }

        // Handle an ongoing attack.
        if (isAttacking)
        {
            UpdateAttack();
            return;
        }

        if (isChasing)
        {
            if (distance <= swingStartRange && cooldownTimer <= 0f)
            {
                StopMoving();

                StartAttack();

            }
            else
            {
                ChasePlayer();
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
            if (back.sqrMagnitude > 36f) wanderDirection = back.normalized;
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

        stateTimer = Random.Range(
            minIdleTime,
            maxIdleTime
        );
    }

    void StartWalking()
    {
        isWalking = true;

        stateTimer = Random.Range(
            minWalkTime,
            maxWalkTime
        );

        float angle = Random.Range(0f, 360f);

        wanderDirection =
            Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
    }

    void ChasePlayer()
    {
        Vector3 direction =
            player.transform.position - transform.position;

        direction.y = 0f;
        direction.Normalize();

        var desired = Time.time < detourUntil ? detourDirection : direction;
        Vector3 before = transform.position;
        Move(desired, pursuitSpeed);
        float advanced = Vector3.Dot(transform.position - before, desired);
        if (advanced < pursuitSpeed * Time.deltaTime * .22f && Time.time >= detourUntil)
        {
            // At a steep bank or tree, give the controller room to round the obstruction.
            Vector3 left = Quaternion.AngleAxis(-58, Vector3.up) * direction;
            Vector3 right = Quaternion.AngleAxis(58, Vector3.up) * direction;
            detourDirection = Clearance(left) > Clearance(right) ? left : right;
            detourUntil = Time.time + .48f;
        }
        animator.SetFloat("Speed", 1f);
    }

    private float Clearance(Vector3 direction)
    {
        float radius = controller.bounds.extents.x * .75f;
        Vector3 origin = controller.bounds.center - Vector3.up * (controller.bounds.extents.y * .4f);
        return Physics.SphereCast(origin, radius, direction, out var hit, 9f, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore) ? hit.distance : 9f;
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

    void ApplyGravity()
    {
        controller.Move(
            Vector3.up * verticalVelocity * Time.deltaTime
        );

        UpdateGravity();
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

    void StartAttack()
    {
        if (creature != null && creature.Sounds != null)
            creature.Sounds.Signal(creature.creatureData.attack);
        var offset = Vector3.ProjectOnPlane(player.transform.position - transform.position, Vector3.up);
        swingDirection = offset.normalized;
        swingAdvance = Mathf.Max(0, offset.magnitude - attackRange * .7f);
        if (swingDirection.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(swingDirection);
        isAttacking = true;
        hasHitPlayer = false;
        attackTimer = 0f;

        animator.SetBool("IsAttacking", true);
    }

    void UpdateAttack()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= attackHitDelay - .35f && attackTimer < attackHitDelay + .07f && swingAdvance > 0 && Time.deltaTime > 0)
        {
            float step = Mathf.Min(swingAdvance, stepSpeed * Time.deltaTime);
            Move(swingDirection, step / Time.deltaTime);
            swingAdvance -= step;
        }
        else ApplyGravity();

        // Hold the facing chosen at windup. Stepping aside evades the committed swing.
        if (attackVisual != null)
        {
            float tilt = attackTimer < attackHitDelay
                ? -16f * Mathf.Sin(Mathf.PI * .5f * attackTimer / attackHitDelay)
                : 13f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((attackTimer - attackHitDelay) / .48f));
            attackVisual.localRotation = restPose * Quaternion.Euler(tilt, 0f, 0f);
        }

        if (!hasHitPlayer && attackTimer >= attackHitDelay)
        {
            float distance = Vector3.Distance(
                transform.position,
                player.transform.position
            );

            if (distance <= attackRange + 1.4f && Vector3.Angle(swingDirection, Vector3.ProjectOnPlane(player.transform.position - transform.position, Vector3.up)) <= 58f)
                player.TakeDamage(attackDamage);

            hasHitPlayer = true;
        }

        if (attackTimer >= attackDuration)
        {
            isAttacking = false;
            if (attackVisual != null) attackVisual.localRotation = restPose;

            animator.SetBool("IsAttacking", false);

            if (creature != null && creature.Sounds != null)
                creature.Sounds.FinishAttack();
            cooldownTimer = attackCooldown;
        }
    }
}
