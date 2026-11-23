using UnityEngine;

/// <summary>What one hit carries: how much, which way, and who dealt it (null when unknown).</summary>
public readonly struct DamageInfo
{
    public readonly float Amount;
    public readonly Vector3 Direction;
    public readonly GameObject Source;

    public DamageInfo(float amount, Vector3 direction, GameObject source)
    {
        Amount = amount;
        Direction = direction;
        Source = source;
    }
}

/// <summary>Anything that can be hurt: the player, animals, monsters, bosses.</summary>
public interface IDamageable
{
    bool IsAlive { get; }
    void ApplyDamage(in DamageInfo info);
}
