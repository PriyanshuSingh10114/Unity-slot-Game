#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SlotGame.Editor
{
    /// <summary>
    /// Editor build automation for exporting the slot game to WebGL.
    /// Can be invoked directly from the Unity Editor menu or from the command line in batchmode:
    /// Unity.exe -quit -batchmode -projectPath . -executeMethod SlotGame.Editor.SlotGameBuildScript.BuildWebGL
    /// </summary>
    public static class SlotGameBuildScript
    {
        private const string SCENE_PATH = "Assets/Scenes/MainScene.unity";
        private const string BUILD_OUTPUT_PATH = "Build/WebGL";

        [MenuItem("Build/Build WebGL")]
        public static void BuildWebGL()
        {
            Debug.Log("[SlotGameBuildScript] Starting WebGL Build...");

            // Ensure the main scene is saved and valid
            if (!File.Exists(SCENE_PATH))
            {
                Debug.LogWarning($"[SlotGameBuildScript] Scene not found at {SCENE_PATH}. Rebuilding scene...");
                SlotGameSceneBuilder.BuildMainScene();
            }

            // Ensure output directory exists
            Directory.CreateDirectory(BUILD_OUTPUT_PATH);

            // Configure build options
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { SCENE_PATH },
                locationPathName = BUILD_OUTPUT_PATH,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[SlotGameBuildScript] WebGL Build SUCCEEDED: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1}s.");
                Debug.Log($"[SlotGameBuildScript] Output written to {Path.GetFullPath(BUILD_OUTPUT_PATH)}");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[SlotGameBuildScript] WebGL Build FAILED with {summary.totalErrors} errors.");
            }
        }
    }
}
#endif
