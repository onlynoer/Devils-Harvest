using UnityEngine;

public sealed class AltarOffering : MonoBehaviour, IInteractable
{
    public const float HoldSeconds = .8f;
    private WeaponSystem hand;
    private float elapsed, glowTime;
    private Light ember;
    public float Progress => Mathf.Clamp01(elapsed / HoldSeconds);

    public string GetPrompt(WeaponSystem user) => user.GetComponent<HarvestTrade>()?.AltarPrompt;
    public bool CanInteract(WeaponSystem user) => user.TryGetComponent(out HarvestTrade trade) && trade.OfferAmount > 0;

    public void Interact(WeaponSystem user)
    {
        if (hand == null && CanInteract(user)) hand = user;
    }

    private void OnDisable()
    {
        hand = null;
        elapsed = glowTime = 0;
        if (ember != null) ember.enabled = false;
    }

    private void LateUpdate()
    {
        if (hand != null)
        {
            if (!GameInput.GameplayEnabled || !hand.isActiveAndEnabled || hand.CurrentInteractable != (IInteractable)this ||
                !GameInput.IsHeld(GameAction.Interact) || !CanInteract(hand))
            {
                hand = null;
                elapsed = 0;
            }
            else if ((elapsed += Time.deltaTime) >= HoldSeconds)
            {
                if (hand.GetComponent<HarvestTrade>().OfferMeat() > 0) glowTime = .75f;
                hand = null;
                elapsed = 0;
            }
        }
        if (glowTime > 0 && ember == null)
        {
            var flame = new GameObject("Offering ember");
            flame.transform.SetParent(transform,false);
            var slab = GetComponentInChildren<Collider>();
            Vector3 surface = slab != null ? slab.bounds.center + Vector3.up * slab.bounds.extents.y : transform.position;
            flame.transform.position = surface + Vector3.up * .35f;
            ember = flame.AddComponent<Light>();
            ember.color = new Color(1f,.25f,.065f);
            ember.range = 9f;
        }
        if (ember != null)
        {
            ember.enabled = glowTime > 0;
            ember.intensity = 9f*glowTime/.75f;
            glowTime = Mathf.Max(0,glowTime-Time.deltaTime);
        }
    }
}
