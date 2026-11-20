using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Controls tab of the settings panel. Builds one <see cref="KeybindRow"/> per rebindable action from an
/// authored row template, and handles rebinding. To restyle the rows, edit the template row in the scene.
/// </summary>
public sealed class ControlsSettingsPage : MonoBehaviour
{
    [Tooltip("Parent the rows are created under (usually a scroll view's Content with a Vertical Layout Group).")]
    [SerializeField] private RectTransform rowContainer;
    [Tooltip("Styled row to copy for each action. Kept inactive; it's only a template.")]
    [SerializeField] private KeybindRow rowTemplate;
    [Tooltip("Optional button that restores every default key.")]
    [SerializeField] private Button resetButton;
    [Tooltip("Shown on a key button while waiting for the new key.")]
    [SerializeField] private string waitingText = "Press a key...";

    [Header("Mouse Sensitivity (ranges come from ControlSettings)")]
    [Tooltip("Left/right camera speed slider.")]
    [FormerlySerializedAs("sensitivitySlider")]
    [SerializeField] private Slider horizontalSlider;
    [Tooltip("Optional text showing the horizontal value, e.g. 1.00x.")]
    [FormerlySerializedAs("sensitivityValue")]
    [SerializeField] private TMPro.TMP_Text horizontalValue;
    [Tooltip("Up/down camera speed slider.")]
    [SerializeField] private Slider verticalSlider;
    [Tooltip("Optional text showing the vertical value, e.g. 1.00x.")]
    [SerializeField] private TMPro.TMP_Text verticalValue;

    private readonly List<KeybindRow> rows = new List<KeybindRow>();

    private void Awake()
    {
        if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
        if (resetButton != null) resetButton.onClick.AddListener(ResetToDefaults);
        SetupSlider(horizontalSlider, OnHorizontalChanged);
        SetupSlider(verticalSlider, OnVerticalChanged);
    }

    private static void SetupSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null) return;
        slider.minValue = ControlSettings.MinSensitivity;
        slider.maxValue = ControlSettings.MaxSensitivity;
        slider.wholeNumbers = false;
        slider.onValueChanged.AddListener(callback);
    }

    private void OnEnable()
    {
        BuildRows();
        GameInput.BindingsChanged += RefreshAll;
        RefreshAll();
        RefreshSensitivity();
    }

    private void OnDisable()
    {
        GameInput.BindingsChanged -= RefreshAll;
        GameInput.CancelRebind();
    }

    private void OnDestroy()
    {
        if (resetButton != null) resetButton.onClick.RemoveListener(ResetToDefaults);
        if (horizontalSlider != null) horizontalSlider.onValueChanged.RemoveListener(OnHorizontalChanged);
        if (verticalSlider != null) verticalSlider.onValueChanged.RemoveListener(OnVerticalChanged);
    }

    private void OnHorizontalChanged(float value)
    {
        ControlSettings.HorizontalSensitivity = value;
        ShowValue(horizontalValue, ControlSettings.HorizontalSensitivity);
    }

    private void OnVerticalChanged(float value)
    {
        ControlSettings.VerticalSensitivity = value;
        ShowValue(verticalValue, ControlSettings.VerticalSensitivity);
    }

    private void RefreshSensitivity()
    {
        if (horizontalSlider != null) horizontalSlider.SetValueWithoutNotify(ControlSettings.HorizontalSensitivity);
        if (verticalSlider != null) verticalSlider.SetValueWithoutNotify(ControlSettings.VerticalSensitivity);
        ShowValue(horizontalValue, ControlSettings.HorizontalSensitivity);
        ShowValue(verticalValue, ControlSettings.VerticalSensitivity);
    }

    private static void ShowValue(TMPro.TMP_Text text, float value)
    {
        if (text != null) text.text = value.ToString("0.00") + "x";
    }

    public void BeginRebind(KeybindRow row)
    {
        if (row == null) return;
        RefreshAll(); // clears any other row still showing the waiting text
        row.ShowWaiting(waitingText);
        PlayClick();
        GameInput.StartRebind(row.Action, _ => RefreshAll());
    }

    private void ResetToDefaults()
    {
        PlayClick();
        GameInput.ResetToDefaults();
        ControlSettings.ResetToDefaults();
        RefreshSensitivity();
    }

    private void BuildRows()
    {
        if (rows.Count > 0 || rowTemplate == null) return;
        Transform parent = rowContainer != null ? rowContainer : rowTemplate.transform.parent;
        foreach (GameAction action in GameInput.AllActions)
        {
            KeybindRow row = Instantiate(rowTemplate, parent);
            row.name = "Keybind " + action;
            row.gameObject.SetActive(true);
            row.Setup(action, this);
            rows.Add(row);
        }
    }

    private void RefreshAll()
    {
        foreach (KeybindRow row in rows)
            if (row != null) row.Refresh();
    }

    private static void PlayClick()
    {
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPressedSound();
    }
}
