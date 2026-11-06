using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace DevilsHarvest.Editor
{
    public static class IslandGroundingAudit
    {

        internal const string ScenePath=IslandReleaseTools.ScenePath;
        private static Terrain terrain;
        private static NaturalIslandDesign map;
        internal static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
        private static bool In(Transform t,Transform parent)=>parent!=null&&(t==parent||t.IsChildOf(parent));
        private static string Reason(GameObject root)
        {
            if(In(root.transform,map.altar))return "altar assembly - handled together";
            if(In(root.transform,map.rockfall.transform.parent))return "rockfall source/warning/impact assembly";
            if(In(root.transform,map.spikePit.transform.parent))return "intentional pit interior/rim";
            string n=root.name.ToLowerInvariant();
            if(new[]{"ocean","shelter","overhang","hanging","lantern","ceiling","lintel","door left","door right","grotto floor","grotto west wall","grotto east wall","grotto rear wall","entrance ramp","crossing","bridge","rope","notch","rock roof"}.Any(n.Contains))return "intentional structure / suspended geometry";
            return "";
        }
        private static GameObject[] Props()=>Object.FindObjectsByType<MeshRenderer>().Select(r=>PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject)??r.gameObject).Distinct().ToArray();
        private static List<Vector3> Footprint(GameObject root)
        {
            var points=new List<Vector3>();var filters=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null&&!(f.name=="Distant Crown"||f.name=="Distant Canopy"||f.name=="Distant Palm Fronds")).ToArray();
            bool tree=root.GetComponents<CapsuleCollider>().Any(c=>c.height>4)&&!root.name.Contains("Log");
            if(tree){var wood=filters.Where(f=>new[]{"Trunk","Branches","Charred","Limbs"}.Any(f.name.Contains)).ToArray();if(wood.Length>0)filters=wood;}
            foreach(var f in filters)if(f.sharedMesh.isReadable)foreach(Vector3 v in f.sharedMesh.vertices)points.Add(f.transform.TransformPoint(v));
            if(points.Count==0)return points;
            float low=points.Min(v=>v.y);float band=tree?.13f:.16f;
            return points.Where(v=>v.y<=low+band).GroupBy(v=>new Vector3(Mathf.Round(v.x*100)/100,Mathf.Round(v.y*100)/100,Mathf.Round(v.z*100)/100)).Select(g=>g.First()).Take(100).ToList();
        }
        private static bool Surface(Vector3 p,GameObject self,out float height)
        {
            height=float.NegativeInfinity;
            var ray=new Ray(p+Vector3.up*.85f,Vector3.down);
            foreach(var hit in Physics.RaycastAll(ray,terrain.terrainData.size.y+100,~0,QueryTriggerInteraction.Ignore))
            {
                if(In(hit.transform,self.transform)||hit.collider is CharacterController||hit.normal.y<.5f)continue;
                // Tree trunks/soft vegetation are not furniture foundations.
                if(hit.collider is CapsuleCollider&&hit.transform.GetComponentInParent<LODGroup>()!=null)continue;
                if(hit.point.y>height)height=hit.point.y;
            }
            // Buried geometry can sit above the ray origin; sample the actual TerrainCollider from above too.
            var tc=terrain.GetComponent<TerrainCollider>();
            if(tc.Raycast(new Ray(new Vector3(p.x,terrain.terrainData.size.y+20,p.z),Vector3.down),out var terrainHit,terrain.terrainData.size.y+100))height=Mathf.Max(height,terrainHit.point.y);
            return !float.IsNegativeInfinity(height);
        }
        private static float[] Gaps(GameObject root)
        {var gaps=new List<float>();foreach(var p in Footprint(root))if(Surface(p,root,out float h))gaps.Add(p.y-h);gaps.Sort();return gaps.ToArray();}
        [MenuItem("Tools/Devils Harvest/Island Release/Audit Grounding")]
        public static void Audit()
        {
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                if(Application.isBatchMode||UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=ScenePath||UnityEngine.SceneManagement.SceneManager.sceneCount!=1)
                {
                    if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                    EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
                }
                Directory.CreateDirectory("Logs/Release");
                map=Object.FindObjectsByType<NaturalIslandDesign>().Single();terrain=map.terrain;Physics.SyncTransforms();
                var extra=new[]{map.ungradedTerrainPath}.Where(File.Exists).ToArray();
                var dependencies=AssetDatabase.GetDependencies(new[]{UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}.Concat(extra).ToArray(),true).Where(p=>p.StartsWith("Assets/")).Distinct().OrderBy(p=>p).ToArray();
                File.WriteAllLines("Logs/Release/scene-dependencies.tsv",dependencies.Select(p=>AssetDatabase.AssetPathToGUID(p)+"\t"+AssetDatabase.GetMainAssetTypeAtPath(p)+"\t"+new FileInfo(p).Length+"\t"+p));
                var report=new List<string>();foreach(var g in Props())
                {
                    string reason=Reason(g);if(reason!=""){report.Add("PRESERVE\t"+reason+"\t"+PathOf(g.transform));continue;}
                    var gaps=Gaps(g);if(gaps.Length==0){report.Add("NO SUPPORT\t"+PathOf(g.transform));continue;}
                    float mid=gaps[gaps.Length/2];if(mid>.14f||gaps.Last()<-.4f)report.Add((gaps.First()>.14f?"FLOATING":gaps.Last()<-.4f?"EMBEDDED":"SUPPORTED_SLOPE")+"\tmin="+gaps.First().ToString("F3")+" median="+mid.ToString("F3")+" max="+gaps.Last().ToString("F3")+"\t"+PathOf(g.transform)+"\t"+g.transform.position.ToString("F2"));
                }
                File.WriteAllLines("Logs/Release/grounding-audit.tsv",report);
                File.WriteAllLines("Logs/Release/altar-audit.tsv",map.altar.GetComponentsInChildren<Transform>(true).Select(t=>PathOf(t)+"\t"+t.position.ToString("F3")+"\t"+t.localScale));
                Debug.Log("RELEASE AUDIT COMPLETE dependencies="+dependencies.Length+" props="+Props().Length+" check="+report.Count(l=>l.StartsWith("FLOATING")));if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
    }
}
