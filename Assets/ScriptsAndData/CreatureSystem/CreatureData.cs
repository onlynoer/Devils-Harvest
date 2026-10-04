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

    [Header("Loot")]
    public int lootAmount = 5;
}
