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
    private RectTransform holdBar, filled;

    private void SetHoldBar(bool visible,float progress)
    {
        if (visible && holdBar == null)
        {
            holdBar = (RectTransform)new GameObject("Offering hold", typeof(RectTransform)).transform;
            holdBar.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            holdBar.SetParent(panel.transform, false);
            holdBar.anchorMin = holdBar.anchorMax = new Vector2(.5f,0f);
            holdBar.sizeDelta = new Vector2(210,7);
            holdBar.anchoredPosition = new Vector2(0,-5);
            var track = holdBar.gameObject.AddComponent<UnityEngine.UI.Image>();
            track.color = new Color(.16f,.08f,.09f,.96f);
            track.raycastTarget = false;
            filled = (RectTransform)new GameObject("Progress",typeof(RectTransform)).transform;
            filled.SetParent(holdBar,false);
            filled.anchorMin = Vector2.zero;
            filled.offsetMin = filled.offsetMax = Vector2.zero;
            var ink = filled.gameObject.AddComponent<UnityEngine.UI.Image>();
            ink.color = new Color(1,.65f,.31f);
            ink.raycastTarget = false;
        }
        if (holdBar == null) return;
        holdBar.gameObject.SetActive(visible);
        if (visible) filled.anchorMax = new Vector2(progress,1);
    }

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

        var altar = !hint ? target as AltarOffering : null;
        SetHoldBar(altar != null && altar.CanInteract(weaponSystem),altar != null ? altar.Progress : 0);
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
        else if (altar != null && canInteract) label.text = $"Hold [{binding}] to {action}";
        else label.text = canInteract ? "[" + binding + "]  " + action : action;
        label.color = canInteract ? availableColor : blockedColor;
    }
}
