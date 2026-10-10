using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    public AudioMixer audioMixer;

    private const string Prefix = "DevilsHarvest.Settings.Audio.";
    private const float DefaultVolume = 0.5f;
    private bool savePending;

    private static readonly string[] Keys = { "Master", "Music", "SFX", "UI" };
    private static readonly string[] MixerParameters = { "MasterVolume", "MusicVolume", "SFXVolume", "UIVolume" };
    private readonly float[] values = { DefaultVolume, DefaultVolume, DefaultVolume, DefaultVolume };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        for (int i = 0; i < Keys.Length; i++)
        {
            string key = Prefix + Keys[i];
            float fallback = PlayerPrefs.HasKey(key)
                ? PlayerPrefs.GetFloat(key, DefaultVolume)
                : PlayerPrefs.GetFloat(Keys[i] == "SFX" ? "SFXVolume" : Keys[i] + "Volume", DefaultVolume);
            values[i] = Mathf.Clamp01(fallback);
            Apply(i, values[i]);
        }
    }

    public float GetMasterVolume() => values[0];
    public float GetMusicVolume() => values[1];
    public float GetSFXVolume() => values[2];
    public float GetUIVolume() => values[3];

    public void SetMasterVolume(float value) => SetVolume(0, value);
    public void SetMusicVolume(float value) => SetVolume(1, value);
    public void SetSFXVolume(float value) => SetVolume(2, value);
    public void SetUIVolume(float value) => SetVolume(3, value);

    public void SavePreferences()
    {
        if (!savePending) return;
        PlayerPrefs.Save();
        savePending = false;
    }

    private void SetVolume(int index, float value)
    {
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
        if (audioMixer == null) return;
        float decibels = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        audioMixer.SetFloat(MixerParameters[index], decibels);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SavePreferences();
    }

    private void OnApplicationQuit() => SavePreferences();

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SavePreferences();
    }
}
