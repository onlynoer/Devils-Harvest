using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevilsHarvest.Editor
{
    [InitializeOnLoad]
    internal static class WildernessPlayModeGuard
    {
        static WildernessPlayModeGuard()
        {
            EditorApplication.playModeStateChanged += OnStateChanged;
        }

        private static void OnStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            var players = Object.FindObjectsByType<WildernessTestPlayer>();
            if (players.Length == 0) return; // Team scenes and the old prototype retain their own workflow.
            bool singleRig = players.Length == 1 && SceneManager.sceneCount == 1
                && Object.FindObjectsByType<Camera>().Count(c => c.isActiveAndEnabled) == 1
                && Object.FindObjectsByType<AudioListener>().Count(a => a.isActiveAndEnabled) == 1;
            if (singleRig) return;
            EditorApplication.isPlaying = false;
            Debug.LogError("Wilderness map test requires one scene, one active camera, and one Audio Listener. Exit Play mode and choose Tools > Devils Harvest > Island Release > Open Release Map Alone (Unity will prompt to save unsaved scenes). No team objects were disabled or changed.");
        }
    }
}
