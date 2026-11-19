using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevilsHarvest
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RockfallHazard : MonoBehaviour
    {
        public RockfallBoulder rockPrefab;
        public Transform releasePoint;
        public Transform impactCenter;
        public AudioSource warningSound;
        public ParticleSystem warningDust;
        public ParticleSystem warningPebbles;
        [Min(2)] public float warningDuration = 3.5f;
        [Min(5)] public float cooldown = 18;
        [Range(1, 12)] public int rockCount = 5;
        [Range(1, 12)] public int maxActiveRocks = 6;
        [Range(0, 12)] public float rockSpeed = 2;
        [Range(2, 12)] public float rockLifetime = 7;
        [Range(.15f, 1f)] public float releaseInterval = .4f;
        public Vector2 releaseSpread = new Vector2(3, 3);
        [Range(6, 12)] public float impactRadius = 10;
        public bool aimAtTrail;
        public bool allowTestKey = true;

        private readonly HashSet<Collider> occupants = new HashSet<Collider>();
        private readonly HashSet<RockfallBoulder> active = new HashSet<RockfallBoulder>();
        private bool busy;
        private float nextAllowed;
        private System.Random random;
        private BoxCollider approach;

        public int ActiveRockCount => active.Count;
        public bool IsWarningOrFalling => busy;
        public float CooldownRemaining => Mathf.Max(0, nextAllowed - Time.time);

        private void Awake()
        {
            approach = GetComponent<BoxCollider>();
            random = new System.Random(328);
            ClampSettings();
        }
        private void OnValidate() => ClampSettings();

        private void ClampSettings()
        {
            warningDuration = Mathf.Max(2, warningDuration);
            cooldown = Mathf.Max(5, cooldown);
            rockCount = Mathf.Clamp(rockCount, 1, 12);
            maxActiveRocks = Mathf.Clamp(maxActiveRocks, 1, 12);
            rockLifetime = Mathf.Clamp(rockLifetime, 2, 12);
            releaseInterval = Mathf.Clamp(releaseInterval, .15f, 1);
            rockSpeed = Mathf.Clamp(rockSpeed, 0, 12);
            impactRadius = Mathf.Clamp(impactRadius, 6, 12);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!(other is CharacterController)) return;
            var player = other.GetComponentInParent<Player>();
            if (player == null || player.IsDead) return;
            if (occupants.Add(other)) TryTrigger();
        }
        private void OnTriggerExit(Collider other) => occupants.Remove(other);

        private void Update()
        {
            Bounds bounds = approach.bounds;
            occupants.RemoveWhere(collider => collider == null || !collider.enabled ||
                !collider.gameObject.activeInHierarchy || !bounds.Intersects(collider.bounds));
            if (allowTestKey && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
                TryTrigger();
        }

        [ContextMenu("Test Rockfall (Play Mode, stand in approach zone)")]
        private void Test() => TryTrigger();
        public bool TryTrigger()
        {
            if (!Application.isPlaying || busy || Time.time < nextAllowed || occupants.Count == 0 ||
                rockPrefab == null || releasePoint == null || impactCenter == null)
                return false;

            StartCoroutine(Burst());
            return true;
        }

        private IEnumerator Burst()
        {
            busy = true;
            if (warningSound != null) warningSound.Play();
            if (warningDust != null) warningDust.Play();
            if (warningPebbles != null) warningPebbles.Play();
            yield return new WaitForSeconds(warningDuration);
            if (warningPebbles != null) warningPebbles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            for (int i = 0; i < rockCount; i++)
            {
                active.RemoveWhere(r => r == null);
                if (active.Count < maxActiveRocks)
                {
                    Vector3 spread = new Vector3(
                        ((float)random.NextDouble() * 2 - 1) * releaseSpread.x,
                        0,
                        ((float)random.NextDouble() * 2 - 1) * releaseSpread.y);
                    var rock = Instantiate(rockPrefab, releasePoint.position + spread,
                        Quaternion.Euler(i * 31, i * 53, i * 17));
                    rock.name = "Rockfall - Temporary Boulder " + (i + 1);
                    active.Add(rock);

                    Vector3 target = impactCenter.position + spread * .6f;
                    Vector3 velocity = aimAtTrail
                        ? TrailVelocity(rock.transform.position, target + Vector3.up * .65f, Physics.gravity, rockSpeed)
                        : (target - rock.transform.position).normalized * rockSpeed;
                    rock.Launch(this, velocity, rockLifetime);
                }
                yield return new WaitForSeconds(releaseInterval);
            }
            if (warningDust != null) warningDust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            busy = false;
            nextAllowed = Time.time + cooldown;
        }

        internal void Release(RockfallBoulder rock) => active.Remove(rock);

        internal bool ContainsImpact(Vector3 position)
        {
            if (impactCenter == null) return false;
            Vector3 offset = position - impactCenter.position;
            return new Vector2(offset.x, offset.z).sqrMagnitude <= impactRadius * impactRadius;
        }

        private static Vector3 TrailVelocity(Vector3 origin, Vector3 target, Vector3 gravity, float speed)
        {
            Vector3 displacement = target - origin;
            float distance = new Vector2(displacement.x, displacement.z).magnitude;
            float time = Mathf.Clamp(distance / Mathf.Max(2, speed), .65f, 1.4f);
            return displacement / time - gravity * (time * .5f);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (warningSound != null) warningSound.Stop();
            busy = false;
            foreach (var rock in active)
                if (rock != null) Destroy(rock.gameObject);
            active.Clear();
            occupants.Clear();
            if (warningDust != null) warningDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (warningPebbles != null) warningPebbles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
