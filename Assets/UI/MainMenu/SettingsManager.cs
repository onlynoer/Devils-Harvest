using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>Which mixer group an AudioSource plays through.</summary>
public enum AudioChannel
{
    Music,
    SFX,
    UI,
}

/// <summary>
/// Loads, applies and saves the audio volume settings. Put one in every scene that has sound.
///
/// Master scales everything the player hears (it sets the listener volume, so it also covers sounds that
/// aren't routed through the mixer). Music, SFX and UI set their mixer groups' volume, and stack with Master.
/// Any AudioSource left without a mixer group is routed to SFX so the SFX slider controls it.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Tooltip("Mixer with Music, SFX and UI groups and the exposed volume parameters listed below.")]
    public AudioMixer audioMixer;
    [Tooltip("Route AudioSources that have no mixer group to the SFX group when a scene loads.")]
    [SerializeField] private bool routeUnassignedSources = true;

    private const string Prefix = "DevilsHarvest.Settings.Audio.";
    private const float DefaultVolume = 0.5f;
    private bool savePending;

    private static readonly string[] Keys = { "Master", "Music", "SFX", "UI" };
    // Must match the parameters exposed on the mixer. Master's mixer parameter is held at 0 dB because
    // Master is applied through AudioListener.volume instead.
    private static readonly string[] MixerParameters = { "MasterVolume", "MusicVolume", "SFXVolume", "UIVolume" };
    private static readonly string[] GroupNames = { "Music", "SFX", "UI" }; // indexed by AudioChannel
    private readonly float[] values = { DefaultVolume, DefaultVolume, DefaultVolume, DefaultVolume };
    private readonly AudioMixerGroup[] groups = new AudioMixerGroup[3];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        LoadValues();
        ResolveGroups();
    }

    // Mixer values set in Awake are ignored by Unity, so they're applied here.
    private void Start()
    {
        for (int i = 0; i < values.Length; i++) Apply(i, values[i]);
        RouteUnassignedInLoadedScenes();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public float GetMasterVolume() => values[0];
    public float GetMusicVolume() => values[1];
    public float GetSFXVolume() => values[2];
    public float GetUIVolume() => values[3];

    public void SetMasterVolume(float value) => SetVolume(0, value);
    public void SetMusicVolume(float value) => SetVolume(1, value);
    public void SetSFXVolume(float value) => SetVolume(2, value);
    public void SetUIVolume(float value) => SetVolume(3, value);

    /// <summary>The mixer group for a channel, or null when no SettingsManager/mixer is in the scene.</summary>
    public static AudioMixerGroup GetGroup(AudioChannel channel) =>
        Instance != null ? Instance.groups[(int)channel] : null;

    /// <summary>
    /// Sends a source through a channel's mixer group so its slider controls it. Sources that already
    /// have a group are left alone. Call this for AudioSources created from code.
    /// </summary>
    public static void Route(AudioSource source, AudioChannel channel)
    {
        if (source == null || source.outputAudioMixerGroup != null) return;
        source.outputAudioMixerGroup = GetGroup(channel);
    }

    public void SavePreferences()
    {
        if (!savePending) return;
        PlayerPrefs.Save();
        savePending = false;
    }

    private void LoadValues()
    {
        for (int i = 0; i < Keys.Length; i++)
        {
            string key = Prefix + Keys[i];
            float saved = PlayerPrefs.HasKey(key)
                ? PlayerPrefs.GetFloat(key, DefaultVolume)
                : PlayerPrefs.GetFloat(Keys[i] == "SFX" ? "SFXVolume" : Keys[i] + "Volume", DefaultVolume);
            values[i] = Mathf.Clamp01(saved);
        }
    }

    private void ResolveGroups()
    {
        if (audioMixer == null) return;
        for (int i = 0; i < GroupNames.Length; i++)
        {
            AudioMixerGroup[] matches = audioMixer.FindMatchingGroups("Master/" + GroupNames[i]);
            if (matches.Length == 0) matches = audioMixer.FindMatchingGroups(GroupNames[i]);
            groups[i] = matches.Length > 0 ? matches[0] : null;
        }
    }

    private void SetVolume(int index, float value)
    {
        if (index < 0 || index >= values.Length) return;
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(values[index], value)) return;
        values[index] = value;
        Apply(index, value);
        PlayerPrefs.SetFloat(Prefix + Keys[index], value);
        savePending = true;
        CancelInvoke(nameof(SavePreferences));
        Invoke(nameof(SavePreferences), 0.6f);
    }

    private void Apply(int index, float value)
    {
        if (index == 0)
        {
            AudioListener.volume = value;
            if (audioMixer != null) audioMixer.SetFloat(MixerParameters[0], 0f);
            return;
        }
        if (audioMixer == null) return;
        float decibels = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        audioMixer.SetFloat(MixerParameters[index], decibels);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RouteUnassignedInLoadedScenes();

    private void RouteUnassignedInLoadedScenes()
    {
        if (!routeUnassignedSources || groups[(int)AudioChannel.SFX] == null) return;
        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include))
            Route(source, AudioChannel.SFX);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SavePreferences();
    }

    private void OnApplicationQuit() => SavePreferences();

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
        SavePreferences();
    }
}
