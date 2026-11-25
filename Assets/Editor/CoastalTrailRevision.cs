using System;
using System.Collections.Generic;
using DevilsHarvest;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CoastalTrailRevision
{
    const string ScenePath = "Assets/Scenes/DevilsHarvest_Island.unity";
    const string TerrainPath = "Assets/Environment/Terrain/Tropical Editable Terrain.asset";
    const string OceanPath = "Assets/Environment/Meshes/Coastal Ocean.asset";

    // Survey stations along the original northern ascent, with the intended tread elevation.
    static readonly Vector3[] Stations = {
        new(339,52,520), new(344.5f,46,560), new(350,38.5f,600),
        new(365,36.5f,620), new(380,36,640), new(395,36.7f,660),
        new(410,38,680), new(434,41,695), new(458,48.5f,710),
        new(474,56.2f,720), new(490,63.5f,730)
    };

    static float Ease(float a, float b, float value) =>
        Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, value));

    static void Section(Vector2 p, out float side, out float grade, out float taper)
    {
        float nearest = float.PositiveInfinity;
        side = grade = taper = 0;
        for (int i = 1; i < Stations.Length; i++)
        {
            Vector3 a = Stations[i-1], b = Stations[i];
            Vector2 start = new(a.x, a.z), run = new(b.x-a.x, b.z-a.z);
            float along = Mathf.Clamp01(Vector2.Dot(p-start,run) / run.sqrMagnitude);
            Vector2 at = start + along*run, offset = p-at;
            float dist = offset.magnitude;
            if (dist >= nearest) continue;
            nearest = dist;
            side = dist * (offset.x*run.y >= offset.y*run.x ? 1 : -1);
            grade = Mathf.Lerp(a.y,b.y,along);
            taper = Ease(520,549,at.y) * (1-Ease(708,730,at.y));
        }
    }

    static void Grade(TerrainData data)
    {
        int n = data.heightmapResolution;
        var heights = data.GetHeights(0,0,n,n);
        for (int z=0; z<n; z++)
        for (int x=0; x<n; x++)
        {
            Vector2 p = new(x*data.size.x/(n-1),z*data.size.z/(n-1));
            if (p.x<285 || p.x>545 || p.y<500 || p.y>759) continue;
            Section(p, out float side, out float grade, out float taper);
            float distance = Mathf.Abs(side);
            // A shallow crown drains onto a lower shoulder. The opposite bank climbs above the cut.
            float tread = grade - .025f*Mathf.Min(distance,4);
            float bank = side>0 ? 4.6f : -2.4f;
            float target = tread + bank*Ease(3,19,distance);
            float influence = taper * (1-Ease(13,43,distance));
            heights[z,x] = Mathf.Lerp(heights[z,x],target/data.size.y,influence);
        }
        data.SetHeights(0,0,heights);

        int soil = Array.FindIndex(data.terrainLayers, l => l && l.name=="Tropical Forest Floor");
        if (soil<0) throw new InvalidOperationException("Missing Tropical Forest Floor terrain layer");
        var map = data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
        for (int z=0; z<data.alphamapHeight; z++)
        for (int x=0; x<data.alphamapWidth; x++)
        {
            Vector2 p = new(x*data.size.x/(data.alphamapWidth-1),z*data.size.z/(data.alphamapHeight-1));
            if (p.x<330 || p.x>500 || p.y<520 || p.y>731) continue;
            Section(p,out float side,out _,out float taper);
            float width = 2.8f + Mathf.PerlinNoise(p.x*.08f,p.y*.08f);
            float blend = .84f*taper*(1-Ease(1.4f,width,Mathf.Abs(side)));
            if (blend<=0) continue;
            for (int i=0;i<data.alphamapLayers;i++)
                map[z,x,i] = Mathf.Lerp(map[z,x,i], i==soil ? 1 : 0, blend);
        }
        data.SetAlphamaps(0,0,map);
    }

    static bool GroundedRoot(Transform t)
    {
        if (t.GetComponent<LODGroup>() || t.GetComponent<Collider>() || (t.childCount==0 && t.GetComponent<WildernessRegionMarker>()))
            return true;
        // The documented island rock and deadwood placements own their mesh children as a unit.
        return t.name.StartsWith("Headland ") || t.name.StartsWith("Collapsed Sanctuary Rubble ")
            || t.name.StartsWith("Trail Edge Cairn") || t.name.StartsWith("Cairn Base")
            || t.name.StartsWith("Cairn Cap");
    }

    static Dictionary<Transform,float> CaptureAnchors(Terrain terrain)
    {
        var anchors = new Dictionary<Transform,float>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            Vector3 p=t.position;
            if (p.x<285 || p.x>545 || p.z<500 || p.z>759 || !GroundedRoot(t)) continue;
            Section(new Vector2(p.x,p.z),out float side,out _,out float taper);
            if (taper<=0 || Mathf.Abs(side)>=43) continue;
            if (t.root.name!="Map_Objects" && !t.GetComponent<CharacterController>()) continue;
            bool owned=false;
            for (var parent=t.parent; parent; parent=parent.parent)
                if (GroundedRoot(parent)) { owned=true; break; }
            if (!owned) anchors.Add(t,p.y-terrain.SampleHeight(p));
        }
        return anchors;
    }

    public static void ExtendOcean()
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(OceanPath);
        const int gridSide = 101;
        if (!mesh || mesh.vertexCount != gridSide*gridSide)
            throw new InvalidOperationException("Expected the original 101 by 101 coastal grid");
        var vertices = new List<Vector3>(mesh.vertices);
        var normals = new List<Vector3>(mesh.normals);
        var triangles = new List<int>(mesh.triangles);
        var rim = new List<int>();
        for (int z=0; z<gridSide; z++)
        for (int x=0; x<gridSide; x++)
        {
            int i = z*gridSide+x;
            vertices[i] = new Vector3((x-50)*24, 0, (z-50)*24);
            if (x==0 || x==100 || z==0 || z==100) rim.Add(i);
        }
        rim = rim.OrderBy(i=>Mathf.Atan2(vertices[i].z,vertices[i].x)).ToList();
        // Concentric rings give each view ray one transparent surface at the horizon.
        // The first ring is level with the sea. The next two carry the atmospheric fade.
        foreach (var ring in new[] {new Vector2(4200,0), new Vector2(14000,1650), new Vector2(45200,7000)})
        {
            var outer = new List<int>(rim.Count);
            foreach (int i in rim)
            {
                var direction = new Vector2(vertices[i].x,vertices[i].z).normalized;
                outer.Add(vertices.Count);
                vertices.Add(new Vector3(direction.x*ring.x,ring.y,direction.y*ring.x));
                normals.Add(Vector3.up);
            }
            for (int i=0; i<rim.Count; i++)
            {
                int next=(i+1)%rim.Count;
                triangles.AddRange(new[] {rim[i],rim[next],outer[i],rim[next],outer[next],outer[i]});
            }
            rim=outer;
        }
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles,0);
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssetIfDirty(mesh);
        Debug.Log($"COAST ocean saved {mesh.vertexCount} vertices, bounds {mesh.bounds}");
    }

    public static void Apply()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var terrain=Terrain.activeTerrain;
        if (!terrain || AssetDatabase.GetAssetPath(terrain.terrainData)!=TerrainPath || terrain.transform.position!=Vector3.zero)
            throw new InvalidOperationException("Expected island terrain at origin in "+scene.path);
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(OceanPath);
        var water=Object.FindObjectsByType<MeshFilter>().SingleOrDefault(f=>f.sharedMesh==mesh);
        var camera=Object.FindObjectsByType<Camera>().SingleOrDefault(c=>c.name=="MainCamera");
        if (!mesh || !water || !camera || camera.gameObject.scene!=scene)
            throw new InvalidOperationException("Missing coastal mesh, water instance or player camera in "+scene.path);
        if (Mathf.Abs(mesh.bounds.extents.x-1200)>1)
            throw new InvalidOperationException("This authoring pass requires the original coastal grid");
        var anchors=CaptureAnchors(terrain);
        Grade(terrain.terrainData);
        terrain.Flush();
        int moved=0;
        foreach (var entry in anchors)
        {
            var t=entry.Key;
            var p=t.position;
            Section(new Vector2(p.x,p.z),out float side,out _,out float taper);
            if (t.GetComponent<LODGroup>() && Mathf.Abs(side)<3.6f && taper>.4f)
                p.x += (side<0 ? -1 : 1)*(6-Mathf.Abs(side))*1.9f;
            p.y=terrain.SampleHeight(p)+entry.Value;
            if (Vector3.Distance(p,t.position)<.025f) continue;
            t.position=p;
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            moved++;
        }
        ExtendOcean();
        camera.farClipPlane=24000;
        PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(terrain.terrainData);
        AssetDatabase.SaveAssetIfDirty(terrain.terrainData);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save "+scene.path);
        Debug.Log($"COAST saved {scene.path}, reseated {moved} placements, ocean bounds {mesh.bounds}, clip {camera.farClipPlane}");
    }
}
