using UnityEditor;
using UnityEngine;

namespace DevilsHarvest.Editor
{
    [CustomEditor(typeof(RockfallHazard))]
    internal sealed class RockfallHazardInspector : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint()=>Application.isPlaying;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var hazard=(RockfallHazard)target;
            if(!Application.isPlaying)return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Live rocks",hazard.ActiveRockCount.ToString());
            EditorGUILayout.LabelField("Warning / burst active",hazard.IsWarningOrFalling.ToString());
            EditorGUILayout.LabelField("Cooldown remaining",hazard.CooldownRemaining.ToString("F1")+" s");
            if(GUILayout.Button("Test Burst (player must be in approach zone)"))
                if(!hazard.TryTrigger())Debug.Log("Rockfall test needs the player inside the approach zone, a completed cooldown, and no active warning/burst.",hazard);
        }
    }
}
