using UnityEngine;

// A single voice per living creature keeps its footsteps from layering over its calls.
[DisallowMultipleComponent]
public sealed class CreatureAudio : MonoBehaviour
{
    private static AudioListener listener;
    private static float nextNearbyCall;
    private static float nextHerdCall;
    private bool herdAnimal;
    private static float nextNearbyStep;
    private CreatureData data;
    private BossAI boss;
    private AudioSource source;
    private CharacterController controller;
    private Vector3 previousPosition;
    private float travel;
    private float nextCall;
    private float nextStep;
    private float nextRecoveryBreath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSceneAudio()
    {
        listener = null;
        nextNearbyCall = 0f;
        nextHerdCall = 0f;
        nextNearbyStep = 0f;
    }

    public void Initialize(CreatureData profile)
    {
        data = profile;
        herdAnimal = GetComponent<CreatureAI>() != null;
        boss = GetComponent<BossAI>();
        controller = GetComponent<CharacterController>();
        previousPosition = transform.position;
        source = CreateSource(gameObject);
        nextCall = Time.time + (herdAnimal ? Random.Range(8f, 32f) : Random.Range(2f, 12f));
    }

    private AudioSource CreateSource(GameObject owner)
    {
        AudioSource voice = owner.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 1f;
        voice.rolloffMode = AudioRolloffMode.Linear;
        voice.minDistance = 2f;
        voice.maxDistance = data.callDistance;
        voice.dopplerLevel = 0f;
        voice.outputAudioMixerGroup = data.soundGroup;
        SettingsManager.Route(voice, AudioChannel.SFX); // only if the creature data has no group
        return voice;
    }

    private void LateUpdate()
    {
        if (source == null) return;
        Vector3 delta = transform.position - previousPosition;
        previousPosition = transform.position;
        delta.y = 0f;
        if (listener == null || !listener.isActiveAndEnabled)
            listener = FindFirstObjectByType<AudioListener>();
        if (listener == null) return;

        float distanceSquared = (transform.position - listener.transform.position).sqrMagnitude;
        if (distanceSquared > data.callDistance * data.callDistance) return;
        if (Time.time >= nextCall && Time.time >= (herdAnimal ? nextHerdCall : nextNearbyCall) && !source.isPlaying &&
            (boss == null || !boss.IsEngaged))
        {
            Play(source, data.call, .65f, data.callDistance, 140);
            if (herdAnimal)
            {
                nextCall = Time.time + Random.Range(34f, 68f);
                nextHerdCall = Time.time + Random.Range(18f, 31f);
            }
            else
            {
                nextCall = Time.time + Random.Range(13f, 27f);
                nextNearbyCall = Time.time + Random.Range(3.6f, 6f);
            }
            return;
        }

        if (controller == null || !controller.isGrounded || delta.sqrMagnitude > 4f)
        {
            travel = 0f;
            return;
        }

        travel += delta.magnitude;
        if (travel < data.stride || Time.time < nextStep) return;
        travel = 0f;
        nextStep = Time.time + .25f;
        if (!source.isPlaying && Time.time >= nextNearbyStep && distanceSquared < data.stepDistance * data.stepDistance)
        {
            Play(source, data.step, .38f, data.stepDistance, 190);
            nextNearbyStep = Time.time + .14f;
        }
    }

    public void Signal(AudioClip clip)
    {
        if (clip == null) return;
        nextCall = Time.time + 15f;
        float range = data.callDistance;
        // The island overrides the boss detection radius. Its first warning must carry that far.
        if (boss != null && clip == data.warning)
            range = Mathf.Max(range, boss.detectionRange * 1.5f);
        Play(source, clip, .86f, range, 70);
    }

    public void FinishAttack()
    {
        if (source.isPlaying || Time.time < nextRecoveryBreath) return;
        Play(source, data.call, .48f, data.stepDistance, 130);
        nextRecoveryBreath = Time.time + Random.Range(8f, 14f);
    }

    public void Die()
    {
        if (data.death == null) return;
        source.Stop();
        var tail = new GameObject(data.creatureName + " death sound");
        tail.transform.position = transform.position;
        var voice = CreateSource(tail);
        Play(voice, data.death, .8f, data.callDistance, 80);
        Destroy(tail, data.death.length / voice.pitch + .1f);
    }

    private static void Play(AudioSource voice, AudioClip clip, float level, float range, int priority)
    {
        if (clip == null) return;
        voice.clip = clip;
        voice.maxDistance = range;
        voice.priority = priority;
        voice.pitch = Random.Range(.90f, 1.10f);
        voice.volume = level * Random.Range(.86f, 1f);
        voice.Play();
    }
}
