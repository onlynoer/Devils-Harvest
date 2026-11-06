using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Scales a UI element while hovered and plays the shared UI hover sound.</summary>
public class HoverEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Min(1f)]
    public float hoverScale = 1.1f;

    private Vector3 originalSize;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null) originalSize = rectTransform.localScale;
    }

    private void OnEnable()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null) rectTransform.localScale = originalSize;
    }

    private void OnDisable()
    {
        if (rectTransform != null) rectTransform.localScale = originalSize;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (rectTransform == null) return;
        rectTransform.localScale = originalSize * hoverScale;

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayHoverSound();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (rectTransform != null) rectTransform.localScale = originalSize;
    }
}
