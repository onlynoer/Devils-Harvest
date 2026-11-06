using System.Linq;
using UnityEditor;
using UnityEngine;
namespace DevilsHarvest.Editor {
    public static class IslandReleaseHazardTools
    {
        private static string Hierarchy(Transform t)=>t.parent==null?t.name:Hierarchy(t.parent)+"/"+t.name;
        [MenuItem("Tools/Devils Harvest/Island Release/Hazards/Select and Frame Rockfall")]
        public static void Rockfall()=>Frame(Object.FindObjectsByType<RockfallHazard>().FirstOrDefault(),38);
        [MenuItem("Tools/Devils Harvest/Island Release/Hazards/Select and Frame Spike Pit")]
        public static void Pit()=>Frame(Object.FindObjectsByType<SpikePitHazard>().FirstOrDefault(),16);
        private static void Frame(Component hazard,float size)
        {
            if(hazard==null){Debug.LogWarning("Open the island scene alone first.");return;}Selection.activeGameObject=hazard.gameObject;
            if(SceneView.lastActiveSceneView!=null){Vector3 center=hazard is RockfallHazard h?h.impactCenter.position:hazard.transform.position;SceneView.lastActiveSceneView.Frame(new Bounds(center,Vector3.one*size),false);}
            Debug.Log(Hierarchy(hazard.transform)+" at "+hazard.transform.position.ToString("F2"));
        }
        [DrawGizmo(GizmoType.Selected|GizmoType.NonSelected)]
        private static void RockfallGizmo(RockfallHazard h,GizmoType type)
        {
            if(h.impactCenter==null||h.releasePoint==null)return;
            var box=h.GetComponent<BoxCollider>();Gizmos.color=new Color(1,.6f,.1f,.7f);Gizmos.DrawWireCube(box.bounds.center,box.bounds.size);
            Handles.color=new Color(1,.5f,.05f);Handles.DrawWireDisc(h.impactCenter.position,Vector3.up,h.impactRadius);Handles.DrawDottedLine(h.releasePoint.position,h.impactCenter.position,5);
            Handles.Label(box.bounds.center+Vector3.up*3,"ROCKFALL APPROACH\n"+box.bounds.center.ToString("F1"));Handles.Label(h.releasePoint.position+Vector3.up,"Rock source - warning pebbles");
        }
        [DrawGizmo(GizmoType.Selected|GizmoType.NonSelected)]
        private static void PitGizmo(SpikePitHazard h,GizmoType type)
        {
            var box=h.GetComponent<BoxCollider>();Gizmos.color=new Color(1,.2f,.2f,.8f);Gizmos.DrawWireCube(box.bounds.center,box.bounds.size);Handles.Label(box.bounds.center+Vector3.up*4,"SPIKE PIT - REAL TERRAIN HOLE\n"+box.bounds.center.ToString("F1"));
            if(h.safeCheckpoint!=null){Handles.color=Color.green;Handles.DrawWireDisc(h.safeCheckpoint.position,Vector3.up,1.2f);Handles.Label(h.safeCheckpoint.position+Vector3.up*2,"Safe pit reset");}
        }
    }
}
