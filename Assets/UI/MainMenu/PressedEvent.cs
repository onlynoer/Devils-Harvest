using UnityEngine;
using UnityEngine.EventSystems;

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
        // Handle pointer up event
    }
}