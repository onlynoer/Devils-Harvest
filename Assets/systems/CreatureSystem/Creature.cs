using System;
using System.Collections;
using UnityEngine;

public class Creature : MonoBehaviour, IDamageable
{
    public CreatureData creatureData;

    [Tooltip("Testing: always apply this secret (index into Creature Data > Secrets). -1 = roll normally.")]
    public int forceSecretIndex = -1;

    public CreatureAudio Sounds { get; private set; }
    /// <summary>The secret rolled at spawn, or null.</summary>
    public CreatureSecret ActiveSecret { get; private set; }
    public float CurrentHealth => currentHealth;
    public float MaxHealth { get; private set; }
    public float HealthFraction => MaxHealth > 0f ? currentHealth / MaxHealth : 0f;
    public bool IsAlive => creatureData != null && currentHealth > 0f;
    /// <summary>Species temperament, or the secret's when one is active.</summary>
    public CreatureTemperament Temperament =>
        ActiveSecret != null ? ActiveSecret.temperament : creatureData != null ? creatureData.temperament : CreatureTemperament.Passive;

    /// <summary>Raised after health is reduced (also on the killing blow, before Died).</summary>
    public event Action<Creature, DamageInfo> Damaged;
    public event Action<Creature> Died;

    private float currentHealth;

    private Renderer[] renderers;
    private Color[] originalColors;

    private Coroutine flashCoroutine;

    void Awake()
    {
        if (creatureData == null)
        {
            Debug.LogWarning("Creature Data is missing on " + name + "!", this);
            return;
        }

        RollSecret();
        MaxHealth = creatureData.maxHealth * (ActiveSecret != null ? ActiveSecret.healthMultiplier : 1f);
        currentHealth = MaxHealth;
    }

    void Start()
    {
        if (creatureData == null) return;

        if (creatureData.call != null)
        {
            Sounds = gameObject.AddComponent<CreatureAudio>();
            Sounds.Initialize(creatureData);
        }

        renderers = GetComponentsInChildren<Renderer>();

        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }

        Debug.Log(
            creatureData.creatureName +
            (ActiveSecret != null ? " [" + ActiveSecret.label + "]" : "") +
            " spawned with " +
            currentHealth +
            " health."
        );
    }

    private void RollSecret()
    {
        CreatureSecret[] secrets = creatureData.secrets;
        if (secrets == null || secrets.Length == 0) return;

        if (forceSecretIndex >= 0 && forceSecretIndex < secrets.Length)
            ActiveSecret = secrets[forceSecretIndex];
        else
            foreach (CreatureSecret secret in secrets)
                if (secret != null && UnityEngine.Random.value < secret.chance) { ActiveSecret = secret; break; }

        if (ActiveSecret != null && ActiveSecret.weapon != null)
            gameObject.AddComponent<ArmedCreature>().Setup(ActiveSecret);
    }

    public void ApplyDamage(in DamageInfo info) => TakeDamage(info.Amount, info.Direction, info.Source);

    public void TakeDamage(float damage, Vector3 damageDirection) => TakeDamage(damage, damageDirection, null);

    /// <param name="source">Who dealt the damage. Null is treated as the player by animal AI.</param>
    public void TakeDamage(float damage, Vector3 damageDirection, GameObject source)
    {
        if (creatureData == null || currentHealth <= 0)
            return;

        float actualDamage = Mathf.Max(
            damage - creatureData.defense,
            0f
        );

        currentHealth -= actualDamage;
        MonsterAI monsterAI = GetComponent<MonsterAI>();
        if (monsterAI != null)
        {
            monsterAI.Provoke();
        }
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log(
            creatureData.creatureName +
            " took " +
            actualDamage +
            " damage. Health: " +
            currentHealth +
            "/" +
            MaxHealth
        );

        if (renderers != null)
        {
            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);

            flashCoroutine = StartCoroutine(DamageFlash());
        }

        // CreatureAI listens here and decides whether to flee or fight back.
        Damaged?.Invoke(this, new DamageInfo(actualDamage, damageDirection, source));

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (actualDamage > 0f && Sounds != null)
        {
            Sounds.Signal(creatureData.hurt);
        }
    }

    IEnumerator DamageFlash()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].material.color = creatureData.damageFlashColor;
        }

        yield return new WaitForSeconds(
            creatureData.damageFlashDuration
        );

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].material.color = originalColors[i];
        }

        flashCoroutine = null;
    }

    void Die()
    {
        Debug.Log(creatureData.creatureName + " died!");

        Died?.Invoke(this);
        if (Sounds != null) Sounds.Die();
        if (TryGetComponent(out ArmedCreature armed)) armed.DropWeapon();
        if (creatureData.meatDrop != null && creatureData.lootAmount > 0)
        {
            Vector3 position = transform.position;
            var terrain = Terrain.activeTerrain;
            if (terrain != null) position.y = terrain.transform.position.y + terrain.SampleHeight(position);
            var drop = Instantiate(creatureData.meatDrop, position, transform.rotation);
            drop.SetYield(creatureData.lootAmount);
        }
        Destroy(gameObject);
    }
}
