using UnityEngine;

public class Creature : MonoBehaviour
{
    public CreatureData creatureData;

    private float currentHealth;

    private Renderer[] renderers;
    private Color[] originalColors;

    void Start()
    {
        if (creatureData == null)
        {
            Debug.LogWarning("Creature Data is missing!");
            return;
        }

        currentHealth = creatureData.maxHealth;

        // Find all renderers on the deer and its children.
        renderers = GetComponentsInChildren<Renderer>();

        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }

        Debug.Log(
            creatureData.creatureName +
            " spawned with " +
            currentHealth +
            " health."
        );
    }

    public void TakeDamage(float damage, Vector3 damageDirection)
    {
        if (creatureData == null)
        {
            return;
        }

        float actualDamage = Mathf.Max(
            damage - creatureData.defense,
            0f
        );

        currentHealth -= actualDamage;
        CreatureAI ai = GetComponent<CreatureAI>();
        if (ai != null)
        {
            ai.Flee(transform.position - damageDirection);
        }
        Debug.Log(
            creatureData.creatureName +
            " took " +
            actualDamage +
            " damage. Health: " +
            currentHealth +
            "/" +
            creatureData.maxHealth
        );

        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator DamageFlash()
    {
        // Turn red
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = Color.Lerp(
                originalColors[i],
                Color.red,
                0.5f
            );
        }

        yield return new WaitForSeconds(0.15f);

        // Return to normal
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i];
        }
    }

    void Die()
    {
        Debug.Log(creatureData.creatureName + " died!");

        Destroy(gameObject);
    }
}