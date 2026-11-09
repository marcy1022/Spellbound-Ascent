using UnityEditor;
using UnityEngine;
using System.Linq;

public class WebGLBuilder
{
    [MenuItem("Build/Build WebGL")]
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

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.defaultScreenWidth = 540;
        PlayerSettings.defaultScreenHeight = 960;

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Build/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        var result = BuildPipeline.BuildPlayer(options);
        if (result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log($"WebGL Build succeeded: {result.summary.totalSize} bytes");
        else
            Debug.LogError($"WebGL Build failed: {result.summary.result}");
    }
}
