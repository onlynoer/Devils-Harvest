using UnityEngine;
using UnityEngine.EventSystems;

public class HoverEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverScale = 1.1f;

    private Vector3 originalSize;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalSize = rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rectTransform.localScale = originalSize * hoverScale;

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayHoverSound();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.localScale = originalSize;
    }
}