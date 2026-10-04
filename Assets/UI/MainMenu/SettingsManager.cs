using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    public AudioMixer audioMixer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        ApplyVolume("MasterVolume", masterVolume);
        ApplyVolume("MusicVolume", musicVolume);
        ApplyVolume("SFXVolume", sfxVolume);
    }

    private void ApplyVolume(string parameterName, float volume)
    {
        float volumeDB = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
        audioMixer.SetFloat(parameterName, volumeDB);
    }

    public void SetMasterVolume(float volume)
    {
        float volumeDB = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f; //multi by 20 to convert to decibels
        audioMixer.SetFloat("MasterVolume", volumeDB);

        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        float volumeDB = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
        audioMixer.SetFloat("MusicVolume", volumeDB);

        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        float volumeDB = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
        audioMixer.SetFloat("SFXVolume", volumeDB);

        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
    }
}