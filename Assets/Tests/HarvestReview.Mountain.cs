#if HARVEST_PLAYTEST
using System.Collections;
using System.Linq;
using UnityEngine;

public sealed partial class HarvestReview
{
    static IEnumerator CoastalTrailCircuit()
    {
        var terrain = Terrain.activeTerrain;
        foreach (var view in new[] {
            ("beach", new Vector2(333,115), new Vector3(-.24f,-.025f,-1)),
            ("shoreline", new Vector2(330,70), new Vector3(.28f,-.052f,-1)),
            ("far-side", new Vector2(798,821), new Vector3(.3f,-.05f,1)),
            ("west-summit", new Vector2(746,730), new Vector3(-1,-.055f,.23f)),
            ("east-summit", new Vector2(831,739), new Vector3(.8f,-.055f,.65f)),
            ("hillside", new Vector2(416,648), new Vector3(-.71f,.13f,.65f)),
            ("trail-grade", new Vector2(387,647), new Vector3(.6f,-.045f,.8f)) })
        {
            Stage(view.Item2.x,view.Item2.y);
            yield return Frames(6);
            Face(gun.playerCamera.transform.position + view.Item3 * 200);
            yield return Frames(2);
            yield return Pic(routeMode + "-" + view.Item1);
        }
        if (routeMode == "coast-before") yield break;
        Vector3 across = new Vector3(.8f, 0, -.6f);
        Vector3 trail = new Vector3(380, 0, 640);
        float tread = terrain.SampleHeight(trail);
        float downhill = terrain.SampleHeight(trail - 16 * across);
        float uphill = terrain.SampleHeight(trail + 16 * across);
        Check(downhill < tread && tread < uphill && uphill-downhill < 9,
            $"northern tread lies between the two shoulders ({downhill:F1}, {tread:F1}, {uphill:F1} m)");
        var points = new[] {new Vector2(339,520),new Vector2(350,600),new Vector2(410,680),new Vector2(433,695),new Vector2(457,711),new Vector2(487,730)};
        Stage(points[0].x,points[0].y);
        yield return Frames(8);
        foreach (var point in points.Skip(1)) yield return Walk(point,1.1f);
        foreach (var point in points.Reverse().Skip(1)) yield return Walk(point,1.1f);
        Check(Vector2.Distance(XZ(p.transform.position),points[0]) < 1.2f,"graded northern trail walked in both directions");
        var camera = gun.playerCamera;
        var ocean = GameObject.Find("Ocean - Animated Coastal Water (Visual Only)").GetComponent<Renderer>();
        Check(camera.farClipPlane >= 24000 && ocean.bounds.extents.x >= camera.farClipPlane && ocean.bounds.extents.z >= camera.farClipPlane,
            "ocean extends past the distant haze in every heading");
        float rim = 0;
        for (int i = 0; i < terrain.terrainData.heightmapResolution; i++)
        {
            float u = (float)i / (terrain.terrainData.heightmapResolution - 1);
            foreach(var edge in new[] {new Vector2(0,u),new Vector2(1,u),new Vector2(u,0),new Vector2(u,1)})
                rim = Mathf.Max(rim,terrain.terrainData.GetInterpolatedHeight(edge.x,edge.y));
        }
        Check(rim + terrain.transform.position.y < ocean.transform.position.y - 1,"entire terrain perimeter submerged");
    }

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
