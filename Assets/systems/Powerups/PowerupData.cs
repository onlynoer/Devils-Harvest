using UnityEngine;

/// <summary>A player stat a powerup can boost. Add new entries here, then read them where the stat is used.</summary>
public enum PowerupStat
{
    MoveSpeed,
    JumpHeight,
    Damage,
}

/// <summary>One stat change of a powerup, e.g. Move Speed x1.5.</summary>
[System.Serializable]
public struct PowerupModifier
{
    public PowerupStat stat;
    [Tooltip("1 = no change, 1.5 = +50%, 2 = double.")]
    [Min(0f)] public float multiplier;
}

/// <summary>
/// Definition of a powerup (one asset per kind, shared by every pickup of it). A powerup applies its modifiers
/// to the player for Duration seconds. Create with Create ▸ Powerups ▸ Powerup Data.
/// </summary>
[CreateAssetMenu(fileName = "NewPowerup", menuName = "Powerups/Powerup Data")]
public class PowerupData : ScriptableObject
{
    [Tooltip("Name shown to the player, e.g. on a HUD.")]
    public string displayName = "Powerup";
    [Tooltip("Optional HUD icon.")]
    public Sprite icon;
    [Tooltip("Optional color for the pickup glow / HUD.")]
    public Color color = Color.white;

    [Header("Effect")]
    [Tooltip("How long the effect lasts, in seconds. Pausing the game pauses the timer.")]
    [Min(.1f)] public float duration = 5f;
    public PowerupModifier[] modifiers = { new PowerupModifier { stat = PowerupStat.MoveSpeed, multiplier = 1.5f } };

    [Header("Sounds (play through the SFX volume slider)")]
    [Tooltip("When the powerup is collected.")]
    public SoundEffect collectSound = new SoundEffect();
    [Tooltip("When the effect runs out.")]
    public SoundEffect expireSound = new SoundEffect();

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
}
