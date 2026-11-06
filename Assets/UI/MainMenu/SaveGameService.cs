using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Mutable progress payload stored with a world save.</summary>
[Serializable]
public sealed class SaveProgress
{
    public string currentScene = "";
    public string startingLevelId = "main-level";
    public int day = 1;
    public int harvestedItems;
    // Game systems can serialize additional versioned progress here without changing this save format.
    public string customData = "{}";
}

/// <summary>Serializable save record for one named world.</summary>
[Serializable]
public sealed class SaveGameData
{
    public string id;
    public string saveName;
    public string lastPlayedUtc;
    public double timePlayedSeconds;
    public SaveProgress progress = new SaveProgress();
}

/// <summary>Creates, loads, lists, and updates JSON world saves in persistentDataPath/DevilsHarvest/Saves.</summary>
public static class SaveGameService
{
    private const string FolderName = "DevilsHarvest/Saves";
    private const string ActiveSavePreferenceKey = "DevilsHarvest.ActiveSaveId";
    private static SaveGameData activeSave;
    private static double lastTimerTick;

    public static SaveGameData ActiveSave => activeSave;

    public static SaveGameData Create(string name, string startingLevelId = "main-level")
    {
        string cleanName = (name ?? "").Trim();
        if (cleanName.Length == 0) throw new ArgumentException("Enter a name for this harvest."); //text to display if no name is entered when making a new save
        var save = new SaveGameData
        {
            id = Guid.NewGuid().ToString("N"),
            saveName = cleanName,
            lastPlayedUtc = DateTime.UtcNow.ToString("o"),
            timePlayedSeconds = 0,
            progress = new SaveProgress { startingLevelId = string.IsNullOrEmpty(startingLevelId) ? "main-level" : startingLevelId }
        };
        Write(save);
        SetActive(save);
        return save;
    }

    public static List<SaveGameData> GetAll()
    {
        var saves = new List<SaveGameData>();
        string directory = SaveDirectory;
        if (!Directory.Exists(directory)) return saves;
        foreach (string path in Directory.GetFiles(directory, "*.json"))
        {
            try
            {
                var save = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path));
                if (save != null && !string.IsNullOrEmpty(save.id) && save.progress != null)
                {
                    if (string.IsNullOrEmpty(save.progress.startingLevelId)) save.progress.startingLevelId = "main-level";
                    saves.Add(save);
                }
            }
            catch (Exception exception) { Debug.LogWarning("Could not read save " + Path.GetFileName(path) + ": " + exception.Message); }
        }
        saves.Sort((a, b) => string.CompareOrdinal(b.lastPlayedUtc, a.lastPlayedUtc));
        return saves;
    }

    public static bool Load(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        foreach (var save in GetAll())
            if (save.id == id)
            {
                save.lastPlayedUtc = DateTime.UtcNow.ToString("o");
                if (save.progress == null) save.progress = new SaveProgress();
                Write(save);
                SetActive(save);
                return true;
            }
        return false;
    }

    /// <summary>Deletes one save file and clears the active-save preference when it points to that save.</summary>
    public static bool Delete(string id)
    {
        Guid parsedId;
        if (!Guid.TryParseExact(id, "N", out parsedId)) return false;

        string path = Path.Combine(SaveDirectory, parsedId.ToString("N") + ".json");
        if (!File.Exists(path)) return false;

        bool wasActive = activeSave != null && activeSave.id == id;
        if (wasActive) FlushTime();

        File.Delete(path);

        if (wasActive) activeSave = null;
        if (PlayerPrefs.GetString(ActiveSavePreferenceKey, "") == id)
        {
            PlayerPrefs.DeleteKey(ActiveSavePreferenceKey);
            PlayerPrefs.Save();
        }

        return true;
    }

    public static void SaveActive()
    {
        if (activeSave == null) return;
        activeSave.lastPlayedUtc = DateTime.UtcNow.ToString("o");
        Write(activeSave);
    }

    public static void AddHarvestedItems(int amount)
    {
        if (activeSave == null || activeSave.progress == null) return;
        activeSave.progress.harvestedItems = Math.Max(0, activeSave.progress.harvestedItems + amount);
        SaveActive();
    }

    public static void SetProgress(int day, string currentScene, string customDataJson = null)
    {
        if (activeSave == null) return;
        if (activeSave.progress == null) activeSave.progress = new SaveProgress();
        activeSave.progress.day = Math.Max(1, day);
        if (currentScene != null) activeSave.progress.currentScene = currentScene;
        if (!string.IsNullOrEmpty(customDataJson)) activeSave.progress.customData = customDataJson;
        SaveActive();
    }

    // saves to locallow
    private static string SaveDirectory => Path.Combine(Application.persistentDataPath, FolderName);

    private static void SetActive(SaveGameData save)
    {
        activeSave = save;
        lastTimerTick = Time.realtimeSinceStartupAsDouble;
        SaveGameRuntime.EnsureExists();
    }

    private static void Write(SaveGameData save)
    {
        // Write a temporary file first so an interrupted write does not leave a partially written save.
        Directory.CreateDirectory(SaveDirectory);
        string path = Path.Combine(SaveDirectory, save.id + ".json");
        string tempPath = path + ".tmp";
        File.WriteAllText(tempPath, JsonUtility.ToJson(save, true));
        if (File.Exists(path)) File.Delete(path);
        File.Move(tempPath, path);
    }

    internal static void TickTime()
    {
        if (activeSave == null) return;
        double now = Time.realtimeSinceStartupAsDouble;
        double elapsed = Math.Max(0, now - lastTimerTick);
        lastTimerTick = now;
        if (SceneManager.GetActiveScene().name != "MainMenu") activeSave.timePlayedSeconds += elapsed;
    }

    internal static void FlushTime() { TickTime(); SaveActive(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RestoreSession()
    {
        string id = PlayerPrefs.GetString(ActiveSavePreferenceKey, "");
        if (!string.IsNullOrEmpty(id)) Load(id);
    }

    public static void SelectAsActive(SaveGameData save)
    {
        if (save == null) return;
        PlayerPrefs.SetString(ActiveSavePreferenceKey, save.id);
        PlayerPrefs.Save();
    }

    private sealed class SaveGameRuntime : MonoBehaviour
    {
        private static SaveGameRuntime instance;
        internal static void EnsureExists()
        {
            if (instance != null) return;
            var host = new GameObject("SaveGameRuntime");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<SaveGameRuntime>();
        }
        private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
        private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu" || activeSave == null) return;
            if (activeSave.progress == null) activeSave.progress = new SaveProgress();
            activeSave.progress.currentScene = scene.name;
            SaveActive();
        }
        private void Update()
        {
            // Unscaled realtime keeps playtime accurate while gameplay time is paused.
            TickTime();
            if (Time.frameCount % 900 == 0) SaveActive();
        }
        private void OnApplicationPause(bool paused) { if (paused) FlushTime(); }
        private void OnApplicationQuit() { FlushTime(); }
    }
}
