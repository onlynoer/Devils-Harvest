using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    public Player player;
    public Image healthBarFill;

    void Update()
    {
        if (player == null || healthBarFill == null)
        {
            return;
        }

        healthBarFill.fillAmount =
            player.MaxHealth > 0
            ? player.CurrentHealth / player.MaxHealth
            : 0f;
    }
}