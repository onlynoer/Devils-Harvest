using UnityEngine;
using System.Collections;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class Creature : MonoBehaviour
{
    public CreatureData creatureData;
    public AnimationClip deathClip;
    private PlayableGraph deathGraph;

    public CreatureAudio Sounds { get; private set; }

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
        MonsterAI monsterAI = GetComponent<MonsterAI>();
        if (monsterAI != null)
        {
            monsterAI.Provoke();
        }
        if (currentHealth > 0f && actualDamage > 0f && TryGetComponent(out BossAI boss)) boss.AlertToHit();
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
        else if (actualDamage > 0f && Sounds != null)
        {
            Sounds.Signal(creatureData.hurt);
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

        if (Sounds != null) Sounds.Die();
        if (creatureData.meatDrop != null && creatureData.lootAmount > 0)
        {
            Vector3 position = transform.position;
            var terrain = Terrain.activeTerrain;
            if (terrain != null) position.y = terrain.transform.position.y + terrain.SampleHeight(position);
            var drop = Instantiate(creatureData.meatDrop, position, transform.rotation);
            drop.SetYield(creatureData.lootAmount);
        }
        foreach (var behaviour in GetComponentsInChildren<MonoBehaviour>())
        {
            if (behaviour != this) behaviour.enabled = false;
        }
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        StartCoroutine(Fall());
    }

    private IEnumerator Fall()
    {
        var skin = GetComponentInChildren<SkinnedMeshRenderer>();
        var animator = skin != null && skin.rootBone != null ? skin.rootBone.GetComponentInParent<Animator>() : null;
        // Imported island monsters have an outer gameplay Animator and an inner model rig.
        // The death clip paths start at the model, so give only that Animator the pose.
        foreach (var other in GetComponentsInChildren<Animator>())
            if (other != animator || deathClip == null) other.enabled = false;
        float duration = .8f;
        if (animator != null && deathClip != null)
        {
            animator.applyRootMotion = false;
            deathGraph = PlayableGraph.Create("Creature death");
            deathGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var clip = AnimationClipPlayable.Create(deathGraph, deathClip);
            var output = AnimationPlayableOutput.Create(deathGraph, "Death", animator);
            output.SetSourcePlayable(clip);
            duration = Mathf.Clamp(deathClip.length, .52f, 1.15f);
            clip.SetSpeed(deathClip.length / duration);
            deathGraph.Play();
        }
        // Tip around the grounded root immediately, including clips with a long upright lead-in.
        var upright = transform.rotation;
        var fallen = upright * Quaternion.Euler(4f, 0f, 82f);
        for (float t = 0f; t < .45f; t += Time.deltaTime)
        {
            transform.rotation = Quaternion.Slerp(upright, fallen, Mathf.SmoothStep(0f, 1f, t / .45f));
            yield return null;
        }
        transform.rotation = fallen;
        yield return new WaitForSeconds(duration - .45f);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (deathGraph.IsValid()) deathGraph.Destroy();
    }
}