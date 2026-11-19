
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

    [Header("Fireball")]
    public GameObject fireballPrefab;
    public Transform fireballSpawn;
    public float fireballSpeed = 8f;
    public float fireballDamage = 10f;
    public float shootingCooldown = 2f;

    private float nextShotTime;

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

    void Start()
    {
        StartIdle();
    }

    void Update()
    {
        if (player == null || animator == null || controller == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            player.transform.position
        );

        if (!isAggressive && distance <= detectionRange)
        {
            isAggressive = true;
        }
        else if (isAggressive && distance > loseTargetRange)
        {
            isAggressive = false;
            StartIdle();
        }

        if (isAggressive)
        {
            FacePlayer();
            StopMoving();

            if (distance <= shootingRange && Time.time >= nextShotTime)
            {
                ShootFireball();
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

        var target = player.GetComponent<CharacterController>();
        Vector3 aimPoint = target != null ? target.bounds.center : player.transform.position;
        Vector3 direction = (aimPoint - fireballSpawn.position).normalized;

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

        nextShotTime = Time.time + shootingCooldown;
    }
}


