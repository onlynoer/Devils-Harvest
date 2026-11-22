
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
            ApplyGravity();
            return;
        }

        if (isChasing)
        {
            if (distance <= attackRange)
            {
                StopMoving();

                if (cooldownTimer <= 0f)
                    StartAttack();

                FacePlayer();
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

        Move(direction, runSpeed);
        animator.SetFloat("Speed", 1f);
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
        isAttacking = true;
        hasHitPlayer = false;
        attackTimer = 0f;

        animator.SetBool("IsAttacking", true);
    }

    void UpdateAttack()
    {
        attackTimer += Time.deltaTime;

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

            if (distance <= attackRange + 0.3f && Vector3.Angle(transform.forward, Vector3.ProjectOnPlane(player.transform.position - transform.position, Vector3.up)) <= 64f)
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
