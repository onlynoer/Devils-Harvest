using UnityEngine;

/// <summary>
/// Default bite/swipe/charge: runs at the target until the hit moment, deals damage if still in reach and in
/// front, then recovers. Uses Damage / Attack Range / Duration / Hit Delay / Cooldown from CreatureData.
/// </summary>
public sealed class CreatureMeleeAttack : ICreatureAttack
{
    private readonly CreatureAI brain;
    private readonly Creature creature;
    private readonly CreatureData data;
    private readonly Animator animator;
    private readonly int triggerHash;
    private readonly bool hasTrigger;

    private float startedAt;
    private float readyAt;
    private bool hasHit;
    private bool running;

    public CreatureMeleeAttack(CreatureAI brain, Creature creature)
    {
        this.brain = brain;
        this.creature = creature;
        data = creature.creatureData;
        animator = brain.GetComponent<Animator>();

        string trigger = data.attackAnimatorTrigger;
        if (animator != null && animator.runtimeAnimatorController != null && !string.IsNullOrEmpty(trigger))
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == trigger)
                {
                    triggerHash = parameter.nameHash;
                    hasTrigger = true;
                }
    }

    public float MinRange => 0f;
    public float MaxRange => Mathf.Max(data.attackRange, .3f);
    public bool IsReady => !running && Time.time >= readyAt;
    public bool IsRunning => running;
    public bool Lunging => running && !hasHit;

    public void Begin(Transform target)
    {
        running = true;
        hasHit = false;
        startedAt = Time.time;
        if (hasTrigger) animator.SetTrigger(triggerHash);
        if (creature.Sounds != null) creature.Sounds.Signal(data.attack);
    }

    public void Tick(Transform target)
    {
        float elapsed = Time.time - startedAt;

        if (!hasHit && elapsed >= data.attackHitDelay)
        {
            hasHit = true;
            if (target != null && brain.GapTo(target) <= MaxRange + .5f && brain.IsFacing(target, .3f))
            {
                IDamageable victim = target.GetComponentInParent<IDamageable>();
                Vector3 direction = Vector3.ProjectOnPlane(target.position - brain.transform.position, Vector3.up).normalized;
                if (victim != null && victim.IsAlive)
                    victim.ApplyDamage(new DamageInfo(data.damage, direction, brain.gameObject));
            }
        }

        if (elapsed >= Mathf.Max(data.attackDuration, data.attackHitDelay))
        {
            running = false;
            readyAt = Time.time + data.attackCooldown;
            if (creature.Sounds != null) creature.Sounds.FinishAttack();
        }
    }

    public void Cancel()
    {
        if (!running) return;
        running = false;
        readyAt = Time.time + data.attackCooldown * .5f;
    }
}
