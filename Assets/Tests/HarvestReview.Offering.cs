#if HARVEST_PLAYTEST
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed partial class HarvestReview
{
    // Two simulation frames per video frame at the replay's fixed 30 Hz.
    static IEnumerator CaptureClip(string shot, int count)
    {
        var folder = Path.Combine(Output, "OfferingCapture");
        Directory.CreateDirectory(folder);
        for (int i = 0; i < count; i++)
        {
            yield return new WaitForEndOfFrame();
            var frame = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder, $"{shot}-{i:D3}.jpg"), frame.EncodeToJPG(88));
            Destroy(frame);
            yield return null;
            yield return null;
        }
    }

    static string AltarHint() => Object.FindAnyObjectByType<InteractPromptHud>()
        .GetComponentsInChildren<TMP_Text>().Single(text => text.isActiveAndEnabled).text;

    static IEnumerator HoldAtAltar(AltarOffering altar)
    {
        var amount = trade.OfferAmount;
        var key = GameInput.GetBindingText(GameAction.Interact);
        yield return Frames(2);
        Check(amount > 0 && AltarHint() == $"Hold [{key}] to offer {amount} meat", "altar displays exact amount and bound input");
        if (routeMode == "day") p.StartCoroutine(CaptureClip("02-offering", 36));
        Keys(Key.E);
        yield return Frames(12);
        Canvas.ForceUpdateCanvases();
        var fill = GameObject.Find("Offering hold/Progress").GetComponent<RectTransform>();
        var track = (RectTransform)fill.parent;
        Check(altar.Progress > .25f && altar.Progress < .8f && track.rect.width >= 200 &&
            fill.rect.height >= 6 && fill.rect.width > 35 && fill.rect.width < track.rect.width,
            "holding advances a visibly sized progress bar");
        yield return Pic(routeMode + "-hold");
        yield return Frames(18);
        Check(altar.Progress == 0 && altar.GetComponentInChildren<Light>().enabled, "one hold triggered altar response");
        yield return Pic(routeMode + "-offered");
        Keys();
        yield return Frames(2);
        if (routeMode == "day") yield return Frames(48);
    }

    static IEnumerator OfferingCases(bool timeout)
    {
        var altar = Object.FindAnyObjectByType<AltarOffering>();
        var world = SaveGameService.ActiveSave.progress;
        var day = p.GetComponent<FirstQuotaDay>();
        Stage(793,754.4f);
        yield return Frames(6);
        yield return Target(altar);
        Check(AltarHint().Contains("No meat"), "empty altar explains the outstanding quota");
        world.meat = timeout ? 15 : 4;
        world.firstDayElapsed = FirstQuotaDay.DaySeconds - 55;
        yield return Frames(3);
        Check(GameObject.Find("Decree").GetComponent<TMP_Text>().text == "FINAL MINUTE", "tracker warns in final minute");
        yield return Press(Key.Escape);
        var pausedAt = world.firstDayElapsed;
        yield return Frames(8);
        Check(GamePause.IsPaused && world.firstDayElapsed == pausedAt, "pause freezes quota clock");
        yield return Press(Key.Escape);
        yield return Target(altar);
        if (timeout)
        {
            world.firstDayElapsed = FirstQuotaDay.DaySeconds - .46f;
            Keys(Key.E);
            yield return Frames(8);
            Check(altar.Progress > 0 && day.Accepting, "hold in progress before nightfall");
            yield return Frames(26);
            Keys();
            yield return Frames(3);
            Check(day.Result == 2 && world.meat == 15 && world.meatOffered == 0 && altar.Progress == 0, "timeout cancels hold without consuming meat");
            Check(day.objective.text == "Time ran out" && AltarHint().Contains("closed"), "expired tracker and altar agree");
            yield return Pic("timeout-expired");
            yield break;
        }

        Keys(Key.E);
        yield return Frames(11);
        Keys();
        yield return Frames(2);
        Check(world.meat == 4 && world.meatOffered == 0 && altar.Progress == 0, "early release cancels the transaction");
        Keys(Key.E,Key.S);
        yield return Frames(22);
        Keys();
        yield return Frames(2);
        Check(gun.CurrentInteractable != (IInteractable)altar && world.meat == 4 && world.meatOffered == 0 && altar.Progress == 0, "walking out of reach cancels the transaction");
        Stage(793,754.4f);
        yield return Frames(6);
        yield return Target(altar);
        yield return HoldAtAltar(altar);
        Check(world.meat == 0 && day.Remaining == 6 && day.objective.text.Contains("6 meat"), "one partial hold offers four and leaves six outstanding");
        Check(SaveGameService.Load(SaveGameService.ActiveSave.id), "loaded the partial offering save");
        SceneManager.LoadScene("DevilsHarvest_Island");
        yield return Frames(9);
        Player();
        day = p.GetComponent<FirstQuotaDay>();
        world = SaveGameService.ActiveSave.progress;
        altar = Object.FindAnyObjectByType<AltarOffering>();
        Stage(793,754.4f);
        yield return Frames(4);
        yield return Target(altar);
        Check(day.Remaining == 6 && day.Carried == 0 && AltarHint().Contains("6") && day.objective.text.Contains("6 meat at the altar")
            && GameObject.Find("Decree").GetComponent<TMP_Text>().text == "FINAL MINUTE", "reload restores partial balance, compact tracker and urgency");

        // Stage an independent full-quota balance and complete it during the final pulse.
        world.meat = 15;
        world.meatOffered = 0;
        world.firstDayElapsed = FirstQuotaDay.DaySeconds - 8;
        yield return Frames(3);
        Keys(Key.E);
        yield return Frames(32);
        Check(world.meat == 5 && world.meatOffered == 10 && day.Result == 1 && day.objective.text == "Offering accepted", "one full hold paid ten out of fifteen during the warning");
        yield return Frames(45);
        Keys();
        yield return Frames(2);
        Check(world.meat == 5 && world.meatOffered == 10 && !altar.GetComponentInChildren<Light>().enabled &&
            AltarHint().Contains("complete") && GameObject.Find("Decree").GetComponent<TMP_Text>().text == "THE DEVIL IS SATISFIED", "continued hold never repeats payment or feedback");
    }
}
#endif
