using System;
using UnityEngine;

/// <summary>
/// A rare variant rolled when a creature spawns. Listed on <see cref="CreatureData"/>; the first one whose
/// chance hits is applied. A secret with a weapon makes the creature carry and fire that weapon.
/// </summary>
[Serializable]
public sealed class CreatureSecret
{
    [Tooltip("Name shown in logs, e.g. \"Deer With A Gun\".")]
    public string label = "Armed";
    [Tooltip("Chance per spawn. 0.01 = 1 in 100.")]
    [Range(0f, 1f)] public float chance = .01f;
    [Tooltip("How the variant behaves. An armed deer usually stops being skittish.")]
    public CreatureTemperament temperament = CreatureTemperament.Aggressive;
    [Tooltip("Max health multiplier for this variant.")]
    [Min(.01f)] public float healthMultiplier = 1f;

    [Header("Weapon (leave empty for a non-armed secret)")]
    public WeaponData weapon;
    [Tooltip("Child bone the gun attaches to, e.g. spine.003. Empty = the creature's root.")]
    public string mountBone = "";
    public Vector3 mountOffset = new Vector3(0f, 1f, .3f);
    public Vector3 mountRotation = Vector3.zero;
    [Tooltip("Multiplies the weapon's Equipped Scale.")]
    [Min(.01f)] public float mountScale = 1f;

    [Header("Shooting")]
    [Tooltip("Multiplies the weapon's damage. Animals are bad shots, keep it low.")]
    [Min(0f)] public float damageMultiplier = .5f;
    [Tooltip("Random aim error in degrees.")]
    [Range(0f, 30f)] public float spreadDegrees = 4f;
    [Tooltip("Shots per burst.")]
    [Min(1)] public int burstCount = 3;
    [Tooltip("Multiplies the weapon's cooldown between shots in a burst.")]
    [Min(.1f)] public float shotIntervalMultiplier = 1.5f;
    [Tooltip("Seconds the creature aims before the first shot (the player's warning).")]
    [Min(0f)] public float aimTime = .5f;
    [Tooltip("Random pause between bursts, seconds.")]
    public Vector2 burstCooldown = new Vector2(1.5f, 3f);
    [Tooltip("Backs away when the player is closer than this.")]
    [Min(0f)] public float minRange = 4f;
    [Tooltip("Shoots when the player is within this distance.")]
    [Min(1f)] public float maxRange = 20f;
    [Tooltip("Leaves the weapon as a pickup when killed.")]
    public bool dropWeaponOnDeath = true;
}
