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
    private Player threat;
    private PlayerMovement threatMovement;
    private Vector3 danger;
    private static readonly float[] escapeAngles = { 0, 30, -30, 60, -60, 95, -95, 135, -135 };
    public bool IsFleeing => isFleeing;
    private float NoticeRadius => threatMovement != null && threatMovement.isActiveAndEnabled && threatMovement.IsSprinting ? 28f : 18f;

    private float directionChangeTimer;

    void Start()
    {
        threat = FindAnyObjectByType<Player>();
        if (threat != null) threatMovement = threat.GetComponent<PlayerMovement>();
        StartIdle();
    }

    void Update()
    {
        stateTimer -= Time.deltaTime;

        if (!isFleeing && threat != null && threat.playerData != null && !threat.IsDead &&
            (threat.transform.position - transform.position).sqrMagnitude < NoticeRadius * NoticeRadius)
            Flee(threat.transform.position);

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

        danger = dangerPosition;
        stateTimer = fleeTime;
        Steer();
    }

    private void UpdateFlee()
    {
        if (threat != null && !threat.IsDead) danger = threat.transform.position;
        if (stateTimer <= 0 && (transform.position - danger).sqrMagnitude > fleeDistance * fleeDistance)
        {
            isFleeing = false;
            StartIdle();
            return;
        }
        directionChangeTimer -= Time.deltaTime;
        if (directionChangeTimer <= 0) Steer();
        mover.SetInput(Vector2.up, transform.position + fleeDirection, true, false);
    }

    private void Steer()
    {
        Vector3 away = Vector3.ProjectOnPlane(transform.position - danger, Vector3.up).normalized;
        if (away.sqrMagnitude < .1f) away = transform.forward;
        away = Quaternion.Euler(0, Random.Range(-15f, 15f), 0) * away;
        var origin = transform.position + Vector3.up * .75f;
        foreach (float angle in escapeAngles)
        {
            Vector3 candidate = Quaternion.Euler(0, angle, 0) * away;
            if (Physics.SphereCast(origin, .28f, candidate, out _, 2.8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            Vector3 ahead = origin + candidate * 3f;
            if (!Physics.Raycast(ahead + Vector3.up * 2, Vector3.down, out var ground, 5f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || ground.normal.y < .66f) continue;
            if (Terrain.activeTerrain != null && ground.point.y < 18.4f) continue;
            fleeDirection = candidate;
            directionChangeTimer = Random.Range(.3f, .55f);
            return;
        }
        fleeDirection = -transform.forward;
        directionChangeTimer = .2f;
    }
}
