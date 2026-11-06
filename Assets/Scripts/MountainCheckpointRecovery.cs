using UnityEngine;

namespace DevilsHarvest
{
    /// <summary>Adapter for the temporary map player. Leaves its movement and normal beach respawn unchanged.</summary>
    [RequireComponent(typeof(WildernessTestPlayer))]
    public sealed class MountainCheckpointRecovery : MonoBehaviour
    {
        public Transform checkpoint;
        [Min(1)] public float recoveryGraceSeconds = 3;
        private WildernessTestPlayer player;
        private float immuneUntil;
        private bool pending;
        private Transform pendingCheckpoint;
        public bool IsRecovering => Time.time < immuneUntil;
        private void Awake() => player = GetComponent<WildernessTestPlayer>();
        public void RequestReset()
            => RequestReset(null);
        public void RequestReset(Transform overrideCheckpoint)
        {
            if (IsRecovering || pending) return;
            pendingCheckpoint=overrideCheckpoint;
            pending = true;
        }
        private void LateUpdate()
        {
            if (!pending) return;
            pending = false;
            Transform normal=checkpoint;
            try
            {
                if(pendingCheckpoint!=null)checkpoint=pendingCheckpoint;
                ResetToMountain(); // Outside CharacterController.Move / physics callbacks.
            }
            finally{checkpoint=normal;pendingCheckpoint=null;}
        }
        private void OnDisable() {pending=false;pendingCheckpoint=null;}

        public bool ResetToMountain()
        {
            if (IsRecovering || player == null || !player.enabled) return false;
            Transform original = player.spawn;
            bool safe = CheckpointIsClear();
            if (!safe) Debug.LogWarning("Mountain checkpoint obstructed or missing; using the original beach spawn instead.", this);
            try
            {
                if (safe) player.spawn = checkpoint;
                bool reset = player.Respawn(); // Resets the controller's vertical velocity and look state too.
                if (reset) immuneUntil = Time.time + recoveryGraceSeconds;
                return reset;
            }
            finally { player.spawn = original; }
        }

        private bool CheckpointIsClear()
        {
            if (checkpoint == null || player.ground == null) return false;
            var terrain = player.ground;
            var collider = terrain.GetComponent<TerrainCollider>();
            if (collider == null || !collider.enabled || collider.terrainData != terrain.terrainData) return false;
            Vector3 p = checkpoint.position - terrain.transform.position;
            if (p.x < 0 || p.z < 0 || p.x > terrain.terrainData.size.x || p.z > terrain.terrainData.size.z) return false;
            float h = terrain.SampleHeight(checkpoint.position) + terrain.transform.position.y;
            if (!collider.Raycast(new Ray(new Vector3(checkpoint.position.x, h + 5, checkpoint.position.z), Vector3.down), out _, 10)) return false;
            Vector3 feet = new Vector3(checkpoint.position.x, h + .6f, checkpoint.position.z);
            var body = GetComponent<CharacterController>();
            foreach (var hit in Physics.OverlapCapsule(feet + Vector3.up * body.radius,
                feet + Vector3.up * (body.height - body.radius), body.radius, ~0, QueryTriggerInteraction.Ignore))
                if (hit != body) return false;
            return true;
        }

    }
}
