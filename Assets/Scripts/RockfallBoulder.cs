using UnityEngine;

namespace DevilsHarvest
{
    /// <summary>Implement on a character health adapter; return true when the hit has been handled.</summary>
    public interface IRockfallHitReceiver
    {
        bool ReceiveRockfallHit(Vector3 point, Vector3 velocity);
    }

    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class RockfallBoulder : MonoBehaviour
    {
        private RockfallHazard owner;
        private Rigidbody body;
        private bool spent;
        private Vector3 incomingVelocity;
        public void Launch(RockfallHazard hazard, Vector3 velocity, float lifetime)
        {
            owner = hazard; body = GetComponent<Rigidbody>();
            body.linearVelocity = velocity;
            incomingVelocity = velocity;
            Destroy(gameObject, lifetime);
        }
        private void OnCollisionEnter(Collision collision)
        {
            TryImpact(collision.collider, collision.contactCount > 0 ? collision.GetContact(0).point : transform.position);
        }
        private void FixedUpdate()
        {
            if (body != null) incomingVelocity = body.linearVelocity;
            if (owner != null && !owner.ContainsImpact(transform.position)) Destroy(gameObject);
        }
        public void TryImpact(Collider other, Vector3 point)
        {
            if (spent) return;
            // Teammates can consume the hit without adding a health system to this map prototype.
            foreach (var behaviour in other.GetComponentsInParent<MonoBehaviour>())
            {
                if (!(behaviour is IRockfallHitReceiver receiver)) continue;
                spent = true;
                if (receiver.ReceiveRockfallHit(point, incomingVelocity)) { Destroy(gameObject); return; }
                spent = false;
            }
            var recovery = other.GetComponentInParent<MountainCheckpointRecovery>();
            if (recovery == null) return;
            spent = true;
            recovery.RequestReset();
            Destroy(gameObject);
        }
        private void OnDestroy() { if (owner != null) owner.Release(this); }
    }
}
