using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DevilsHarvest.Editor
{
    public static class IslandReleaseTools
    {
        public const string ScenePath="Assets/DevilsHarvest/Scenes/DevilsHarvest_Island.unity";
        [MenuItem("Tools/Devils Harvest/Island Release/Open Release Map Alone")]
        public static void Open(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);}
        [MenuItem("Tools/Devils Harvest/Island Release/Validate Open Release Map")]
        public static void ValidateOpen(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;bool ok=Validate();Debug.Log(ok?"Release Edit mode checks passed. Play/build testing is separate.":"Release validation failed; inspect Logs/Release.");}
        public static void BatchValidate(){try{EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);EditorApplication.Exit(Validate()?0:4);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(5);}}
        internal static bool Validate()
        {
            Directory.CreateDirectory("Logs/Release");var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath||SceneManager.sceneCount!=1)throw new Exception("Open the release scene alone.");
            var map=UnityEngine.Object.FindObjectsByType<NaturalIslandDesign>().Single();var result=IslandReleaseChecks.Validate(map);File.WriteAllText("Logs/Release/EditChecks.txt",result.Text);
            var problems=new List<string>();var dependencies=AssetDatabase.GetDependencies(new[]{ScenePath,map.ungradedTerrainPath},true).Where(p=>p.StartsWith("Assets/")).Distinct().ToArray();
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)problems.Add("Missing script: "+t.name);
                if(PrefabUtility.GetPrefabInstanceStatus(t.gameObject)==PrefabInstanceStatus.MissingAsset)problems.Add("Missing prefab: "+t.name);
                foreach(var c in t.GetComponents<Component>().Where(c=>c!=null))
                {var serialized=new SerializedObject(c);var p=serialized.GetIterator();while(p.NextVisible(true))if(p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue==null&&!p.objectReferenceEntityIdValue.Equals(EntityId.None))problems.Add("Missing reference: "+t.name+"/"+p.propertyPath);}
            }
            foreach(string path in dependencies)
            {
                if(!File.Exists(path)){problems.Add("Missing dependency: "+path);continue;}
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset is Material mat)
                {if(mat.shader==null||mat.shader.name=="Hidden/InternalErrorShader"||ShaderUtil.ShaderHasError(mat.shader))problems.Add("Missing/error shader: "+path);}
                if(new FileInfo(path).Length<30*1024*1024)
                {byte[] bytes=File.ReadAllBytes(path);if(bytes.Length>5&&bytes[0]=='%'&&bytes[1]=='Y')foreach(Match m in Regex.Matches(System.Text.Encoding.UTF8.GetString(bytes),@"guid: ([0-9a-f]{32})")){string guid=m.Groups[1].Value;if(!guid.StartsWith("0000000000000000")&&string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))problems.Add("Unresolved GUID "+guid+" in "+path);}}
            }
            var player=UnityEngine.Object.FindObjectsByType<WildernessTestPlayer>().Single();var body=player.GetComponent<CharacterController>();var expanded=UnityEngine.Object.FindObjectsByType<ExpandedIslandDesign>().Single();
            foreach(var marker in expanded.dealerPassageChecks)
            {Vector3 feet=marker.position+Vector3.up*.25f;if(Physics.OverlapCapsule(feet+Vector3.up*body.radius,feet+Vector3.up*(body.height-body.radius),body.radius,~0,QueryTriggerInteraction.Ignore).Any(c=>!(c is TerrainCollider)&&c!=body))problems.Add("Dealer passage blocked: "+marker.name);}
            if(map.altarAccess==null||map.altarAccess.Length<2)problems.Add("Missing altar contour connector.");
            if(map.terrain.terrainData.terrainLayers.Any(l=>l==null||l.diffuseTexture==null))problems.Add("Missing terrain layer/texture.");
            File.WriteAllLines("Logs/Release/MissingReferences.txt",new[]{"Scene and dependencies missing-reference/shader problems="+problems.Count}.Concat(problems));
            File.WriteAllLines("Logs/Release/final-dependencies.tsv",dependencies.OrderBy(p=>p).Select(p=>AssetDatabase.AssetPathToGUID(p)+"\t"+new FileInfo(p).Length+"\t"+p));
            foreach(string p in problems)Debug.LogError(p);return result.Failures==0&&problems.Count==0;
        }
        public static void ConfigureBuild()
        {
            var original=EditorBuildSettings.scenes;var stale=original.Where(s=>s.path=="Assets/Scenes/SampleScene.unity"&&!File.Exists(s.path)).ToArray();
            var keep=original.Where(s=>s.path!=ScenePath&&!stale.Contains(s)).ToArray();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(keep).ToArray();
            File.WriteAllLines("Logs/Release/BuildConfiguration.txt",new[]{"Release scene first; valid existing team entries and configuration objects preserved."}.Concat(stale.Select(s=>"Removed missing template entry: "+s.path)).Concat(EditorBuildSettings.scenes.Select(s=>s.enabled+" "+s.path)));
        }
        public static void BuildDevelopment()
        {
            try
            {
                EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);if(!Validate())throw new Exception("Cannot build failing release scene.");ConfigureBuild();
                if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))throw new Exception("Windows standalone module unavailable.");
                Directory.CreateDirectory("Builds/DevilsHarvest_Development");
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName="Builds/DevilsHarvest_Development/DevilsHarvest.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development|BuildOptions.AllowDebugging|BuildOptions.CompressWithLz4});
                File.WriteAllText("Logs/Release/BuildResult.txt",report.summary.result+"\nBytes="+report.summary.totalSize+"\nErrors="+report.summary.totalErrors+"\nWarnings="+report.summary.totalWarnings+"\nTime="+report.summary.totalTime);
                EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:6);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(7);}
        }
        public static void MarkValidatedRelease()
        {
            try
            {
                foreach(string path in new[]{"Logs/Release/PlayReport.txt","Logs/Release/SprintReport.txt","Logs/Release/StandaloneSmoke.txt"})
                {string report=File.ReadAllText(path);if(report.Contains("FAIL ")||!report.Contains("Failures=0"))throw new Exception("Checks not passing: "+path);}
                if(!File.ReadAllText("Logs/Release/BuildResult.txt").StartsWith("Succeeded"))throw new Exception("Development build not successful.");
                EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);if(!Validate())throw new Exception("Final Edit checks failed.");
                var map=UnityEngine.Object.FindObjectsByType<NaturalIslandDesign>().Single();map.generationStatus="RELEASE VALIDATED - EDIT, GRAPHICAL INPUT, LIVE HAZARDS, WINDOWS DEVELOPMENT SMOKE PASSED";
                var meta=UnityEngine.Object.FindObjectsByType<TropicalIslandDesign>().Single();meta.status=map.generationStatus;meta.name="20 Release Map - Environment Metadata";
                EditorUtility.SetDirty(map);EditorUtility.SetDirty(meta);EditorSceneManager.SaveScene(map.gameObject.scene);
                EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);if(!Validate())throw new Exception("Post-save/reopen validation failed.");
                File.WriteAllText("Logs/Release/FinalStatus.txt","PASS - final release saved and reopened; dependencies/missing references checked again.\nScene="+ScenePath+"\nGUID="+AssetDatabase.AssetPathToGUID(ScenePath)+"\nUnity="+Application.unityVersion+"\nNo commits or pushes performed by this tool.\n");Debug.Log("FINAL ISLAND RELEASE VALIDATION PASSED");EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(11);}
        }    }
}
