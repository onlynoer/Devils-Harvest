using UnityEngine;

namespace DevilsHarvest
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SpikePitHazard : MonoBehaviour
    {
        [SerializeField] private Transform safeCheckpoint;

        private void OnTriggerEnter(Collider other)
        {
            if (!(other is CharacterController)) return;
            var player = other.GetComponentInParent<Player>();
            if (player != null && !player.IsDead)
                player.KillAndRecover(safeCheckpoint);
        }
    }
}
