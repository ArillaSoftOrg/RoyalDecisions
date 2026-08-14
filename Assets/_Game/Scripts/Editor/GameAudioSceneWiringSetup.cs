using System.Collections.Generic;
using RoyalDecisions.Composition;
using RoyalDecisions.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Fills in empty audio object-reference fields on components that already exist in
    /// <c>Game.unity</c> and <c>MainMenu.unity</c>.
    /// </summary>
    /// <remarks>
    /// Reads and writes only through <see cref="SerializedObject"/>/<see cref="SerializedProperty"/>,
    /// never the scene YAML directly. Never creates, deletes or reparents a GameObject and never
    /// touches a field that already points at something — only a reference sitting at
    /// <c>{fileID: 0}</c> is filled in, so a hand-authored choice is never silently replaced.
    /// </remarks>
    public static class GameAudioSceneWiringSetup
    {
        private const string MenuPath = "Tools/Royal Decisions/Audio/Wire Scene Audio References";

        public const string GameScenePath = "Assets/_Game/Scenes/Game.unity";
        public const string MainMenuScenePath = "Assets/_Game/Scenes/MainMenu.unity";

        [MenuItem(MenuPath)]
        public static void WireFromMenu()
        {
            Debug.Log("[GameAudioSceneWiringSetup] " + WireAll());
        }

        /// <summary>Idempotent: a second run reports nothing left to change.</summary>
        public static string WireAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return "Cancelled: unsaved scene changes were not saved.";
            }

            AudioCueLibrary library = AssetDatabase.LoadAssetAtPath<AudioCueLibrary>(
                AudioCueLibrarySetup.LibraryPath);
            FeedbackCueProfile profile = AssetDatabase.LoadAssetAtPath<FeedbackCueProfile>(
                AudioCueLibrarySetup.ProfilePath);

            if (library == null || profile == null)
            {
                return "Missing required asset(s) — cannot wire anything: "
                    + (library == null ? AudioCueLibrarySetup.LibraryPath + " " : string.Empty)
                    + (profile == null ? AudioCueLibrarySetup.ProfilePath : string.Empty);
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            var report = new List<string>();

            try
            {
                WireGameScene(library, profile, report);
                WireMainMenuScene(library, profile, report);
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            return report.Count > 0
                ? string.Join(" | ", report)
                : "Nothing to change; every reference this tool owns is already assigned.";
        }

        private static void WireGameScene(
            AudioCueLibrary library, FeedbackCueProfile profile, List<string> report)
        {
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                report.Add("Game.unity could not be opened.");
                return;
            }

            bool changed = false;

            AudioService audioService = FindInScene<AudioService>(scene);
            if (audioService != null)
            {
                changed |= TrySetObjectField(
                    audioService, "cueLibrary", library,
                    "Game.unity: AudioService.Cue Library -> MainAudioCueLibrary", report);
            }
            else
            {
                report.Add("Game.unity: no AudioService found — nothing to wire.");
            }

            GameSceneController gameSceneController = FindInScene<GameSceneController>(scene);
            if (gameSceneController != null)
            {
                changed |= TrySetObjectField(
                    gameSceneController, "cues", profile,
                    "Game.unity: GameSceneController.Cues -> DefaultFeedbackCueProfile", report);
            }
            else
            {
                report.Add("Game.unity: no GameSceneController found — nothing to wire.");
            }

            GameFeedbackController feedbackController = FindInScene<GameFeedbackController>(scene);
            if (feedbackController != null)
            {
                if (audioService != null)
                {
                    changed |= TrySetObjectField(
                        feedbackController, "audioService", audioService,
                        "Game.unity: GameFeedbackController.Audio Service -> AudioService", report);
                }
                changed |= TrySetObjectField(
                    feedbackController, "cues", profile,
                    "Game.unity: GameFeedbackController.Cues -> DefaultFeedbackCueProfile", report);
            }
            else
            {
                report.Add("Game.unity: no GameFeedbackController found — nothing to wire.");
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, GameScenePath);
            }
        }

        private static void WireMainMenuScene(
            AudioCueLibrary library, FeedbackCueProfile profile, List<string> report)
        {
            Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                report.Add("MainMenu.unity could not be opened.");
                return;
            }

            bool changed = false;

            AudioService audioService = FindInScene<AudioService>(scene);
            if (audioService != null)
            {
                changed |= TrySetObjectField(
                    audioService, "cueLibrary", library,
                    "MainMenu.unity: AudioService.Cue Library -> MainAudioCueLibrary", report);
            }
            else
            {
                report.Add("MainMenu.unity: no AudioService found — nothing to wire.");
            }

            SettingsController settingsController = FindInScene<SettingsController>(scene);
            if (settingsController != null)
            {
                changed |= TrySetObjectField(
                    settingsController, "cues", profile,
                    "MainMenu.unity: SettingsController.Cues -> DefaultFeedbackCueProfile", report);
            }
            else
            {
                report.Add("MainMenu.unity: no SettingsController found — nothing to wire.");
            }

            MainMenuController mainMenuController = FindInScene<MainMenuController>(scene);
            if (mainMenuController != null)
            {
                if (audioService != null)
                {
                    changed |= TrySetObjectField(
                        mainMenuController, "audioService", audioService,
                        "MainMenu.unity: MainMenuController.Audio Service -> AudioService", report);
                }
                changed |= TrySetObjectField(
                    mainMenuController, "cues", profile,
                    "MainMenu.unity: MainMenuController.Cues -> DefaultFeedbackCueProfile", report);
            }
            else
            {
                report.Add("MainMenu.unity: no MainMenuController found — nothing to wire.");
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            }
        }

        /// <summary>
        /// Assigns <paramref name="value"/> to <paramref name="fieldName"/> on
        /// <paramref name="target"/> only if that field is currently empty. Returns whether it made
        /// a change, and appends a line to <paramref name="report"/> either way something happened.
        /// </summary>
        private static bool TrySetObjectField(
            Object target, string fieldName, Object value, string changeMessage, List<string> report)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                report.Add(target.GetType().Name + "." + fieldName + " not found on the component.");
                return false;
            }

            if (property.objectReferenceValue != null)
            {
                return false;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            report.Add(changeMessage);
            return true;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }
    }
}
