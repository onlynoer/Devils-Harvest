using UnityEngine;

/// <summary>How a creature reacts to the player. Set per species on <see cref="CreatureData"/>.</summary>
public enum CreatureTemperament
{
    [Tooltip("Runs from the player when it notices them (deer, chicken).")]
    Skittish,
    [Tooltip("Ignores the player; only runs when hurt (cow, horse).")]
    Passive,
    [Tooltip("Warns when the player gets close, attacks if they come closer, stay too long or hurt it (bear).")]
    Defensive,
    [Tooltip("Hunts the player on sight and calls its pack (wolf).")]
    Aggressive
}

/// <summary>
/// One way a creature can hurt its target. <see cref="CreatureAI"/> moves the creature into range,
/// then calls <see cref="Begin"/> and <see cref="Tick"/> every frame until <see cref="IsRunning"/> is false.
/// </summary>
public interface ICreatureAttack
{
    /// <summary>Gap (meters, body edge to body edge) the creature tries to keep. 0 for melee.</summary>
    float MinRange { get; }
    /// <summary>Gap at which the attack can start.</summary>
    float MaxRange { get; }
    bool IsReady { get; }
    bool IsRunning { get; }
    /// <summary>True while the creature should run at the target instead of standing still.</summary>
    bool Lunging { get; }
    void Begin(Transform target);
    void Tick(Transform target);
    void Cancel();
}
