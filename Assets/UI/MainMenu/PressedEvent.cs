using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Plays the shared pressed sound when a pointer begins interacting with this UI element.</summary>
public class PressedEvent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayPressedSound();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Reserved for optional release feedback; pressed audio is intentionally played on pointer down.
    }
}
