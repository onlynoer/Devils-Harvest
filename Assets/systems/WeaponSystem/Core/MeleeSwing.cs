using UnityEngine;

/// <summary>
/// Procedural first-person swing for the equipped model: wind-up, fast slash across the screen, recover.
/// Animates the model's local pose (relative to the Hand), so it needs no Animator or animation clips.
/// Driven by WeaponSystem; tune it per weapon with the Melee fields on WeaponData.
/// </summary>
public sealed class MeleeSwing
{
    // Phase boundaries in normalized swing time (0..1).
    private const float WindUpEnd = .25f;
    private const float StrikeEnd = .55f;

    private Transform model;
    private Quaternion restRotation;
    private Vector3 restPosition;
    private WeaponData weapon;
    private float startTime;
    private bool hitDone;

    public bool IsSwinging => model != null;

    /// <summary>Starts a swing. Any swing already playing is snapped back to rest first.</summary>
    public void Start(Transform swingModel, WeaponData swingWeapon)
    {
        Cancel();
        if (swingModel == null || swingWeapon == null) return;
        model = swingModel;
        weapon = swingWeapon;
        restRotation = model.localRotation;
        restPosition = model.localPosition;
        startTime = Time.time;
        hitDone = false;
    }

    /// <summary>Advances the animation. Returns true on the one frame the hit should land.</summary>
    public bool Tick()
    {
        if (model == null)
        {
            weapon = null;
            return false;
        }

        float duration = Mathf.Max(.05f, weapon.swingDuration);
        float t = (Time.time - startTime) / duration;
        if (t >= 1f)
        {
            bool lateHit = !hitDone; // very short swings can skip past the hit frame
            Cancel();
            return lateHit;
        }

        ApplyPose(t);
        if (hitDone || t < weapon.swingHitTime) return false;
        hitDone = true;
        return true;
    }

    /// <summary>Stops the swing and puts the model back in its rest pose.</summary>
    public void Cancel()
    {
        if (model != null)
        {
            model.localRotation = restRotation;
            model.localPosition = restPosition;
        }
        model = null;
        weapon = null;
    }

    private void ApplyPose(float t)
    {
        float halfArc = weapon.swingAngle * .5f;
        // Wind-up: raised and pulled to the right. Strike: swept to the left and pushed forward.
        var windUp = new Vector3(-25f, halfArc, -weapon.swingTilt);
        var strike = new Vector3(20f, -halfArc, weapon.swingTilt);
        var rest = Vector3.zero;
        float reach = weapon.swingReach;

        Vector3 euler;
        float push;
        if (t < WindUpEnd)
        {
            float k = EaseOut(t / WindUpEnd);
            euler = Vector3.Lerp(rest, windUp, k);
            push = 0f;
        }
        else if (t < StrikeEnd)
        {
            float k = EaseOut((t - WindUpEnd) / (StrikeEnd - WindUpEnd));
            euler = Vector3.Lerp(windUp, strike, k);
            push = Mathf.Sin(k * Mathf.PI) * reach;
        }
        else
        {
            float k = EaseInOut((t - StrikeEnd) / (1f - StrikeEnd));
            euler = Vector3.Lerp(strike, rest, k);
            push = 0f;
        }

        model.localRotation = Quaternion.Euler(euler) * restRotation;
        model.localPosition = restPosition + Vector3.forward * push;
    }

    private static float EaseOut(float x) => 1f - (1f - x) * (1f - x);
    private static float EaseInOut(float x) => x * x * (3f - 2f * x);
}
