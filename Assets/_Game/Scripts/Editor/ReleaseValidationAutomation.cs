using System;
using System.Diagnostics;
using System.IO;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    /// <summary>Read-only Android release gate plus explicit development/release build commands.</summary>
    public static class ReleaseValidationAutomation
    {
        public const string ReportPath = "Logs/Build/RoyalDecisionsReleaseValidation.json";
        private const string ExpectedVersion = "6000.3.20f1";
        private const string ExpectedIdentifier = "com.arilla.royaldecisions";

        [MenuItem("Tools/Royal Decisions/Release/Validate Android")]
        public static void ValidateMenu() => WriteAndLog(Validate());

        public static void ValidateBatch()
        {
            SceneSetupAutomation.ValidateBatch();
            ReleaseValidationReport report = Validate();
            WriteAndLog(report);
            if (!report.Succeeded)
            {
                throw new BuildFailedException(
                    "Release validation failed. See " + ReportPath + ".");
            }
        }

        [MenuItem("Tools/Royal Decisions/Release/Build Development APK")]
        public static void BuildDevelopmentAndroid()
        {
            BuildAndroid("Builds/Android/Development/RoyalDecisions.apk", BuildOptions.Development);
        }

        [MenuItem("Tools/Royal Decisions/Release/Build Unsigned Release AAB")]
        public static void BuildReleaseAndroid()
        {
            if (!PlayerSettings.Android.useCustomKeystore
                || string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName))
            {
                throw new BuildFailedException(
                    "Release signing is not configured locally; no credential was invented.");
            }
            bool previous = EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle = true;
                BuildAndroid("Builds/Android/Release/RoyalDecisions.aab", BuildOptions.None);
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = previous;
            }
        }

        public static ReleaseValidationReport Validate()
        {
            ReleaseValidationReport report = new ReleaseValidationReport
            {
                unityVersion = UnityEngine.Application.unityVersion
            };
            Require(report, UnityEngine.Application.unityVersion == ExpectedVersion,
                "UNITY_VERSION", "Unity must be exactly " + ExpectedVersion + ".");
            Require(report,
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)
                    == ExpectedIdentifier,
                "APPLICATION_IDENTIFIER",
                "Android identifier must be exactly " + ExpectedIdentifier + ".");
            bool portraitOnly = PlayerSettings.defaultInterfaceOrientation
                    == UIOrientation.Portrait
                || (PlayerSettings.defaultInterfaceOrientation == UIOrientation.AutoRotation
                    && PlayerSettings.allowedAutorotateToPortrait
                    && !PlayerSettings.allowedAutorotateToPortraitUpsideDown
                    && !PlayerSettings.allowedAutorotateToLandscapeLeft
                    && !PlayerSettings.allowedAutorotateToLandscapeRight);
            Require(report, portraitOnly, "ORIENTATION", "Android orientation must be portrait-only.");
            Require(report,
                (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0,
                "ARM64", "Android ARM64 must be enabled.");
            Require(report,
                PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)
                    == ScriptingImplementation.IL2CPP,
                "IL2CPP", "Android scripting backend must be IL2CPP.");
            ValidateInputSystem(report);
            ValidateBuildScenes(report);
            ValidateContent(report);
            ValidateFont(report);
            ValidateTrackedFiles(report);
            if (!PlayerSettings.Android.useCustomKeystore)
            {
                report.Add(ReleaseValidationIssueSeverity.Information,
                    "SIGNING_MANUAL_BLOCKER",
                    "Local release signing is not configured; release build remains a manual blocker.");
            }
            return report;
        }

        private static void ValidateBuildScenes(ReleaseValidationReport report)
        {
            string[] expected =
            {
                SceneSetupAutomation.BootstrapScenePath,
                SceneSetupAutomation.MainMenuScenePath,
                SceneSetupAutomation.GameScenePath
            };
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            bool exact = scenes.Length == expected.Length;
            for (int i = 0; exact && i < expected.Length; i++)
            {
                exact = scenes[i].enabled && scenes[i].path == expected[i];
            }
            Require(report, exact, "BUILD_SCENES",
                "Enabled build scenes must be Bootstrap, MainMenu, Game in exact order.");
        }

        private static void ValidateInputSystem(ReleaseValidationReport report)
        {
            UnityEngine.Object[] settings = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/ProjectSettings.asset");
            SerializedProperty property = settings.Length > 0
                ? new SerializedObject(settings[0]).FindProperty("activeInputHandler")
                : null;
            Require(report, property != null && property.intValue != 0,
                "INPUT_SYSTEM", "The Input System must be enabled.");
        }

        private static void ValidateContent(ReleaseValidationReport report)
        {
            ContentCatalogue catalogue = AssetDatabase.LoadAssetAtPath<ContentCatalogue>(
                SceneSetupAutomation.CataloguePath);
            Require(report, catalogue != null, "CATALOGUE", "Content catalogue is missing.");
            if (catalogue == null)
            {
                return;
            }
            ContentValidationReport content = ProjectContentAudit.Validate(catalogue);
            Require(report, content.ErrorCount == 0 && content.WarningCount == 0,
                "CONTENT_VALIDATION",
                "Content validation must have zero errors and warnings; found "
                + content.ErrorCount + "/" + content.WarningCount + ".");
        }

        private static void ValidateFont(ReleaseValidationReport report)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                SceneSetupAutomation.TurkishFontPath);
            Require(report,
                TurkishGlyphValidator.TryValidate(font, out _),
                "TURKISH_FONT", "Project Turkish TMP font is missing required glyphs.");
            Require(report,
                AssetDatabase.LoadAssetAtPath<InterfaceTextDefinition>(
                    SceneSetupAutomation.InterfaceTextPath) != null,
                "INTERFACE_TEXT", "Turkish interface text asset is missing.");
        }

        private static void ValidateTrackedFiles(ReleaseValidationReport report)
        {
            string tracked = RunGit("ls-files");
            string[] lines = tracked.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string path = lines[i].Replace('\\', '/');
                string extension = Path.GetExtension(path).ToLowerInvariant();
                bool secret = extension == ".jks" || extension == ".keystore"
                    || path.IndexOf("credential", StringComparison.OrdinalIgnoreCase) >= 0;
                bool generated = path.StartsWith("Logs/", StringComparison.Ordinal)
                    || path.StartsWith("Library/", StringComparison.Ordinal)
                    || path.StartsWith("Temp/", StringComparison.Ordinal)
                    || path.StartsWith("Builds/", StringComparison.Ordinal)
                    || extension == ".apk" || extension == ".aab";
                if (secret || generated)
                {
                    report.Add(ReleaseValidationIssueSeverity.Error,
                        secret ? "TRACKED_SECRET" : "TRACKED_GENERATED_OUTPUT",
                        "Tracked release-prohibited file: " + path);
                }
            }
        }

        private static string RunGit(string arguments)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo("git", arguments)
                {
                    WorkingDirectory = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..")),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (Process process = Process.Start(info))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return process.ExitCode == 0 ? output : string.Empty;
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning("Release git audit unavailable: " + exception.Message);
                return string.Empty;
            }
        }

        private static void BuildAndroid(string outputPath, BuildOptions options)
        {
            ValidateBatch();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            BuildPlayerOptions build = new BuildPlayerOptions
            {
                scenes = Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path),
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = options
            };
            BuildReport report = BuildPipeline.BuildPlayer(build);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException("Android build failed: " + report.summary.result);
            }
        }

        private static void Require(
            ReleaseValidationReport report,
            bool condition,
            string code,
            string message)
        {
            if (!condition)
            {
                report.Add(ReleaseValidationIssueSeverity.Error, code, message);
            }
        }

        private static void WriteAndLog(ReleaseValidationReport report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            UnityEngine.Debug.Log(
                "[ReleaseValidation] errors=" + report.ErrorCount
                + " warnings=" + report.WarningCount + " report=" + ReportPath);
        }
    }
}
