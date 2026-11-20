using TMPro;
using UnityEngine;

/// <summary>
/// Shows "Press E to ..." on the canvas for whatever the player is looking at (see IInteractable).
/// Style the panel and text in the scene; this script only sets the text and color and shows/hides the panel.
/// </summary>
public sealed class InteractPromptHud : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponSystem weaponSystem;
    [Tooltip("Shown while there is something to interact with. Must be a child, not this object.")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text label;

    [Header("Text")]
    [Tooltip("{0} is replaced by the action, e.g. 'pick up AK47'.")]
    [SerializeField] private string promptFormat = "Press E to {0}";
    [SerializeField] private Color availableColor = Color.white;
    [Tooltip("Color when the action isn't possible right now (inventory full, ammo full...).")]
    [SerializeField] private Color blockedColor = new Color(1f, .62f, .5f);

    private string lastAction;
    private bool lastCanInteract;

    private void OnDisable()
    {
        if (panel != null) panel.SetActive(false);
        lastAction = null;
    }

    private void LateUpdate()
    {
        IInteractable target = weaponSystem != null && weaponSystem.isActiveAndEnabled ? weaponSystem.CurrentInteractable : null;
        string action = target != null ? target.GetPrompt(weaponSystem) : null;

        bool show = !string.IsNullOrEmpty(action);
        if (panel != null && panel.activeSelf != show) panel.SetActive(show);
        if (!show || label == null)
        {
            lastAction = null;
            return;
        }

        bool canInteract = target.CanInteract(weaponSystem);
        if (ReferenceEquals(action, lastAction) && canInteract == lastCanInteract) return;
        lastAction = action;
        lastCanInteract = canInteract;
        label.text = canInteract ? string.Format(promptFormat, action) : action;
        label.color = canInteract ? availableColor : blockedColor;
    }
}
