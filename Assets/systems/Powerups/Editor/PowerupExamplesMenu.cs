using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Creates the starter powerup assets (Speed Boost, High Jump). Skips any that already exist.</summary>
internal static class PowerupExamplesMenu
{
    private const string Folder = "Assets/systems/Powerups/Data";

    [MenuItem("Tools/Powerups/Create Example Powerups")]
    private static void CreateExamples()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder(Path.GetDirectoryName(Folder).Replace('\\', '/'), Path.GetFileName(Folder));

        Create("Speed Boost", new Color(.3f, .8f, 1f), 5f, PowerupStat.MoveSpeed, 1.6f);
        Create("High Jump", new Color(.5f, 1f, .4f), 8f, PowerupStat.JumpHeight, 2.5f);
        AssetDatabase.SaveAssets();
    }

    private static void Create(string displayName, Color color, float duration, PowerupStat stat, float multiplier)
    {
        string path = Folder + "/" + displayName.Replace(" ", "") + ".asset";
        if (AssetDatabase.LoadAssetAtPath<PowerupData>(path) != null)
        {
            Debug.Log("Powerup already exists, skipped: " + path);
            return;
        }
        var data = ScriptableObject.CreateInstance<PowerupData>();
        data.displayName = displayName;
        data.color = color;
        data.duration = duration;
        data.modifiers = new[] { new PowerupModifier { stat = stat, multiplier = multiplier } };
        AssetDatabase.CreateAsset(data, path);
        Debug.Log("Created powerup: " + path);
    }
}
