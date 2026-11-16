using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One authored slot of the inventory bar. Style it freely in the scene (sprites, sizes, fonts, extra decoration);
/// InventoryHud only touches the references below.
/// </summary>
[DisallowMultipleComponent]
public sealed class InventorySlotView : MonoBehaviour
{
    [Tooltip("Slot background. Tinted with the InventoryHud slot colors when 'Tint Backgrounds' is on.")]
    [SerializeField] private Image background;
    [Tooltip("Shows the item icon. Hidden while the slot is empty.")]
    [SerializeField] private RawImage icon;
    [Tooltip("Slot number label. Written by InventoryHud when 'Number Slots' is on, otherwise left as authored.")]
    [SerializeField] private TMP_Text label;
    [Tooltip("Optional: shown only while this slot is selected (border, glow, arrow...).")]
    [SerializeField] private GameObject selectedIndicator;
    [Tooltip("Optional: shown only while this slot holds no item.")]
    [SerializeField] private GameObject emptyIndicator;

    private void Reset() => AutoAssign();

    /// <summary>Fills any unassigned reference from this slot's own hierarchy.</summary>
    public void AutoAssign()
    {
        if (background == null) background = GetComponent<Image>();
        if (icon == null) icon = GetComponentInChildren<RawImage>(true);
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
    }

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
    }

    public void SetLabel(string text)
    {
        if (label != null && label.text != text) label.text = text;
    }

    public void SetIcon(Texture texture, Rect uvRect)
    {
        if (icon != null)
        {
            if (icon.texture != texture) icon.texture = texture;
            if (icon.uvRect != uvRect) icon.uvRect = uvRect;
            bool show = texture != null;
            if (icon.enabled != show) icon.enabled = show;
        }
        SetActive(emptyIndicator, texture == null);
    }

    public void SetSelected(bool selected, bool tint, Color normalColor, Color selectedColor)
    {
        if (tint && background != null)
        {
            Color color = selected ? selectedColor : normalColor;
            if (background.color != color) background.color = color;
        }
        SetActive(selectedIndicator, selected);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active) target.SetActive(active);
    }
}
