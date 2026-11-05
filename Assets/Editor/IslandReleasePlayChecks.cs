using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
namespace DevilsHarvest.Editor {
 [InitializeOnLoad] public static class IslandReleasePlayChecks {
  static IEnumerator tests; static int failures; static double deadline; static string report;
  static string ReportPath=>SessionState.GetBool("IslandReleaseSprintOnly",false)?"Logs/Release/SprintReport.txt":"Logs/Release/PlayReport.txt";
  static IslandReleasePlayChecks(){EditorApplication.playModeStateChanged+=State;}
  public static void Run(){
   string path=IslandReleaseTools.ScenePath;
   if(path.Contains("INCOMPLETE")){Debug.LogError("Refusing Play certification of incomplete output");EditorApplication.Exit(4);return;}
   EditorSceneManager.OpenScene(path,OpenSceneMode.Single);if(!Application.isBatchMode)EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();SessionState.SetBool("IslandReleaseSprintOnly",!Application.isBatchMode);SessionState.SetBool("IslandReleasePlayChecks",true);EditorApplication.EnterPlaymode();
  }
  static void State(PlayModeStateChange state){
   if(!SessionState.GetBool("IslandReleasePlayChecks",false))return;
   if(state==PlayModeStateChange.EnteredPlayMode){if(!Application.isBatchMode)EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();report="Native Unity Play mode checks\nScene: "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path+"\nGraphical input only: "+SessionState.GetBool("IslandReleaseSprintOnly",false)+"\n";failures=0;tests=Checks();deadline=EditorApplication.timeSinceStartup+300;EditorApplication.update+=Tick;}
   if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool("IslandReleasePlayChecks",false);EditorApplication.Exit(File.ReadAllText(ReportPath).Contains("FAIL ")?5:0);}
  }
  static void Tick(){try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Play checks timed out");if(tests.MoveNext())return;Finish();}catch(Exception e){Check(false,e.ToString());Finish();}}
  static void Finish(){EditorApplication.update-=Tick;File.WriteAllText(ReportPath,report+"Failures="+failures+"\nGraphical synthetic-input checks are in SprintReport.txt. Human control feel and visual quality require manual review.\n");EditorApplication.ExitPlaymode();}
  static void Check(bool pass,string label){string line=(pass?"PASS ":"FAIL ")+label;report+=line+"\n";Debug.Log(line);if(!pass)failures++;}
  static double until; static bool Waiting()=>Time.time<until;
  static void Wait(float seconds){until=Time.time+seconds;}
  static void Teleport(CharacterController body,Vector3 feet){body.enabled=false;body.transform.position=feet;body.enabled=true;Physics.SyncTransforms();}
  static float Flat(Vector3 a,Vector3 b)=>new Vector2(a.x-b.x,a.z-b.z).magnitude;
  static IEnumerator Checks(){
   Application.runInBackground=true;Wait(2);while(Waiting())yield return null;
   var map=UnityEngine.Object.FindObjectsByType<NaturalIslandDesign>().Single();var player=UnityEngine.Object.FindObjectsByType<WildernessTestPlayer>().Single();var body=player.GetComponent<CharacterController>();var recovery=player.GetComponent<MountainCheckpointRecovery>();var terrain=map.terrain;
   Check(player.enabled&&body.isGrounded&&Flat(player.transform.position,player.spawn.position)<1,"Player Start/Respawn lands on arrival terrain");
   if(SessionState.GetBool("IslandReleaseSprintOnly",false)){
   // Test the actual Input System binding and movement speed, not just serialized speed numbers.
   var originalSettings=InputSystem.settings;InputSystem.settings=UnityEngine.Object.Instantiate(originalSettings);InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
   var keyboard=InputSystem.AddDevice<Keyboard>("TropicalValidationKeyboard");
   Vector3 testFeet=player.transform.position;float testYaw=player.transform.eulerAngles.y;
   Cursor.lockState=CursorLockMode.Locked;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputSystem.Update();
   Vector3 beforeWalk=player.transform.position;float began=Time.time;
   Wait(.8f);while(Waiting()){if(!Application.isBatchMode)EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();Cursor.lockState=CursorLockMode.Locked;yield return null;}
   float walkDistance=Flat(beforeWalk,player.transform.position),walkSeconds=Time.time-began;
   report+="Walk diagnostics: distance="+walkDistance+", time="+walkSeconds+", cursor="+Cursor.lockState+", keyboard="+keyboard.enabled+", W="+keyboard.wKey.isPressed+", move="+((InputAction)typeof(WildernessTestPlayer).GetField("move",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player)).ReadValue<Vector2>()+"\n";
   InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();player.Respawn();Cursor.lockState=CursorLockMode.Locked;
   Vector3 beforeSprint=player.transform.position;began=Time.time;
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));InputSystem.Update();bool sawConfiguredSprint=false;
   Wait(.8f);while(Waiting()){sawConfiguredSprint|=player.IsSprinting&&Mathf.Abs(player.CurrentMoveSpeed-player.sprintSpeed)<.01f;if(!Application.isBatchMode)EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();Cursor.lockState=CursorLockMode.Locked;yield return null;}
   float sprintDistance=Flat(beforeSprint,player.transform.position),sprintSeconds=Time.time-began;
   report+="Sprint diagnostics: distance="+sprintDistance+", time="+sprintSeconds+", cursor="+Cursor.lockState+", keyboard="+keyboard.enabled+", Shift="+keyboard.leftShiftKey.isPressed+", action="+((InputAction)typeof(WildernessTestPlayer).GetField("sprint",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player)).IsPressed()+"\n";
   Check(sawConfiguredSprint,"Left Shift Input System action activates configured sprint speed");
   float ratio=(sprintDistance/Mathf.Max(.01f,sprintSeconds))/(walkDistance/Mathf.Max(.01f,walkSeconds));
   Check(walkDistance>2&&sprintDistance>walkDistance*1.3f&&ratio>1.4f&&ratio<1.8f,"Actual player Update walking/sprinting displacement ratio="+ratio.ToString("F2"));
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputSystem.Update();Wait(.12f);while(Waiting())yield return null;
   Check(!player.IsSprinting&&Mathf.Abs(player.CurrentMoveSpeed-player.speed)<.01f,"Releasing Left Shift returns to walk speed");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();player.Respawn();Wait(.7f);while(Waiting()){EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();Cursor.lockState=CursorLockMode.Locked;yield return null;}
   float jumpStartInput=body.transform.position.y,maxJumpInput=jumpStartInput;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));float jumpTime=Time.time;bool jumpReleased=false;Wait(1.5f);
   while(Waiting()){EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();Cursor.lockState=CursorLockMode.Locked;maxJumpInput=Mathf.Max(maxJumpInput,body.transform.position.y);if(!jumpReleased&&Time.time-jumpTime>.12f){InputSystem.QueueStateEvent(keyboard,new KeyboardState());jumpReleased=true;}yield return null;}
   Check(maxJumpInput-jumpStartInput>.8f&&body.isGrounded,"Space Input System jump and actual landing (height="+(maxJumpInput-jumpStartInput).ToString("F2")+")");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();InputSystem.RemoveDevice(keyboard);var testSettings=InputSystem.settings;InputSystem.settings=originalSettings;UnityEngine.Object.DestroyImmediate(testSettings);player.Respawn();yield break; }
   // Exercise the actual CharacterController capsule against native scene colliders, following every saved route.
   map.rockfall.enabled=false;player.enabled=false;int total=0;
   var interaction=UnityEngine.Object.FindObjectsByType<Transform>().Single(t=>t.name=="AltarInteractionSpace");
   var altarRoute=new ExpandedIslandDesign.Route("Saddle to AltarInteractionSpace",map.altarAccess.Select(t=>t.position).ToArray());
   foreach(var route in map.routes.Concat(new[]{altarRoute})){
    Vector3 start=route.points[0];start.y=terrain.SampleHeight(start)+.15f;Teleport(body,start);bool clear=true;int stuck=0;
    for(int k=1;k<route.points.Length&&clear;k++){
     Vector3 target=route.points[k];int count=0;
     while(Flat(body.transform.position,target)>.45f&&count++<400){
      Vector3 before=body.transform.position;Vector3 delta=target-before;delta.y=0;delta=Vector3.ClampMagnitude(delta,.14f);
      body.Move(delta+Vector3.down*.12f);total++;
      if(Flat(before,body.transform.position)<.005f)stuck++;else stuck=0;
      if(stuck>20){report+="STUCK "+route.name+" at "+body.transform.position+" target "+target+"\n";clear=false;break;}
      if(total%200==0)yield return null;
     }
     if(count>=400)clear=false;
    }
    Check(clear,"Native CharacterController route traversal: "+route.name);
   }
   player.enabled=true;player.Respawn();map.rockfall.enabled=true;Wait(1);while(Waiting())yield return null;
   // Apply a test jump impulse through the controller's existing gravity state, rather than faking a teleport.
   float jumpStart=body.transform.position.y;typeof(WildernessTestPlayer).GetField("vertical",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,Mathf.Sqrt(44*player.jumpHeight));
   float maximum=jumpStart;Wait(1.5f);while(Waiting()){maximum=Mathf.Max(maximum,body.transform.position.y);yield return null;}
   Check(maximum>jumpStart+.8f&&body.isGrounded,"Existing jump/gravity state rises and lands (input binding not simulated)");
   Vector3 center=(map.pitBoundsMin+map.pitBoundsMax)*.5f;center.y=terrain.SampleHeight(center)+.6f;Teleport(body,center);
   Wait(3);while(Waiting())yield return null;
   Check(Flat(body.transform.position,map.pitReset.position)<1&&body.isGrounded,"Actual fall through terrain hole invokes pit trigger and safe checkpoint reset");
   Wait(4);while(Waiting())yield return null;
   var hazard=map.rockfall;Teleport(body,new Vector3(hazard.impactCenter.position.x,terrain.SampleHeight(hazard.impactCenter.position)+.6f,hazard.impactCenter.position.z));
   var rock=UnityEngine.Object.Instantiate(hazard.rockPrefab,body.transform.position+Vector3.up*3,Quaternion.identity);rock.Launch(hazard,Vector3.down*3,4);
   Application.runInBackground=true;Wait(2);while(Waiting())yield return null;
   Check(Flat(body.transform.position,map.checkpoint.position)<1,"Gravity-driven boulder contact resets temporary player to mountain checkpoint");
   Wait(4);while(Waiting())yield return null;
   Vector3 zone=hazard.GetComponent<BoxCollider>().bounds.center;zone.y=terrain.SampleHeight(zone)+.6f;Teleport(body,zone);
   Wait(.7f);while(Waiting())yield return null;
   Check(hazard.IsWarningOrFalling&&hazard.ActiveRockCount==0&&hazard.warningDust.isPlaying,"Approach trigger starts warning before any boulder release");
   Check(!hazard.TryTrigger(),"Busy hazard rejects continuous retriggering");
   Teleport(body,new Vector3(map.shelter.position.x,terrain.SampleHeight(map.shelter.position)+.6f,map.shelter.position.z));
   bool reachedTrail=false;int peak=0;float end=Time.time+hazard.warningDuration+hazard.rockCount*hazard.releaseInterval+2;
   while(Time.time<end){peak=Mathf.Max(peak,hazard.ActiveRockCount); foreach(var falling in UnityEngine.Object.FindObjectsByType<RockfallBoulder>())if(Flat(falling.transform.position,hazard.impactCenter.position)<3&&falling.transform.position.y<hazard.impactCenter.position.y+2)reachedTrail=true; yield return null;}
   Check(peak>0&&peak<=hazard.maxActiveRocks,"Burst creates gravity-driven rocks within configured cap (peak="+peak+")");
   Check(reachedTrail,"Released burst rocks physically reach the intended trail"); Check(Flat(body.transform.position,map.shelter.position)<2,"Shelter remains safe during the live burst");
   Check(!hazard.TryTrigger()&&hazard.CooldownRemaining>0,"Cooldown blocks immediate retrigger");
   Wait(hazard.rockLifetime+1);while(Waiting())yield return null;
   Check(hazard.ActiveRockCount==0,"Temporary rocks cleaned up after lifetime");
   while(hazard.CooldownRemaining>0)yield return null;
   Teleport(body,zone);Wait(.7f);while(Waiting())yield return null;
   Check(hazard.IsWarningOrFalling,"Hazard can be triggered again after cooldown by re-entering approach");
  }
 }
}
