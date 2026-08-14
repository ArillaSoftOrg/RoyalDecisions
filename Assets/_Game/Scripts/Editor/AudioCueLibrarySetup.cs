using System.Collections.Generic;
using RoyalDecisions.Presentation;
using UnityEditor;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Adds or refreshes the feedback-audio cue entries in <see cref="AudioCueLibrary"/> without
    /// hand-editing its serialized YAML.
    /// </summary>
    /// <remarks>
    /// Reads and writes through <see cref="SerializedObject"/> only, so the asset is always left in
    /// a format Unity itself considers valid. Any cue already in the library — including ones this
    /// tool does not know about — is preserved; a desired cue already present has only its clip
    /// reference refreshed, never its position or id casing.
    /// </remarks>
    public static class AudioCueLibrarySetup
    {
        private const string MenuPath = "Tools/Royal Decisions/Audio/Update Main Audio Cue Library";

        public const string LibraryPath = "Assets/_Game/Audio/MainAudioCueLibrary.asset";

        /// <summary>Cue IDs this tool owns and the clip each should point at.</summary>
        private static readonly (string Id, string ClipPath)[] DesiredCues =
        {
            ("slider_tick", "Assets/_Game/Audio/SFX/slider_tick.wav"),
            ("card_enter", "Assets/_Game/Audio/SFX/card_enter.wav"),
            ("snap_back", "Assets/_Game/Audio/SFX/snap_back.wav"),
            ("game_over", "Assets/_Game/Audio/SFX/game_over.wav"),
            ("stat_increase", "Assets/_Game/Audio/SFX/stat_increase.wav"),
            ("stat_decrease", "Assets/_Game/Audio/SFX/stat_decrease.wav"),
            ("critical", "Assets/_Game/Audio/SFX/critical.wav"),
            ("menu_music", "Assets/_Game/Audio/Music/menu_music.mp3"),
            ("gameplay_music", "Assets/_Game/Audio/Music/gameplay_music.wav"),
        };

        public const string ProfilePath = "Assets/_Game/Content/UI/DefaultFeedbackCueProfile.asset";

        /// <summary>
        /// Desired cue ID per <see cref="FeedbackCueProfile"/> field. Fields not listed here
        /// (<c>threshold</c>, <c>exit</c>, <c>restart</c>, <c>ambientLoop</c>) are intentionally left
        /// alone: the first two have no cue by design, Restart already plays ui_click directly from
        /// composition code rather than through this profile, and nothing produces an ambient loop yet.
        /// </summary>
        private static readonly (string Field, string CueId)[] DesiredProfileFields =
        {
            ("uiClick", "ui_click"),
            ("sliderTick", "slider_tick"),
            ("cardEnter", "card_enter"),
            ("cardPreview", "card_preview"),
            ("snapBack", "snap_back"),
            ("leftConfirmation", "card_swipe"),
            ("rightConfirmation", "card_swipe"),
            ("statIncrease", "stat_increase"),
            ("statDecrease", "stat_decrease"),
            ("critical", "critical"),
            ("gameOver", "game_over"),
            ("menuMusic", "menu_music"),
            ("gameplayMusic", "gameplay_music"),
        };

        [MenuItem(MenuPath)]
        public static void UpdateFromMenu()
        {
            Debug.Log("[AudioCueLibrarySetup] " + Update());
        }

        [MenuItem("Tools/Royal Decisions/Audio/Update Default Feedback Cue Profile")]
        public static void UpdateProfileFromMenu()
        {
            Debug.Log("[AudioCueLibrarySetup] " + UpdateFeedbackCueProfile());
        }

        [MenuItem("Tools/Royal Decisions/Audio/Update Cue Library and Profile")]
        public static void UpdateAllFromMenu()
        {
            Debug.Log("[AudioCueLibrarySetup] " + Update());
            Debug.Log("[AudioCueLibrarySetup] " + UpdateFeedbackCueProfile());
        }

        /// <summary>
        /// Fills the cue-ID string fields on <see cref="ProfilePath"/> with this project's chosen
        /// mapping. A field that already carries a non-empty value different from the desired one is
        /// left untouched and reported rather than overwritten — this asset may carry hand-authored
        /// choices this tool should not clobber.
        /// </summary>
        public static string UpdateFeedbackCueProfile()
        {
            FeedbackCueProfile profile = AssetDatabase.LoadAssetAtPath<FeedbackCueProfile>(ProfilePath);
            if (profile == null)
            {
                return "No FeedbackCueProfile asset at " + ProfilePath + "; nothing changed.";
            }

            var serialized = new SerializedObject(profile);
            var set = new List<string>();
            var skipped = new List<string>();

            foreach ((string field, string cueId) in DesiredProfileFields)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null)
                {
                    skipped.Add(field + " (field not found)");
                    continue;
                }
                if (!string.IsNullOrEmpty(property.stringValue) && property.stringValue != cueId)
                {
                    skipped.Add(field + " (already '" + property.stringValue + "', left untouched)");
                    continue;
                }
                property.stringValue = cueId;
                set.Add(field);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary = "Set [" + string.Join(", ", set) + "].";
            if (skipped.Count > 0)
            {
                summary += " Left alone: [" + string.Join(", ", skipped) + "].";
            }
            return summary;
        }

        /// <summary>Idempotent: safe to run repeatedly as clips are replaced or added.</summary>
        public static string Update()
        {
            AudioCueLibrary library = AssetDatabase.LoadAssetAtPath<AudioCueLibrary>(LibraryPath);
            if (library == null)
            {
                return "No AudioCueLibrary asset at " + LibraryPath + "; nothing changed.";
            }

            var serialized = new SerializedObject(library);
            SerializedProperty cuesProperty = serialized.FindProperty("cues");
            if (cuesProperty == null || !cuesProperty.isArray)
            {
                return "AudioCueLibrary has no serialized 'cues' array; nothing changed.";
            }

            // Preserve every existing entry, in order, keyed by id. A duplicate id keeps only its
            // first occurrence, matching AudioCueLibrary.RebuildIndex's own "first entry wins" rule.
            var clipById = new Dictionary<string, AudioClip>();
            var order = new List<string>();
            for (int i = 0; i < cuesProperty.arraySize; i++)
            {
                SerializedProperty element = cuesProperty.GetArrayElementAtIndex(i);
                string id = element.FindPropertyRelative("id")?.stringValue;
                if (string.IsNullOrEmpty(id) || clipById.ContainsKey(id))
                {
                    continue;
                }
                clipById[id] = element.FindPropertyRelative("clip")?.objectReferenceValue as AudioClip;
                order.Add(id);
            }

            var added = new List<string>();
            var refreshed = new List<string>();
            var missingClips = new List<string>();

            foreach ((string id, string clipPath) in DesiredCues)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip == null)
                {
                    missingClips.Add(id + " -> " + clipPath);
                }

                if (clipById.ContainsKey(id))
                {
                    refreshed.Add(id);
                }
                else
                {
                    order.Add(id);
                    added.Add(id);
                }
                clipById[id] = clip;
            }

            cuesProperty.arraySize = order.Count;
            for (int i = 0; i < order.Count; i++)
            {
                SerializedProperty element = cuesProperty.GetArrayElementAtIndex(i);
                string id = order[i];
                element.FindPropertyRelative("id").stringValue = id;
                element.FindPropertyRelative("clip").objectReferenceValue = clipById[id];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary = string.Format(
                "{0} cues total. Added: [{1}]. Refreshed: [{2}].",
                order.Count,
                string.Join(", ", added),
                string.Join(", ", refreshed));

            if (missingClips.Count > 0)
            {
                summary += " Missing clips (cue left unassigned): " + string.Join(", ", missingClips);
            }

            return summary;
        }
    }
}
