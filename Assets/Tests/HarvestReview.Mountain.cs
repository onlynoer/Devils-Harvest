#if HARVEST_PLAYTEST
using System.Collections;
using System.Linq;
using UnityEngine;

public sealed partial class HarvestReview
{
    static IEnumerator MountainCircuit()
    {
        foreach (var waypoint in BeachTrail.Split(',').Select(ParsePoint)) yield return Walk(waypoint);
        foreach (var waypoint in SaddleTrail.Split(',').Select(ParsePoint)) yield return Walk(waypoint);
        yield return Walk(new Vector2(793,754.4f));
        yield return Target(Object.FindAnyObjectByType<AltarOffering>());
        Check(gun.CurrentInteractable is AltarOffering, "walked from the grotto to the saddle table");
        yield return TraverseBack(new Vector2(789,802));
        yield return TraverseBack(new Vector2(798,821));
        yield return MountainView("trail",new Vector3(798,0,821),new Vector3(800,150,733));
        yield return TraverseBack(new Vector2(740,848));
        yield return TraverseBack(new Vector2(680,853));
        yield return MountainView("overlook",new Vector3(680,0,853),new Vector3(796,144,739));
        yield return TraverseBack(new Vector2(629,824));
        yield return MountainView("distant",new Vector3(629,0,824),new Vector3(794,143,742));
    }
    static IEnumerator TraverseBack(Vector2 destination)
    {
        var route=PathAround(XZ(p.transform.position),destination,2f,12000);
        foreach (var point in route) yield return Walk(point,1.15f);
        Debug.Log("MOUNTAIN WALK "+p.transform.position);
    }
    static IEnumerator MountainView(string label,Vector3 point,Vector3 target)
    {
        // Set the camera on a repeatable eye-height mark after walking to it.
        var cam=gun.playerCamera.transform;
        var position=cam.localPosition;
        var rotation=cam.localRotation;
        point.y=Terrain.activeTerrain.SampleHeight(point)+1.74f;
        Face(target);
        yield return new WaitForEndOfFrame();
        cam.position=point;
        cam.LookAt(target);
        var buffer=RenderTexture.GetTemporary(Screen.width,Screen.height,24);
        var old=RenderTexture.active;
        gun.playerCamera.targetTexture=buffer;
        gun.playerCamera.Render();
        gun.playerCamera.targetTexture=null;
        RenderTexture.active=buffer;
        var image=new Texture2D(buffer.width,buffer.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,buffer.width,buffer.height),0,0);
        image.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(Output,routeMode+"-"+label+".png"),image.EncodeToPNG());
        Destroy(image);
        RenderTexture.active=old;
        RenderTexture.ReleaseTemporary(buffer);
        cam.localPosition=position;
        cam.localRotation=rotation;
        Debug.Log("MOUNTAIN VIEW "+label+" eye="+point+" target="+target);
    }
}
#endif
