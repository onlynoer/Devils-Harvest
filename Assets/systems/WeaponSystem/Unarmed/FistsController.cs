using UnityEngine;

/// <summary>
/// First-person fists, used while the selected hotbar slot holds no weapon. Attacking punches, alternating
/// right and left. The two fist pivots are empty objects under the camera; where they sit in the scene is the
/// resting pose. This script animates them and deals the damage; FirstPersonArmsRig (visible arms) and
/// FirstPersonArmsIK (shadow) make the hands follow them. WeaponSystem calls <see cref="TryPunch"/> on attack.
/// </summary>
public sealed class FistsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponSystem weaponSystem;
    [Tooltip("Empty object marking where the left hand rests; the punch animates it.")]
    [SerializeField] private Transform leftFist;
    [SerializeField] private Transform rightFist;

    [Header("Damage")]
    [Min(0f)] [SerializeField] private float damage = 5f;
    [Tooltip("How far in front of the camera a punch reaches, in meters.")]
    [Min(0f)] [SerializeField] private float range = 1.6f;
    [Tooltip("Width of the punch hit area. Bigger = more forgiving aim.")]
    [Min(0f)] [SerializeField] private float radius = .35f;

    [Header("Sounds (play through the SFX volume slider)")]
    [Tooltip("Each punch thrown (a whoosh).")]
    [SerializeField] private SoundEffect punchSound = new SoundEffect();
    [Tooltip("When a punch hits a creature.")]
    [SerializeField] private SoundEffect punchHitSound = new SoundEffect();

    [Header("Timing")]
    [Tooltip("Seconds between punches. Shorter than Punch Duration lets the next fist start while the last returns.")]
    [Min(.05f)] [SerializeField] private float punchInterval = .3f;
    [Tooltip("Length of one punch animation (out and back), in seconds.")]
    [Min(.05f)] [SerializeField] private float punchDuration = .35f;
    [Tooltip("When the hit lands, as a fraction of the punch (0 = start, 1 = end).")]
    [Range(0f, 1f)] [SerializeField] private float hitTime = .3f;

    [Header("Animation")]
    [Tooltip("How far the fist travels forward, in meters.")]
    [SerializeField] private float reach = .35f;
    [Tooltip("How far the fist moves toward the center of the screen, in meters.")]
    [SerializeField] private float inward = .14f;
    [Tooltip("How far the fist rises while punching, in meters.")]
    [SerializeField] private float lift = .05f;
    [Tooltip("Wrist twist at full extension, in degrees.")]
    [SerializeField] private float twist = 25f;

    // Phase boundary: fist goes out until this point, then comes back.
    private const float ExtendEnd = .35f;

    private sealed class Fist
    {
        public Transform transform;
        public float side; // -1 left, +1 right
        public Vector3 restPosition;
        public Quaternion restRotation;
        public float startTime = -1f;
        public bool hitDone;
        public bool Punching => startTime >= 0f;
    }

    private Fist left;
    private Fist right;
    private bool rightIsNext = true;
    private float nextPunchTime;

    /// <summary>True while the fists are out (no weapon selected).</summary>
    public bool FistsVisible { get; private set; }
    public Transform LeftFist => leftFist;
    public Transform RightFist => rightFist;

    private void Awake()
    {
        left = new Fist { transform = leftFist, side = -1f };
        right = new Fist { transform = rightFist, side = 1f };
    }

    private void Update()
    {
        bool show = weaponSystem == null || weaponSystem.EquippedWeapon == null;
        FistsVisible = show;
        SetVisible(left, show);
        SetVisible(right, show);
        if (!show) return;
        Animate(left);
        Animate(right);
    }

    private void OnDisable()
    {
        Stop(left);
        Stop(right);
    }

    /// <summary>Throws the next punch if the fists are showing and the interval has passed.</summary>
    public bool TryPunch()
    {
        if (!isActiveAndEnabled || Time.time < nextPunchTime) return false;
        Fist fist = rightIsNext ? right : left;
        if (fist.transform == null || !fist.transform.gameObject.activeInHierarchy) fist = rightIsNext ? left : right;
        if (fist.transform == null || !fist.transform.gameObject.activeInHierarchy) return false;

        Stop(fist); // restart cleanly if this fist is still coming back
        fist.restPosition = fist.transform.localPosition;
        fist.restRotation = fist.transform.localRotation;
        fist.startTime = Time.time;
        fist.hitDone = false;

        rightIsNext = fist == left;
        nextPunchTime = Time.time + punchInterval;
        if (weaponSystem != null) weaponSystem.Audio.Play(punchSound);
        return true;
    }

    private void Animate(Fist fist)
    {
        if (fist.transform == null || !fist.Punching) return;

        float t = (Time.time - fist.startTime) / punchDuration;
        if (!fist.hitDone && t >= hitTime)
        {
            fist.hitDone = true;
            if (weaponSystem != null) weaponSystem.DealMeleeDamage(range, radius, damage, punchHitSound);
        }
        if (t >= 1f)
        {
            Stop(fist);
            return;
        }

        float k = t < ExtendEnd
            ? 1f - Mathf.Pow(1f - t / ExtendEnd, 3f)           // snap out
            : 1f - Smooth((t - ExtendEnd) / (1f - ExtendEnd)); // ease back
        var offset = new Vector3(-fist.side * inward, lift, reach) * k;
        fist.transform.localPosition = fist.restPosition + offset;
        fist.transform.localRotation = fist.restRotation * Quaternion.Euler(0f, 0f, -fist.side * twist * k);
    }

    private static void Stop(Fist fist)
    {
        if (fist == null || !fist.Punching) return;
        if (fist.transform != null)
        {
            fist.transform.localPosition = fist.restPosition;
            fist.transform.localRotation = fist.restRotation;
        }
        fist.startTime = -1f;
    }

    private static void SetVisible(Fist fist, bool visible)
    {
        if (fist.transform == null || fist.transform.gameObject.activeSelf == visible) return;
        if (!visible) Stop(fist);
        fist.transform.gameObject.SetActive(visible);
    }

    private static float Smooth(float x) => x * x * (3f - 2f * x);
}
