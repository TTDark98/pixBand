using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PixBand.EditorTools
{
    /// <summary>
    /// One-click builds. Menu: PixBand > Build Android APK / Build Linux Desktop.
    /// Batch mode:
    ///   Unity -batchmode -quit -projectPath . -executeMethod PixBand.EditorTools.PixBandBuild.BuildAndroidBatch
    /// </summary>
    public static class PixBandBuild
    {
        private static string[] Scenes
        {
            get
            {
                return new[]
                {
                    "Assets/Scenes/PixBand_Demo.unity"
                };
            }
        }

        [MenuItem("PixBand/Build Android APK")]
        public static void BuildAndroid()
        {
            PrepareAndroid();
            Build("Builds/pixBand.apk", BuildTarget.Android);
        }

        [MenuItem("PixBand/Build Linux Desktop")]
        public static void BuildLinux()
        {
            Build("Builds/Linux/pixBand.x86_64", BuildTarget.StandaloneLinux64);
        }

        public static void BuildAndroidBatch()
        {
            PrepareAndroid();
            Build("Builds/pixBand.apk", BuildTarget.Android);
        }

        private static void PrepareAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.pixband.vr");
            PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Scenes[0], true)
            };
        }

        private static void Build(string outputPath, BuildTarget target)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "Builds");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
                Debug.Log("[PixBand] Build succeeded: " + outputPath + " (" + (summary.totalSize / (1024 * 1024)) + " MB)");
            else
                Debug.LogError("[PixBand] Build failed: " + summary.result + " errors=" + summary.totalErrors);
        }
    }
}
