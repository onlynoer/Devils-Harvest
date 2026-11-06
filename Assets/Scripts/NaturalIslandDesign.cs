using UnityEngine;

namespace DevilsHarvest
{
    public sealed class NaturalIslandDesign : MonoBehaviour
    {
        public string sourceScene;
        [TextArea] public string generationStatus="INCOMPLETE - construction has not passed Editor checks";
        public Terrain terrain;
        public Vector3 mountainCenter;
        public float coordinateScale;
        public Transform westPeak,eastPeak,altar,checkpoint,shelter;
        public Transform[] altarAccess;
        public ExpandedIslandDesign.Route[] routes;
        public float[] widths;
        public RockfallHazard rockfall;
        public SpikePitHazard spikePit;
        public Transform pitReset,pitApproach;
        public Vector2Int holeStart,holeSize;
        public Vector3 pitBoundsMin,pitBoundsMax;
        public float maximumTrailCut;
        public string ungradedTerrainPath;
        [TextArea] public string notes;
    }
}
