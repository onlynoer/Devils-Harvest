using UnityEngine;
using System.Collections;

public class Creature : MonoBehaviour
{
    public CreatureData creatureData;

    private float currentHealth;

    private Renderer[] renderers;
    private Color[] originalColors;

    private Coroutine flashCoroutine;

    void Start()
    {
        if (creatureData == null)
        {
            Debug.LogWarning("Creature Data is missing!");
            return;
        }

        currentHealth = creatureData.maxHealth;

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
        if (creatureData == null || currentHealth <= 0)
            return;

        float actualDamage = Mathf.Max(
            damage - creatureData.defense,
            0f
        );

        currentHealth -= actualDamage;
        currentHealth = Mathf.Max(currentHealth, 0f);

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

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator DamageFlash()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color =
                creatureData.damageFlashColor;
        }

        yield return new WaitForSeconds(
            creatureData.damageFlashDuration
        );

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i];
        }

        flashCoroutine = null;
    }

    void Die()
    {
        Debug.Log(creatureData.creatureName + " died!");

        Destroy(gameObject);
    }
}