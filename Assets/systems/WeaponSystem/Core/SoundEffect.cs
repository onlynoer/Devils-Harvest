using UnityEngine;

/// <summary>
/// One sound slot in the inspector (gunshot, sword swing, punch...). Add several clips to pick one at random each
/// time, so repeated sounds don't feel robotic. Played through <see cref="WeaponAudio"/>, which sends it to the
/// SFX mixer group so the SFX and Master sliders control it. Leave Clips empty for no sound.
/// </summary>
[System.Serializable]
public sealed class SoundEffect
{
    [Tooltip("One is picked at random each time. Empty = silent.")]
    public AudioClip[] clips = new AudioClip[0];
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Random pitch change each play (0.05 = ±5%).")]
    [Range(0f, .5f)] public float pitchVariation = .05f;

    public bool HasClip
    {
        get
        {
            if (clips == null) return false;
            foreach (AudioClip clip in clips)
                if (clip != null) return true;
            return false;
        }
    }

    /// <summary>A random non-empty clip, or null if there are none.</summary>
    public AudioClip PickClip()
    {
        if (clips == null || clips.Length == 0) return null;
        int start = Random.Range(0, clips.Length);
        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[(start + i) % clips.Length];
            if (clip != null) return clip;
        }
        return null;
    }

    public float PickPitch() => 1f + Random.Range(-pitchVariation, pitchVariation);
}
