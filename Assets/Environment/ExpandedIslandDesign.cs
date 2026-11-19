using System;
using UnityEngine;

namespace DevilsHarvest
{
    /// <summary>Saved construction references only. No runtime generation or gameplay systems.</summary>
    public sealed class ExpandedIslandDesign : MonoBehaviour
    {
        [Serializable]
        public sealed class Route
        {
            public string name;
            public Vector3[] points;
            public Route(string name, params Vector3[] points) { this.name = name; this.points = points; }
        }

        public float mapSize;
        public string sourceScene;
        public string sourceEnvironmentFolder;
        public Terrain terrain;
        public Transform mountainSanctuary;
        public Transform dealerEntrance;
        public Transform dealerSpawn;
        public Transform[] dealerPassageChecks;
        public Route[] routes;
        [TextArea] public string notes;
    }
}
