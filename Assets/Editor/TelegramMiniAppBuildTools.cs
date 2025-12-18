#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TelegramMiniApp.Editor
{
    public static class TelegramMiniAppBuildTools
    {
        private const string TemplateId = "PROJECT:Telegram";
        private const string DefaultScenePath = "Assets/Scenes/Main.unity";
        private const string DefaultOutputDir = "Builds/TelegramWebGL";
        private const string GitHubPagesOutputDir = "docs";

        [MenuItem("Tools/Telegram Mini App/Configure WebGL")]
        public static void ConfigureWebGL()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            PlayerSettings.WebGL.template = TemplateId;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.runInBackground = true;

            TrySetWebGLBool("threadsSupport", false);
            TrySetWebGLBool("dataCaching", false);

            Debug.Log("Telegram Mini App: WebGL configured (template + compression disabled).");
        }

        [MenuItem("Tools/Telegram Mini App/Build WebGL")]
        public static void BuildWebGL()
        {
            ConfigureWebGL();
            EnsureSceneInBuildSettings(DefaultScenePath);

            var enabledScenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            Directory.CreateDirectory(DefaultOutputDir);

            var options = new BuildPlayerOptions
            {
                scenes = enabledScenes,
                locationPathName = DefaultOutputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"WebGL build failed: {report.summary.result}");

            Debug.Log($"Telegram Mini App: WebGL build done -> {Path.GetFullPath(DefaultOutputDir)}");
        }

        [MenuItem("Tools/Telegram Mini App/Build WebGL (GitHub Pages)")]
        public static void BuildWebGLForGitHubPages()
        {
            ConfigureWebGL();
            EnsureSceneInBuildSettings(DefaultScenePath);

            var enabledScenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            PrepareGitHubPagesOutput(GitHubPagesOutputDir);

            var options = new BuildPlayerOptions
            {
                scenes = enabledScenes,
                locationPathName = GitHubPagesOutputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"WebGL build failed: {report.summary.result}");

            File.WriteAllText(Path.Combine(GitHubPagesOutputDir, ".nojekyll"), string.Empty);

            Debug.Log($"Telegram Mini App: GitHub Pages build done -> {Path.GetFullPath(GitHubPagesOutputDir)}");
            Debug.Log("GitHub Pages: Settings -> Pages -> Build and deployment -> Source: Deploy from a branch -> /docs");
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            if (EditorBuildSettings.scenes.Any(s => string.Equals(s.path, scenePath, StringComparison.OrdinalIgnoreCase)))
                return;

            if (!File.Exists(scenePath))
                throw new FileNotFoundException($"Scene not found: {scenePath}");

            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void PrepareGitHubPagesOutput(string outputDir)
        {
            Directory.CreateDirectory(outputDir);

            var knownDirs = new[] { "Build", "TemplateData", "StreamingAssets" };
            foreach (var dir in knownDirs)
            {
                var path = Path.Combine(outputDir, dir);
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }

            var knownFiles = new[]
            {
                "index.html",
                "favicon.ico",
                "manifest.webmanifest",
                "ServiceWorker.js",
                ".nojekyll"
            };

            foreach (var file in knownFiles)
            {
                var path = Path.Combine(outputDir, file);
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static void TrySetWebGLBool(string propertyName, bool value)
        {
            try
            {
                var type = typeof(PlayerSettings.WebGL);
                var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
                if (prop is null || prop.PropertyType != typeof(bool) || !prop.CanWrite)
                    return;

                prop.SetValue(null, value);
            }
            catch
            {
                // ignored
            }
        }
    }
}
#endif
