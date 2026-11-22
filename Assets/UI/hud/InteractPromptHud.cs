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
    [SerializeField] private Color availableColor = Color.white;
    [Tooltip("Color when the action isn't possible right now (inventory full, ammo full...).")]
    [SerializeField] private Color blockedColor = new Color(1f, .62f, .5f);

    private string lastAction;
    private string binding;
    private bool lastNearby;

    private void OnEnable()
    {
        RefreshBinding();
        GameInput.BindingsChanged += RefreshBinding;
    }

    private void RefreshBinding()
    {
        binding = GameInput.GetBindingText(GameAction.Interact);
        lastAction = null;
    }
    private bool lastCanInteract;

    private void OnDisable()
    {
        GameInput.BindingsChanged -= RefreshBinding;
        if (panel != null) panel.SetActive(false);
        lastAction = null;
    }

    private void LateUpdate()
    {
        IInteractable target = weaponSystem != null && weaponSystem.isActiveAndEnabled ? weaponSystem.CurrentInteractable : null;
        bool hint = target == null;
        if (hint && weaponSystem != null && weaponSystem.isActiveAndEnabled) target = weaponSystem.NearbyInteraction;
        if (!(target is Behaviour component) || component == null || !component.isActiveAndEnabled || !GameInput.GameplayEnabled)
            target = null;
        string action = target?.GetPrompt(weaponSystem);

        bool show = !string.IsNullOrEmpty(action);
        if (panel != null && panel.activeSelf != show) panel.SetActive(show);
        if (!show || label == null)
        {
            lastAction = null;
            return;
        }

        bool canInteract = target.CanInteract(weaponSystem);
        if (action == lastAction && canInteract == lastCanInteract && hint == lastNearby) return;
        lastNearby = hint;
        lastAction = action;
        lastCanInteract = canInteract;
        if (hint)
        {
            string description = target is MeatPickup ? "Dropped meat" :
                target is WeaponPickup pickup && pickup.Definition != null ? pickup.Definition.DisplayName :
                target is AltarOffering ? "Offering altar" : target is DevilDealer ? "Devil Dealer" : "Ammo";
            label.text = description + " nearby  /  Look toward it";
        }
        else label.text = canInteract ? "[" + binding + "]  " + action : action;
        label.color = canInteract ? availableColor : blockedColor;
    }
}
