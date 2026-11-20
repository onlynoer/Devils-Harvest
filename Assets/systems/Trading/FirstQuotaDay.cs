using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class FirstQuotaDay : MonoBehaviour
{
    public const int RequiredMeat = 10;
    public const int DaySeconds = 480;
    public TMP_Text objective;
    private SaveProgress progress;
    private int shownSeconds = -1, shownOffered = -1, shownMeat = -1, shownResult = -1;
    public bool Accepting => progress != null && progress.firstQuotaResult == 0;
    public int Remaining => Mathf.Max(0, RequiredMeat - progress.meatOffered);

    public void Bind(SaveProgress world) { progress = world; }

    private void Update()
    {
        if (progress == null) return;
        if (Accepting)
        {
            progress.firstDayElapsed = Mathf.Min(DaySeconds, progress.firstDayElapsed + Time.deltaTime);
            Evaluate();
        }
        else if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame &&
                 !GetComponent<HarvestTrade>().IsOpen)
        {
            SaveGameService.SaveActive();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("MainMenu");
            return;
        }
        int seconds = Mathf.CeilToInt(DaySeconds - progress.firstDayElapsed);
        if (objective == null || (seconds == shownSeconds && shownOffered == progress.meatOffered &&
            shownMeat == progress.meat && shownResult == progress.firstQuotaResult)) return;
        shownSeconds = seconds;
        shownOffered = progress.meatOffered;
        shownMeat = progress.meat;
        shownResult = progress.firstQuotaResult;
        if (Accepting)
            objective.text = $"FIRST OFFERING   {seconds / 60}:{seconds % 60:00}\nBring {Remaining} more meat to the mountain altar\n{progress.meat} carried   ·   {Mathf.Min(RequiredMeat, progress.meatOffered)}/{RequiredMeat} offered";
        else
            objective.text = (shownResult == 1 ? "FIRST QUOTA MET" : "FIRST QUOTA MISSED") +
                $"\n{Mathf.Min(RequiredMeat, progress.meatOffered)}/{RequiredMeat} offered   ·   {progress.meat} carried\nExplore, or press Enter for the menu";
    }

    public void Evaluate()
    {
        if (!Accepting) return;
        if (Remaining == 0) progress.firstQuotaResult = 1;
        else if (progress.firstDayElapsed >= DaySeconds) progress.firstQuotaResult = 2;
        else return;
        SaveGameService.SaveActive();
    }
}
