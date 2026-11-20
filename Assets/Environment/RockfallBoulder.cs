using UnityEngine;

namespace DevilsHarvest
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class RockfallBoulder : MonoBehaviour
    {
        private RockfallHazard owner;
        private bool reachedImpactArea;
        private bool spent;

        public void Launch(RockfallHazard hazard, Vector3 velocity, float lifetime)
        {
            owner = hazard;
            GetComponent<Rigidbody>().linearVelocity = velocity;
            Destroy(gameObject, lifetime);
        }

        private void FixedUpdate()
        {
            if (owner == null) return;
            if (owner.ContainsImpact(transform.position))
                reachedImpactArea = true;
            else if (reachedImpactArea)
                Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryImpact(collision.collider.GetComponentInParent<Player>());
        }

        public void TryImpact(Player player)
        {
            if (spent || owner == null || player == null || player.IsDead) return;
            spent = true;
            player.KillAndRecover();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (owner != null) owner.Release(this);
        }
    }
}
