using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ColorFlow.Editor
{
    /// <summary>
    /// Point d'entrée du build Android debug, utilisable en GUI (menu) et en
    /// batchmode :
    ///
    ///   Unity -batchmode -nographics -quit -projectPath . -buildTarget Android \
    ///         -executeMethod ColorFlow.Editor.BuildScript.BuildAndroidDebug \
    ///         -logFile Builds/build.log
    ///
    /// Sortie : Builds/Android/colorflow-debug.apk
    /// </summary>
    public static class BuildScript
    {
        public const string OutputDirectory = "Builds/Android";
        public const string OutputPath = OutputDirectory + "/colorflow-debug.apk";
        public const string PackageName = "com.digitalholding.colorflow";

        private const string LogPrefix = "[ColorFlow][Build]";

        [MenuItem("Color Flow/Build Android Debug")]
        public static void BuildAndroidDebugMenu()
        {
            BuildAndroidDebug();
        }

        public static void BuildAndroidDebug()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                ConfigurePlayerSettings();

                var scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();

                if (scenes.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Aucune scène activée dans les Build Settings (File > Build Settings).");
                }

                Directory.CreateDirectory(OutputDirectory);
                if (File.Exists(OutputPath))
                {
                    File.Delete(OutputPath);
                }

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = OutputPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.Development
                };

                Debug.LogFormat(
                    "{0} Début du build Android debug — Unity {1} — {2} scène(s) — sortie {3}",
                    LogPrefix, Application.unityVersion, scenes.Length, OutputPath);

                var report = BuildPipeline.BuildPlayer(options);
                stopwatch.Stop();

                var summary = report.summary;
                var duration = summary.totalTime.TotalSeconds;

                if (summary.result == BuildResult.Succeeded && File.Exists(OutputPath))
                {
                    var bytes = new FileInfo(OutputPath).Length;
                    Debug.LogFormat(
                        "{0} SUCCÈS — APK={1} — taille={2} — durée={3:0.0}s — erreurs={4} — avertissements={5}",
                        LogPrefix, OutputPath, FormatSize(bytes), duration,
                        summary.totalErrors, summary.totalWarnings);
                    ExitBatch(0);
                    return;
                }

                Debug.LogErrorFormat(
                    "{0} ÉCHEC — résultat={1} — APK présent={2} — durée={3:0.0}s — erreurs={4} — avertissements={5}",
                    LogPrefix, summary.result, File.Exists(OutputPath), duration,
                    summary.totalErrors, summary.totalWarnings);
                ExitBatch(1);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                Debug.LogErrorFormat(
                    "{0} EXCEPTION après {1:0.0}s — {2}",
                    LogPrefix, stopwatch.Elapsed.TotalSeconds, exception);
                ExitBatch(1);
            }
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Digital Holding";
            PlayerSettings.productName = "Color Flow";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = Math.Max(1, PlayerSettings.Android.bundleVersionCode);

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // APK (pas d'AAB) pour `adb install` et le transfert manuel.
            EditorUserBuildSettings.buildAppBundle = false;
        }

        private static string FormatSize(long bytes)
        {
            return (bytes / (1024.0 * 1024.0)).ToString("0.00") + " Mo (" + bytes + " octets)";
        }

        private static void ExitBatch(int exitCode)
        {
            // En GUI, on laisse l'éditeur ouvert : le log suffit.
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
        }
    }
}
