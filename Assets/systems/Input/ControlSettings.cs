using System;
using UnityEngine;

/// <summary>Non-key control settings (mouse sensitivity per axis), saved in PlayerPrefs and shared by every scene.</summary>
public static class ControlSettings
{
    private const string HorizontalKey = "DevilsHarvest.Settings.Controls.HorizontalSensitivity";
    private const string VerticalKey = "DevilsHarvest.Settings.Controls.VerticalSensitivity";
    private const string LegacyKey = "DevilsHarvest.Settings.Controls.MouseSensitivity"; // before the X/Y split

    public const float MinSensitivity = 0.1f;
    public const float MaxSensitivity = 3f;
    public const float DefaultSensitivity = 1f;

    private static float? horizontal;
    private static float? vertical;

    /// <summary>Raised when a setting changes.</summary>
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        horizontal = null;
        vertical = null;
        Changed = null;
    }

    /// <summary>Multiplier on left/right camera speed. 1 = the speed set on FirstPersonLook.</summary>
    public static float HorizontalSensitivity
    {
        get => Load(ref horizontal, HorizontalKey);
        set => Store(ref horizontal, HorizontalKey, value);
    }

    /// <summary>Multiplier on up/down camera speed. 1 = the speed set on FirstPersonLook.</summary>
    public static float VerticalSensitivity
    {
        get => Load(ref vertical, VerticalKey);
        set => Store(ref vertical, VerticalKey, value);
    }

    public static void ResetToDefaults()
    {
        HorizontalSensitivity = DefaultSensitivity;
        VerticalSensitivity = DefaultSensitivity;
    }

    private static float Load(ref float? cache, string key)
    {
        if (cache == null)
        {
            // Players who set the old single slider keep that value on both axes.
            float fallback = PlayerPrefs.GetFloat(LegacyKey, DefaultSensitivity);
            cache = Mathf.Clamp(PlayerPrefs.GetFloat(key, fallback), MinSensitivity, MaxSensitivity);
        }
        return cache.Value;
    }

    private static void Store(ref float? cache, string key, float value)
    {
        float clamped = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
        if (cache.HasValue && Mathf.Approximately(cache.Value, clamped)) return;
        cache = clamped;
        PlayerPrefs.SetFloat(key, clamped);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
