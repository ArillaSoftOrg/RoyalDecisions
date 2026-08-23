using RoyalDecisions.Composition;
using RoyalDecisions.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Repoints the Game scene's <see cref="GameSceneController"/> at a given content catalogue.
    /// </summary>
    /// <remarks>
    /// Deliberately separate from <see cref="SceneSetupAutomation"/>: that tool's default output —
    /// the placeholder catalogue wired in, everything else about the scene untouched — must keep
    /// working exactly as it did before this file existed, for anyone who has not asked for the
    /// story. This one is opt-in, touches only the one serialized reference named below, and does
    /// so through <see cref="SerializedObject"/> the same way <see cref="ContentAuthoringWindow"/>
    /// already edits catalogue references — never by hand-editing scene YAML.
    /// </remarks>
    public static class StorySceneWiring
    {
        public const string StoryCataloguePath =
            StoryContentGenerator.DefaultRoot + "/" + StoryContentGenerator.CatalogueAssetName;

        private const string CatalogueFieldName = "catalogue";
        private const string GameControllerPath = "/GameSceneController";

        [MenuItem("Tools/Royal Decisions/Scene Setup/Use Story Catalogue In Game Scene")]
        public static void UseStoryCatalogueMenu()
        {
            LogReport(SetCatalogue(StoryCataloguePath));
        }

        [MenuItem("Tools/Royal Decisions/Scene Setup/Use Placeholder Catalogue In Game Scene")]
        public static void UsePlaceholderCatalogueMenu()
        {
            LogReport(SetCatalogue(SceneSetupAutomation.CataloguePath));
        }

        /// <summary>CLI entry point for <c>-executeMethod</c>: exits non-zero on failure.</summary>
        public static void UseStoryCatalogueBatch()
        {
            SceneSetupReport report = SetCatalogue(StoryCataloguePath);
            LogReport(report);
            EditorApplication.Exit(report.Succeeded ? 0 : 1);
        }

        /// <summary>
        /// Opens the Game scene, repoints <see cref="GameSceneController"/>'s catalogue field at
        /// <paramref name="cataloguePath"/>, and saves — only if that asset exists and the field is
        /// found; otherwise nothing about the scene is touched.
        /// </summary>
        public static SceneSetupReport SetCatalogue(string cataloguePath)
        {
            SceneSetupReport report =
                new SceneSetupReport("Use Catalogue In Game Scene: " + cataloguePath);

            // Loading the catalogue before opening the scene would not survive it: OpenScene(...,
            // Single) unloads assets nothing currently references, and a local variable is not a
            // keep-alive root, so the reference would go stale (Unity's "fake null") the moment the
            // scene finished loading. Open the scene first; load the catalogue only after.
            Scene scene = EditorSceneManager.OpenScene(
                SceneSetupAutomation.GameScenePath, OpenSceneMode.Single);

            ContentCatalogue catalogue = AssetDatabase.LoadAssetAtPath<ContentCatalogue>(cataloguePath);
            if (catalogue == null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "CATALOGUE_MISSING", "Content",
                    cataloguePath, string.Empty,
                    "No ContentCatalogue asset exists at this path. Generate it first.");
                return report;
            }

            GameObject controllerObject = GameObject.Find(GameControllerPath);
            GameSceneController controller =
                controllerObject != null ? controllerObject.GetComponent<GameSceneController>() : null;

            if (controller == null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "CONTROLLER_MISSING", "Scene",
                    SceneSetupAutomation.GameScenePath, GameControllerPath,
                    "GameSceneController was not found at this path in the Game scene.");
                return report;
            }

            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty catalogueProperty = serializedController.FindProperty(CatalogueFieldName);

            if (catalogueProperty == null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "FIELD_MISSING", "Scene",
                    SceneSetupAutomation.GameScenePath, GameControllerPath,
                    "GameSceneController no longer has a field named '" + CatalogueFieldName + "'.");
                return report;
            }

            // Compared and reported by asset path throughout, deliberately never by object
            // reference: an object reference loaded on one side of a scene load/save/reload cycle
            // is not guaranteed to still resolve to a live object on the other side of it (Unity is
            // free to unload an asset nothing currently references), so "==" here would be
            // comparing against a reference that may already be stale.
            ContentCatalogue previous = catalogueProperty.objectReferenceValue as ContentCatalogue;
            string previousPath = previous != null ? AssetDatabase.GetAssetPath(previous) : string.Empty;

            if (string.Equals(previousPath, cataloguePath, System.StringComparison.Ordinal))
            {
                report.Add(SceneSetupIssueSeverity.Info, "CATALOGUE_UNCHANGED", "Content",
                    cataloguePath, GameControllerPath,
                    "The Game scene already points at this catalogue; nothing to save.");
                return report;
            }

            catalogueProperty.objectReferenceValue = catalogue;
            serializedController.ApplyModifiedProperties();

            // Re-read from the SerializedObject itself, not the local variable, so a failed or
            // silently-reverted assignment is caught here rather than reported as success.
            serializedController.Update();
            SerializedProperty verifyProperty = serializedController.FindProperty(CatalogueFieldName);
            ContentCatalogue verifiedInMemory = verifyProperty.objectReferenceValue as ContentCatalogue;
            string verifiedInMemoryPath =
                verifiedInMemory != null ? AssetDatabase.GetAssetPath(verifiedInMemory) : string.Empty;

            if (!string.Equals(verifiedInMemoryPath, cataloguePath, System.StringComparison.Ordinal))
            {
                report.Add(SceneSetupIssueSeverity.Error, "ASSIGNMENT_DID_NOT_STICK", "Scene",
                    SceneSetupAutomation.GameScenePath, GameControllerPath,
                    string.Format(
                        "Setting the catalogue field did not stick when read back before saving " +
                        "(reads '{0}'); the scene was not saved.",
                        string.IsNullOrEmpty(verifiedInMemoryPath) ? "<none>" : verifiedInMemoryPath));
                return report;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);

            if (!saved)
            {
                report.Add(SceneSetupIssueSeverity.Error, "SCENE_SAVE_FAILED", "Scene",
                    SceneSetupAutomation.GameScenePath, GameControllerPath,
                    "EditorSceneManager.SaveScene reported failure.");
                return report;
            }

            // Verify against what actually landed on disk, not just what is held in memory: reopen
            // the scene fresh (a separate load, not the same in-memory Scene handle) and re-read.
            EditorSceneManager.OpenScene(SceneSetupAutomation.GameScenePath, OpenSceneMode.Single);
            GameObject reopenedControllerObject = GameObject.Find(GameControllerPath);
            GameSceneController reopenedController = reopenedControllerObject != null
                ? reopenedControllerObject.GetComponent<GameSceneController>()
                : null;
            ContentCatalogue onDisk = reopenedController != null
                ? new SerializedObject(reopenedController).FindProperty(CatalogueFieldName)
                    .objectReferenceValue as ContentCatalogue
                : null;
            string onDiskPath = onDisk != null ? AssetDatabase.GetAssetPath(onDisk) : string.Empty;

            if (!string.Equals(onDiskPath, cataloguePath, System.StringComparison.Ordinal))
            {
                report.Add(SceneSetupIssueSeverity.Error, "VERIFICATION_AFTER_SAVE_FAILED", "Scene",
                    SceneSetupAutomation.GameScenePath, GameControllerPath,
                    string.Format(
                        "The scene was saved, but reopening it shows the catalogue field reads " +
                        "'{0}', not the path just set. Do not trust this scene without manual " +
                        "inspection.",
                        string.IsNullOrEmpty(onDiskPath) ? "<none>" : onDiskPath));
                return report;
            }

            report.Add(SceneSetupIssueSeverity.Info, "CATALOGUE_SET", "Content",
                cataloguePath, GameControllerPath,
                string.Format(
                    "GameSceneController.catalogue changed from '{0}' to '{1}', verified after " +
                    "save by reopening the scene and reading the field back from disk.",
                    string.IsNullOrEmpty(previousPath) ? "<none>" : previousPath,
                    cataloguePath));

            return report;
        }

        private static void LogReport(SceneSetupReport report)
        {
            for (int i = 0; i < report.Issues.Count; i++)
            {
                SceneSetupIssue issue = report.Issues[i];
                string line = "[Story Scene Wiring] " + issue.Message;

                switch (issue.Severity)
                {
                    case SceneSetupIssueSeverity.Error:
                        Debug.LogError(line);
                        break;
                    case SceneSetupIssueSeverity.Warning:
                        Debug.LogWarning(line);
                        break;
                    default:
                        Debug.Log(line);
                        break;
                }
            }
        }
    }
}
