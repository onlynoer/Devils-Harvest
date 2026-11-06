using UnityEngine;
namespace DevilsHarvest
{
    /// <summary>Saved art-pass references. No runtime map generation.</summary>
    public sealed class TropicalIslandDesign : MonoBehaviour
    {
        public string sourceScene;
        public NaturalIslandDesign natural;
        public string outputFolder;
        public int replacedTrees,addedTrees,addedUnderstory;
        [TextArea] public string status="INCOMPLETE - construction not validated";
    }
}
