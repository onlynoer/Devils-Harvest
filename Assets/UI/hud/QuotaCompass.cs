using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuotaCompass : MonoBehaviour
{
    private FirstQuotaDay quota;
    private WeaponSystem weapons;
    private Transform altar, dealer;
    private RectTransform panel, altarArrow, dealerArrow;

    public void Bind(FirstQuotaDay day, TMP_Text objective)
    {
        quota = day;
        weapons = GetComponent<WeaponSystem>();
        var table = FindAnyObjectByType<AltarOffering>();
        var trade = GetComponent<HarvestTrade>();
        if (table == null || trade == null || trade.dealer == null || objective.canvas == null) return;
        altar = table.transform;
        dealer = trade.dealer.transform;
        panel = MakeRect("Destination bearings", objective.canvas.transform);
        panel.anchorMin = panel.anchorMax = new Vector2(1f, .72f);
        panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-24f, 0f);
        panel.sizeDelta = new Vector2(246f, 82f);
        var background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(.05f, .045f, .045f, .85f);
        background.raycastTarget = false;
        altarArrow = Row(objective.font, "Altar  /  Offer meat", new Color(1f, .74f, .37f), -20f);
        dealerArrow = Row(objective.font, "Dealer  /  Optional", new Color(.52f, .90f, .94f), -60f);
        panel.gameObject.SetActive(false);
    }

    private RectTransform Row(TMP_FontAsset font, string caption, Color color, float y)
    {
        var arrow = MakeRect("Bearing", panel);
        arrow.anchorMin = arrow.anchorMax = new Vector2(0f, 1f);
        arrow.anchoredPosition = new Vector2(24f, y);
        arrow.sizeDelta = new Vector2(21f, 21f);
        var pointer = arrow.gameObject.AddComponent<BearingArrow>();
        pointer.color = color;
        pointer.raycastTarget = false;
        var label = MakeRect(caption, panel).gameObject.AddComponent<TextMeshProUGUI>();
        var rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, .5f);
        rect.anchoredPosition = new Vector2(46f,y);
        rect.sizeDelta = new Vector2(198f,38f);
        label.font = font;
        label.fontSize = 21f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = caption;
        label.color = color;
        label.raycastTarget = false;
        return arrow;
    }

    private static RectTransform MakeRect(string title, Transform parent)
    {
        var rect = (RectTransform)new GameObject(title, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private void LateUpdate()
    {
        if (panel == null || weapons == null) return;
        bool visible = quota.ReadyToOffer && weapons.isActiveAndEnabled && GameInput.GameplayEnabled;
        if (panel.gameObject.activeSelf != visible) panel.gameObject.SetActive(visible);
        if (!visible) return;
        Point(altarArrow, altar);
        Point(dealerArrow, dealer);
    }

    private void Point(RectTransform arrow, Transform destination)
    {
        var delta = Vector3.ProjectOnPlane(destination.position - transform.position, Vector3.up);
        float bearing = Vector3.SignedAngle(transform.forward, delta, Vector3.up);
        arrow.localRotation = Quaternion.Euler(0f, 0f, -bearing);
    }
}

internal sealed class BearingArrow : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        float r = rectTransform.rect.width * .5f;
        mesh.AddVert(new Vector2(0f,r),color,Vector2.zero);
        mesh.AddVert(new Vector2(r,-r),color,Vector2.zero);
        mesh.AddVert(new Vector2(0f,-r * .45f),color,Vector2.zero);
        mesh.AddVert(new Vector2(-r,-r),color,Vector2.zero);
        mesh.AddTriangle(0,1,2);
        mesh.AddTriangle(0,2,3);
    }
}