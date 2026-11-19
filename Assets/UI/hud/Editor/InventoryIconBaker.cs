using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Bakes an InventoryItemData's model prefab into an editable PNG sprite and assigns it as the item icon.</summary>
public static class InventoryIconBaker
{
    private const int Resolution = 256;

    [MenuItem("CONTEXT/InventoryItemData/Bake Icon From Prefab")]
    private static void BakeFromContext(MenuCommand command) => Bake(command.context as InventoryItemData);

    [MenuItem("Assets/Inventory/Bake Item Icons", false, 2000)]
    private static void BakeSelected()
    {
        foreach (Object selected in Selection.objects)
            if (selected is InventoryItemData item) Bake(item);
    }

    [MenuItem("Assets/Inventory/Bake Item Icons", true)]
    private static bool BakeSelectedValidate()
    {
        foreach (Object selected in Selection.objects)
            if (selected is InventoryItemData) return true;
        return false;
    }

    public static void Bake(InventoryItemData item)
    {
        if (item == null) return;
        if (item.ModelPrefab == null)
        {
            Debug.LogWarning($"'{item.name}' has no model prefab to bake an icon from.", item);
            return;
        }

        Texture2D texture = InventoryIconRenderer.Render(item.ModelPrefab, Resolution, item.IconRotation, keepReadable: true);
        if (texture == null)
        {
            Debug.LogWarning($"'{item.name}': the prefab has no renderers to bake.", item);
            return;
        }

        string itemPath = AssetDatabase.GetAssetPath(item);
        string iconPath = Path.Combine(Path.GetDirectoryName(itemPath) ?? "Assets", item.name + "_Icon.png").Replace('\\', '/');
        File.WriteAllBytes(iconPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceUpdate);

        if (AssetImporter.GetAtPath(iconPath) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        var serialized = new SerializedObject(item);
        serialized.FindProperty("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        serialized.ApplyModifiedProperties();
        AssetDatabase.SaveAssetIfDirty(item);
        Debug.Log($"Baked inventory icon for '{item.name}' to {iconPath}.", item);
    }
}
