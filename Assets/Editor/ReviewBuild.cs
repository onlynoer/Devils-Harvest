using UnityEditor;
using UnityEditor.Build.Reporting;
using System.Linq;
using UnityEngine;

public static class ReviewBuild
{
    public static void Windows()
    {
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64,
            locationPathName = "Builds/Windows/DevilsHarvest.exe",
            options = BuildOptions.None,
            extraScriptingDefines = new[] { "HARVEST_PLAYTEST" }
        };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log("WINDOWS BUILD " + report.summary.result + " errors=" + report.summary.totalErrors + " warnings=" + report.summary.totalWarnings);
        if (report.summary.result != BuildResult.Succeeded) throw new System.Exception(report.summary.result.ToString());
    }
}
