using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerData", menuName = "Players/Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float damageAmplifier = 1f;
    public float defense = 5f;
    public float speed = 5f;
}