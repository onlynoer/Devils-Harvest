
using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    [Header("References")]
    public Player player;
    public Animator animator;
    public CharacterController controller;

    [Header("Detection")]
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

    private bool isWalking;
    private bool isAggressive;
    private bool isAttacking;
    private bool hasHitPlayer;

    private float stateTimer;
    private float attackTimer;
    private float cooldownTimer;
    private float verticalVelocity;

    private Vector3 wanderDirection;

    void Start()
    {
        if (player == null) player = FindAnyObjectByType<Player>();
        StartIdle();
    }

    void Update()
    {
        if (player == null || player.IsDead || animator == null || controller == null) return;

        cooldownTimer -= Time.deltaTime;

        float distance = Vector3.Distance(
            transform.position,
            player.transform.position
        );

        if (!isAggressive && distance <= loseTargetRange * .72f)
        {
            Provoke();
            if (TryGetComponent(out Creature creature) && creature.Sounds != null)
                creature.Sounds.Signal(creature.creatureData.warning != null ? creature.creatureData.warning : creature.creatureData.call);
        }

        // Return to passive mode if the player escapes.
        if (isAggressive && distance > loseTargetRange)
        {
            isAggressive = false;
            isAttacking = false;
            animator.SetBool("IsAttacking", false);
            StartIdle();
        }

        if (isAttacking)
        {
            UpdateAttack();
            ApplyGravity();
            return;
        }

        if (isAggressive)
        {
            if (distance <= attackRange)
            {
                StopMoving();
                FacePlayer();

                if (cooldownTimer <= 0f)
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

    // Called when the monster takes damage.
    public void Provoke()
    {
        if (player == null)
            return;

        isAggressive = true;
        isWalking = false;
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
        isAttacking = true;
        hasHitPlayer = false;
        attackTimer = 0f;

        animator.SetBool("IsAttacking", true);
    }

    void UpdateAttack()
    {
        attackTimer += Time.deltaTime;

        FacePlayer();

        if (!hasHitPlayer && attackTimer >= attackHitDelay)
        {
            float distance = Vector3.Distance(
                transform.position,
                player.transform.position
            );

            if (distance <= attackRange + 0.3f)
                player.TakeDamage(attackDamage);

            hasHitPlayer = true;
        }

        if (attackTimer >= attackDuration)
        {
            isAttacking = false;

            animator.SetBool("IsAttacking", false);

            cooldownTimer = attackCooldown;
        }
    }
}
