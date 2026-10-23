using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerData playerData;

    private float currentHealth;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => playerData != null ? playerData.maxHealth : 0f;

    void Start()
    {
        if (playerData == null)
        {
            Debug.LogWarning("Player Data is missing!");
            return;
        }

        currentHealth = playerData.maxHealth;

        Debug.Log(
            "Player spawned with " +
            currentHealth +
            "/" +
            playerData.maxHealth +
            " health."
        );
    }

    public void TakeDamage(float damage)
    {
        if (playerData == null)
        {
            return;
        }

        float actualDamage = Mathf.Max(
            damage - playerData.defense,
            0f
        );

        currentHealth = Mathf.Max(currentHealth - actualDamage, 0f);

        Debug.Log(
            "Player took " +
            actualDamage +
            " damage. Health: " +
            currentHealth +
            "/" +
            playerData.maxHealth
        );

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player died!");
    }
}
