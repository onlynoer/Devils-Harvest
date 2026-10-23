using UnityEngine;

/// <summary>Provides shared hover and pressed feedback for menu UI elements.</summary>
public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }

    [Header("Audio source and clips")]
    public AudioSource audioSource;
    public AudioClip hoverSound;
    public AudioClip pressedSound;

    [Header("Playback levels")]
    [Range(0f, 1f)]
    public float hoverVolume = 1.0f;

    [Range(0f, 1f)]
    public float pressedVolume = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate UIAudioManager was found and disabled.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void PlayHoverSound()
    {
        if (audioSource != null && hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound, hoverVolume);
        }
    }

    public void PlayPressedSound()
    {
        if (audioSource != null && pressedSound != null)
        {
            audioSource.PlayOneShot(pressedSound, pressedVolume);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
