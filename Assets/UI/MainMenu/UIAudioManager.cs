using UnityEngine;

public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance;

    public AudioSource audioSource;
    public AudioClip hoverSound;
    public AudioClip pressedSound;

    [Range(0f, 1f)]
    public float hoverVolume = 1.0f;

    [Range(0f, 1f)]
    public float pressedVolume = 1.0f;

    private void Awake()
    {
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
}