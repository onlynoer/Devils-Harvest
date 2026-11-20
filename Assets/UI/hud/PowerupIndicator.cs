using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One active powerup on the HUD: its icon with the time left underneath. PowerupHud copies an inactive
/// template of this per active powerup, so restyle the template in the scene to restyle them all.
/// </summary>
public sealed class PowerupIndicator : MonoBehaviour
{
    [Tooltip("Shows the powerup's Icon. With no icon set, it's tinted with the powerup's Color instead.")]
    [SerializeField] private Image icon;
    [Tooltip("Time left, e.g. 4.2s.")]
    [SerializeField] private TMP_Text timeLabel;
    [Tooltip("Optional. Shows the powerup's first letter when it has no icon.")]
    [SerializeField] private TMP_Text fallbackLetter;
    [Tooltip("Below this many seconds left, the time shows tenths (4.2s); above it, whole seconds (12s).")]
    [Min(0f)] [SerializeField] private float showTenthsBelow = 10f;
    [Tooltip("Time text color when the powerup is about to run out.")]
    [SerializeField] private Color endingColor = new Color(1f, .35f, .3f);
    [Tooltip("Seconds left when the time turns Ending Color.")]
    [Min(0f)] [SerializeField] private float endingWarning = 2f;

    private PowerupData shownData;
    private int shownTime = int.MinValue;
    private Color normalTimeColor = Color.white;
    private Sprite defaultIconSprite;

    private void Awake()
    {
        if (timeLabel != null) normalTimeColor = timeLabel.color;
        if (icon != null) defaultIconSprite = icon.sprite;
    }

    public void Show(PlayerPowerups.Active active)
    {
        if (active == null || active.data == null) return;
        if (active.data != shownData) ShowData(active.data);
        ShowTime(active.Remaining);
    }

    private void ShowData(PowerupData data)
    {
        shownData = data;
        shownTime = int.MinValue;
        bool hasIcon = data.icon != null;
        if (icon != null)
        {
            icon.sprite = hasIcon ? data.icon : defaultIconSprite;
            icon.color = hasIcon ? Color.white : data.color;
            icon.preserveAspect = hasIcon;
        }
        if (fallbackLetter != null)
        {
            fallbackLetter.gameObject.SetActive(!hasIcon);
            string label = data.DisplayName;
            fallbackLetter.text = string.IsNullOrEmpty(label) ? string.Empty : label.Substring(0, 1).ToUpperInvariant();
        }
    }

    private void ShowTime(float remaining)
    {
        if (timeLabel == null) return;
        // Only rebuild the string when the shown value changes (avoids per-frame garbage).
        bool tenths = remaining < showTenthsBelow;
        int value = tenths ? Mathf.CeilToInt(remaining * 10f) : Mathf.CeilToInt(remaining) * 1000;
        if (value == shownTime) return;
        shownTime = value;
        timeLabel.text = tenths ? (value / 10f).ToString("0.0") + "s" : Mathf.CeilToInt(remaining) + "s";
        timeLabel.color = remaining <= endingWarning ? endingColor : normalTimeColor;
    }
}
