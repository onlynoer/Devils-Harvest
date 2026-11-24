using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class MountainSurvey
{
    public static void Inspect()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/DevilsHarvest_Island.unity");
        var t=Terrain.activeTerrain;
        if(!t)throw new Exception("Missing island terrain");
        var d=t.terrainData;var h=d.GetHeights(0,0,d.heightmapResolution,d.heightmapResolution);
        Debug.Log($"MAP terrain {AssetDatabase.GetAssetPath(d)} origin={t.transform.position} size={d.size} res={d.heightmapResolution}");
        string suffix = Environment.GetCommandLineArgs().Contains("-mountain-after") ? "-after" : "";
        using(var f=new BinaryWriter(File.Create("Builds/island-heights" + suffix + ".bin")))
            foreach(float height in h)f.Write(height);
        using(var f=File.CreateText("Builds/island-solids" + suffix + ".txt"))
        foreach(var r in Object.FindObjectsByType<Renderer>())
        {
            var b=r.bounds; var p=b.center;
            if(p.z>500 && b.size.y>8 || p.z<255 && p.x>315 && p.x<380 && b.size.y>2){
            string path=r.name;var parent=r.transform.parent;while(parent!=null){path=parent.name+"/"+path;parent=parent.parent;}
            f.WriteLine($"{path}\t{p.x:F2} {p.y:F2} {p.z:F2} {b.size.x:F2} {b.size.y:F2} {b.size.z:F2} ground={t.SampleHeight(p)+t.transform.position.y:F2}");
            }
        }
        var q=Object.FindAnyObjectByType<FirstQuotaDay>();var o=q.objective;
        Debug.Log($"MAP objective {o.name} parent={o.transform.parent.name} canvas={o.canvas.name} rect={o.rectTransform.rect} parentRect={((RectTransform)o.transform.parent).rect} scaler={o.canvas.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution}");
    }
}
