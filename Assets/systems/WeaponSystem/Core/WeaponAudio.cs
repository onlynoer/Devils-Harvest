using UnityEngine;

/// <summary>
/// Plays the player's weapon sound effects (shots, swings, punches, hits, reloads). Every sound goes through
/// the SFX mixer group, so the SFX and Master volume sliders control it, and it pauses with the game.
/// WeaponSystem adds this automatically if the player doesn't have one; add it yourself to tweak the settings.
/// Uses a small pool of AudioSources so overlapping sounds (rapid fire, punch + hit) don't cut each other off.
/// </summary>
public sealed class WeaponAudio : MonoBehaviour
{
    [Tooltip("How many sounds can play at once. The oldest is cut off when all are busy.")]
    [Range(1, 16)] [SerializeField] private int voices = 6;
    [Tooltip("Sounds played at a world position (e.g. melee hits) fade out past this distance, in meters.")]
    [Min(1f)] [SerializeField] private float maxDistance = 30f;

    private AudioSource[] pool;
    private int nextVoice;

    private void Awake() => BuildPool();

    private void BuildPool()
    {
        if (pool != null) return;
        pool = new AudioSource[voices];
        for (int i = 0; i < voices; i++)
        {
            var voice = new GameObject("Weapon Audio Voice " + (i + 1));
            voice.transform.SetParent(transform, false);
            AudioSource source = voice.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.maxDistance = maxDistance;
            pool[i] = source;
        }
    }

    /// <summary>Plays a sound on the player (not positioned, same in both ears). Used for the player's own weapon.</summary>
    public void Play(SoundEffect sound) => PlayInternal(sound, null);

    /// <summary>Plays a sound in the world at a point, e.g. where a melee hit landed.</summary>
    public void PlayAt(SoundEffect sound, Vector3 position) => PlayInternal(sound, position);

    private void PlayInternal(SoundEffect sound, Vector3? position)
    {
        if (sound == null || !isActiveAndEnabled) return;
        AudioClip clip = sound.PickClip();
        if (clip == null) return;

        BuildPool();
        AudioSource source = NextVoice();
        // Routed at play time because SettingsManager may not exist yet when this component wakes up.
        SettingsManager.Route(source, AudioChannel.SFX);

        if (position.HasValue)
        {
            source.transform.position = position.Value;
            source.spatialBlend = 1f;
        }
        else
        {
            source.transform.localPosition = Vector3.zero;
            source.spatialBlend = 0f;
        }
        source.clip = clip;
        source.volume = sound.volume;
        source.pitch = sound.PickPitch();
        source.Play();
    }

    private AudioSource NextVoice()
    {
        for (int i = 0; i < pool.Length; i++)
        {
            AudioSource candidate = pool[(nextVoice + i) % pool.Length];
            if (!candidate.isPlaying)
            {
                nextVoice = (nextVoice + i + 1) % pool.Length;
                return candidate;
            }
        }
        AudioSource oldest = pool[nextVoice];
        nextVoice = (nextVoice + 1) % pool.Length;
        return oldest;
    }
}
