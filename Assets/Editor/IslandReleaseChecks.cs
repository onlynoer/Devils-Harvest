using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DevilsHarvest.Editor {
    public static class IslandReleaseChecks {
        internal sealed class CheckResult
        {
            internal readonly string Text;
            internal readonly int Failures;
            internal CheckResult(string text,int failures){Text=text;Failures=failures;}
        }
        internal static CheckResult Validate(NaturalIslandDesign map)
        {
            var text=new StringBuilder("Island release - native Edit mode collision/preservation queries, NOT Play mode\n");int failures=0;
            void Check(bool value,string label){text.AppendLine((value?"PASS ":"FAIL ")+label);if(!value)failures++;}
            var players=UnityEngine.Object.FindObjectsByType<WildernessTestPlayer>();var terrain=map.terrain;var data=terrain.terrainData;var collider=terrain.GetComponent<TerrainCollider>();
            Check(SceneManager.sceneCount==1&&players.Length==1,"One loaded scene and temporary player.");
            Check(UnityEngine.Object.FindObjectsByType<Camera>().Count(c=>c.isActiveAndEnabled)==1&&UnityEngine.Object.FindObjectsByType<AudioListener>().Count(c=>c.isActiveAndEnabled)==1,"One gameplay camera and Audio Listener.");
            Check(collider!=null&&collider.enabled&&collider.terrainData==data,"Enabled matching TerrainCollider.");
            if(players.Length!=1||collider==null)return new CheckResult(text.ToString(),Mathf.Max(1,failures));var body=players[0].GetComponent<CharacterController>();Physics.SyncTransforms();
            Check(players[0].ground==terrain&&body.enabled&&body.detectCollisions&&!Physics.GetIgnoreLayerCollision(body.gameObject.layer,terrain.gameObject.layer),"Working collision/controller references preserved.");
            Check(Mathf.Abs(data.size.x-1000)<1&&Mathf.Abs(data.size.z-1000)<1,"Approximately 1000 x 1000 meter footprint retained.");
            Check(map.widths.All(w=>w>body.radius*2+.8f),"Saved trail widths exceed actual capsule diameter plus clearance margin.");
            bool Clear(Vector3 feet)
            { var hits=Physics.OverlapCapsule(feet+Vector3.up*body.radius,feet+Vector3.up*(body.height-body.radius),body.radius,~0,QueryTriggerInteraction.Ignore).Where(c=>c!=body&&c!=collider).ToArray(); foreach(var hit in hits)text.AppendLine("OBSTRUCTION "+feet+" : "+hit.name+" at "+hit.transform.position); return hits.Length==0; }
            bool Safe(Transform marker)
            {
                if(marker==null)return false;Vector3 p=marker.position;float h=terrain.SampleHeight(p);int x=Mathf.Clamp((int)(p.x/data.size.x*data.holesResolution),0,data.holesResolution-1),z=Mathf.Clamp((int)(p.z/data.size.z*data.holesResolution),0,data.holesResolution-1);
                return !data.IsHole(x,z)&&collider.Raycast(new Ray(new Vector3(p.x,h+3,p.z),Vector3.down),out _,6)&&Clear(new Vector3(p.x,h+.6f,p.z));
            }
            Check(Safe(map.checkpoint)&&Safe(map.pitReset),"Mountain and pit reset points are solid, outside holes and capsule-clear.");
            Check(Safe(players[0].spawn),"Original arrival spawn remains safe.");
            var reference=AssetDatabase.LoadAssetAtPath<TerrainData>(map.ungradedTerrainPath);
            int unsupported=0,obstructed=0,steep=0;float maxGrade=0,maxGroundSlope=0,maxArtificialRise=0,maxAddedSideStep=0,bermIndicators=0;
            foreach(var route in map.routes)
            for(int segment=1;segment<route.points.Length;segment++)
            {
                Vector3 a=route.points[segment-1],b=route.points[segment];float length=FlatDistance(a,b);int count=Mathf.Max(1,Mathf.CeilToInt(length));Vector3 side=new Vector3(b.z-a.z,0,a.x-b.x).normalized;
                float previous=0;
                for(int i=0;i<=count;i++)
                {
                    Vector3 p=Vector3.Lerp(a,b,i/(float)count);float h=terrain.SampleHeight(p);
                    if(!collider.Raycast(new Ray(new Vector3(p.x,h+2,p.z),Vector3.down),out _,4))unsupported++;
                    if(!Clear(new Vector3(p.x,h+.12f,p.z)))obstructed++;
                    foreach(float sign in new[]{-1f,1f})
                    {
                        int index=Array.IndexOf(map.routes,route);Vector3 q=p+side*sign*(map.widths[index]*.5f-body.radius-.25f);float qh=terrain.SampleHeight(q);
                        if(!collider.Raycast(new Ray(new Vector3(q.x,qh+2,q.z),Vector3.down),out _,4))unsupported++;
                        if(!Clear(new Vector3(q.x,qh+.12f,q.z)))obstructed++;
                    }
                    float slope=data.GetSteepness(p.x/data.size.x,p.z/data.size.z);maxGroundSlope=Mathf.Max(maxGroundSlope,slope);
                    if(i>0&&h-terrain.SampleHeight(p+side*6)>3&&h-terrain.SampleHeight(p-side*6)>3)bermIndicators+=length/count;
                    if(i>0){float grade=Mathf.Atan2(Mathf.Abs(h-previous),Mathf.Max(.01f,length/count))*Mathf.Rad2Deg;maxGrade=Mathf.Max(maxGrade,grade);if(grade>35)steep++;}previous=h;
                    if(reference!=null)
                    {
                        float original=reference.GetInterpolatedHeight(p.x/data.size.x,p.z/data.size.z),centerDelta=h-original;maxArtificialRise=Mathf.Max(maxArtificialRise,centerDelta);
                        foreach(float offset in new[]{-16f,-10,-5,-2,2,5,10,16})
                        {
                            Vector3 q=p+side*offset;float finalHeight=terrain.SampleHeight(q),baseHeight=reference.GetInterpolatedHeight(q.x/data.size.x,q.z/data.size.z);
                            // Relative to natural cross-section: distinguishes natural cliffs from grading-generated walls.
                            maxAddedSideStep=Mathf.Max(maxAddedSideStep,Mathf.Abs(centerDelta-(finalHeight-baseHeight)));
                        }
                    }
                }
            }
            Check(unsupported==0&&obstructed==0,"Route support / capsule obstructions: "+unsupported+" / "+obstructed+".");
            Check(steep==0&&maxGroundSlope<body.slopeLimit,"Route forward grade "+maxGrade.ToString("F1")+"deg; full ground slope "+maxGroundSlope.ToString("F1")+"deg; controller limit "+body.slopeLimit+".");
            Check(reference!=null&&maxArtificialRise<.03f&&maxAddedSideStep<=map.maximumTrailCut+.04f,"Approved-terrain cross-section preservation: raised center max "+maxArtificialRise.ToString("F3")+"m, added shoulder contrast "+maxAddedSideStep.ToString("F3")+"m; cut limit "+map.maximumTrailCut+"m.");
            text.AppendLine("Absolute symmetric crest/berm indicators: "+bermIndicators.ToString("F1")+"m. Review against the ungraded terrain; a natural crest is not automatically an artificial wall.");
            bool Destination(Transform marker)=>marker!=null&&map.routes.Any(r=>r.points.Any(p=>FlatDistance(p,marker.position)<1));
            Check(Destination(map.westPeak)&&Destination(map.eastPeak),"Both peak destinations have saved route connections.");
            var visited=new HashSet<Vector2>();Vector2 Key(Vector3 p)=>new Vector2(Mathf.Round(p.x*100)/100,Mathf.Round(p.z*100)/100);
            visited.Add(Key(map.routes[0].points[0]));bool progress=true;
            while(progress){progress=false;foreach(var route in map.routes)if(route.points.Any(p=>visited.Contains(Key(p))))foreach(var p in route.points)if(visited.Add(Key(p)))progress=true;}
            Check(map.routes.All(r=>r.points.All(p=>visited.Contains(Key(p)))),"All new route branches connect to the main approach/saddle.");
            int holeFailure=0,pitBlocked=0;for(int z=0;z<map.holeSize.y;z++)for(int x=0;x<map.holeSize.x;x++)
            {
                int hx=map.holeStart.x+x,hz=map.holeStart.y+z;float cell=data.size.x/data.holesResolution;
                Vector3 p=new Vector3((hx+.5f)*cell,0,(hz+.5f)*cell);p.y=terrain.SampleHeight(p)+2;
                if(!data.IsHole(hx,hz)||collider.Raycast(new Ray(p,Vector3.down),out _,12))holeFailure++;
                p.y=terrain.SampleHeight(p)-.3f;if(!Clear(p))pitBlocked++;
            }
            Check(holeFailure==0,"Pit is a real Terrain/Collider opening across every hole cell (misses="+holeFailure+").");
            Check(pitBlocked==0,"Pit mouth capsule clearance across all hole cells: "+pitBlocked+" blocked.");
            var pit=map.spikePit;if(pit==null||map.rockfall==null){Check(false,"Missing pit/rockfall reference.");return new CheckResult(text.ToString(),failures);}var box=pit.GetComponent<BoxCollider>();
            Check(pit!=null&&box.isTrigger&&pit.safeCheckpoint==map.pitReset&&!box.bounds.Contains(map.pitReset.position+Vector3.up),"Pit trigger and outside reset point assigned without overlap.");
            Vector3 pitCenter=(map.pitBoundsMin+map.pitBoundsMax)*.5f;
            Check(Physics.Raycast(new Ray(new Vector3(pitCenter.x,map.pitBoundsMax.y-.3f,pitCenter.z),Vector3.down),out RaycastHit bottom,8,~0,QueryTriggerInteraction.Ignore)&&bottom.collider.name.Contains("Bottom"),"Modeled pit floor supplies reliable collision below opening.");
            Check(map.routes.Take(5).All(r=>RouteDistance(pitCenter,r.points)>6)&&RouteDistance(pitCenter,map.routes[5].points)>5,"Pit is beside the optional shortcut, with required paths and bypass clear.");
            // Geometric sightline only: material/lighting/readability still require first-person visual review.
            Vector3 eye=new Vector3(map.pitApproach.position.x,terrain.SampleHeight(map.pitApproach.position)+1.65f,map.pitApproach.position.z);
            Vector3 rim=new Vector3(pitCenter.x,map.pitBoundsMax.y+.12f,pitCenter.z);
            Check(!Physics.Linecast(eye,rim,~0,QueryTriggerInteraction.Ignore),"Approach-to-opening geometric sightline clear; lighting/noticeability not assessed.");
            var hazard=map.rockfall;int trajectoryBlocked=0;
            foreach(Vector2 spread in new[]{Vector2.zero,new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,-1),new Vector2(1,1)})
            {
                Vector3 offset=new Vector3(spread.x*hazard.releaseSpread.x,0,spread.y*hazard.releaseSpread.y),origin=hazard.releasePoint.position+offset,target=hazard.impactCenter.position+offset*.6f+Vector3.up*.65f;
                Vector3 velocity=RockfallHazard.TrailVelocity(origin,target,Physics.gravity,hazard.rockSpeed);float duration=Mathf.Clamp(FlatDistance(origin,target)/Mathf.Max(2,hazard.rockSpeed),.65f,1.4f);
                Vector3 previous=origin;
                for(int i=0;i<18;i++)
                {
                    float t=duration*i/20;Vector3 p=origin+velocity*t+Physics.gravity*(.5f*t*t);if(p.y<terrain.SampleHeight(p)+.55f)trajectoryBlocked++;
                    Vector3 delta=p-previous;
                    if(delta.magnitude>.001f&&Physics.SphereCast(previous,.58f,delta.normalized,out _,delta.magnitude,~0,QueryTriggerInteraction.Ignore))trajectoryBlocked++;
                    previous=p;
                }
            }
            Check(hazard.aimAtTrail&&trajectoryBlocked==0,"Center and corner ballistic trajectories clear the terrain toward trail (blocked samples="+trajectoryBlocked+").");
            Check(FlatDistance(map.checkpoint.position,hazard.impactCenter.position)>hazard.impactRadius+5&&FlatDistance(map.shelter.position,hazard.impactCenter.position)>hazard.impactRadius+3,"Rockfall checkpoint and shelter outside bounded impact area.");
            Check(map.routes.Skip(1).Take(2).All(r=>RouteDistance(hazard.impactCenter.position,r.points)>hazard.impactRadius+4),"Alternative and return outside the impact area.");
            Check(UnityEngine.Object.FindObjectsByType<RockfallHazard>().Length==1&&UnityEngine.Object.FindObjectsByType<SpikePitHazard>().Length==1,"Exactly one rockfall and one spike pit.");
            text.AppendLine("Failed checks: "+failures);text.AppendLine("Still required: actual first-person silhouette/foliage/pit clues in current lighting, route walking/jumps, hazard timing and collisions, cooldown/cleanup, safe resets, FPS and save/reopen. Ballistic samples do not simulate Rigidbody contacts.");
            if(failures>0)Debug.LogWarning("Natural island construction checks found "+failures+" issues. Inspect report before assuming the scene is playable.");return new CheckResult(text.ToString(),failures);
        }
        internal static float FlatDistance(Vector3 a,Vector3 b)=>new Vector2(a.x-b.x,a.z-b.z).magnitude;
        internal static float RouteDistance(Vector3 p,Vector3[] points){float result=float.MaxValue;for(int i=1;i<points.Length;i++){Vector3 a=points[i-1],b=points[i];Vector2 d=new Vector2(b.x-a.x,b.z-a.z);float t=d.sqrMagnitude<.00001f?0:Mathf.Clamp01(Vector2.Dot(new Vector2(p.x-a.x,p.z-a.z),d)/d.sqrMagnitude);result=Mathf.Min(result,FlatDistance(p,Vector3.Lerp(a,b,t)));}return result;}
    }
}
