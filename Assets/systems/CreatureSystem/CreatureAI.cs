using UnityEngine;
using ithappy.Animals_FREE;

public class CreatureAI : MonoBehaviour
{
    public CreatureMover mover;

    [Header("Wander")]
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;

    public float minWalkTime = 3f;
    public float maxWalkTime = 7f;

    [Header("Flee")]
    public float fleeTime = 3f;
    public float fleeDistance = 10f;

    public float directionChangeMinTime = 0.7f;
    public float directionChangeMaxTime = 1.5f;

    private float stateTimer;

    private bool isWalking;
    private bool isFleeing;

    private Vector3 fleeDirection;
    private Vector3 fleeTarget;

    private float directionChangeTimer;

    void Start()
    {
        StartIdle();
    }

    void Update()
    {
        stateTimer -= Time.deltaTime;

        if (isFleeing)
        {
            UpdateFlee();
            return;
        }

        if (stateTimer <= 0f)
        {
            if (isWalking)
            {
                StartIdle();
            }
            else
            {
                StartWalking();
            }
        }

        if (isWalking)
        {
            mover.SetInput(
                Vector2.up,
                transform.position + transform.forward,
                false,
                false
            );
        }
        else
        {
            mover.SetInput(
                Vector2.zero,
                transform.position + transform.forward,
                false,
                false
            );
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

        float randomAngle = Random.Range(0f, 360f);

        transform.Rotate(
            0f,
            randomAngle,
            0f
        );
    }

    public void Flee(Vector3 dangerPosition)
    {
        isFleeing = true;
        isWalking = false;

        fleeDirection = transform.position - dangerPosition;
        fleeDirection.y = 0f;

        if (fleeDirection.sqrMagnitude < 0.01f)
        {
            fleeDirection = transform.forward;
        }

        fleeDirection.Normalize();

        float initialAngle = Random.Range(-90f, 90f);

        fleeDirection =
            Quaternion.Euler(0f, initialAngle, 0f) *
            fleeDirection;

        fleeDirection.Normalize();

        directionChangeTimer = Random.Range(
            directionChangeMinTime,
            directionChangeMaxTime
        );

        UpdateFleeTarget();

        stateTimer = fleeTime;
    }

    void UpdateFlee()
    {
        if (stateTimer <= 0f)
        {
            isFleeing = false;
            StartIdle();
            return;
        }

        directionChangeTimer -= Time.deltaTime;

        if (directionChangeTimer <= 0f)
        {
            ChangeFleeDirection();

            directionChangeTimer = Random.Range(
                directionChangeMinTime,
                directionChangeMaxTime
            );
        }

        mover.SetInput(
            Vector2.up,
            fleeTarget,
            true,
            false
        );
    }

    void ChangeFleeDirection()
    {
        float angle = Random.Range(-90f, 90f);

        fleeDirection =
            Quaternion.Euler(0f, angle, 0f) *
            fleeDirection;

        fleeDirection.y = 0f;
        fleeDirection.Normalize();

        UpdateFleeTarget();
    }

    void UpdateFleeTarget()
    {
        fleeTarget =
            transform.position +
            fleeDirection * fleeDistance;
    }
}