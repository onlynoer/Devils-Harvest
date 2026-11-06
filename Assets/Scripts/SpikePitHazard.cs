using System.Collections.Generic;
using UnityEngine;

namespace DevilsHarvest
{
    /// <summary>Scene Terrain holes supply the actual opening; this prefab supplies the interior and hit volume.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SpikePitHazard : MonoBehaviour
    {
        public Transform safeCheckpoint;
        private readonly Dictionary<Transform,float> nextHit=new Dictionary<Transform,float>();
        private readonly HashSet<Collider> contacted=new HashSet<Collider>();
        private void OnTriggerEnter(Collider other)=>Hit(other);
        private void OnTriggerStay(Collider other)=>Hit(other);
        private void OnTriggerExit(Collider other)=>contacted.Remove(other);
        private void OnDisable(){nextHit.Clear();contacted.Clear();}
        private void Update()
        {
            // CharacterController teleport/disable can omit an exit callback; do not leave a stale one-shot latch.
            var bounds=GetComponent<BoxCollider>().bounds;
            contacted.RemoveWhere(c=>c==null||!c.enabled||!bounds.Intersects(c.bounds));
        }
        private void Hit(Collider other)
        {
            if(contacted.Contains(other))return;
            var recovery=other.GetComponentInParent<MountainCheckpointRecovery>();
            if(recovery!=null && recovery.IsRecovering)return;
            Transform actor=recovery!=null?recovery.transform:other.transform.root;
            if(nextHit.TryGetValue(actor,out float time) && Time.time<time)return;
            foreach(var component in other.GetComponentsInParent<MonoBehaviour>())
            {
                if(!(component is IRockfallHitReceiver receiver))continue;
                if(receiver.ReceiveRockfallHit(other.ClosestPoint(transform.position),Vector3.down*6))
                {contacted.Add(other);nextHit[actor]=Time.time+3;return;}
            }
            if(recovery==null)return;
            contacted.Add(other);nextHit[actor]=Time.time+3;
            recovery.RequestReset(safeCheckpoint);
        }
    }
}
