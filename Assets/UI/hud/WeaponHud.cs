using TMPro;
using UnityEngine;

/// <summary>Displays the equipped weapon's existing state without owning ammo or input.</summary>
public sealed class WeaponHud : MonoBehaviour
{
    [SerializeField] private WeaponSystem weaponSystem;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private RectTransform reticle;
    [SerializeField] private GameObject ammoPanel;
    [SerializeField] private TMP_Text ammoText;

    private Canvas canvas;
    private Player player;
    private WeaponData lastWeapon;
    private int lastAmmo = -1;
    private int lastCapacity = -1;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (weaponSystem != null) player = weaponSystem.GetComponent<Player>();
    }

    private void OnEnable()
    {
        lastWeapon = null;
        lastAmmo = -1;
        lastCapacity = -1;
    }

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
        bool showAmmo = visible && weapon != null && weapon.useType == WeaponUseType.Gun;
        if (ammoPanel != null) ammoPanel.SetActive(showAmmo);
        if (!showAmmo || ammoText == null) return;

        int ammo = weaponSystem.LoadedAmmo;
        int capacity = weapon.maxAmmo;
        if (weapon != lastWeapon || ammo != lastAmmo || capacity != lastCapacity)
        {
            ammoText.text = ammo.ToString() + " / " + capacity.ToString();
            ammoText.color = ammo == 0 ? new Color(1f, .62f, .5f) : Color.white;
            lastWeapon = weapon;
            lastAmmo = ammo;
            lastCapacity = capacity;
        }
    }

    private void OnDisable()
    {
        if (reticle != null) reticle.gameObject.SetActive(false);
        if (ammoPanel != null) ammoPanel.SetActive(false);
    }
}
