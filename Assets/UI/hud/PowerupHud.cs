using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shows the player's active powerups side by side: one <see cref="PowerupIndicator"/> (icon + time left) each.
/// Indicators are copies of an inactive template child, laid out by the container's Horizontal Layout Group.
/// </summary>
public sealed class PowerupHud : MonoBehaviour
{
    [Tooltip("The player whose powerups are shown.")]
    [SerializeField] private Player player;
    [Tooltip("Parent the indicators are created under (has a Horizontal Layout Group). Defaults to the template's parent.")]
    [SerializeField] private RectTransform container;
    [Tooltip("Styled indicator to copy for each active powerup. Kept inactive; it's only a template.")]
    [SerializeField] private PowerupIndicator template;

    private readonly List<PowerupIndicator> indicators = new List<PowerupIndicator>();
    private PlayerPowerups powerups;

    private void Awake()
    {
        if (template != null) template.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (powerups == null)
        {
            if (player == null) return;
            powerups = PlayerPowerups.For(player.gameObject);
        }

        IReadOnlyList<PlayerPowerups.Active> active = powerups.ActivePowerups;
        while (indicators.Count < active.Count && template != null)
        {
            Transform parent = container != null ? container : template.transform.parent;
            PowerupIndicator indicator = Instantiate(template, parent, false);
            indicator.name = "Powerup Indicator " + (indicators.Count + 1);
            indicators.Add(indicator);
        }

        for (int i = 0; i < indicators.Count; i++)
        {
            bool used = i < active.Count;
            if (indicators[i].gameObject.activeSelf != used) indicators[i].gameObject.SetActive(used);
            if (used) indicators[i].Show(active[i]);
        }
    }
}
