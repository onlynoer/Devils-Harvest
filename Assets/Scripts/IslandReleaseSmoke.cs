#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace DevilsHarvest
{
    /// <summary>Opt-in Windows development-build smoke checks. Inert without -dh-island-smoke.</summary>
    public sealed class IslandReleaseSmoke : MonoBehaviour
    {
        private readonly List<string> lines=new List<string>();private int failures;private string reportPath;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize(){if(!Environment.GetCommandLineArgs().Contains("-dh-island-smoke"))return;Application.runInBackground=true;new GameObject("Development Island Smoke Checks").AddComponent<IslandReleaseSmoke>();}
        private void Check(bool ok,string label){lines.Add((ok?"PASS ":"FAIL ")+label);if(!ok)failures++;Debug.Log(lines[lines.Count-1]);}
        private void OnLog(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error){lines.Add("FAIL runtime log: "+message);failures++;}}
        private IEnumerator Start()
        {
            reportPath=Path.GetFullPath("Logs/Release/StandaloneSmoke.txt");Directory.CreateDirectory(Path.GetDirectoryName(reportPath));Application.logMessageReceived+=OnLog;
            var run=Checks();while(true){bool more;try{more=run.MoveNext();}catch(Exception e){Check(false,e.ToString());break;}if(!more)break;yield return run.Current;}
            Application.logMessageReceived-=OnLog;File.WriteAllLines(reportPath,new[]{"Windows development player smoke; scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,"Graphics="+SystemInfo.graphicsDeviceType}.Concat(lines).Concat(new[]{"Failures="+failures}));Application.Quit(failures==0?0:1);
        }
        private static float Flat(Vector3 a,Vector3 b)=>new Vector2(a.x-b.x,a.z-b.z).magnitude;
        private static void Place(CharacterController body,Vector3 p){body.enabled=false;body.transform.position=p;body.enabled=true;Physics.SyncTransforms();}
        private IEnumerator Checks()
        {
            float until=Time.time+2;while(Time.time<until)yield return null;
            var map=FindObjectsByType<NaturalIslandDesign>().Single();var player=FindObjectsByType<WildernessTestPlayer>().Single();var body=player.GetComponent<CharacterController>();var terrain=map.terrain;var hazard=map.rockfall;
            Check(body.isGrounded&&player.enabled&&Flat(body.transform.position,player.spawn.position)<1,"Built scene loads; player spawns and lands");
            Check(terrain.GetComponent<TerrainCollider>().enabled&&terrain.GetComponent<TerrainCollider>().terrainData==terrain.terrainData,"Matching enabled TerrainCollider");
            Check(FindObjectsByType<Camera>().Count(c=>c.isActiveAndEnabled)==1&&FindObjectsByType<AudioListener>().Count(c=>c.isActiveAndEnabled)==1,"One camera/listener");
            int missing=0;foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<Component>(true))if(c==null)missing++;
            Check(missing==0,"No missing built-scene scripts/components");
            var renderers=FindObjectsByType<MeshRenderer>();Check(renderers.All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader!=null&&m.shader.name!="Hidden/InternalErrorShader")),"Built renderer materials/shaders present");
            if(SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Null){Check(renderers.All(r=>r.sharedMaterials.All(m=>m.shader.isSupported)),"Shaders supported on actual Windows graphics device");Check(terrain.materialTemplate!=null&&terrain.materialTemplate.shader.isSupported&&GraphicsSettings.currentRenderPipeline!=null,"URP terrain shader and active render pipeline available");}
            hazard.enabled=false;player.enabled=false;
            var routes=map.routes.Concat(new[]{new ExpandedIslandDesign.Route("Saddle-to-altar contour access",map.altarAccess.Select(t=>t.position).ToArray())}).Select(r=>(r,false)).ToList();
            var dealer=FindObjectsByType<ExpandedIslandDesign>().Single();routes.Add((new ExpandedIslandDesign.Route("Dealer grotto entrance and interior",dealer.dealerPassageChecks.Select(t=>t.position).ToArray()),true));
            int steps=0;
            foreach(var item in routes)
            {
                var route=item.r;bool grotto=item.Item2;float Ground(Vector3 p){if(grotto&&Physics.Raycast(p+Vector3.up*2.5f,Vector3.down,out var hit,12,~0,QueryTriggerInteraction.Ignore))return hit.point.y;return terrain.SampleHeight(p);}
                Vector3 start=route.points[0];start.y=Ground(start)+.15f;Place(body,start);bool ok=true;int stuck=0;float falling=-3;
                for(int i=1;i<route.points.Length&&ok;i++)
                {
                    int count=0;while(Flat(body.transform.position,route.points[i])>.45f&&count++<400)
                    {Vector3 before=body.transform.position,delta=route.points[i]-before;delta.y=0;falling=body.isGrounded?-3:Mathf.Max(-45,falling-22*.025f);body.Move(Vector3.ClampMagnitude(delta,.14f)+Vector3.up*(falling*.025f));if(Flat(before,body.transform.position)<.005f)stuck++;else stuck=0;if(stuck>20){lines.Add("STUCK "+route.name+" at "+body.transform.position);ok=false;break;}if(++steps%200==0)yield return null;}if(count>=400)ok=false;
                }
                Check(ok,"Native player collision traversal: "+route.name);
            }
            player.enabled=true;player.Respawn();hazard.enabled=true;until=Time.time+1;while(Time.time<until)yield return null;
            Vector3 pit=(map.pitBoundsMin+map.pitBoundsMax)*.5f;pit.y=terrain.SampleHeight(pit)+.6f;Place(body,pit);until=Time.time+3;while(Time.time<until)yield return null;
            Check(Flat(body.transform.position,map.pitReset.position)<1&&body.isGrounded,"Real pit opening, trigger and safe reset");
            until=Time.time+4;while(Time.time<until)yield return null;Vector3 impact=hazard.impactCenter.position;Place(body,new Vector3(impact.x,terrain.SampleHeight(impact)+.6f,impact.z));var boulder=Instantiate(hazard.rockPrefab,body.transform.position+Vector3.up*3,Quaternion.identity);boulder.Launch(hazard,Vector3.down*3,4);
            until=Time.time+2;while(Time.time<until)yield return null;Check(Flat(body.transform.position,map.checkpoint.position)<1,"Physical boulder hit / checkpoint reset");
            until=Time.time+4;while(Time.time<until)yield return null;Vector3 zone=hazard.GetComponent<BoxCollider>().bounds.center;zone.y=terrain.SampleHeight(zone)+.6f;Place(body,zone);until=Time.time+.7f;while(Time.time<until)yield return null;
            Check(hazard.IsWarningOrFalling&&hazard.ActiveRockCount==0&&hazard.warningDust.isPlaying,"Visible warning before rock release");Check(!hazard.TryTrigger(),"Busy burst rejects continuous triggering");
            Place(body,new Vector3(map.shelter.position.x,terrain.SampleHeight(map.shelter.position)+.6f,map.shelter.position.z));int peak=0;bool reached=false;until=Time.time+hazard.warningDuration+hazard.rockCount*hazard.releaseInterval+2;
            while(Time.time<until){peak=Mathf.Max(peak,hazard.ActiveRockCount);foreach(var rock in FindObjectsByType<RockfallBoulder>())if(Flat(rock.transform.position,impact)<3&&rock.transform.position.y<impact.y+2)reached=true;yield return null;}
            Check(peak>0&&peak<=hazard.maxActiveRocks&&reached,"Bounded live burst reaches trail");Check(Flat(body.transform.position,map.shelter.position)<2,"Shelter remains safe");Check(hazard.CooldownRemaining>0&&!hazard.TryTrigger(),"Cooldown blocks retrigger");
            until=Time.time+hazard.rockLifetime+1;while(Time.time<until)yield return null;Check(hazard.ActiveRockCount==0,"Temporary rocks cleaned up");while(hazard.CooldownRemaining>0)yield return null;Place(body,zone);until=Time.time+.7f;while(Time.time<until)yield return null;Check(hazard.IsWarningOrFalling,"Rockfall retriggers after cooldown");hazard.enabled=false;player.Respawn();
            if(SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Null){var rt=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32);rt.Create();player.playerCamera.aspect=960f/540;RenderPipeline.SubmitRenderRequest(player.playerCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(960,540,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();File.WriteAllBytes(Path.GetFullPath("Logs/Release/Standalone.png"),image.EncodeToPNG());RenderTexture.active=previous;Destroy(image);rt.Release();Destroy(rt);Check(File.Exists("Logs/Release/Standalone.png"),"Windows offscreen GPU frame captured");}
        }
    }
}
#endif
