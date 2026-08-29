using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RoyalDecisions.Composition;
using RoyalDecisions.Data;
using RoyalDecisions.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Creates and wires the standalone prologue scene (<c>Prologue.unity</c>: <c>PrologueCanvas</c>,
    /// <c>EventSystem</c>, <see cref="PrologueSequenceController"/>,
    /// <see cref="PrologueSceneController"/>), its default placeholder content
    /// (<c>DefaultPrologue.asset</c>, five generated placeholder illustrations), and its Build
    /// Settings registration, without hand-authored scene YAML.
    /// </summary>
    /// <remarks>
    /// This opens <c>MainMenu.unity</c> only for a read-only report inside
    /// <see cref="ReportCurrentState"/> (never <c>MarkSceneDirty</c>/<c>SaveScene</c> on it) and
    /// never opens <c>Bootstrap.unity</c> or <c>Game.unity</c> at all — the New Game→Prologue and
    /// Continue→Game routing that makes those checks meaningful lives entirely in
    /// <c>MainMenuController</c>'s own already-serialized default field values, not in anything this
    /// tool writes. Mirrors <see cref="IntroSceneSetup"/> and <c>StartupLoadingSetup</c>: small,
    /// self-contained, every step finds-or-creates rather than duplicating, so re-running is always
    /// safe.
    /// </remarks>
    public static class PrologueSequenceSetup
    {
        private const string ScenePath = "Assets/_Game/scenes/Prologue.unity";
        private const string MainMenuScenePath = "Assets/_Game/scenes/MainMenu.unity";
        private const string DefaultDataAssetPath = "Assets/_Game/Content/Story/Prologue/DefaultPrologue.asset";
        private const string PlaceholderArtFolder = "Assets/_Game/Art/Prologue/Placeholders";

        // The real, final illustrations. Preferred over the generated placeholders whenever present
        // — see EnsureDefaultPrologueAsset/SyncSlidesWithRealArt.
        private const string RealArtFolder = "Assets/_Game/Art/Prologue/Illustrations";

        private static readonly string[] RealIllustrationFileNames =
        {
            "Prologue_01.png", "Prologue_02.png", "Prologue_03.png", "Prologue_04.png", "Prologue_05.png",
        };

        // The per-slide motion PrologueDefaultContent assigned before the real illustrations were
        // reviewed. Used only to detect "still at the original generated default, never hand-tuned",
        // so the one-time sync to RealArtMotions below can never silently overwrite a later manual
        // edit in the Inspector.
        private static readonly PrologueSlideMotion[] LegacyDefaultMotions =
        {
            PrologueSlideMotion.Zoom, PrologueSlideMotion.Pan, PrologueSlideMotion.Zoom,
            PrologueSlideMotion.Pan, PrologueSlideMotion.Zoom,
        };

        // Chosen by reviewing the real artwork's focal composition — see the matching comment in
        // PrologueDefaultContent.
        private static readonly PrologueSlideMotion[] RealArtMotions =
        {
            PrologueSlideMotion.Zoom, PrologueSlideMotion.Zoom, PrologueSlideMotion.Pan,
            PrologueSlideMotion.Zoom, PrologueSlideMotion.Pan,
        };

        private const string CanvasName = "PrologueCanvas";
        private const string EventSystemName = "EventSystem";
        private const string ControllerName = "PrologueSequenceController";
        private const string SceneControllerName = "PrologueSceneController";
        private const string MainMenuControllerName = "MainMenuController";
        private const string AudioObjectName = "PrologueAudio";
        private const string MusicSourceChildName = "MusicSource";

        private const int PlaceholderWidth = 540;
        private const int PlaceholderHeight = 960;

        // Distinct, obviously-temporary vertical gradients (top -> bottom) so slide-to-slide
        // crossfades, cropping, and text readability are all visibly testable today. None of these
        // reuse or derive from LoadingBackground.png, ArillaGamesLogo.png, or app icon artwork.
        private static readonly (Color32 Top, Color32 Bottom)[] PlaceholderGradients =
        {
            (new Color32(0x1B, 0x2A, 0x3D, 0xFF), new Color32(0x05, 0x08, 0x0C, 0xFF)),
            (new Color32(0x3D, 0x2A, 0x1B, 0xFF), new Color32(0x0C, 0x08, 0x05, 0xFF)),
            (new Color32(0x2A, 0x1B, 0x3D, 0xFF), new Color32(0x08, 0x05, 0x0C, 0xFF)),
            (new Color32(0x1B, 0x3D, 0x2A, 0xFF), new Color32(0x05, 0x0C, 0x08, 0xFF)),
            (new Color32(0x3D, 0x1B, 0x1B, 0xFF), new Color32(0x0C, 0x05, 0x05, 0xFF)),
        };

        // No readability gradient/scrim: even the tuned-down 4-stop version still read as a large
        // dark band at the bottom (see RemoveReadabilityGradientIfPresent). Readability now comes
        // entirely from text styling — a soft shadow plus a thin outline — so the illustration stays
        // visible, unobstructed, all the way to the bottom edge.

        // Raised out of the very bottom margin and given a touch more room so the subtitle sits in
        // the lower third and feels integrated with the artwork, rather than pinned to the screen
        // edge like a status bar.
        private const float StoryTextBottomOffset = 320f;
        private static readonly Vector2 StoryTextGroupSize = new Vector2(-140f, 460f);

        // Kept well clear of StoryText (roughly 220px of gap at the reference resolution) and sized
        // down so it never competes with the subtitle for attention.
        private const float ContinueIndicatorBottomOffset = 96f;
        private const float SkipButtonMargin = 40f;
        // Slightly smaller than before (was 160x84) for a more secondary feel, but the height stays
        // close to the project's established ~96px touch-target floor rather than chasing "small" at
        // the cost of being hard to tap.
        private static readonly Vector2 SkipButtonSize = new Vector2(144f, 80f);
        private const float SkipButtonCornerRadius = 22f;

        private static readonly Color StoryTextColour = new Color32(0xF2, 0xE7, 0xCF, 0xFF);
        private static readonly Color StoryTextShadowColour = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color StoryTextOutlineColour = new Color(0f, 0f, 0f, 0.55f);
        // TMP outline width is in the 0..1 material-property range, not reference units; this is
        // deliberately thin — just enough to separate letters from a bright patch of artwork, not a
        // heavy arcade-text outline.
        private const float StoryTextOutlineWidth = 0.12f;
        private static readonly Color ContinueIndicatorColour = new Color(0.85f, 0.76f, 0.55f, 0.68f);
        private static readonly Color SkipButtonColour = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color SkipButtonTextColour = new Color(0.95f, 0.91f, 0.81f, 0.9f);

        [MenuItem("Tools/Royal Decisions/Scene Setup/Prologue/Apply Prologue Setup")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play Mode before applying Prologue Setup.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[PrologueSequenceSetup] Cancelled: unsaved scenes.");
                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                ApplyToPrologueScene();
            }
            catch (Exception exception)
            {
                // Nothing is saved to disk until the very end of ApplyToPrologueScene, so an
                // exception here means Prologue.unity on disk was never touched (or never created).
                Debug.LogError("[PrologueSequenceSetup] Apply failed: " + exception);
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        [MenuItem("Tools/Royal Decisions/Scene Setup/Prologue/Validate Prologue Setup")]
        public static void ValidateMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play Mode before validating Prologue Setup.");
                return;
            }

            if (!File.Exists(ScenePath))
            {
                Debug.LogWarning(
                    "[PrologueSequenceSetup] " + ScenePath + " does not exist yet. Run "
                    + "'Apply Prologue Setup' first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[PrologueSequenceSetup] Validate cancelled: unsaved scenes.");
                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                ReportCurrentState();
            }
            catch (Exception exception)
            {
                Debug.LogError("[PrologueSequenceSetup] Validate failed: " + exception);
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        private static void ApplyToPrologueScene()
        {
            // Registers the Prologue ambient/accent cue IDs in the one shared MainAudioCueLibrary
            // (never a separate library) before anything below reads it — idempotent, and never
            // touches any cue this tool does not own.
            Debug.Log("[PrologueSequenceSetup] " + AudioCueLibrarySetup.Update());

            PrologueSequenceData data = EnsureDefaultPrologueAsset();

            Scene scene = OpenOrCreatePrologueScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[PrologueSequenceSetup] Could not open or create " + ScenePath);
                return;
            }

            EnsureEventSystem(scene);

            GameObject canvasObject = EnsurePrologueCanvas(scene);
            Transform canvasTransform = canvasObject.transform;

            Button tapCatcherButton = EnsureTapCatcher(canvasTransform);
            (Image layerAImage, AspectRatioFitter layerAFitter, Image layerBImage, AspectRatioFitter layerBFitter) =
                EnsureSlideLayers(canvasTransform);
            RemoveReadabilityGradientIfPresent(canvasTransform);

            RectTransform safeArea = EnsureSafeArea(canvasTransform);
            (CanvasGroup storyGroup, TMP_Text storyText) = EnsureStoryText(safeArea);
            TMP_Text continueText = EnsureContinueIndicator(safeArea);
            (Button skipButton, TMP_Text skipLabel) = EnsureSkipButton(safeArea);

            CanvasGroup fadeOverlay = EnsureFadeOverlay(canvasTransform);

            AudioService audioService = EnsurePrologueAudio(scene);

            GameObject controllerObject = FindRoot(scene, ControllerName)
                ?? new GameObject(ControllerName);
            PrologueSequenceController controller =
                EnsureComponent<PrologueSequenceController>(controllerObject);
            controller.SetAuthoringReferences(
                data, layerAImage, layerAFitter, layerBImage, layerBFitter,
                storyGroup, storyText, continueText, skipLabel, fadeOverlay);
            controller.SetAudioAuthoringReferences(audioService);

            WireButtonClick(tapCatcherButton, controller.OnTapAdvance);
            WireButtonClick(skipButton, controller.Skip);

            GameObject sceneControllerObject = FindRoot(scene, SceneControllerName)
                ?? new GameObject(SceneControllerName);
            PrologueSceneController sceneController =
                EnsureComponent<PrologueSceneController>(sceneControllerObject);
            sceneController.SetAuthoringReferences(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("[PrologueSequenceSetup] Prologue scene could not be saved.");
                return;
            }

            // Only registered once the scene file actually exists on disk (just saved above), so
            // Build Settings never points at a scene that isn't there yet.
            EnsureBuildSettingsRegistration();

            Debug.Log("[PrologueSequenceSetup] Prologue scene wiring applied at " + ScenePath + ".");
            ValidateAfterApply(scene);
        }

        /// <summary>
        /// Registers <see cref="ScenePath"/> in Build Settings the first time it is missing, inserted
        /// immediately after MainMenu when that scene is present (so the list reads Bootstrap,
        /// MainMenu, Prologue, Game), or appended otherwise. Idempotent: if the scene is already
        /// registered — at any position, in whatever enabled state the team left it — this leaves
        /// the whole list untouched, so it never reorders or re-enables scenes on a repeat run.
        /// </summary>
        private static void EnsureBuildSettingsRegistration()
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;

            foreach (EditorBuildSettingsScene existing in current)
            {
                if (string.Equals(existing.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            List<EditorBuildSettingsScene> updated = new List<EditorBuildSettingsScene>(current);
            int insertIndex = updated.FindIndex(entry =>
                string.Equals(entry.path, MainMenuScenePath, StringComparison.OrdinalIgnoreCase));
            EditorBuildSettingsScene prologueEntry = new EditorBuildSettingsScene(ScenePath, true);

            if (insertIndex >= 0)
            {
                updated.Insert(insertIndex + 1, prologueEntry);
            }
            else
            {
                updated.Add(prologueEntry);
            }

            EditorBuildSettings.scenes = updated.ToArray();
            Debug.Log("[PrologueSequenceSetup] Registered " + ScenePath + " in Build Settings"
                + (insertIndex >= 0 ? " immediately after MainMenu." : " (MainMenu entry not found; appended)."));
        }

        // --- Content -----------------------------------------------------------------

        /// <summary>
        /// Creates <see cref="DefaultDataAssetPath"/> the first time this runs — using the real
        /// Prologue_01–05 illustrations wherever present, falling back per-slide to a generated
        /// placeholder only where a real file is missing. If the asset already exists, it is loaded,
        /// then <see cref="SyncSlidesWithRealArt"/> updates only what is still missing/placeholder;
        /// hand-edited slides (subtitles, order, count, or any illustration/motion already tuned by
        /// hand) are never overwritten by a later Apply.
        /// </summary>
        private static PrologueSequenceData EnsureDefaultPrologueAsset()
        {
            Sprite[] realArt = LoadRealIllustrations();

            PrologueSequenceData existing =
                AssetDatabase.LoadAssetAtPath<PrologueSequenceData>(DefaultDataAssetPath);
            if (existing != null)
            {
                SyncSlidesWithRealArt(existing, realArt);
                return existing;
            }

            Sprite[] placeholders = EnsurePlaceholderSprites();
            Sprite[] chosen = new Sprite[placeholders.Length];
            for (int i = 0; i < chosen.Length; i++)
            {
                chosen[i] = (i < realArt.Length && realArt[i] != null) ? realArt[i] : placeholders[i];
            }

            PrologueSequenceData data = ScriptableObject.CreateInstance<PrologueSequenceData>();
            data.SetAuthoringData(PrologueDefaultContent.CreateSlides(chosen));

            string folder = Path.GetDirectoryName(DefaultDataAssetPath)?.Replace('\\', '/');
            EnsureFolder(folder);
            AssetDatabase.CreateAsset(data, DefaultDataAssetPath);
            AssetDatabase.SaveAssets();

            Debug.Log("[PrologueSequenceSetup] Created " + DefaultDataAssetPath + " with "
                + data.SlideCount + " slide(s) (real illustrations used where present).");
            return data;
        }

        /// <summary>
        /// Loads the five real illustrations from <see cref="RealArtFolder"/>, forcing each to
        /// Sprite/Single import if needed (never touching pixel data). An entry is null when that
        /// file does not exist yet, or Unity has not imported it into the AssetDatabase yet — both
        /// are supported configurations that simply fall back to a placeholder or leave that slide's
        /// existing illustration untouched.
        /// </summary>
        private static Sprite[] LoadRealIllustrations()
        {
            Sprite[] result = new Sprite[RealIllustrationFileNames.Length];
            for (int i = 0; i < RealIllustrationFileNames.Length; i++)
            {
                result[i] = EnsureRealSprite(RealArtFolder + "/" + RealIllustrationFileNames[i]);
            }

            return result;
        }

        /// <summary>
        /// Forces <paramref name="path"/> to Sprite Mode "Single" and reimports it if it is not
        /// already — mirroring <c>IntroSceneSetup.EnsureLogoIsSingleSprite</c> — then returns the
        /// loaded sprite. Only ever touches import metadata, never the source pixels.
        /// </summary>
        private static Sprite EnsureRealSprite(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                // Not in the AssetDatabase yet (Unity has not imported this file in this session).
                // A later Apply will pick it up once it has.
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                Debug.Log("[PrologueSequenceSetup] '" + path + "' set to Sprite Mode 'Single' and reimported.");
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// Updates an already-existing <see cref="PrologueSequenceData"/> in place: for each slide
        /// (up to whichever of slide-count/real-art-count is shorter), swaps in the matching real
        /// illustration only when that slide's current illustration is missing or is itself a
        /// generated placeholder, syncs the motion style only when it still exactly matches
        /// <see cref="LegacyDefaultMotions"/> (i.e. was never hand-tuned), and fills in a default
        /// accent cue ID only when the slide currently has none at all — the same "only fill an empty
        /// field" rule <see cref="AudioCueLibrarySetup.UpdateFeedbackCueProfile"/> already uses for
        /// cue-ID string fields. Subtitles, slide count, order, and any already-real/hand-picked
        /// illustration, motion, or accent cue are left completely alone.
        /// </summary>
        private static void SyncSlidesWithRealArt(PrologueSequenceData data, Sprite[] realArt)
        {
            IReadOnlyList<PrologueSlideData> slides = data.Slides;
            bool changed = false;

            for (int i = 0; i < slides.Count; i++)
            {
                PrologueSlideData slide = slides[i];

                if (i < realArt.Length)
                {
                    Sprite real = realArt[i];
                    if (real != null && slide.Illustration != real)
                    {
                        bool illustrationIsMissingOrPlaceholder =
                            slide.Illustration == null || IsGeneratedPlaceholderSprite(slide.Illustration);
                        if (illustrationIsMissingOrPlaceholder)
                        {
                            slide.SetIllustration(real);
                            changed = true;
                        }
                    }
                }

                if (i < LegacyDefaultMotions.Length && i < RealArtMotions.Length
                    && slide.Motion == LegacyDefaultMotions[i] && slide.Motion != RealArtMotions[i])
                {
                    slide.SetMotion(RealArtMotions[i]);
                    changed = true;
                }

                if (!slide.HasAccentCue)
                {
                    string defaultCue = PrologueDefaultContent.DefaultAccentCueId(i);
                    if (!string.IsNullOrEmpty(defaultCue))
                    {
                        slide.SetAccentCueId(defaultCue);
                        changed = true;
                    }
                }
            }

            if (!changed)
            {
                return;
            }

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("[PrologueSequenceSetup] Synced " + DefaultDataAssetPath + " with the real "
                + "Prologue_01–05 illustrations, their reviewed motion styles, and default accent "
                + "cues — only slides still at their placeholder/default/empty state were touched.");
        }

        private static bool IsGeneratedPlaceholderSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return false;
            }

            string path = AssetDatabase.GetAssetPath(sprite);
            return !string.IsNullOrEmpty(path)
                && path.Replace('\\', '/')
                    .StartsWith(PlaceholderArtFolder + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static Sprite[] EnsurePlaceholderSprites()
        {
            EnsureFolder(PlaceholderArtFolder);

            Sprite[] sprites = new Sprite[PlaceholderGradients.Length];
            for (int i = 0; i < PlaceholderGradients.Length; i++)
            {
                string path = PlaceholderArtFolder + "/ProloguePlaceholder"
                    + (i + 1).ToString("00") + ".png";
                sprites[i] = EnsurePlaceholderSprite(path, PlaceholderGradients[i].Top, PlaceholderGradients[i].Bottom);
            }

            return sprites;
        }

        /// <summary>
        /// Generates a simple portrait gradient PNG (a vertical colour ramp with a soft radial
        /// vignette) the first time this path is missing, then ensures it imports as a Sprite. These
        /// are obviously temporary placeholders — never web-downloaded, never derived from any real
        /// artwork in the project.
        /// </summary>
        private static Sprite EnsurePlaceholderSprite(string path, Color32 top, Color32 bottom)
        {
            if (!File.Exists(path))
            {
                Texture2D texture = new Texture2D(PlaceholderWidth, PlaceholderHeight, TextureFormat.RGBA32, false);
                Color32[] pixels = new Color32[PlaceholderWidth * PlaceholderHeight];
                Vector2 vignetteCenter = new Vector2(PlaceholderWidth * 0.5f, PlaceholderHeight * 0.45f);
                float maxDistance = new Vector2(PlaceholderWidth * 0.5f, PlaceholderHeight * 0.5f).magnitude;

                for (int y = 0; y < PlaceholderHeight; y++)
                {
                    float v = y / (float)(PlaceholderHeight - 1);
                    Color rowColour = Color.Lerp(bottom, top, v);

                    for (int x = 0; x < PlaceholderWidth; x++)
                    {
                        float distance01 = Vector2.Distance(new Vector2(x, y), vignetteCenter) / maxDistance;
                        float vignette = Mathf.Clamp01(1f - (distance01 * 0.55f));
                        Color pixelColour = rowColour * vignette;
                        pixelColour.a = 1f;
                        pixels[(y * PlaceholderWidth) + x] = pixelColour;
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();

                byte[] png = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);
                File.WriteAllBytes(path, png);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                Debug.Log("[PrologueSequenceSetup] Generated placeholder illustration at " + path
                    + " — obviously temporary; replace via DefaultPrologue.asset when final art exists.");
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null
                && (importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // --- Hierarchy -----------------------------------------------------------------

        private static Scene OpenOrCreatePrologueScene()
        {
            if (File.Exists(ScenePath))
            {
                return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Debug.Log("[PrologueSequenceSetup] " + ScenePath + " does not exist yet; creating it.");
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static Button EnsureTapCatcher(Transform parent)
        {
            GameObject catcherObject = EnsureChild(parent, "TapCatcher");
            catcherObject.transform.SetSiblingIndex(0);
            RectTransform rect = catcherObject.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Configure TapCatcher");
            Stretch(rect);

            Image image = EnsureComponent<Image>(catcherObject);
            Undo.RecordObject(image, "Configure TapCatcher");
            image.sprite = null;
            image.color = Color.clear;
            image.raycastTarget = true;

            Button button = EnsureComponent<Button>(catcherObject);
            Undo.RecordObject(button, "Configure TapCatcher");
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;

            return button;
        }

        private static (Image layerAImage, AspectRatioFitter layerAFitter, Image layerBImage, AspectRatioFitter layerBFitter)
            EnsureSlideLayers(Transform parent)
        {
            GameObject layersRoot = EnsureChild(parent, "SlideLayers");
            RectTransform layersRect = layersRoot.GetComponent<RectTransform>();
            Undo.RecordObject(layersRect, "Configure SlideLayers");
            Stretch(layersRect);

            (Image imageA, AspectRatioFitter fitterA) = EnsureSlideLayer(layersRoot.transform, "SlideLayerA");
            (Image imageB, AspectRatioFitter fitterB) = EnsureSlideLayer(layersRoot.transform, "SlideLayerB");

            return (imageA, fitterA, imageB, fitterB);
        }

        private static (Image image, AspectRatioFitter fitter) EnsureSlideLayer(Transform parent, string name)
        {
            GameObject layerObject = EnsureChild(parent, name);
            RectTransform rect = layerObject.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Configure " + name);
            // Cover-fit: fills the viewport and crops overflow instead of stretching or
            // letterboxing. EnvelopeParent needs the rect free to resize, so it is centred rather
            // than Stretch-anchored.
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;

            Image image = EnsureComponent<Image>(layerObject);
            Undo.RecordObject(image, "Configure " + name);
            image.sprite = null;
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;

            AspectRatioFitter fitter = EnsureComponent<AspectRatioFitter>(layerObject);
            Undo.RecordObject(fitter, "Configure " + name);
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.enabled = false;

            return (image, fitter);
        }

        /// <summary>
        /// Destroys the "ReadabilityGradient" node from any earlier Apply run, if present. The
        /// illustration must stay visible all the way to the bottom edge — readability now comes
        /// entirely from StoryText's own shadow/outline (see <see cref="EnsureStoryText"/>), never
        /// from a darkening panel behind it. Idempotent: a re-run with the node already gone is a
        /// no-op.
        /// </summary>
        private static void RemoveReadabilityGradientIfPresent(Transform parent)
        {
            Transform existing = parent.Find("ReadabilityGradient");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
                Debug.Log("[PrologueSequenceSetup] Removed ReadabilityGradient — the illustration now "
                    + "shows unobstructed to the bottom edge.");
            }
        }

        private static RectTransform EnsureSafeArea(Transform parent)
        {
            GameObject safeAreaObject = EnsureChild(parent, "SafeArea");
            RectTransform rect = safeAreaObject.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Configure SafeArea");
            Stretch(rect);

            EnsureComponent<SafeAreaFitter>(safeAreaObject);

            return rect;
        }

        private static (CanvasGroup group, TMP_Text text) EnsureStoryText(RectTransform safeArea)
        {
            GameObject groupObject = EnsureChild(safeArea, "StoryTextGroup");
            RectTransform groupRect = groupObject.GetComponent<RectTransform>();
            Undo.RecordObject(groupRect, "Configure StoryTextGroup");
            groupRect.anchorMin = new Vector2(0f, 0f);
            groupRect.anchorMax = new Vector2(1f, 0f);
            groupRect.pivot = new Vector2(0.5f, 0f);
            groupRect.anchoredPosition = new Vector2(0f, StoryTextBottomOffset);
            groupRect.sizeDelta = StoryTextGroupSize;

            CanvasGroup group = EnsureComponent<CanvasGroup>(groupObject);

            GameObject textObject = EnsureChild(groupObject.transform, "StoryText");
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            Undo.RecordObject(textRect, "Configure StoryText");
            Stretch(textRect);

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                TurkishGlyphValidator.FontAssetPath);

            TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(textObject);
            Undo.RecordObject(text, "Configure StoryText");
            text.alignment = TextAlignmentOptions.Center;
            text.color = StoryTextColour;
            text.fontSize = 46f;
            text.lineSpacing = 8f;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            if (font != null)
            {
                text.font = font;
            }

            // Readability comes only from text styling — a soft drop shadow plus a thin outline —
            // never from a panel or gradient behind the text, so the illustration stays fully
            // visible and the subtitle reads as part of the image rather than UI chrome.
            text.outlineWidth = StoryTextOutlineWidth;
            text.outlineColor = StoryTextOutlineColour;

            Shadow shadow = EnsureComponent<Shadow>(textObject);
            Undo.RecordObject(shadow, "Configure StoryText");
            shadow.effectColor = StoryTextShadowColour;
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            shadow.useGraphicAlpha = true;

            return (group, text);
        }

        private static TMP_Text EnsureContinueIndicator(RectTransform safeArea)
        {
            GameObject indicatorObject = EnsureChild(safeArea, "ContinueIndicator");
            RectTransform rect = indicatorObject.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Configure ContinueIndicator");
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, ContinueIndicatorBottomOffset);
            rect.sizeDelta = new Vector2(760f, 60f);

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                TurkishGlyphValidator.FontAssetPath);

            TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(indicatorObject);
            Undo.RecordObject(text, "Configure ContinueIndicator");
            text.alignment = TextAlignmentOptions.Center;
            text.color = ContinueIndicatorColour;
            text.fontSize = 28f;
            text.raycastTarget = false;
            if (font != null)
            {
                text.font = font;
            }
            // Design-time preview value only — the controller overwrites this from its own
            // serialized `continueLabel` default in Awake, the same as the skip label.
            text.text = "DEVAM ETMEK İÇİN DOKUN";

            return text;
        }

        private static (Button button, TMP_Text label) EnsureSkipButton(RectTransform safeArea)
        {
            GameObject buttonObject = EnsureChild(safeArea, "SkipButton");
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Configure SkipButton");
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-SkipButtonMargin, -SkipButtonMargin);
            rect.sizeDelta = SkipButtonSize;

            EnsureComponent<CanvasRenderer>(buttonObject);
            ProceduralRoundedRectGraphic graphic = EnsureComponent<ProceduralRoundedRectGraphic>(buttonObject);
            Undo.RecordObject(graphic, "Configure SkipButton");
            graphic.color = SkipButtonColour;
            graphic.raycastTarget = true;
            graphic.SetCornerRadius(SkipButtonCornerRadius);

            Button button = EnsureComponent<Button>(buttonObject);
            Undo.RecordObject(button, "Configure SkipButton");
            button.transition = Selectable.Transition.None;
            button.targetGraphic = graphic;

            GameObject labelObject = EnsureChild(buttonObject.transform, "Label");
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            Undo.RecordObject(labelRect, "Configure SkipButton Label");
            Stretch(labelRect);

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                TurkishGlyphValidator.FontAssetPath);

            TextMeshProUGUI label = EnsureComponent<TextMeshProUGUI>(labelObject);
            Undo.RecordObject(label, "Configure SkipButton Label");
            label.alignment = TextAlignmentOptions.Center;
            label.color = SkipButtonTextColour;
            label.fontSize = 32f;
            label.raycastTarget = false;
            if (font != null)
            {
                label.font = font;
            }
            label.text = "ATLA";

            return (button, label);
        }

        private static CanvasGroup EnsureFadeOverlay(Transform parent)
        {
            GameObject overlayObject = EnsureChild(parent, "FadeOverlay");
            overlayObject.transform.SetAsLastSibling();
            RectTransform rect = overlayObject.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Configure FadeOverlay");
            Stretch(rect);

            Image image = EnsureComponent<Image>(overlayObject);
            Undo.RecordObject(image, "Configure FadeOverlay");
            image.sprite = null;
            image.color = Color.black;
            image.raycastTarget = false;

            CanvasGroup group = EnsureComponent<CanvasGroup>(overlayObject);
            Undo.RecordObject(group, "Configure FadeOverlay");
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            return group;
        }

        /// <summary>
        /// Builds/reuses a root <c>PrologueAudio</c> object carrying the SFX <see cref="AudioSource"/>
        /// (for the one-shot slide accents) plus a child <c>MusicSource</c> object with a second,
        /// looping <see cref="AudioSource"/> (for the ambient bed) — mirrors the same dual-source
        /// pattern <c>SceneSetupAutomation</c> already uses for Game/MainMenu, and the single-source
        /// pattern <c>IntroSceneSetup.EnsureIntroAudio</c> uses where only SFX is needed. Wired to the
        /// one shared <see cref="AudioCueLibrarySetup.LibraryPath"/>, never a Prologue-only library.
        /// </summary>
        private static AudioService EnsurePrologueAudio(Scene scene)
        {
            GameObject audioObject = FindRoot(scene, AudioObjectName) ?? new GameObject(AudioObjectName);

            AudioSource sfxSource = EnsureComponent<AudioSource>(audioObject);
            Undo.RecordObject(sfxSource, "Configure PrologueAudio");
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;

            Transform musicTransform = audioObject.transform.Find(MusicSourceChildName);
            GameObject musicObject;
            if (musicTransform != null)
            {
                musicObject = musicTransform.gameObject;
            }
            else
            {
                musicObject = new GameObject(MusicSourceChildName);
                musicObject.transform.SetParent(audioObject.transform, false);
            }

            AudioSource musicSource = EnsureComponent<AudioSource>(musicObject);
            Undo.RecordObject(musicSource, "Configure PrologueAudio MusicSource");
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;

            AudioService audioService = EnsureComponent<AudioService>(audioObject);
            AudioCueLibrary library =
                AssetDatabase.LoadAssetAtPath<AudioCueLibrary>(AudioCueLibrarySetup.LibraryPath);
            audioService.SetAuthoringReferences(sfxSource, library, musicSource);

            if (library == null)
            {
                Debug.LogWarning(
                    "[PrologueSequenceSetup] No AudioCueLibrary found at '" + AudioCueLibrarySetup.LibraryPath
                    + "'; the prologue is wired but plays silently until it exists.");
            }

            return audioService;
        }

        private static GameObject EnsurePrologueCanvas(Scene scene)
        {
            GameObject canvasObject = FindRoot(scene, CanvasName)
                ?? new GameObject(CanvasName, typeof(RectTransform));

            Canvas canvas = EnsureComponent<Canvas>(canvasObject);
            Undo.RecordObject(canvas, "Configure PrologueCanvas");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvasObject);
            Undo.RecordObject(scaler, "Configure PrologueCanvas");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            EnsureComponent<GraphicRaycaster>(canvasObject);

            return canvasObject;
        }

        private static void EnsureEventSystem(Scene scene)
        {
            GameObject eventSystemObject = FindRoot(scene, EventSystemName)
                ?? new GameObject(EventSystemName);

            EnsureComponent<EventSystem>(eventSystemObject);
            InputSystemUIInputModule module = EnsureComponent<InputSystemUIInputModule>(eventSystemObject);
            if (module.actionsAsset == null)
            {
                Undo.RecordObject(module, "Assign default UI actions");
                module.AssignDefaultActions();
            }
        }

        /// <summary>
        /// Clears any existing persistent listeners and adds exactly one, so repeated Apply runs
        /// never accumulate duplicate calls to <paramref name="action"/>.
        /// </summary>
        private static void WireButtonClick(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            }

            Undo.RecordObject(button, "Wire button listener");
            UnityEventTools.AddPersistentListener(button.onClick, action);
            EditorUtility.SetDirty(button);
        }

        // --- Validation -----------------------------------------------------------------

        /// <summary>Logs a clear pass/fail summary immediately after Apply saves the scene.</summary>
        private static void ValidateAfterApply(Scene scene)
        {
            bool ok = true;

            GameObject controllerObject = FindRoot(scene, ControllerName);
            PrologueSequenceController controller = controllerObject != null
                ? controllerObject.GetComponent<PrologueSequenceController>()
                : null;
            if (controller == null)
            {
                Debug.LogError("[PrologueSequenceSetup] Validation: PrologueSequenceController not found.");
                ok = false;
            }
            else
            {
                SerializedObject serialized = new SerializedObject(controller);
                ok &= ValidateReference(serialized, "sequenceData", "PrologueSequenceController.sequenceData");
                ok &= ValidateReference(serialized, "slideLayerAImage", "SlideLayerA Image");
                ok &= ValidateReference(serialized, "slideLayerBImage", "SlideLayerB Image");
                ok &= ValidateReference(serialized, "storyText", "StoryText TMP_Text");
                ok &= ValidateReference(serialized, "skipButtonLabel", "SkipButton label TMP_Text");

                PrologueSequenceData data = serialized.FindProperty("sequenceData")
                    ?.objectReferenceValue as PrologueSequenceData;
                ok &= ValidateSlideArtwork(data);

                TMP_Text storyTextForShadowCheck = serialized.FindProperty("storyText")
                    ?.objectReferenceValue as TMP_Text;
                ok &= ValidateStoryTextReadabilityStyling(storyTextForShadowCheck);

                ok &= ValidateReference(serialized, "audioService", "PrologueSequenceController.audioService");
                SerializedProperty ambientCueProperty = serialized.FindProperty("ambientCueId");
                if (ambientCueProperty == null || string.IsNullOrEmpty(ambientCueProperty.stringValue))
                {
                    Debug.LogError(
                        "[PrologueSequenceSetup] Validation: PrologueSequenceController.ambientCueId is empty.");
                    ok = false;
                }
            }

            Transform canvasTransformForValidation = FindRoot(scene, CanvasName)?.transform;
            if (canvasTransformForValidation != null
                && canvasTransformForValidation.Find("ReadabilityGradient") != null)
            {
                Debug.LogError("[PrologueSequenceSetup] Validation: a ReadabilityGradient/dark footer "
                    + "panel is still present — the illustration must show unobstructed to the bottom.");
                ok = false;
            }

            GameObject sceneControllerObject = FindRoot(scene, SceneControllerName);
            PrologueSceneController sceneController = sceneControllerObject != null
                ? sceneControllerObject.GetComponent<PrologueSceneController>()
                : null;
            if (sceneController == null)
            {
                Debug.LogError("[PrologueSequenceSetup] Validation: PrologueSceneController not found.");
                ok = false;
            }
            else
            {
                SerializedObject serializedSceneController = new SerializedObject(sceneController);
                ok &= ValidateReference(
                    serializedSceneController, "prologueSequence",
                    "PrologueSceneController.prologueSequence");
            }

            if (!IsPrologueSceneRegisteredAndEnabled())
            {
                Debug.LogError(
                    "[PrologueSequenceSetup] Validation: " + ScenePath
                    + " is not registered (enabled) in Build Settings.");
                ok = false;
            }

            Debug.Log(ok
                ? "[PrologueSequenceSetup] Validation passed: hierarchy and references are correct."
                : "[PrologueSequenceSetup] Validation FAILED — see errors above.");
        }

        private static bool IsPrologueSceneRegisteredAndEnabled()
        {
            foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            {
                if (entry.enabled && string.Equals(entry.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ValidateReference(SerializedObject serializedObject, string propertyName, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
            {
                Debug.LogError("[PrologueSequenceSetup] Validation: " + label + " is not wired.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Confirms every slide (up to the five real illustrations) has an illustration, that it is
        /// not a leftover generated placeholder, and — as a name-match sanity check only, not a hard
        /// failure — that it looks like the expected <c>Prologue_0N</c> file.
        /// </summary>
        private static bool ValidateSlideArtwork(PrologueSequenceData data)
        {
            if (data == null)
            {
                Debug.LogError("[PrologueSequenceSetup] Validation: no data asset to check slide artwork.");
                return false;
            }

            bool ok = true;
            IReadOnlyList<PrologueSlideData> slides = data.Slides;

            if (slides.Count != RealIllustrationFileNames.Length)
            {
                Debug.LogWarning("[PrologueSequenceSetup] Validation: " + slides.Count + " slide(s) "
                    + "present (expected " + RealIllustrationFileNames.Length + "); this is a "
                    + "supported configuration, but the per-slide artwork check below only covers "
                    + "the first " + RealIllustrationFileNames.Length + ".");
            }

            for (int i = 0; i < slides.Count && i < RealIllustrationFileNames.Length; i++)
            {
                Sprite sprite = slides[i].Illustration;
                string expectedName = Path.GetFileNameWithoutExtension(RealIllustrationFileNames[i]);

                if (sprite == null)
                {
                    Debug.LogError("[PrologueSequenceSetup] Validation: Slide " + (i + 1)
                        + " has no illustration.");
                    ok = false;
                    continue;
                }

                if (IsGeneratedPlaceholderSprite(sprite))
                {
                    Debug.LogError("[PrologueSequenceSetup] Validation: Slide " + (i + 1)
                        + " is still using a generated placeholder illustration.");
                    ok = false;
                    continue;
                }

                if (!string.Equals(sprite.name, expectedName, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning("[PrologueSequenceSetup] Validation: Slide " + (i + 1)
                        + " illustration is '" + sprite.name + "', expected '" + expectedName + "'.");
                }
            }

            return ok;
        }

        /// <summary>
        /// Confirms StoryText carries the shadow that now does all the readability work (required)
        /// and reports on its outline (optional, so only a warning if absent).
        /// </summary>
        private static bool ValidateStoryTextReadabilityStyling(TMP_Text storyText)
        {
            if (storyText == null)
            {
                Debug.LogError(
                    "[PrologueSequenceSetup] Validation: no StoryText to check readability styling.");
                return false;
            }

            Shadow shadow = storyText.GetComponent<Shadow>();
            if (shadow == null || shadow.effectColor.a <= 0f)
            {
                Debug.LogError("[PrologueSequenceSetup] Validation: StoryText has no configured drop "
                    + "shadow — with no readability gradient behind it, this is the primary way the "
                    + "subtitle stays legible over the artwork.");
                return false;
            }

            if (storyText.outlineWidth <= 0f)
            {
                Debug.LogWarning("[PrologueSequenceSetup] Validation: StoryText has no outline "
                    + "configured (optional, but helps against bright patches of artwork).");
            }

            return true;
        }

        private static void ReportCurrentState()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[PrologueSequenceSetup] Could not open " + ScenePath);
                return;
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("[PrologueSequenceSetup] Validate Prologue Setup report:");

            GameObject controllerObject = FindRoot(scene, ControllerName);
            PrologueSequenceController controller = controllerObject != null
                ? controllerObject.GetComponent<PrologueSequenceController>()
                : null;

            if (controller == null)
            {
                report.AppendLine("- PrologueSequenceController: NOT FOUND");
                Debug.Log(report.ToString());
                return;
            }

            report.AppendLine("- PrologueSequenceController: OK");

            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty dataProperty = serialized.FindProperty("sequenceData");
            PrologueSequenceData data = dataProperty != null
                ? dataProperty.objectReferenceValue as PrologueSequenceData
                : null;
            report.AppendLine(data != null
                ? "- Data asset: OK ('" + data.name + "', " + data.SlideCount + " slide(s))"
                : "- Data asset: MISSING");

            if (data != null)
            {
                IReadOnlyList<PrologueSlideData> slides = data.Slides;
                for (int i = 0; i < slides.Count; i++)
                {
                    Sprite sprite = slides[i].Illustration;
                    string status;
                    if (sprite == null)
                    {
                        status = "NO ILLUSTRATION";
                    }
                    else if (IsGeneratedPlaceholderSprite(sprite))
                    {
                        status = "PLACEHOLDER ('" + sprite.name + "')";
                    }
                    else
                    {
                        status = "OK ('" + sprite.name + "')";
                    }

                    string accentStatus = slides[i].HasAccentCue ? "'" + slides[i].AccentCueId + "'" : "(none)";
                    report.AppendLine("  Slide " + (i + 1) + ": " + status + ", motion=" + slides[i].Motion
                        + ", accentCue=" + accentStatus);
                }
            }

            report.AppendLine("- SlideLayerA image: " + DescribeReference(serialized, "slideLayerAImage"));
            report.AppendLine("- SlideLayerB image: " + DescribeReference(serialized, "slideLayerBImage"));
            report.AppendLine("- StoryText (TMP): " + DescribeReference(serialized, "storyText"));
            report.AppendLine("- StoryTextGroup (CanvasGroup): " + DescribeReference(serialized, "storyTextGroup"));
            report.AppendLine("- SkipButton label (TMP): " + DescribeReference(serialized, "skipButtonLabel"));
            report.AppendLine("- ContinueIndicator (TMP): " + DescribeReference(serialized, "continueIndicatorText"));
            report.AppendLine("- FadeOverlay (CanvasGroup): " + DescribeReference(serialized, "fadeOverlayGroup"));

            report.AppendLine("- Audio service: " + DescribeReference(serialized, "audioService"));
            SerializedProperty ambientCueProperty = serialized.FindProperty("ambientCueId");
            string ambientCueId = ambientCueProperty != null ? ambientCueProperty.stringValue : null;
            report.AppendLine("- Ambient cue ID: "
                + (string.IsNullOrEmpty(ambientCueId) ? "MISSING" : "'" + ambientCueId + "'"));

            TMP_Text storyTextForReport = serialized.FindProperty("storyText")?.objectReferenceValue as TMP_Text;
            Shadow storyTextShadow = storyTextForReport != null
                ? storyTextForReport.GetComponent<Shadow>()
                : null;
            report.AppendLine("- StoryText shadow: "
                + (storyTextShadow != null && storyTextShadow.effectColor.a > 0f
                    ? "OK (colour " + storyTextShadow.effectColor + ", offset " + storyTextShadow.effectDistance + ")"
                    : "MISSING"));
            report.AppendLine("- StoryText outline: "
                + (storyTextForReport != null && storyTextForReport.outlineWidth > 0f
                    ? "OK (width " + storyTextForReport.outlineWidth + ")"
                    : "none (optional)"));

            GameObject canvasObject = FindRoot(scene, CanvasName);
            Transform canvasTransform = canvasObject != null ? canvasObject.transform : null;
            Transform safeArea = canvasTransform != null ? canvasTransform.Find("SafeArea") : null;
            SafeAreaFitter safeAreaFitter = safeArea != null ? safeArea.GetComponent<SafeAreaFitter>() : null;
            report.AppendLine("- SafeArea/SafeAreaFitter: " + (safeAreaFitter != null ? "OK" : "MISSING"));

            Transform readabilityGradient =
                canvasTransform != null ? canvasTransform.Find("ReadabilityGradient") : null;
            report.AppendLine("- No dark footer/panel present (ReadabilityGradient removed): "
                + (readabilityGradient == null ? "OK" : "STILL PRESENT — rerun Apply to remove it"));

            CanvasScaler scaler = canvasObject != null ? canvasObject.GetComponent<CanvasScaler>() : null;
            report.AppendLine(scaler != null
                    && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                    && scaler.referenceResolution == new Vector2(1080f, 1920f)
                ? "- CanvasScaler reference resolution: OK (1080x1920, Scale With Screen Size)"
                : "- CanvasScaler reference resolution: CHECK (expected 1080x1920, Scale With Screen Size)");

            Transform tapCatcher = canvasTransform != null ? canvasTransform.Find("TapCatcher") : null;
            Button tapCatcherButton = tapCatcher != null ? tapCatcher.GetComponent<Button>() : null;
            report.AppendLine("- TapCatcher button wired to OnTapAdvance: "
                + DescribeButtonListener(tapCatcherButton, controller, "OnTapAdvance"));

            Transform safeAreaTransform = safeArea;
            Transform skipButtonTransform = safeAreaTransform != null ? safeAreaTransform.Find("SkipButton") : null;
            Button skipButton = skipButtonTransform != null ? skipButtonTransform.GetComponent<Button>() : null;
            report.AppendLine("- SkipButton wired to Skip: "
                + DescribeButtonListener(skipButton, controller, "Skip"));

            GameObject sceneControllerObject = FindRoot(scene, SceneControllerName);
            PrologueSceneController sceneController = sceneControllerObject != null
                ? sceneControllerObject.GetComponent<PrologueSceneController>()
                : null;
            if (sceneController == null)
            {
                report.AppendLine("- PrologueSceneController: NOT FOUND");
            }
            else
            {
                report.AppendLine("- PrologueSceneController: OK");
                SerializedObject serializedSceneController = new SerializedObject(sceneController);
                report.AppendLine("- PrologueSceneController.prologueSequence: "
                    + DescribeReference(serializedSceneController, "prologueSequence"));
                SerializedProperty gameSceneProperty =
                    serializedSceneController.FindProperty("gameSceneName");
                string gameDestination = gameSceneProperty != null ? gameSceneProperty.stringValue : null;
                report.AppendLine("- PrologueSceneController Game destination: "
                    + (string.IsNullOrEmpty(gameDestination) ? "MISSING" : "'" + gameDestination + "'")
                    + (gameDestination == "Game" ? " (OK)" : " (CHECK — expected 'Game')"));
            }

            report.AppendLine();
            report.AppendLine("- Build Settings scenes:");
            foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            {
                report.AppendLine("  " + (entry.enabled ? "[x] " : "[ ] ") + entry.path);
            }
            report.AppendLine("- Prologue scene registered & enabled in Build Settings: "
                + (IsPrologueSceneRegisteredAndEnabled() ? "OK" : "MISSING"));

            AppendMainMenuRoutingReport(report);

            Debug.Log(report.ToString());
        }

        /// <summary>
        /// Opens MainMenu.unity purely to read <c>MainMenuController</c>'s serialized destination
        /// fields — never marks it dirty and never saves it. The scene left open afterward is
        /// whatever <see cref="ValidateMenu"/>'s own setup/restore already handles, exactly like
        /// every other scene switch this tool performs.
        /// </summary>
        private static void AppendMainMenuRoutingReport(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("- MainMenu routing (read-only check):");

            Scene mainMenuScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            if (!mainMenuScene.IsValid())
            {
                report.AppendLine("  Could not open " + MainMenuScenePath + " to check routing.");
                return;
            }

            GameObject mainMenuControllerObject = FindRoot(mainMenuScene, MainMenuControllerName);
            MainMenuController mainMenuController = mainMenuControllerObject != null
                ? mainMenuControllerObject.GetComponent<MainMenuController>()
                : null;

            if (mainMenuController == null)
            {
                report.AppendLine("  MainMenuController: NOT FOUND in " + MainMenuScenePath);
                return;
            }

            SerializedObject serializedMenu = new SerializedObject(mainMenuController);
            string newGameDestination = serializedMenu.FindProperty("prologueSceneName")?.stringValue;
            string continueDestination = serializedMenu.FindProperty("gameSceneName")?.stringValue;

            report.AppendLine("  New Game destination (prologueSceneName): "
                + (string.IsNullOrEmpty(newGameDestination) ? "MISSING" : "'" + newGameDestination + "'")
                + (newGameDestination == "Prologue" ? " (OK)" : " (CHECK — expected 'Prologue')"));
            report.AppendLine("  Continue destination (gameSceneName): "
                + (string.IsNullOrEmpty(continueDestination) ? "MISSING" : "'" + continueDestination + "'")
                + (continueDestination == "Game" ? " (OK)" : " (CHECK — expected 'Game')"));
        }

        private static string DescribeButtonListener(Button button, UnityEngine.Object target, string methodName)
        {
            if (button == null)
            {
                return "MISSING (no button)";
            }

            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) == target
                    && button.onClick.GetPersistentMethodName(i) == methodName)
                {
                    return "OK";
                }
            }

            return "MISSING";
        }

        private static string DescribeReference(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return "FIELD NOT FOUND";
            }

            UnityEngine.Object value = property.objectReferenceValue;
            return value != null ? "OK (" + value.name + ")" : "MISSING";
        }

        // --- Generic scene helpers -----------------------------------------------------------------

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
