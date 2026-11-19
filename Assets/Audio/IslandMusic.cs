using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class IslandMusic : MonoBehaviour
{
    [SerializeField, Min(0f)] private float fadeSeconds = 3f;

    private IEnumerator Start()
    {
        var source = GetComponent<AudioSource>();
        float level = source.volume;
        source.volume = 0f;
        source.Play();

        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = level * Mathf.SmoothStep(0f, 1f, elapsed / fadeSeconds);
            yield return null;
        }
        source.volume = level;
    }
}
