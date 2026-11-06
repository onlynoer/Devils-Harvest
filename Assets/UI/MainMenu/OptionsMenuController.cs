using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Coordinates the Options tabs, audio sliders, tab styling, and close behavior.</summary>
public class OptionsMenuController : MonoBehaviour
{
    [Header("Scene references")]
    public SettingsManager settingsManager;
    public MenuManager menuManager;
    public Button[] tabs;
    public Image[] tabImages;
    public GameObject[] pages;
    public TMP_Text sectionHeading;
    public Slider[] volumeSliders;
    public TMP_Text[] volumeValues;
    public Button closeButton;

    [Header("Tab colors")]
    [SerializeField] private Color selectedTabColor = new Color(0.37f, 0.35f, 0.32f, 0.98f);
    [SerializeField] private Color idleTabColor = new Color(0.19f, 0.19f, 0.20f, 0.94f);
    private readonly Color selectedTextColor = new Color(0.97f, 0.94f, 0.88f, 1f);
    private readonly Color idleTextColor = new Color(0.72f, 0.70f, 0.67f, 1f);
    private UnityEngine.Events.UnityAction[] tabCallbacks;
    private UnityEngine.Events.UnityAction<float>[] volumeCallbacks;
    private UnityEngine.Events.UnityAction closeCallback;

    private void OnEnable() => Initialize();

    public void Initialize()
    {
        // Resolve optional scene services and keep this usable when a page has no assigned reference.
        if (settingsManager == null) settingsManager = SettingsManager.Instance;
        if (sectionHeading == null)
        {
            Transform headingTransform = transform.Find("SettingsCardBorder/SettingsCardSurface/SectionHeading");
            if (headingTransform != null) sectionHeading = headingTransform.GetComponent<TMP_Text>();
        }
        BindControls();
        if (settingsManager != null && volumeSliders != null && volumeSliders.Length >= 4)
        {
            volumeSliders[0].SetValueWithoutNotify(settingsManager.GetMasterVolume());
            volumeSliders[1].SetValueWithoutNotify(settingsManager.GetMusicVolume());
            volumeSliders[2].SetValueWithoutNotify(settingsManager.GetSFXVolume());
            volumeSliders[3].SetValueWithoutNotify(settingsManager.GetUIVolume());
            RefreshValues();
        }
        SelectTab(0);
    }

    private void BindControls()
    {
        if (tabs != null)
        {
            if (tabCallbacks == null || tabCallbacks.Length != tabs.Length)
                tabCallbacks = new UnityEngine.Events.UnityAction[tabs.Length];
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                if (tabs[i] == null) continue;
                if (tabCallbacks[i] != null) tabs[i].onClick.RemoveListener(tabCallbacks[i]);
                tabCallbacks[i] = () => { SelectTab(index); PlayButtonSound(); };
                tabs[i].onClick.AddListener(tabCallbacks[i]);
            }
        }

        if (volumeSliders != null && settingsManager != null)
        {
            if (volumeCallbacks == null || volumeCallbacks.Length != volumeSliders.Length)
                volumeCallbacks = new UnityEngine.Events.UnityAction<float>[volumeSliders.Length];
            for (int i = 0; i < volumeSliders.Length && i < 4; i++)
            {
                if (volumeSliders[i] == null) continue;
                if (volumeCallbacks[i] != null) volumeSliders[i].onValueChanged.RemoveListener(volumeCallbacks[i]);
                int index = i;
                volumeCallbacks[i] = value => SetVolume(index, value);
                volumeSliders[i].onValueChanged.AddListener(volumeCallbacks[i]);
            }
        }

        if (closeButton != null)
        {
            if (closeCallback != null) closeButton.onClick.RemoveListener(closeCallback);
            closeCallback = CloseMenuWithSound;
            closeButton.onClick.AddListener(closeCallback);
        }
    }

    private void CloseMenuWithSound()
    {
        CloseMenu();
        PlayButtonSound();
    }

    public void SelectTab(int index)
    {
        // Page index is shared by the tab buttons, heading labels, and page objects.
        if (pages == null || pages.Length == 0) return;
        index = Mathf.Clamp(index, 0, pages.Length - 1);
        if (sectionHeading != null)
        {
            string[] headings = { "Audio", "Graphics", "Controls", "Credits" };
            sectionHeading.text = index < headings.Length ? headings[index] : "Options";
        }
        for (int i = 0; i < pages.Length; i++)
            if (pages[i] != null) pages[i].SetActive(i == index);

        if (tabs != null)
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                if (tabImages != null && i < tabImages.Length && tabImages[i] != null)
                    tabImages[i].color = i == index ? selectedTabColor : idleTabColor;
                TMP_Text label = tabs[i].GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.color = i == index ? selectedTextColor : idleTextColor;
            }

        if (EventSystem.current != null && tabs != null && index < tabs.Length && tabs[index] != null)
            EventSystem.current.SetSelectedGameObject(tabs[index].gameObject);
    }

    public void OpenMenu()
    {
        if (menuManager != null) menuManager.OpenSettings();
        SelectTab(0);
        if (tabs != null && tabs.Length > 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(tabs[0].gameObject);
    }

    public void CloseMenu()
    {
        if (settingsManager != null) settingsManager.SavePreferences();
        if (menuManager != null) menuManager.CloseSettings();
    }

    private void SetVolume(int index, float value)
    {
        if (settingsManager == null) return;
        switch (index)
        {
            case 0: settingsManager.SetMasterVolume(value); break;
            case 1: settingsManager.SetMusicVolume(value); break;
            case 2: settingsManager.SetSFXVolume(value); break;
            case 3: settingsManager.SetUIVolume(value); break;
        }
        if (volumeValues != null && index < volumeValues.Length && volumeValues[index] != null)
            volumeValues[index].text = value.ToString("0.00");
    }

    private void RefreshValues()
    {
        if (volumeSliders == null || volumeValues == null) return;
        for (int i = 0; i < volumeSliders.Length && i < volumeValues.Length; i++)
            if (volumeSliders[i] != null && volumeValues[i] != null)
                volumeValues[i].text = volumeSliders[i].value.ToString("0.00");
    }

    private static void PlayButtonSound()
    {
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPressedSound();
    }

    private void OnDisable()
    {
        UnbindControls();
        if (settingsManager != null) settingsManager.SavePreferences();
    }

    private void UnbindControls()
    {
        if (tabs != null && tabCallbacks != null)
            for (int i = 0; i < tabs.Length && i < tabCallbacks.Length; i++)
                if (tabs[i] != null && tabCallbacks[i] != null) tabs[i].onClick.RemoveListener(tabCallbacks[i]);
        if (volumeSliders != null && volumeCallbacks != null)
            for (int i = 0; i < volumeSliders.Length && i < volumeCallbacks.Length; i++)
                if (volumeSliders[i] != null && volumeCallbacks[i] != null) volumeSliders[i].onValueChanged.RemoveListener(volumeCallbacks[i]);
        if (closeButton != null && closeCallback != null) closeButton.onClick.RemoveListener(closeCallback);
    }
}
