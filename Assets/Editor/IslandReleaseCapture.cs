using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace DevilsHarvest.Editor
{
    public static class IslandReleaseCapture
    {
        public static void Run(){EditorSceneManager.OpenScene(IslandReleaseTools.ScenePath,OpenSceneMode.Single);EditorApplication.delayCall+=Capture;}
        private static void Capture()
        {
            try
            {
                var design=UnityEngine.Object.FindObjectsByType<TropicalIslandDesign>().Single();var map=design.natural;var player=UnityEngine.Object.FindObjectsByType<WildernessTestPlayer>().Single();var camera=player.playerCamera;
                Directory.CreateDirectory("Logs/Release/Views");
                void Shot(string name,Vector3 from,Vector3 target)
                {
                    camera.transform.SetPositionAndRotation(from,Quaternion.LookRotation(target-from));camera.farClipPlane=1500;camera.aspect=16f/9f;
                    var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);rt.Create();
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                    var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Logs/Release/Views/"+name+".png",tex.EncodeToPNG());
                    RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
                }
                Vector3 Surface(float x,float z)=>new Vector3(x,map.terrain.SampleHeight(new Vector3(x,0,z)),z);
                Shot("Arrival_FirstPerson",player.spawn.position+Vector3.up*1.65f,Surface(345,210)+Vector3.up*5);
                var forest=UnityEngine.Object.FindObjectsByType<ExpandedIslandDesign>().Single().routes.Single(r=>r.name=="Deep forest exploration loop");Vector3 forestEye=Vector3.Lerp(forest.points[0],forest.points[1],.4f);
                Shot("HuntingForest_FirstPerson",Surface(forestEye.x,forestEye.z)+Vector3.up*1.65f,Surface(forest.points[1].x,forest.points[1].z)+Vector3.up*1.5f);
                Vector3 altar=map.altar.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Altar Base").position;
                Shot("Sanctuary_Approach",map.altarAccess[0].position+Vector3.up*1.65f,altar+Vector3.up*.8f);
                Shot("Altar_Standing",map.altarAccess[map.altarAccess.Length-1].position+Vector3.up*1.65f,altar+Vector3.up*.8f);
                bool fog=RenderSettings.fog;RenderSettings.fog=false;Shot("Island_Overview_NoFog_Diagnostic",new Vector3(520,630,-140),new Vector3(500,35,540));RenderSettings.fog=fog;
                Shot("Rockfall_Approach",map.rockfall.transform.position+new Vector3(0,1.65f,-7),map.rockfall.impactCenter.position+Vector3.up*3);
                Shot("Pit_Approach",map.pitApproach.position+Vector3.up*1.65f,(map.pitBoundsMin+map.pitBoundsMax)*.5f+Vector3.up*2);
                Debug.Log("TROPICAL RENDER CAPTURES SAVED: Logs/Release/Views");EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(7);}
        }
    }
}
