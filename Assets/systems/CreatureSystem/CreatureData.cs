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