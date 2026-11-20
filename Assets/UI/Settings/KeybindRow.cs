using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One row of the Controls page: action name on the left, its key on a button on the right.
/// Clicking the button waits for a new key. Style the row in the scene; ControlsSettingsPage copies it per action.
/// </summary>
public sealed class KeybindRow : MonoBehaviour
{
    [SerializeField] private TMP_Text actionLabel;
    [SerializeField] private Button bindingButton;
    [SerializeField] private TMP_Text bindingLabel;

    private GameAction action;
    private ControlsSettingsPage page;

    public GameAction Action => action;

    public void Setup(GameAction rowAction, ControlsSettingsPage owner)
    {
        action = rowAction;
        page = owner;
        if (actionLabel != null) actionLabel.text = GameInput.GetLabel(action);
        if (bindingButton != null)
        {
            bindingButton.onClick.RemoveListener(OnClicked);
            bindingButton.onClick.AddListener(OnClicked);
        }
        Refresh();
    }

    public void Refresh()
    {
        if (bindingLabel != null) bindingLabel.text = GameInput.GetBindingText(action);
    }

    public void ShowWaiting(string text)
    {
        if (bindingLabel != null) bindingLabel.text = text;
    }

    private void OnClicked()
    {
        if (page != null) page.BeginRebind(this);
    }

    private void OnDestroy()
    {
        if (bindingButton != null) bindingButton.onClick.RemoveListener(OnClicked);
    }
}
