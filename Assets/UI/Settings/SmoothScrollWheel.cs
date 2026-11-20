using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Smooth mouse-wheel scrolling for a vertical ScrollRect: each wheel notch glides the list instead of jumping.
/// Takes over the ScrollRect's own wheel handling (its Scroll Sensitivity is set to 0). Uses unscaled time,
/// so it also works in the pause menu. For the wheel to work anywhere over the list, the ScrollRect object
/// needs a raycast-target graphic (e.g. a fully transparent Image).
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public sealed class SmoothScrollWheel : MonoBehaviour, IScrollHandler
{
    // InputSystemUIInputModule reports one wheel notch as this much scroll delta (its Scroll Delta Per Tick).
    private const float DeltaPerNotch = 6f;

    [Tooltip("How far one wheel notch scrolls, in UI pixels (a keybind row is about 64).")]
    [Min(1f)] [SerializeField] private float pixelsPerNotch = 96f;
    [Tooltip("Seconds to glide to the new position. 0 = jump instantly.")]
    [Min(0f)] [SerializeField] private float smoothTime = .1f;

    private ScrollRect scroll;
    private float targetOffset;
    private float velocity;
    private bool gliding;

    private void Awake()
    {
        scroll = GetComponent<ScrollRect>();
        scroll.scrollSensitivity = 0f; // this component handles the wheel
    }

    private void OnDisable() => gliding = false;

    public void OnScroll(PointerEventData eventData)
    {
        float range = ScrollableHeight();
        if (!scroll.vertical || range <= 0f) return;

        if (!gliding) targetOffset = CurrentOffset();
        float notches = eventData.scrollDelta.y / DeltaPerNotch;
        // Wheel up (positive) moves toward the top of the list (offset 0).
        targetOffset = Mathf.Clamp(targetOffset - notches * pixelsPerNotch, 0f, range);
        scroll.StopMovement();
        gliding = true;
    }

    private void LateUpdate()
    {
        if (!gliding || scroll.content == null) return;

        // Grabbing the scrollbar or dragging the list takes over from the glide.
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            gliding = false;
            return;
        }

        float current = CurrentOffset();
        float next = smoothTime > 0f
            ? Mathf.SmoothDamp(current, targetOffset, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime)
            : targetOffset;
        if (Mathf.Abs(next - targetOffset) < .5f)
        {
            next = targetOffset;
            gliding = false;
            velocity = 0f;
        }
        Vector2 position = scroll.content.anchoredPosition;
        position.y = next;
        scroll.content.anchoredPosition = position;
    }

    // Content is anchored to the top, so its anchored Y is how far down the list is scrolled.
    private float CurrentOffset() => scroll.content != null ? scroll.content.anchoredPosition.y : 0f;

    private float ScrollableHeight()
    {
        if (scroll.content == null) return 0f;
        RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        return Mathf.Max(0f, scroll.content.rect.height - viewport.rect.height);
    }
}
