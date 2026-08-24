using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace RoyalDecisions.Editor
{
    /// <summary>Minimal, validation-free Android APK build for ad-hoc internal test distribution.</summary>
    public static class InternalTestApkBuilder
    {
        private const string OutputPath = "Builds/Android/RoyalDecisions-Android.apk";

        [MenuItem("Tools/Royal Decisions/Release/Build Internal Test APK")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? "Builds/Android");
            EditorUserBuildSettings.buildAppBundle = false;

            BuildPlayerOptions build = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray(),
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(build);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("Android build failed: " + report.summary.result);
            }
        }
    }
}
