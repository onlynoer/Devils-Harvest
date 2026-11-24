using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OfferingTracker : MonoBehaviour
{
    private FirstQuotaDay day;
    private RectTransform card;
    private TMP_Text heading, demand, clock, caption, tally;
    private Image border;
    private OfferingSeal seal;
    private float revealTime, payoffTime;
    private int lastResult, lastSecond = -1, lastCarried = -1, lastRemaining = -1;
    private bool lastExpanded;
    private static readonly Color Gold = new Color(.97f,.78f,.53f);
    private static readonly Color Red = new Color(.93f,.27f,.19f);

    public void Bind(FirstQuotaDay source, bool fresh)
    {
        day = source;
        revealTime = fresh && day.Accepting ? 2.65f : 0f;
        lastResult = day.Result;
        demand = day.objective;
        card = (RectTransform)demand.transform.parent;
        card.sizeDelta = new Vector2(748,128);
        var background = card.GetComponent<Image>();
        background.color = new Color(.085f,.038f,.054f,.96f);
        background.raycastTarget = false;
        border = Element("Ember rule",0,0,748,3).gameObject.AddComponent<Image>();
        border.raycastTarget = false;
        seal = Element("Red sun over the altar",15,10,102,108).gameObject.AddComponent<OfferingSeal>();
        seal.raycastTarget = false;
        heading = Text("Decree",124,12,457,25,20);
        heading.fontStyle = FontStyles.Bold;
        heading.characterSpacing = 3;
        Position(demand.rectTransform,124,40,468,49);
        demand.enableAutoSizing = true;
        demand.fontSizeMin = 24; demand.fontSizeMax = 33;
        demand.textWrappingMode = TextWrappingModes.NoWrap;
        demand.alignment = TextAlignmentOptions.MidlineLeft;
        if (day.displayFont != null) demand.font = day.displayFont;
        demand.color = new Color(1,.95f,.83f);
        demand.raycastTarget = false;
        tally = Text("Tithe",125,91,465,27,20);
        caption = Text("Deadline",594,20,143,28,18);
        caption.alignment = TextAlignmentOptions.Center;
        caption.fontStyle = FontStyles.Bold;
        clock = Text("Clock",587,47,153,58,45);
        clock.alignment = TextAlignmentOptions.Center;
        clock.fontStyle = FontStyles.Bold;
        if (revealTime > 0 && day.revealSound != null)
        {
            var audio = gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.pitch = .76f;
            SettingsManager.Route(audio, AudioChannel.SFX);
            audio.PlayOneShot(day.revealSound,.54f);
        }
        Render();
    }

    private static void Position(RectTransform rect, float x,float y,float w,float h)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1);
        rect.anchoredPosition = new Vector2(x,-y);
        rect.sizeDelta = new Vector2(w,h);
    }

    private RectTransform Element(string name,float x,float y,float w,float h)
    {
        var rect = (RectTransform)new GameObject(name,typeof(RectTransform)).transform;
        rect.SetParent(card,false);
        Position(rect,x,y,w,h);
        return rect;
    }

    private TMP_Text Text(string name,float x,float y,float w,float h,float size)
    {
        var text = Element(name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.color = Gold;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        return text;
    }

    private void LateUpdate()
    {
        if (day == null) return;
        if (day.Result != lastResult)
        {
            revealTime = 0;
            payoffTime = 1.9f;
            lastSecond = -1;
            lastResult = day.Result;
        }
        Render();
        revealTime = Mathf.Max(0,revealTime-Time.deltaTime);
        payoffTime = Mathf.Max(0,payoffTime-Time.deltaTime);
    }

    private void Render()
    {
        float remaining = Mathf.Max(revealTime,payoffTime);
        float compact = 1-Mathf.SmoothStep(0,1,Mathf.Clamp01(remaining/.65f));
        card.anchorMin = card.anchorMax = Vector2.Lerp(new Vector2(.5f,.84f),Vector2.one,compact);
        card.pivot = Vector2.Lerp(new Vector2(.5f,1),Vector2.one,compact);
        card.anchoredPosition = new Vector2(-20,-18)*compact;
        card.localScale = Vector3.one*Mathf.Lerp(1,.67f,compact);
        int seconds = day.SecondsLeft;
        bool urgent = day.Accepting && seconds <= 60;
        float beat = urgent && seconds <= 10 ? Mathf.Pow(Mathf.Max(0,Mathf.Cos(2*Mathf.PI*day.DayFraction*FirstQuotaDay.DaySeconds)),4) : 0;
        border.color = day.Result == 1 ? Gold : Color.Lerp(urgent ? Red : new Color(.58f,.20f,.16f),Gold,beat*.60f);
        seal.color = Color.Lerp(Red,Gold,beat*.48f);
        bool expanded = compact < .95f;
        if (lastSecond == seconds && lastCarried == day.Carried && lastRemaining == day.Remaining && lastExpanded == expanded) return;
        lastSecond = seconds; lastCarried = day.Carried; lastRemaining = day.Remaining; lastExpanded = expanded;
        heading.text = day.Result == 1 ? "THE DEVIL IS SATISFIED" : day.Result == 2 ? "NIGHT HAS FALLEN" :
            urgent ? "FINAL MINUTE" : expanded ? "THE DEVIL DEMANDS AN OFFERING" : "THE FIRST OFFERING";
        heading.color = urgent ? new Color(1,.57f,.40f) : Gold;
        demand.text = day.Result == 1 ? "Offering accepted" : day.Result == 2 ? "Time ran out" :
            expanded ? $"Offer {day.Remaining} meat before nightfall" : $"Offer {day.Remaining} meat at the altar";
        tally.text = day.Result != 0 ? "Explore   /   Enter for the menu" :
            $"Carried  {day.Carried}     /     Offered  {FirstQuotaDay.RequiredMeat-day.Remaining} of {FirstQuotaDay.RequiredMeat}";
        caption.text = day.Result == 1 ? "ACCEPTED" : day.Result == 2 ? "EXPIRED" : "TIME LEFT";
        clock.text = $"{seconds / 60}:{seconds % 60:00}";
        clock.color = urgent ? new Color(1,.85f,.72f) : Color.white;
    }
}
