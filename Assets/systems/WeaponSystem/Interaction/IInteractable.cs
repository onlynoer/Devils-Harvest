/// <summary>
/// Anything the player can look at and press E on (weapon pickups, ammo boxes...).
/// Put a component implementing this on an object with a Collider. WeaponSystem finds it with a look raycast,
/// and the InteractPromptHud shows its prompt on the canvas.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Action text for the prompt, e.g. "pick up AK47". When <see cref="CanInteract"/> is false, this is shown
    /// as-is instead, so return the reason, e.g. "Inventory full". Return null for no prompt.
    /// Called every frame while looked at, so cache the strings instead of building new ones each call.
    /// </summary>
    string GetPrompt(WeaponSystem user);

    bool CanInteract(WeaponSystem user);

    void Interact(WeaponSystem user);
}
