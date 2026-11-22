using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerData playerData;

    private float currentHealth;
    private bool isDead;
    private Pose arrival;
    private bool resumeWeapons, resumeLook;
    private FirstPersonLook look;
    private float deathTime;
    private Quaternion viewAtDeath;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => playerData != null ? playerData.maxHealth : 0f;
    public bool IsDead => isDead;

    void Start()
    {
        look = GetComponentInChildren<FirstPersonLook>();
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
            KillAndRecover();
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
        const float duration = 1.25f;
        while (Time.time - deathTime < duration)
        {
            if (look != null)
            {
                float fall = Mathf.SmoothStep(0f, 1f, (Time.time - deathTime) / .65f);
                look.transform.localRotation = viewAtDeath * Quaternion.Euler(15f * fall, 0f, 32f * fall);
            }
            yield return null;
        }
        if (look != null)
        {
            look.transform.localRotation = viewAtDeath;
            look.enabled = resumeLook;
        }

        var controller = GetComponent<CharacterController>();
        var movement = GetComponent<PlayerMovement>();
        Pose destination = checkpoint != null
            ? new Pose(checkpoint.position, checkpoint.rotation)
            : arrival;
        transform.SetPositionAndRotation(destination.position, destination.rotation);
        currentHealth = MaxHealth;
        isDead = false;

        if (controller != null) controller.enabled = true;
        if (resumeWeapons && TryGetComponent(out WeaponSystem weapons)) weapons.enabled = true;
        if (movement != null)
        {
            movement.ResetMotion();
            movement.enabled = true;
        }
    }

    private void OnGUI()
    {
        if (!isDead) return;
        float fade = Mathf.SmoothStep(0f, .95f, (Time.time - deathTime) / .9f);
        var previous = GUI.color;
        GUI.depth = -50;
        GUI.color = new Color(.12f, .018f, .025f, fade);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = new Color(1f, .93f, .84f, fade);
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(18, Screen.height / 25) };
        GUI.Label(new Rect(0f, Screen.height * .42f, Screen.width, Screen.height * .15f), "Recovering", style);
        GUI.color = previous;
    }

    void Die()
    {
        if (isDead)
        {
            return;
        }

        if (TryGetComponent(out HarvestTrade trade) && trade.IsOpen) trade.CloseForRespawn();
        isDead = true;
        deathTime = Time.time;
        if (TryGetComponent(out PlayerPowerups powerups)) powerups.ClearAll();
        if (TryGetComponent(out WeaponSystem weapons))
        {
            resumeWeapons = weapons.enabled;
            weapons.enabled = false;
        }
        if (look != null)
        {
            resumeLook = look.enabled;
            viewAtDeath = look.transform.localRotation;
            look.enabled = false;
        }
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
