using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class MountainRevisionPass
{
    static float Apron(Vector2 p,Vector2 center,float width,float depth)
    {
        float d=new Vector2((p.x-center.x)/width,(p.y-center.y)/depth).sqrMagnitude;
        return Mathf.Pow(Mathf.Max(0,1f-d),3);
    }

    public static void Apply()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/DevilsHarvest_Island.unity");
        var t=Terrain.activeTerrain;var d=t.terrainData;
        if(AssetDatabase.GetAssetPath(d)!="Assets/Environment/Terrain/Tropical Editable Terrain.asset") throw new Exception("Unexpected terrain");
        var group=GameObject.Find("Trail signs and caches");
        var signs=group.transform.Cast<Transform>().Where(c=>c.name.EndsWith(" waypost")).ToArray();
        if(signs.Length!=5) throw new Exception("Expected five placed trail signs");
        foreach(var sign in signs) Undo.DestroyObjectImmediate(sign.gameObject);
        // Compact support fans feather to zero on their entire perimeter, outside the saddle corridor.
        int n=d.heightmapResolution;
        var h=d.GetHeights(0,0,n,n);
        for(int z=0;z<n;z++) for(int x=0;x<n;x++)
        {
            Vector2 pt=new Vector2(x*d.size.x/(n-1),z*d.size.z/(n-1));
            float lift=7.8f*Apron(pt,new Vector2(751,785),58,51)+8.4f*Apron(pt,new Vector2(837,783),36,53);
            lift*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(9,24,Vector2.Distance(pt,new Vector2(793,757))));
            h[z,x]+=lift/d.size.y;
        }
        d.SetHeights(0,0,h);
        t.Flush();
        var rocks=Object.FindObjectsByType<Transform>().Where(a=>(a.name.StartsWith("Ridge Rock Face ")||a.name.StartsWith("Mountain Crown - Split Basalt")||a.name.StartsWith("Jagged Sanctuary Outcrop")||a.name.StartsWith("Twin Peak Basalt Outcrop")) && a.position.z>707 && a.position.z<856 && a.position.x>651 && a.position.x<910 && a.GetComponentInChildren<MeshFilter>()!=null).ToArray();
        var patches=new List<Vector3>();
        foreach(var rock in rocks)
        {
            if(rock.name.StartsWith("Mountain Crown - Split Basalt Fin 2") || rock.name.StartsWith("Ridge Rock Face 86/") || rock.name=="Ridge Rock Face 86" || rock.name=="Jagged Sanctuary Outcrop 11")
            { Debug.Log("REMOVED "+rock.name);Undo.DestroyObjectImmediate(rock.gameObject);continue;}
            var at=rock.position;
            var normal=d.GetInterpolatedNormal(at.x/d.size.x,at.z/d.size.z);
            rock.rotation=Quaternion.FromToRotation(Vector3.up,Vector3.Slerp(Vector3.up,normal,.90f))*rock.rotation;
            rock.localScale=Vector3.Scale(rock.localScale,new Vector3(1.14f,.71f,1.08f));
            var distances=new List<float>();
            foreach(var filter in rock.GetComponentsInChildren<MeshFilter>())
                foreach(var v in filter.sharedMesh.vertices)
                {var world=filter.transform.TransformPoint(v);distances.Add(world.y-t.SampleHeight(world));}
            distances.Sort();
            float embed=distances[Mathf.RoundToInt((distances.Count-1)*.55f)]+.35f;
            rock.position-=Vector3.up*embed;
            PrefabUtility.RecordPrefabInstancePropertyModifications(rock);
            var bounds=rock.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            patches.Add(new Vector3(bounds.center.x,Mathf.Min(18f,Mathf.Max(bounds.size.x,bounds.size.z)*.64f),bounds.center.z));
        }
        var layers=d.terrainLayers.Select(l=>l.name).ToArray();
        int stone=Array.IndexOf(layers,"Volcanic Rock"),soil=Array.IndexOf(layers,"Tropical Forest Floor"), sand=Array.IndexOf(layers,"Beach Sand");
        if(Mathf.Min(stone,soil,sand)<0)throw new Exception("Missing terrain surfaces");
        var map=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
        var trail=new[]{new Vector2(332,109),new Vector2(330,122),new Vector2(327,143),new Vector2(331,165),new Vector2(346,195),new Vector2(359,222),new Vector2(370,244)};
        for(int z=0;z<d.alphamapHeight;z++)for(int x=0;x<d.alphamapWidth;x++)
        {
            Vector2 p=new Vector2(x*d.size.x/(d.alphamapWidth-1),z*d.size.z/(d.alphamapHeight-1));
            float blend=0;int layer=stone;
            if(p.y>690 && p.x>630) foreach(var patch in patches) blend=Mathf.Max(blend,.80f*Apron(p,new Vector2(patch.x,patch.z),patch.y*1.35f,patch.y));
            if(p.y>=109 && p.y<=245 && p.x>315 && p.x<382)
            {
                float dist=100;
                for(int i=1;i<trail.Length;i++){var v=trail[i]-trail[i-1];dist=Mathf.Min(dist,(p-trail[i-1]-v*Mathf.Clamp01(Vector2.Dot(p-trail[i-1],v)/v.sqrMagnitude)).magnitude);}
                blend=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.2f,4.8f,dist)))*.76f*Mathf.Clamp01((245-p.y)/12f);
                layer=p.y<144?sand:soil;
            }
            if(blend<=0)continue;
            for(int i=0;i<layers.Length;i++)map[z,x,i]=Mathf.Lerp(map[z,x,i],i==layer?1:0,blend);
        }
        d.SetAlphamaps(0,0,map);
        EditorUtility.SetDirty(d); AssetDatabase.SaveAssetIfDirty(d);
        var quota=Object.FindAnyObjectByType<FirstQuotaDay>();
        quota.revealSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Island Sounds/Monster - Warning Growl.wav");
        quota.displayFont=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/FontdinerSwanky-Regular SDF.asset");
        if(!quota.revealSound||!quota.displayFont)throw new Exception("Missing quota assets");
        EditorUtility.SetDirty(quota);
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new Exception("Could not save island");
        Debug.Log("PASS island grounded "+patches.Count+" outcrops; removed five signs. Terrain and clock assets saved.");
    }
}
