using UnityEngine;

namespace DevilsHarvest
{
    public sealed class MountainRevisionDesign : MonoBehaviour
    {
        public string sourceScene;
        public Terrain terrain;
        public Transform checkpoint;
        public Transform shelter;
        public RockfallHazard rockfall;
        public ExpandedIslandDesign.Route[] routes;
        public float[] routeWidths;
        public Vector3 patchCenter;
        public Vector3 bridgeStart,bridgeEnd;
        public Vector2 patchRadii;
        public float originalPeak, revisedPeakTarget;
        [TextArea] public string notes;
    }
}
