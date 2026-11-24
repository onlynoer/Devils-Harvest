using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-40)]
public sealed class FirstQuotaDay : MonoBehaviour
{
    public const int RequiredMeat = 10;
    public const int DaySeconds = 480;
    public TMP_Text objective;
    public TMP_FontAsset displayFont;
    public AudioClip revealSound;
    private SaveProgress progress;
    public int Result => progress.firstQuotaResult;
    public bool Accepting => progress != null && Result == 0 && SecondsLeft > 0;
    public int Remaining => Mathf.Max(0, RequiredMeat - progress.meatOffered);
    public int SecondsLeft => Mathf.CeilToInt(Mathf.Max(0, DaySeconds - progress.firstDayElapsed));
    public int Carried => progress.meat;
    public bool ReadyToOffer => Accepting && Remaining > 0 && Carried >= Remaining;
    public float DayFraction => Mathf.Clamp01(progress.firstDayElapsed / DaySeconds);

    private void Start()
    {
        if (objective != null && progress != null)
        {
            gameObject.AddComponent<QuotaCompass>().Bind(this, objective);
            gameObject.AddComponent<OfferingTracker>().Bind(this, SaveGameService.ConsumeNewHarvest());
        }
        gameObject.AddComponent<IslandDaylight>().Bind(this);
    }

    public void Bind(SaveProgress world) => progress = world;

    private void Update()
    {
        if (progress == null || GamePause.IsPaused) return;
        if (Result == 0)
        {
            progress.firstDayElapsed = Mathf.Min(DaySeconds, progress.firstDayElapsed + Time.deltaTime);
            if (Remaining == 0 || SecondsLeft == 0)
            {
                progress.firstQuotaResult = Remaining == 0 ? 1 : 2;
                SaveGameService.SaveActive();
            }
        }
        else if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame && !GetComponent<HarvestTrade>().IsOpen)
        {
            SaveGameService.SaveActive();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
