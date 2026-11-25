using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Displays the equipped weapon's existing state without owning ammo or input.</summary>
public sealed class WeaponHud : MonoBehaviour
{
    [SerializeField] private WeaponSystem weaponSystem;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private RectTransform reticle;
    [SerializeField] private GameObject ammoPanel;
    [SerializeField] private TMP_Text ammoText;

    private Canvas canvas;
    private GunSight sight;
    private Image reloadTrack;
    private RectTransform reloadFill;
    private TMP_Text ammoCaption;
    private int lastProgress = -2;
    private Player player;
    private WeaponData lastWeapon;
    private int lastAmmo = -1;
    private int lastCapacity = -1;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (ammoPanel != null)
            foreach (var text in ammoPanel.GetComponentsInChildren<TMP_Text>(true))
                if (text != ammoText) { ammoCaption = text; break; }
        if (canvas != null)
        {
            reloadTrack = new GameObject("Reload progress", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            reloadTrack.raycastTarget = false;
            reloadTrack.color = new Color(.06f, .04f, .035f, .85f);
            reloadTrack.rectTransform.SetParent(canvas.transform, false);
            reloadTrack.rectTransform.anchoredPosition = new Vector2(0, -48);
            reloadTrack.rectTransform.sizeDelta = new Vector2(108, 7);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.raycastTarget = false;
            fill.color = new Color(.98f, .72f, .40f);
            reloadFill = fill.rectTransform;
            reloadFill.SetParent(reloadTrack.rectTransform, false);
            reloadFill.anchorMin = Vector2.zero;
            reloadFill.offsetMin = reloadFill.offsetMax = Vector2.zero;
            reloadTrack.gameObject.SetActive(false);
            var host = new GameObject("Gun sight", typeof(RectTransform), typeof(GunSight));
            sight = host.GetComponent<GunSight>();
            sight.raycastTarget = false;
            var bounds = sight.rectTransform;
            bounds.SetParent(canvas.transform, false);
            bounds.SetAsFirstSibling();
            bounds.anchorMin = Vector2.zero;
            bounds.anchorMax = Vector2.one;
            bounds.offsetMin = bounds.offsetMax = Vector2.zero;
        }
        if (weaponSystem != null) player = weaponSystem.GetComponent<Player>();
    }

    private void OnEnable()
    {
        GameInput.BindingsChanged += InvalidateWeapon;
        InvalidateWeapon();
        lastAmmo = -1;
        lastCapacity = -1;
    }

    private void InvalidateWeapon() => lastWeapon = null;

    private void LateUpdate()
    {
        bool visible = weaponSystem != null && weaponSystem.isActiveAndEnabled &&
            gameplayCamera != null && gameplayCamera.isActiveAndEnabled &&
            (player == null || !player.IsDead);

        if (reticle != null)
        {
            reticle.gameObject.SetActive(visible);
            if (visible && canvas != null && reticle.parent is RectTransform parent)
            {
                Vector2 screenCenter = gameplayCamera.ViewportToScreenPoint(new Vector3(.5f, .5f, 0f));
                Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenCenter, uiCamera, out Vector2 point))
                {
                    reticle.position = parent.TransformPoint(point);
                }
            }
        }

        WeaponData weapon = weaponSystem != null ? weaponSystem.EquippedWeapon : null;
        if (sight != null)
            sight.Show(weapon != null ? weapon.sightStyle : GunSightStyle.Iron, visible && weapon != null ? weaponSystem.AimBlend : 0f);
        bool showAmmo = visible && weapon != null && (weapon.useType == WeaponUseType.Gun || weapon.useType == WeaponUseType.Bow);
        if (reloadTrack != null)
        {
            bool reloading = showAmmo && weaponSystem.IsReloading;
            reloadTrack.gameObject.SetActive(reloading);
            if (reloading) reloadFill.anchorMax = new Vector2(weaponSystem.ReloadProgress, 1f);
        }
        if (ammoPanel != null) ammoPanel.SetActive(showAmmo);
        if (!showAmmo || ammoText == null) return;

        int ammo = weaponSystem.LoadedAmmo;
        bool bow = weapon.useType == WeaponUseType.Bow;
        int capacity = bow ? weapon.maxAmmo : weaponSystem.Inventory.Selected.ReserveAmmo;
        int progress = weaponSystem.IsReloading ? Mathf.FloorToInt(weaponSystem.ReloadProgress * 100f) : -1;
        if (weapon != lastWeapon || ammo != lastAmmo || capacity != lastCapacity || progress != lastProgress)
        {
            ammoText.text = ammo + " / " + capacity;
            if (ammoCaption != null)
            {
                if (bow) ammoCaption.text = "ARROWS";
                else if (progress >= 0) ammoCaption.SetText("RELOADING {0}%", progress);
                else if (ammo == 0 && capacity > 0) ammoCaption.text = GameInput.GetBindingText(GameAction.Reload) + "  RELOAD";
                else ammoCaption.text = "LOADED / RESERVE";
            }
            lastProgress = progress;
            ammoText.color = ammo == 0 ? new Color(1f, .62f, .5f) : Color.white;
            lastWeapon = weapon;
            lastAmmo = ammo;
            lastCapacity = capacity;
        }
    }

    private void OnDisable()
    {
        GameInput.BindingsChanged -= InvalidateWeapon;
        if (reloadTrack != null) reloadTrack.gameObject.SetActive(false);
        if (sight != null) sight.Show(GunSightStyle.Iron, 0f);
        if (reticle != null) reticle.gameObject.SetActive(false);
        if (ammoPanel != null) ammoPanel.SetActive(false);
    }
}
