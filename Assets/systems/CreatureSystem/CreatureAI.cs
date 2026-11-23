using System.Collections.Generic;
using UnityEngine;
using ithappy.Animals_FREE;

/// <summary>
/// Brain for animals driven by <see cref="CreatureMover"/>. What the animal does about the player comes from
/// the species' <see cref="CreatureData"/> (temperament, radii, combat). Per-instance settings here cover
/// wandering and terrain limits.
///
/// States: Idle / Wander (calm), Alert (staring and warning), Flee, Engage (closing in), Attack, Return (going home).
/// </summary>
[RequireComponent(typeof(Creature))]
public class CreatureAI : MonoBehaviour
{
    public enum State { Idle, Wander, Alert, Flee, Engage, Attack, Return }

    private static readonly List<CreatureAI> Active = new List<CreatureAI>();
    private static readonly float[] EscapeAngles = { 0, 30, -30, 60, -60, 95, -95, 135, -135 };
    private static readonly float[] SteerAngles = { 0, 25, -25, 50, -50, 80, -80, 115, -115 };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => Active.Clear();

    public CreatureMover mover;

    [Header("Wander")]
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;

    public float minWalkTime = 3f;
    public float maxWalkTime = 7f;
    [Tooltip("Wanders within this distance of where it spawned.")]
    [Min(1f)] public float wanderRadius = 25f;

    [Header("Flee")]
    public float fleeTime = 3f;
    public float fleeDistance = 10f;

    [Header("Terrain")]
    [Tooltip("When the scene has a Terrain, never walk onto ground lower than this height (keeps animals out of the water).")]
    public float minGroundHeight = 18.4f;

    private Creature creature;
    private CreatureData data;
    private CharacterController body;
    private ICreatureAttack attack;

    private Player player;
    private PlayerMovement playerMovement;
    private Transform target;

    private State state;
    private float stateTimer;
    private float perceptionTimer;
    private float steerTimer;
    private float calmUntil;
    private bool courageBroken;
    private bool canSeeTarget;

    private Vector3 home;
    private Vector3 destination;
    private Vector3 danger;
    private Vector3 moveDirection;

    public State CurrentState => state;
    public bool IsFleeing => state == State.Flee;
    public bool IsEngaged => state == State.Alert || state == State.Engage || state == State.Attack;
    /// <summary>Effective temperament: a creature that lost its nerve acts skittish for the rest of its life.</summary>
    public CreatureTemperament Temperament => courageBroken ? CreatureTemperament.Skittish : creature.Temperament;
    public ICreatureAttack Attack => attack;

    private float NoticeRadius =>
        playerMovement != null && playerMovement.isActiveAndEnabled && playerMovement.IsSprinting
            ? data.sprintNoticeRadius
            : data.noticeRadius;

    private bool TargetValid
    {
        get
        {
            if (target == null) return false;
            IDamageable victim = target.GetComponentInParent<IDamageable>();
            return victim == null || victim.IsAlive;
        }
    }

    private void Awake()
    {
        creature = GetComponent<Creature>();
        body = GetComponent<CharacterController>();
        if (mover == null) mover = GetComponent<CreatureMover>();
    }

    private void OnEnable()
    {
        Active.Add(this);
        if (creature != null) creature.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        Active.Remove(this);
        if (creature != null) creature.Damaged -= OnDamaged;
    }

    private void Start()
    {
        data = creature.creatureData;
        home = transform.position;
        player = FindAnyObjectByType<Player>();
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            target = player.transform;
        }

        // Creature.Awake has already rolled secrets, so an ArmedCreature (if any) exists by now.
        attack = GetComponent<ICreatureAttack>();
        if (attack == null && data != null && data.damage > 0f) attack = new CreatureMeleeAttack(this, creature);

        perceptionTimer = Random.Range(0f, .2f); // spread perception work across frames
        StartIdle();
    }

    private void Update()
    {
        if (mover == null || data == null) return;

        float deltaTime = Time.deltaTime;
        stateTimer -= deltaTime;
        perceptionTimer -= deltaTime;
        if (perceptionTimer <= 0f)
        {
            perceptionTimer = .2f;
            Perceive();
        }

        switch (state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Wander: UpdateWander(); break;
            case State.Alert: UpdateAlert(); break;
            case State.Flee: UpdateFlee(); break;
            case State.Engage: UpdateEngage(); break;
            case State.Attack: UpdateAttack(); break;
            case State.Return: UpdateReturn(); break;
        }
    }

    // ------------------------------------------------------------------ Perception

    private void Perceive()
    {
        if (!TargetValid) return;
        canSeeTarget = !data.requireLineOfSight || HasLineOfSight(target);

        bool calm = state == State.Idle || state == State.Wander || state == State.Return;
        if (!calm && state != State.Alert) return;

        float distance = FlatDistance(target.position);
        bool settled = Time.time < calmUntil;

        switch (Temperament)
        {
            case CreatureTemperament.Skittish:
                if (distance < NoticeRadius) StartFlee(target.position, true);
                break;

            case CreatureTemperament.Defensive:
                if (settled || attack == null) break;
                if (distance < data.aggroRadius && canSeeTarget) Engage(true);
                else if (state != State.Alert && distance < data.alertRadius && canSeeTarget) StartAlert();
                break;

            case CreatureTemperament.Aggressive:
                if (settled || attack == null) break;
                if (distance < NoticeRadius && canSeeTarget) Engage(true);
                break;
        }
    }

    private void OnDamaged(Creature _, DamageInfo info)
    {
        if (!creature.IsAlive || data == null) return;

        // Animal AI treats unknown damage as the player's (player weapons don't pass a source yet).
        Transform attacker = info.Source != null ? info.Source.transform : target;
        Vector3 threat = attacker != null ? attacker.position : transform.position - info.Direction;

        if (data.fleeHealthFraction > 0f && creature.HealthFraction <= data.fleeHealthFraction)
        {
            courageBroken = true;
            StartFlee(threat, true);
            return;
        }

        bool fights = attack != null && (Temperament == CreatureTemperament.Defensive ||
                                         Temperament == CreatureTemperament.Aggressive ||
                                         Random.value < data.retaliateChance);
        if (fights)
        {
            if (attacker != null) target = attacker;
            if (state != State.Engage && state != State.Attack) Engage(true);
        }
        else
        {
            StartFlee(threat, true);
        }
    }

    // ------------------------------------------------------------------ Calm states

    private void StartIdle()
    {
        state = State.Idle;
        stateTimer = Random.Range(minIdleTime, maxIdleTime);
    }

    private void UpdateIdle()
    {
        Stand(transform.position + transform.forward);
        if (stateTimer <= 0f) StartWander();
    }

    private void StartWander()
    {
        state = State.Wander;
        stateTimer = Random.Range(minWalkTime, maxWalkTime);
        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        destination = home + new Vector3(offset.x, 0f, offset.y);
    }

    private void UpdateWander()
    {
        if (stateTimer <= 0f || FlatDistance(destination) < 1.5f) { StartIdle(); return; }
        MoveTo(destination, false);
    }

    private void StartReturn()
    {
        if (attack != null) attack.Cancel();
        state = State.Return;
        stateTimer = 20f;
    }

    private void UpdateReturn()
    {
        if (stateTimer <= 0f || FlatDistance(home) < wanderRadius * .5f) { StartIdle(); return; }
        MoveTo(home, false);
    }

    // ------------------------------------------------------------------ Alert (defensive warning)

    private void StartAlert()
    {
        state = State.Alert;
        stateTimer = data.alertPatience;
        if (creature.Sounds != null) creature.Sounds.Signal(data.warning);
    }

    private void UpdateAlert()
    {
        if (!TargetValid) { StartIdle(); return; }
        Stand(target.position);
        Face(target.position);

        float distance = FlatDistance(target.position);
        if (distance > data.alertRadius * 1.25f) StartIdle();
        else if (stateTimer <= 0f) Engage(true);
    }

    // ------------------------------------------------------------------ Fighting

    private void Engage(bool alertGroup)
    {
        if (attack == null || !TargetValid) { StartFlee(target != null ? target.position : transform.position - transform.forward, alertGroup); return; }
        if (state != State.Alert && creature.Sounds != null) creature.Sounds.Signal(data.warning);
        state = State.Engage;
        if (alertGroup) AlertGroup(true);
    }

    private void UpdateEngage()
    {
        if (!TargetValid) { StartReturn(); return; }

        float gap = GapTo(target);
        if (gap > data.giveUpDistance || FlatDistance(home) > data.leashRadius)
        {
            calmUntil = Time.time + data.calmDuration;
            StartReturn();
            return;
        }

        if (gap > attack.MaxRange || !canSeeTarget)
        {
            MoveTo(target.position, true);
        }
        else if (gap < attack.MinRange)
        {
            Vector3 away = transform.position + (transform.position - target.position).normalized * 5f;
            MoveTo(away, false);
        }
        else
        {
            Stand(target.position);
            Face(target.position);
            if (attack.IsReady)
            {
                attack.Begin(target);
                state = State.Attack;
            }
        }
    }

    private void UpdateAttack()
    {
        if (!TargetValid) { StartReturn(); return; }

        if (attack.Lunging && GapTo(target) > .2f) MoveTo(target.position, true, false);
        else
        {
            Stand(target.position);
            Face(target.position);
        }

        attack.Tick(target);
        if (!attack.IsRunning) state = State.Engage;
    }

    // ------------------------------------------------------------------ Fleeing

    /// <summary>Runs away from a point. Kept public for older callers.</summary>
    public void Flee(Vector3 dangerPosition) => StartFlee(dangerPosition, false);

    private void StartFlee(Vector3 dangerPosition, bool alertGroup)
    {
        if (attack != null) attack.Cancel();
        bool wasFleeing = state == State.Flee;
        state = State.Flee;
        danger = dangerPosition;
        stateTimer = fleeTime;
        Steer();
        if (alertGroup && !wasFleeing) AlertGroup(false);
    }

    private void UpdateFlee()
    {
        if (player != null && !player.IsDead) danger = player.transform.position;
        if (stateTimer <= 0 && (transform.position - danger).sqrMagnitude > fleeDistance * fleeDistance)
        {
            StartIdle();
            return;
        }
        steerTimer -= Time.deltaTime;
        if (steerTimer <= 0) Steer();
        mover.SetInput(Vector2.up, transform.position + moveDirection, true, false);
    }

    private void Steer()
    {
        Vector3 away = Vector3.ProjectOnPlane(transform.position - danger, Vector3.up).normalized;
        if (away.sqrMagnitude < .1f) away = transform.forward;
        away = Quaternion.Euler(0, Random.Range(-15f, 15f), 0) * away;
        if (TryFindClearDirection(away, EscapeAngles, out moveDirection))
        {
            steerTimer = Random.Range(.3f, .55f);
            return;
        }
        moveDirection = -transform.forward;
        steerTimer = .2f;
    }

    // ------------------------------------------------------------------ Group behaviour

    /// <summary>Same species nearby join in: herds flee together, packs attack together.</summary>
    private void AlertGroup(bool engage)
    {
        if (data.groupAlertRadius <= 0f) return;
        float radiusSquared = data.groupAlertRadius * data.groupAlertRadius;

        for (int i = Active.Count - 1; i >= 0; i--)
        {
            CreatureAI other = Active[i];
            if (other == this || other == null || other.data != data || !other.creature.IsAlive) continue;
            if ((other.transform.position - transform.position).sqrMagnitude > radiusSquared) continue;

            if (engage && other.attack != null && other.Temperament != CreatureTemperament.Skittish)
            {
                if (other.state == State.Engage || other.state == State.Attack) continue;
                other.target = target;
                other.Engage(false);
            }
            else if (!engage || other.attack == null || other.Temperament == CreatureTemperament.Skittish)
            {
                if (other.state == State.Flee) continue;
                other.StartFlee(target != null ? target.position : danger, false);
            }
        }
    }

    // ------------------------------------------------------------------ Movement helpers

    private void MoveTo(Vector3 point, bool run, bool avoidObstacles = true)
    {
        Vector3 desired = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
        if (desired.sqrMagnitude < .01f) { Stand(point); return; }
        desired.Normalize();

        if (!avoidObstacles) moveDirection = desired;
        else
        {
            steerTimer -= Time.deltaTime;
            if (steerTimer <= 0f)
            {
                steerTimer = .25f;
                if (!TryFindClearDirection(desired, SteerAngles, out moveDirection)) moveDirection = desired;
            }
        }
        mover.SetInput(Vector2.up, transform.position + moveDirection * 5f, run, false);
    }

    private void Stand(Vector3 lookAt) => mover.SetInput(Vector2.zero, lookAt, false, false);

    private void Face(Vector3 point)
    {
        Vector3 direction = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
        if (direction.sqrMagnitude < .001f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), data.turnSpeed * Time.deltaTime);
        mover.SyncFacing();
    }

    private bool TryFindClearDirection(Vector3 preferred, float[] angles, out Vector3 direction)
    {
        var origin = transform.position + Vector3.up * .75f;
        foreach (float angle in angles)
        {
            Vector3 candidate = Quaternion.Euler(0, angle, 0) * preferred;
            if (Physics.SphereCast(origin, .28f, candidate, out RaycastHit blocker, 2.8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && !IsTarget(blocker.transform)) continue;
            Vector3 ahead = origin + candidate * 3f;
            if (!Physics.Raycast(ahead + Vector3.up * 2, Vector3.down, out var ground, 5f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || ground.normal.y < .66f) continue;
            if (Terrain.activeTerrain != null && ground.point.y < minGroundHeight) continue;
            direction = candidate;
            return true;
        }
        direction = preferred;
        return false;
    }

    private bool IsTarget(Transform other) => target != null && (other == target || other.IsChildOf(target));

    // ------------------------------------------------------------------ Queries (also used by attacks)

    /// <summary>Horizontal distance between the two bodies' edges.</summary>
    public float GapTo(Transform other)
    {
        float distance = FlatDistance(other.position);
        float myRadius = body != null ? body.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) : .5f;
        float theirRadius = other.TryGetComponent(out CharacterController controller)
            ? controller.radius * Mathf.Max(other.lossyScale.x, other.lossyScale.z)
            : .4f;
        return Mathf.Max(0f, distance - myRadius - theirRadius);
    }

    public bool IsFacing(Transform other, float minDot)
    {
        Vector3 direction = Vector3.ProjectOnPlane(other.position - transform.position, Vector3.up).normalized;
        return Vector3.Dot(transform.forward, direction) >= minDot;
    }

    /// <summary>Center of the target's body, for aiming and sight checks.</summary>
    public static Vector3 AimPoint(Transform other)
    {
        if (other.TryGetComponent(out CharacterController controller)) return controller.bounds.center;
        if (other.TryGetComponent(out Collider collider)) return collider.bounds.center;
        return other.position + Vector3.up;
    }

    private bool HasLineOfSight(Transform other)
    {
        Vector3 eye = body != null ? body.bounds.center + Vector3.up * body.bounds.extents.y * .6f : transform.position + Vector3.up;
        Vector3 to = AimPoint(other);
        if (!Physics.Linecast(eye, to, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return true;
        return hit.transform == other || hit.transform.IsChildOf(other) || hit.transform.IsChildOf(transform);
    }

    private float FlatDistance(Vector3 point)
    {
        Vector3 offset = point - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private void OnDrawGizmosSelected()
    {
        CreatureData profile = data != null ? data : GetComponent<Creature>() != null ? GetComponent<Creature>().creatureData : null;
        if (profile == null) return;
        Vector3 center = Application.isPlaying ? home : transform.position;

        Gizmos.color = new Color(.4f, .8f, 1f, .6f);
        Gizmos.DrawWireSphere(center, wanderRadius);
        Gizmos.color = new Color(1f, 1f, 0f, .6f);
        Gizmos.DrawWireSphere(transform.position, profile.noticeRadius);
        if (profile.temperament == CreatureTemperament.Defensive)
        {
            Gizmos.color = new Color(1f, .5f, 0f, .6f);
            Gizmos.DrawWireSphere(transform.position, profile.alertRadius);
            Gizmos.color = new Color(1f, 0f, 0f, .6f);
            Gizmos.DrawWireSphere(transform.position, profile.aggroRadius);
        }
    }
}
