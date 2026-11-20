using UnityEngine;

/// <summary>
/// Renders a prefab into a transparent square texture. Used as a fallback for items without an authored icon
/// and by the editor "Bake Icon" command. Never call this from OnValidate.
/// </summary>
public static class InventoryIconRenderer
{
    private const int PreviewLayer = 31;
    // Rendered far away from the level, with clip planes fitted to the model, so nothing else on the
    // preview layer (or a leftover preview object) can end up in the icon.
    private static readonly Vector3 StagePosition = new Vector3(0f, -10000f, 0f);

    /// <param name="keepReadable">True only when the pixels are needed afterwards (e.g. saving a PNG).</param>
    public static Texture2D Render(GameObject prefab, int resolution, Vector3 rotation, bool keepReadable = false)
    {
        if (prefab == null) return null;

        var previewRoot = new GameObject("Inventory Icon Preview") { hideFlags = HideFlags.HideAndDontSave };
        previewRoot.transform.position = StagePosition;
        previewRoot.SetActive(false);
        GameObject model = Object.Instantiate(prefab, previewRoot.transform);
        model.hideFlags = HideFlags.HideAndDontSave;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(rotation);
        model.transform.localScale = Vector3.one;
        SetLayerRecursively(model.transform, PreviewLayer);
        foreach (MonoBehaviour behaviour in model.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Rigidbody body in model.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        previewRoot.SetActive(true);

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Dispose(previewRoot);
            return null;
        }
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        var cameraObject = new GameObject("Inventory Icon Camera") { hideFlags = HideFlags.HideAndDontSave };
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.cullingMask = 1 << PreviewLayer;
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * .72f;
        float radius = Mathf.Max(.01f, bounds.extents.magnitude);
        float distance = Mathf.Max(2f, radius * 3f);
        camera.nearClipPlane = Mathf.Max(.01f, distance - radius * 1.5f);
        camera.farClipPlane = distance + radius * 1.5f;
        camera.transform.position = bounds.center + new Vector3(0f, 0f, -distance);
        camera.transform.rotation = Quaternion.identity;

        var lightObject = new GameObject("Inventory Icon Light") { hideFlags = HideFlags.HideAndDontSave };
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.cullingMask = 1 << PreviewLayer;
        light.transform.rotation = Quaternion.Euler(35f, -25f, 0f);

        var target = new RenderTexture(resolution, resolution, 16, RenderTextureFormat.ARGB32) { hideFlags = HideFlags.HideAndDontSave, antiAliasing = 2 };
        camera.targetTexture = target;
        camera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
        texture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        texture.Apply(false, !keepReadable); // non-readable frees the CPU-side copy
        RenderTexture.active = previous;

        camera.targetTexture = null;
        Dispose(target);
        Dispose(cameraObject);
        Dispose(lightObject);
        Dispose(previewRoot);
        return texture;
    }

    private static void Dispose(Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Object.Destroy(target);
        else Object.DestroyImmediate(target);
    }

    private static void SetLayerRecursively(Transform target, int layer)
    {
        target.gameObject.layer = layer;
        for (int i = 0; i < target.childCount; i++) SetLayerRecursively(target.GetChild(i), layer);
    }
}
