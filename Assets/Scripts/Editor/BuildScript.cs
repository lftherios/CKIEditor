using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CKIEditor.EditorTools
{
    /// <summary>
    /// One-command standalone builds - from the Build menu in the editor, or
    /// headless via `-batchmode -executeMethod BuildScript.BuildAll`.
    /// CI (GitHub Actions + game-ci) uses its own build runner; this script is
    /// for building locally without remembering any settings.
    /// </summary>
    public static class BuildScript
    {
        private const string APP_NAME = "Cirklon2 Desktop App";
        private static readonly string[] Scenes = { "Assets/Scenes/MainScene.unity" };

        [MenuItem("Build/macOS")]
        public static void BuildMac()
        {
            Build(BuildTarget.StandaloneOSX, $"Builds/macOS/{APP_NAME}.app");
        }

        [MenuItem("Build/Windows (x64)")]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, $"Builds/Windows/{APP_NAME}/{APP_NAME}.exe");
        }

        [MenuItem("Build/Linux (x64)")]
        public static void BuildLinux()
        {
            Build(BuildTarget.StandaloneLinux64, $"Builds/Linux/{APP_NAME}/Cirklon2DesktopApp.x86_64");
        }

        [MenuItem("Build/All platforms")]
        public static void BuildAll()
        {
            BuildMac();
            BuildWindows();
            BuildLinux();
        }

        private static void Build(BuildTarget target, string path)
        {
            var report = BuildPipeline.BuildPlayer(Scenes, path, target, BuildOptions.None);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {path} ({report.summary.totalSize / (1024 * 1024)} MB)");
                return;
            }

            Debug.LogError($"Build failed for {target}: {report.summary.totalErrors} errors");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }
}
