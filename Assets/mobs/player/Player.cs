using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerData playerData;

    private float currentHealth;
    private bool isDead;
    private Pose arrival;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => playerData != null ? playerData.maxHealth : 0f;
    public bool IsDead => isDead;

    void Start()
    {
        arrival = new Pose(transform.position, transform.rotation);
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
        if (isDead || playerData == null)
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

    public void Kill()
    {
        if (isDead)
        {
            return;
        }

        currentHealth = 0f;
        Die();
    }

    public void KillAndRecover(Transform checkpoint = null)
    {
        if (isDead || playerData == null) return;
        Kill();
        StartCoroutine(Recover(checkpoint));
    }

    private IEnumerator Recover(Transform checkpoint)
    {
        yield return new WaitForSeconds(1f);

        var controller = GetComponent<CharacterController>();
        var movement = GetComponent<PlayerMovement>();
        Pose destination = checkpoint != null
            ? new Pose(checkpoint.position, checkpoint.rotation)
            : arrival;
        transform.SetPositionAndRotation(destination.position, destination.rotation);
        currentHealth = MaxHealth;
        isDead = false;

        if (controller != null) controller.enabled = true;
        if (movement != null)
        {
            movement.ResetMotion();
            movement.enabled = true;
        }
    }

    void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        var movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        var controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        Debug.Log("Player died!");
    }
}
