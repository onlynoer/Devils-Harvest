using UnityEngine;

[CreateAssetMenu(fileName = "NewCreatureData", menuName = "Creatures/Creature Data")]
public class CreatureData : ScriptableObject
{
    [Header("Basic Info")]
    public string creatureName;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float damage = 10f;
    public float defense = 0f;
    public float movementSpeed = 3f;

    [Header("Combat")]
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    public float attackDuration = 1.2f;
    public float attackHitDelay = 0.5f;

    [Tooltip("Animator Trigger fired when a melee attack starts. Ignored if the animator has no such trigger.")]
    public string attackAnimatorTrigger = "Attack";

    [Header("Behaviour (animals using CreatureAI)")]
    public CreatureTemperament temperament = CreatureTemperament.Skittish;
    [Tooltip("Notices a walking player within this distance (m).")]
    [Min(0f)] public float noticeRadius = 18f;
    [Tooltip("Notices a sprinting player within this distance (m).")]
    [Min(0f)] public float sprintNoticeRadius = 28f;
    [Tooltip("Aggressive/Defensive only notice a player they can see.")]
    public bool requireLineOfSight = true;
    [Tooltip("Degrees per second when turning to face a target.")]
    [Min(1f)] public float turnSpeed = 300f;

    [Header("Behaviour - Defensive")]
    [Tooltip("Stares and plays the Warning sound when the player is this close.")]
    [Min(0f)] public float alertRadius = 14f;
    [Tooltip("Attacks immediately when the player is this close.")]
    [Min(0f)] public float aggroRadius = 6f;
    [Tooltip("Attacks if the player stays inside the alert radius this many seconds.")]
    [Min(0f)] public float alertPatience = 4f;

    [Header("Behaviour - Fighting")]
    [Tooltip("Stops chasing when the player gets this far away.")]
    [Min(1f)] public float giveUpDistance = 30f;
    [Tooltip("Stops chasing when this far from where it spawned.")]
    [Min(1f)] public float leashRadius = 45f;
    [Tooltip("Seconds after giving up during which it ignores the player (unless hurt).")]
    [Min(0f)] public float calmDuration = 8f;
    [Tooltip("Flees for good when health drops to this fraction. 0 = fights to the death.")]
    [Range(0f, 1f)] public float fleeHealthFraction = 0f;
    [Tooltip("Skittish/Passive: chance to fight back instead of fleeing when hit. Needs Damage > 0.")]
    [Range(0f, 1f)] public float retaliateChance = 0f;

    [Header("Behaviour - Group")]
    [Tooltip("When one flees or attacks, others of the same species this close join in. 0 = off.")]
    [Min(0f)] public float groupAlertRadius = 15f;

    [Header("Secrets (rare spawn variants)")]
    public CreatureSecret[] secrets = new CreatureSecret[0];

    [Header("Positional Audio")]
    public UnityEngine.Audio.AudioMixerGroup soundGroup;
    public AudioClip call;
    public AudioClip step;
    public AudioClip hurt;
    public AudioClip death;
    public AudioClip warning;
    public AudioClip attack;
    [Min(1f)] public float callDistance = 45f;
    [Min(1f)] public float stepDistance = 14f;
    [Min(.1f)] public float stride = .8f;

    [Header("Loot")]
    public int lootAmount = 5;
    public MeatPickup meatDrop;

    [Header("Damage Flash")]
    public Color damageFlashColor = Color.red;
    public float damageFlashDuration = 0.15f;
}