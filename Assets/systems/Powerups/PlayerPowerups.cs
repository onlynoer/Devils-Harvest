using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The player's active powerups and their timers. Gameplay scripts ask for a stat's multiplier with
/// <see cref="GetMultiplier"/> (1 when nothing boosts it). Picking up a powerup that is already active restarts
/// its timer instead of stacking. Added to the player automatically by <see cref="For"/> if missing.
/// </summary>
public sealed class PlayerPowerups : MonoBehaviour
{
    /// <summary>One running powerup. Read by HUDs.</summary>
    public sealed class Active
    {
        public PowerupData data;
        public float endTime;
        public float Remaining => Mathf.Max(0f, endTime - Time.time);
        public float Fraction => data != null && data.duration > 0f ? Remaining / data.duration : 0f;
    }

    private readonly List<Active> active = new List<Active>();
    private readonly float[] multipliers = new float[Enum.GetValues(typeof(PowerupStat)).Length];
    private AudioSource audioSource;

    /// <summary>Raised when a powerup starts (or its timer is restarted).</summary>
    public event Action<PowerupData> Started;
    /// <summary>Raised when a powerup runs out or is cleared.</summary>
    public event Action<PowerupData> Ended;

    public IReadOnlyList<Active> ActivePowerups => active;

    /// <summary>The player's PlayerPowerups, added if it doesn't have one yet.</summary>
    public static PlayerPowerups For(GameObject player)
    {
        if (player == null) return null;
        PlayerPowerups powerups = player.GetComponent<PlayerPowerups>();
        if (powerups == null) powerups = player.AddComponent<PlayerPowerups>();
        return powerups;
    }

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        Recalculate();
    }

    private void Update()
    {
        // Time.time stops while paused, so timers pause with the game.
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (Time.time < active[i].endTime) continue;
            PowerupData data = active[i].data;
            active.RemoveAt(i);
            Recalculate();
            PlaySound(data.expireSound);
            Ended?.Invoke(data);
        }
    }

    /// <summary>Product of every active modifier for this stat. 1 = no boost.</summary>
    public float GetMultiplier(PowerupStat stat) => multipliers[(int)stat];

    public bool IsActive(PowerupData data) => Find(data) != null;

    /// <summary>Starts a powerup, or restarts its timer if it's already running.</summary>
    public void Apply(PowerupData data)
    {
        if (data == null) return;
        Active entry = Find(data);
        if (entry == null)
        {
            entry = new Active { data = data };
            active.Add(entry);
        }
        entry.endTime = Time.time + data.duration;
        Recalculate();
        PlaySound(data.collectSound);
        Started?.Invoke(data);
    }

    /// <summary>Ends every powerup at once (e.g. when the player dies).</summary>
    public void ClearAll()
    {
        if (active.Count == 0) return;
        var ended = new List<Active>(active);
        active.Clear();
        Recalculate();
        foreach (Active entry in ended) Ended?.Invoke(entry.data);
    }

    private Active Find(PowerupData data)
    {
        foreach (Active entry in active)
            if (entry.data == data) return entry;
        return null;
    }

    private void Recalculate()
    {
        for (int i = 0; i < multipliers.Length; i++) multipliers[i] = 1f;
        foreach (Active entry in active)
        {
            if (entry.data.modifiers == null) continue;
            foreach (PowerupModifier modifier in entry.data.modifiers)
                multipliers[(int)modifier.stat] *= modifier.multiplier;
        }
    }

    private void PlaySound(SoundEffect sound)
    {
        if (sound == null || audioSource == null) return;
        AudioClip clip = sound.PickClip();
        if (clip == null) return;
        // Routed at play time because SettingsManager may not exist yet when this component wakes up.
        SettingsManager.Route(audioSource, AudioChannel.SFX);
        audioSource.pitch = sound.PickPitch();
        audioSource.PlayOneShot(clip, sound.volume);
    }
}
