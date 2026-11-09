using UnityEditor;
using UnityEngine;
using System.Linq;

public class WindowsBuilder
{
    [MenuItem("Build/Build Windows")]
    public static void Build()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            scenes = new[] { "Assets/_Game/Scenes/BattleScene.unity" };
        }

        PlayerSettings.defaultScreenWidth = 540;
        PlayerSettings.defaultScreenHeight = 960;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Build/Windows/SpellboundAscent.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var result = BuildPipeline.BuildPlayer(options);
        if (result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log($"Windows Build succeeded: {result.summary.totalSize} bytes");
        else
            Debug.LogError($"Windows Build failed: {result.summary.result}");
    }
}
