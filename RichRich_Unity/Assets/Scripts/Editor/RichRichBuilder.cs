using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RichRich.Editor
{
    /// <summary>
    /// Automated build pipeline for RichRich Android APK release artifacts.
    /// Configures orientation, IL2CPP backend, ARM64 target architecture, package ID,
    /// and invokes BuildPipeline with deterministic release flags.
    /// </summary>
    public static class RichRichBuilder
    {
        public const string PackageIdentifier = "com.bishal.richrich";
        public const string ApplicationName = "RichRich";
        public const string VersionName = "0.1.0";
        public const int VersionCode = 1;
        public const string DefaultScenePath = "Assets/Scenes/MainGameplay.unity";
        public const string DefaultOutputPath = "Builds/Android/RichRich-v0.1.0-android-arm64.apk";

        [MenuItem("RichRich/Build Android Release APK")]
        public static void BuildAndroidRelease()
        {
            string outputPath = GetOutputPathArgument() ?? DefaultOutputPath;
            ConfigureAndroidPlayerSettings();

            string[] scenes = GetBuildScenes();
            Debug.Log($"[RichRichBuilder] Starting Android Release APK Build...");
            Debug.Log($"[RichRichBuilder] Output path: {outputPath}");
            Debug.Log($"[RichRichBuilder] Scenes included: {string.Join(", ", scenes)}");

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None // Strict release configuration: no development, no debugging
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                long sizeBytes = new FileInfo(outputPath).Length;
                double sizeMb = sizeBytes / (1024.0 * 1024.0);
                Debug.Log($"[RichRichBuilder] SUCCESS! Build completed in {summary.totalTime.TotalSeconds:F1}s.");
                Debug.Log($"[RichRichBuilder] Artifact: {outputPath} ({sizeMb:F2} MB, {sizeBytes} bytes)");
            }
            else
            {
                Debug.LogError($"[RichRichBuilder] FAILED with result: {summary.result}. Total errors: {summary.totalErrors}");
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        {
                            Debug.LogError($"[RichRichBuilder] {msg.content}");
                        }
                    }
                }

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        public static void ConfigureAndroidPlayerSettings()
        {
            // Configure external tool paths if present
            if (Directory.Exists(@"C:\Android\Sdk"))
            {
                EditorPrefs.SetString("AndroidSdkRoot", @"C:\Android\Sdk");
            }
            if (Directory.Exists(@"C:\Android\Sdk\ndk\28.2.13676358"))
            {
                EditorPrefs.SetString("AndroidNdkRoot", @"C:\Android\Sdk\ndk\28.2.13676358");
            }
            if (Directory.Exists(@"C:\Program Files\Microsoft\jdk-17.0.20.101-hotspot"))
            {
                EditorPrefs.SetString("JdkPath", @"C:\Program Files\Microsoft\jdk-17.0.20.101-hotspot");
            }

            PlayerSettings.productName = ApplicationName;
            PlayerSettings.bundleVersion = VersionName;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutoRotatePortrait = true;
            PlayerSettings.allowedAutoRotatePortraitUpsideDown = false;
            PlayerSettings.allowedAutoRotateLandscapeLeft = false;
            PlayerSettings.allowedAutoRotateLandscapeRight = false;

            // Application ID & Version code
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageIdentifier);
            PlayerSettings.Android.bundleVersionCode = VersionCode;

            // Target architecture: ARM64 (arm64-v8a)
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Scripting backend: IL2CPP
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

            // Managed code stripping: Low (safe against reflection drops)
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);

            // SDK Target Levels
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel28; // Android 9.0 Pie
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)35; // Android 15 Vanilla Ice Cream

            // Android export settings
            EditorUserBuildSettings.buildAppBundle = false; // Generate .apk
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
        }

        private static string[] GetBuildScenes()
        {
            if (File.Exists(DefaultScenePath))
            {
                return new[] { DefaultScenePath };
            }

            var enabledScenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (enabledScenes.Length > 0)
            {
                return enabledScenes;
            }

            throw new FileNotFoundException($"No valid scenes found for build. Expected scene at: {DefaultScenePath}");
        }

        private static string GetOutputPathArgument()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-buildOutput" || args[i] == "--output-path" || args[i] == "-o")
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
