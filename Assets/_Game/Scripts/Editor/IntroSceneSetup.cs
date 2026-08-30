using System;
using System.Collections.Generic;
using System.Text;
using RoyalDecisions.Composition;
using RoyalDecisions.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Creates and wires the coded startup intro (<c>IntroCanvas</c>, <see cref="IntroSequenceController"/>)
    /// inside <c>Bootstrap.unity</c>, without hand-authored scene YAML.
    /// </summary>
    /// <remarks>
    /// Deliberately separate from <see cref="SceneSetupAutomation"/>: that tool owns Game/MainMenu
    /// content and a full backup/rollback pipeline across all three scenes. This only ever touches
    /// Bootstrap, so it stays small and idempotent instead of growing the larger apply pass.
    /// Re-running it is always safe: every step finds-or-creates rather than duplicating.
    ///
    /// The AS mark and "ARILLA GAMES" wordmark are wired as two independent sprites
    /// (<see cref="MarkAssetPath"/>, <see cref="WordmarkAssetPath"/>) rather than one shared image,
    /// so each can be sized independently. <see cref="MarkAssetPath"/> is a pixel-exact crop of the
    /// master <c>ArillaGamesLogo.png</c> (never modified, never resampled) generated once by a
    /// deterministic script — see <c>MANUAL_UNITY_STEPS.md</c> for the exact crop bounds.
    /// <see cref="WordmarkAssetPath"/> is a separate, tight alpha-bbox crop of the newer modern
    /// wordmark source (not derived from <c>ArillaGamesLogo.png</c> at all) — see
    /// <c>MANUAL_UNITY_STEPS.md</c>'s "modern wordmark" entry for that crop's bounds. An earlier pass
    /// tried masking a single combined image with measured padding constants; that made the two
    /// elements impossible to size independently and left room for subtle misalignment, so it was
    /// replaced.
    /// </remarks>
    public static class IntroSceneSetup
    {
        private const string BootstrapScenePath = "Assets/_Game/scenes/Bootstrap.unity";

        /// <summary>Pixel-exact AS-mark crop of the master logo. Never hand-edited — regenerate via
        /// the deterministic crop script documented in MANUAL_UNITY_STEPS.md if the master changes.</summary>
        public const string MarkAssetPath = "Assets/_Game/Art/Branding/Generated/ArillaGamesMark.png";

        /// <summary>Tight alpha-bbox crop of the modern "ARILLA GAMES" wordmark source (not the
        /// master <c>ArillaGamesLogo.png</c> — that source's own wordmark crop,
        /// <c>ArillaGamesWordmark.png</c>, is kept on disk untouched as a fallback but is no longer
        /// wired here). Never hand-edited — regenerate via the same deterministic crop method
        /// documented in MANUAL_UNITY_STEPS.md if the modern source is ever replaced.</summary>
        public const string WordmarkAssetPath =
            "Assets/_Game/Art/Branding/Generated/ArillaGamesWordmarkModernRuntime.png";

        private const string CanvasName = "IntroCanvas";
        private const string EventSystemName = "EventSystem";
        private const string BootstrapControllerName = "BootstrapController";
        private const string IntroAudioName = "IntroAudio";

        // IntroCanvas's BlackBackground is a plain, permanently opaque Image that
        // IntroSequenceController never fades (only the logo group fades, back to that same black,
        // not away from it) — it stays on screen for the entire lifetime of Bootstrap.unity. Loading
        // is only ever revealed after the intro's own completion callback (see
        // RoyalDecisions.Composition.BootstrapController.HandleIntroCompleted), at which point
        // IntroCanvas is still sitting there, opaque. LoadingCanvas (StartupLoadingSetup.cs) must
        // therefore sort strictly above this value (currently 30) or its own opaque background stays
        // permanently hidden behind IntroCanvas's — see LoadingCanvasSortingOrder's own comment for
        // the incident that value fixed. Set here, from the intro's own side, so nothing under
        // Loading's ownership needs to change.
        private const int IntroCanvasSortingOrder = 20;

        // Reference-derived target widths on the 1080-wide reference canvas. The wordmark reads
        // clearly wider than the mark — a studio logo with the wordmark as the dominant, legible
        // element and the mark as a smaller emblem above it, matching direct feedback that an
        // earlier pass (460/560) still let the mark dominate and left the wordmark too weak, and a
        // later pass (390/680) still read as too small on a phone screen.
        private const float MarkTargetWidth = 390f;
        private const float WordmarkTargetWidth = 840f;

        // Vertical gap between the mark's own bottom edge and the wordmark's own top edge. Tight
        // enough that the two read as one composed logo rather than a symbol with separate text
        // underneath.
        private const float MarkWordmarkGap = 20f;

        // The previous (now-retired) wordmark crop read too thin/flat at the authored width, so
        // WordmarkRevealRoot was deliberately stretched 25% taller on Y only as a hand-tuned fix.
        // The modern wordmark crop is already correctly proportioned at its own aspect ratio, so
        // this stays at 1 (no extra stretch) — applying the old crop's compensation here would
        // distort the new art instead of fixing anything.
        private const float WordmarkRevealRootVerticalScale = 1f;

        // "around screen centre, perhaps y=50 max" -- unchanged from the original layout.
        private const float LogoGroupVerticalOffset = 50f;

        // RevealMask is sized taller than the wordmark's own height so nothing along its glow
        // edges is ever clipped vertically; only its WIDTH is ever animated.
        private const float RevealMaskVerticalPadding = 16f;

        // A few pixels of built-in RectMask2D softness softens the hard clip edge itself, on top
        // of (not instead of) the travelling RevealGlint highlight.
        private const int RevealMaskSoftnessPixels = 3;

        private const float RevealGlintWidth = 40f;
        private const float RevealGlintExtraHeight = 8f;

        private static readonly Color RevealGlintColor = new Color(0.75f, 0.86f, 1f, 0f);

        private static readonly HashSet<string> KnownLogoGroupChildren =
            new HashSet<string> { "MarkImage", "WordmarkRevealRoot", "RevealGlint" };

        [MenuItem("Tools/Royal Decisions/Scene Setup/Intro/Apply Intro Setup")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play Mode before applying Intro Setup.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[IntroSceneSetup] Cancelled: unsaved scenes.");
                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                ApplyToBootstrapScene();
            }
            catch (Exception exception)
            {
                // Nothing is saved to disk until the very end of ApplyToBootstrapScene, so an
                // exception here means Bootstrap.unity on disk was never touched — only the
                // in-memory scene (about to be discarded by the setup restore below) is affected.
                Debug.LogError("[IntroSceneSetup] Apply failed: " + exception);
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        [MenuItem("Tools/Royal Decisions/Scene Setup/Intro/Validate Intro Setup")]
        public static void ValidateMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play Mode before validating Intro Setup.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[IntroSceneSetup] Validate cancelled: unsaved scenes.");
                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                ReportCurrentState();
            }
            catch (Exception exception)
            {
                Debug.LogError("[IntroSceneSetup] Validate failed: " + exception);
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        private static void ApplyToBootstrapScene()
        {
            Scene scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[IntroSceneSetup] Could not open " + BootstrapScenePath);
                return;
            }

            EnsureEventSystem(scene);

            GameObject canvasObject = EnsureIntroCanvas(scene);
            Transform canvasTransform = canvasObject.transform;

            GameObject background = EnsureChild(canvasTransform, "BlackBackground");
            background.transform.SetSiblingIndex(0);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            Undo.RecordObject(backgroundRect, "Configure IntroCanvas background");
            Stretch(backgroundRect);
            Image backgroundImage = EnsureComponent<Image>(background);
            Undo.RecordObject(backgroundImage, "Configure IntroCanvas background");
            backgroundImage.color = Color.black;
            backgroundImage.raycastTarget = true;

            RemoveLegacyGeneratedNodes(canvasTransform);

            (CanvasGroup logoGroupCanvasGroup, RectTransform logoGroupRect, Image markImage,
                Image wordmarkImage, RectTransform revealMaskRect, Graphic glintGraphic) =
                EnsureLogoGroup(canvasTransform);

            AudioService introAudioService = EnsureIntroAudio(scene);

            // The click-catcher (BlackBackground's raycastable Image) and the controller that
            // reads the click must be the same GameObject — uGUI does not bubble pointer events
            // to parents on its own.
            IntroSequenceController introController = EnsureComponent<IntroSequenceController>(background);
            introController.SetAuthoringReferences(logoGroupCanvasGroup, logoGroupRect, markImage);
            introController.SetWordmarkAuthoringReferences(wordmarkImage, revealMaskRect, glintGraphic);
            introController.SetAudioAuthoringReferences(introAudioService);

            WireBootstrapController(scene, introController, introAudioService);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, BootstrapScenePath))
            {
                Debug.LogError("[IntroSceneSetup] Bootstrap scene could not be saved.");
                return;
            }

            Debug.Log("[IntroSceneSetup] Bootstrap intro wiring applied.");
            ValidateAfterApply(
                scene, canvasTransform, introController, logoGroupCanvasGroup, logoGroupRect, markImage,
                wordmarkImage, revealMaskRect, glintGraphic, introAudioService);
        }

        /// <summary>
        /// Removes anything left behind by earlier versions of this tool that the current layout no
        /// longer creates: the original single-node "Logo", the later combined-image "LogoImage",
        /// and the cover/feather/underline-glow/old-position-glint objects from the mask-based
        /// wordmark reveal this replaced. Everything in <see cref="KnownLogoGroupChildren"/> is
        /// reused, never destroyed, by <see cref="EnsureLogoGroup"/> below. Nothing outside this
        /// specific, known set of names is ever touched.
        /// </summary>
        private static void RemoveLegacyGeneratedNodes(Transform canvasTransform)
        {
            Transform legacyLogo = canvasTransform.Find("Logo");
            if (legacyLogo != null)
            {
                Undo.DestroyObjectImmediate(legacyLogo.gameObject);
            }

            Transform logoGroup = canvasTransform.Find("LogoGroup");
            if (logoGroup == null)
            {
                return;
            }

            for (int i = logoGroup.childCount - 1; i >= 0; i--)
            {
                Transform child = logoGroup.GetChild(i);
                if (!KnownLogoGroupChildren.Contains(child.name))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Builds/updates <c>LogoGroup</c> and its three children — <c>MarkImage</c> (the AS mark,
        /// sized independently of the wordmark), <c>WordmarkRevealRoot/RevealMask/WordmarkImage</c>
        /// (the reveal — see <see cref="EnsureWordmarkReveal"/>), and <c>RevealGlint</c> — and
        /// returns the components <see cref="IntroSequenceController"/> animates.
        /// </summary>
        private static (CanvasGroup, RectTransform, Image, Image, RectTransform, Graphic) EnsureLogoGroup(
            Transform canvasTransform)
        {
            GameObject logoGroup = EnsureChild(canvasTransform, "LogoGroup");
            logoGroup.transform.SetSiblingIndex(1);
            RectTransform logoGroupRect = logoGroup.GetComponent<RectTransform>();
            Undo.RecordObject(logoGroupRect, "Configure LogoGroup");
            ConfigureLogoGroupRect(logoGroupRect);
            CanvasGroup logoGroupCanvasGroup = EnsureComponent<CanvasGroup>(logoGroup);
            Undo.RecordObject(logoGroupCanvasGroup, "Configure LogoGroup");
            logoGroupCanvasGroup.alpha = 0f;

            Sprite markSprite = EnsureSingleSpriteAt(MarkAssetPath);
            Sprite wordmarkSprite = EnsureSingleSpriteAt(WordmarkAssetPath);

            float markAspect = SpriteAspect(markSprite);
            float wordmarkAspect = SpriteAspect(wordmarkSprite);
            float markHeight = markAspect > 0f ? MarkTargetWidth / markAspect : MarkTargetWidth;
            float wordmarkHeight = wordmarkAspect > 0f ? WordmarkTargetWidth / wordmarkAspect : WordmarkTargetWidth;

            // The mark sits above the wordmark with a fixed gap between them; the whole two-piece
            // block is then centred vertically around LogoGroup's own local origin, so retuning
            // either target width only ever grows/shrinks the block symmetrically.
            float totalHeight = markHeight + MarkWordmarkGap + wordmarkHeight;
            float markTop = totalHeight * 0.5f;
            float markBottom = markTop - markHeight;
            float wordmarkTop = markBottom - MarkWordmarkGap;
            float wordmarkBottom = wordmarkTop - wordmarkHeight;
            float markCenterY = (markTop + markBottom) * 0.5f;
            float wordmarkCenterY = (wordmarkTop + wordmarkBottom) * 0.5f;

            GameObject markObject = EnsureChild(logoGroup.transform, "MarkImage");
            markObject.transform.SetSiblingIndex(0);
            RectTransform markRect = markObject.GetComponent<RectTransform>();
            Undo.RecordObject(markRect, "Configure MarkImage");
            markRect.anchorMin = new Vector2(0.5f, 0.5f);
            markRect.anchorMax = new Vector2(0.5f, 0.5f);
            markRect.pivot = new Vector2(0.5f, 0.5f);
            markRect.anchoredPosition = new Vector2(0f, markCenterY);
            markRect.sizeDelta = new Vector2(MarkTargetWidth, markHeight);

            Image markImage = EnsureComponent<Image>(markObject);
            Undo.RecordObject(markImage, "Configure MarkImage");
            markImage.preserveAspect = true;
            markImage.raycastTarget = false;
            AssignSpriteOrWarn(markImage, markSprite, MarkAssetPath);

            (Image wordmarkImage, RectTransform revealMaskRect) = EnsureWordmarkReveal(
                logoGroup.transform, wordmarkSprite, wordmarkCenterY, wordmarkHeight);

            GameObject glintObject = EnsureChild(logoGroup.transform, "RevealGlint");
            glintObject.transform.SetSiblingIndex(2);
            RectTransform glintRect = glintObject.GetComponent<RectTransform>();
            Undo.RecordObject(glintRect, "Configure RevealGlint");
            glintRect.anchorMin = new Vector2(0.5f, 0.5f);
            glintRect.anchorMax = new Vector2(0.5f, 0.5f);
            glintRect.pivot = new Vector2(0.5f, 0.5f);
            glintRect.anchoredPosition = new Vector2(-WordmarkTargetWidth * 0.5f, wordmarkCenterY);
            glintRect.sizeDelta = new Vector2(RevealGlintWidth, wordmarkHeight + RevealGlintExtraHeight);

            // A three-stop gradient (transparent-peak-transparent) instead of a flat-colour Image:
            // the glint reads as a soft travelling highlight, never a rectangle.
            RemoveComponentIfPresent<Image>(glintObject);
            EnsureComponent<CanvasRenderer>(glintObject);
            ProceduralHorizontalGradientGraphic glintGraphic =
                EnsureComponent<ProceduralHorizontalGradientGraphic>(glintObject);
            Undo.RecordObject(glintGraphic, "Configure RevealGlint");
            glintGraphic.color = RevealGlintColor;
            glintGraphic.raycastTarget = false;
            glintGraphic.SetStops(0f, 1f, 0f);

            return (logoGroupCanvasGroup, logoGroupRect, markImage, wordmarkImage, revealMaskRect, glintGraphic);
        }

        /// <summary>
        /// Builds/updates the actual reveal: <c>WordmarkRevealRoot</c> (a stable, centred container
        /// sized to the wordmark's full final dimensions), <c>RevealMask</c> (a left-pivoted
        /// <see cref="RectMask2D"/> whose width is animated from 0 to the wordmark's own width at
        /// runtime — this is what reveals it left-to-right), and <c>WordmarkImage</c> inside the
        /// mask, which is never resized or moved: it is authored at its full final size once here,
        /// and the mask alone determines how much of it is visible at any instant.
        /// </summary>
        private static (Image wordmarkImage, RectTransform revealMaskRect) EnsureWordmarkReveal(
            Transform logoGroupTransform, Sprite wordmarkSprite, float wordmarkCenterY, float wordmarkHeight)
        {
            GameObject rootObject = EnsureChild(logoGroupTransform, "WordmarkRevealRoot");
            rootObject.transform.SetSiblingIndex(1);
            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            Undo.RecordObject(rootRect, "Configure WordmarkRevealRoot");
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(0f, wordmarkCenterY);
            rootRect.sizeDelta = new Vector2(WordmarkTargetWidth, wordmarkHeight);
            rootRect.localScale = new Vector3(1f, WordmarkRevealRootVerticalScale, 1f);

            GameObject maskObject = EnsureChild(rootObject.transform, "RevealMask");
            RectTransform maskRect = maskObject.GetComponent<RectTransform>();
            Undo.RecordObject(maskRect, "Configure RevealMask");
            // Left-pivoted, anchored to the root's own left edge: growing sizeDelta.x at runtime
            // extends the visible clip rightward from a fixed left edge, matching the reveal
            // direction. Height is padded beyond the wordmark's own height so nothing is ever
            // clipped vertically — only the horizontal reveal matters here.
            maskRect.anchorMin = new Vector2(0f, 0.5f);
            maskRect.anchorMax = new Vector2(0f, 0.5f);
            maskRect.pivot = new Vector2(0f, 0.5f);
            maskRect.anchoredPosition = Vector2.zero;
            maskRect.sizeDelta = new Vector2(0f, wordmarkHeight + RevealMaskVerticalPadding);

            RectMask2D revealMask = EnsureComponent<RectMask2D>(maskObject);
            Undo.RecordObject(revealMask, "Configure RevealMask");
            revealMask.softness = new Vector2Int(RevealMaskSoftnessPixels, 0);

            GameObject wordmarkImageObject = EnsureChild(maskObject.transform, "WordmarkImage");
            RectTransform wordmarkImageRect = wordmarkImageObject.GetComponent<RectTransform>();
            Undo.RecordObject(wordmarkImageRect, "Configure WordmarkImage");
            // Same left-pivot convention as RevealMask, so its own left edge lines up with the
            // mask's regardless of the mask's current (animated) width. sizeDelta is the FULL
            // final size and is never touched again at runtime — only RevealMask's width changes.
            wordmarkImageRect.anchorMin = new Vector2(0f, 0.5f);
            wordmarkImageRect.anchorMax = new Vector2(0f, 0.5f);
            wordmarkImageRect.pivot = new Vector2(0f, 0.5f);
            wordmarkImageRect.anchoredPosition = Vector2.zero;
            wordmarkImageRect.sizeDelta = new Vector2(WordmarkTargetWidth, wordmarkHeight);

            Image wordmarkImage = EnsureComponent<Image>(wordmarkImageObject);
            Undo.RecordObject(wordmarkImage, "Configure WordmarkImage");
            wordmarkImage.preserveAspect = true;
            wordmarkImage.raycastTarget = false;
            AssignSpriteOrWarn(wordmarkImage, wordmarkSprite, WordmarkAssetPath);

            return (wordmarkImage, maskRect);
        }

        private static void AssignSpriteOrWarn(Image image, Sprite sprite, string assetPath)
        {
            if (sprite != null)
            {
                image.sprite = sprite;
                return;
            }

            image.sprite = null;
            Debug.LogWarning(
                "[IntroSceneSetup] Sprite at '" + assetPath + "' was not assigned — see the error "
                + "above. Until a valid sprite is wired there, the intro safely skips straight to "
                + "MainMenu.");
        }

        private static float SpriteAspect(Sprite sprite)
        {
            return sprite != null && sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 0f;
        }

        private static void RemoveComponentIfPresent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component != null)
            {
                Undo.DestroyObjectImmediate(component);
            }
        }

        /// <summary>
        /// Forces the sprite at <paramref name="assetPath"/> to Sprite Mode "Single", synchronously
        /// reimports it, then loads and validates the result. Never returns a sprite loaded before
        /// the reimport, and never returns a sprite whose rect is smaller than the full source
        /// texture (a fragment left over from a "Multiple" auto-slice) — the same validation the
        /// original combined logo needed, reused here for each of the two derived crops.
        /// </summary>
        private static Sprite EnsureSingleSpriteAt(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError(
                    "[IntroSceneSetup] No TextureImporter found at '" + assetPath + "'. Generate it "
                    + "first (see MANUAL_UNITY_STEPS.md for the deterministic crop script) or let "
                    + "Unity import it before re-running Apply Intro Setup.");
                return null;
            }

            bool needsFix = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single;
            if (needsFix)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                AssetDatabase.ImportAsset(
                    assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

                // SaveAndReimport can invalidate the importer instance above; re-fetch before
                // trusting its state.
                importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            }

            if (importer == null || importer.spriteImportMode != SpriteImportMode.Single)
            {
                Debug.LogError(
                    "[IntroSceneSetup] '" + assetPath + "' is still not Sprite Mode 'Single' after "
                    + "reimport; refusing to wire a possibly-fragmented sprite.");
                return null;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Debug.LogError("[IntroSceneSetup] No sprite found at '" + assetPath + "' after reimport.");
                return null;
            }

            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            if (sourceWidth <= 0 || sourceHeight <= 0)
            {
                Debug.LogError("[IntroSceneSetup] Could not read source dimensions for '" + assetPath + "'.");
                return null;
            }

            if (Mathf.RoundToInt(sprite.rect.width) != sourceWidth
                || Mathf.RoundToInt(sprite.rect.height) != sourceHeight)
            {
                Debug.LogError(
                    "[IntroSceneSetup] Loaded sprite rect (" + sprite.rect.width + "x"
                    + sprite.rect.height + ") for '" + assetPath + "' does not match the full "
                    + "texture (" + sourceWidth + "x" + sourceHeight + "); refusing to wire it.");
                return null;
            }

            return sprite;
        }

        /// <summary>Logs a clear pass/fail summary immediately after Apply saves the scene.</summary>
        private static void ValidateAfterApply(
            Scene scene,
            Transform canvasTransform,
            IntroSequenceController introController,
            CanvasGroup expectedGroup,
            RectTransform expectedRect,
            Image expectedMark,
            Image expectedWordmark,
            RectTransform expectedRevealMask,
            Graphic expectedGlint,
            AudioService expectedAudioService)
        {
            bool ok = true;

            GameObject controllerObject = FindRoot(scene, BootstrapControllerName);
            BootstrapController controller = controllerObject != null
                ? controllerObject.GetComponent<BootstrapController>()
                : null;
            if (controller == null)
            {
                Debug.LogError("[IntroSceneSetup] Validation: BootstrapController not found.");
                ok = false;
            }
            else
            {
                SerializedObject serializedController = new SerializedObject(controller);
                SerializedProperty introProperty = serializedController.FindProperty("introSequence");
                if (introProperty == null
                    || introProperty.objectReferenceValue as IntroSequenceController != introController)
                {
                    Debug.LogError(
                        "[IntroSceneSetup] Validation: BootstrapController.introSequence does not "
                        + "reference the wired IntroSequenceController.");
                    ok = false;
                }

                ok &= ValidateReference(
                    serializedController, "audioService", expectedAudioService,
                    "BootstrapController.audioService");
            }

            SerializedObject serializedIntro = new SerializedObject(introController);
            ok &= ValidateReference(serializedIntro, "logoCanvasGroup", expectedGroup, "LogoGroup CanvasGroup");
            ok &= ValidateReference(serializedIntro, "logoRectTransform", expectedRect, "LogoGroup RectTransform");
            ok &= ValidateReference(serializedIntro, "markImage", expectedMark, "MarkImage Image");
            ok &= ValidateReference(serializedIntro, "wordmarkImage", expectedWordmark, "WordmarkImage Image");
            ok &= ValidateReference(
                serializedIntro, "wordmarkRevealMaskRect", expectedRevealMask, "RevealMask RectTransform");
            ok &= ValidateReference(serializedIntro, "wordmarkGlintImage", expectedGlint, "RevealGlint Graphic");
            ok &= ValidateReference(
                serializedIntro, "audioService", expectedAudioService, "IntroSequenceController.audioService");

            if (expectedMark.sprite == null)
            {
                Debug.LogError("[IntroSceneSetup] Validation: MarkImage has no sprite assigned.");
                ok = false;
            }
            else
            {
                Debug.Log(
                    "[IntroSceneSetup] Validation: Mark sprite assigned ('" + expectedMark.sprite.name
                    + "'), rect " + expectedMark.rectTransform.rect.width + "x"
                    + expectedMark.rectTransform.rect.height + ".");
            }

            if (expectedWordmark.sprite == null)
            {
                Debug.LogError("[IntroSceneSetup] Validation: WordmarkImage has no sprite assigned.");
                ok = false;
            }
            else
            {
                Debug.Log(
                    "[IntroSceneSetup] Validation: Wordmark sprite assigned ('"
                    + expectedWordmark.sprite.name + "'), final width "
                    + expectedWordmark.rectTransform.rect.width + " (must stay constant at runtime), "
                    + "height " + expectedWordmark.rectTransform.rect.height + ".");
            }

            RectMask2D revealMask = expectedRevealMask.GetComponent<RectMask2D>();
            if (revealMask == null)
            {
                Debug.LogError("[IntroSceneSetup] Validation: RevealMask has no RectMask2D component.");
                ok = false;
            }

            if (expectedRevealMask.pivot != new Vector2(0f, 0.5f)
                || expectedRevealMask.anchorMin != new Vector2(0f, 0.5f)
                || expectedRevealMask.anchorMax != new Vector2(0f, 0.5f))
            {
                Debug.LogError(
                    "[IntroSceneSetup] Validation: RevealMask is not left-pivoted/left-anchored "
                    + "(pivot=" + expectedRevealMask.pivot + ", anchorMin=" + expectedRevealMask.anchorMin
                    + "); the left-to-right reveal will not work.");
                ok = false;
            }
            else
            {
                Debug.Log(
                    "[IntroSceneSetup] Validation: RevealMask pivot/anchor is left-aligned (OK), "
                    + "current width=" + expectedRevealMask.sizeDelta.x + ".");
            }

            if (canvasTransform.Find("Logo") != null)
            {
                Debug.LogError("[IntroSceneSetup] Validation: legacy 'Logo' node still present.");
                ok = false;
            }

            Transform logoGroup = canvasTransform.Find("LogoGroup");
            if (logoGroup != null)
            {
                if (logoGroup.Find("WordmarkUnderlineGlow") != null)
                {
                    Debug.LogError(
                        "[IntroSceneSetup] Validation: legacy 'WordmarkUnderlineGlow' node still "
                        + "present — it must be fully removed.");
                    ok = false;
                }
                else
                {
                    Debug.Log("[IntroSceneSetup] Validation: underline glow object absent (OK).");
                }

                for (int i = 0; i < logoGroup.childCount; i++)
                {
                    string childName = logoGroup.GetChild(i).name;
                    if (!KnownLogoGroupChildren.Contains(childName))
                    {
                        Debug.LogError(
                            "[IntroSceneSetup] Validation: unexpected leftover node '" + childName
                            + "' under LogoGroup.");
                        ok = false;
                    }
                }

                if (logoGroup.childCount != KnownLogoGroupChildren.Count)
                {
                    Debug.LogError(
                        "[IntroSceneSetup] Validation: LogoGroup has " + logoGroup.childCount
                        + " children (expected exactly " + KnownLogoGroupChildren.Count
                        + ": MarkImage, WordmarkRevealRoot, RevealGlint).");
                    ok = false;
                }
            }

            Debug.Log(ok
                ? "[IntroSceneSetup] Validation passed: hierarchy and references are correct."
                : "[IntroSceneSetup] Validation FAILED — see errors above.");
        }

        private static bool ValidateReference(
            SerializedObject serializedIntro, string propertyName, Object expected, string label)
        {
            SerializedProperty property = serializedIntro.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue != expected)
            {
                Debug.LogError(
                    "[IntroSceneSetup] Validation: IntroSequenceController." + propertyName + " ("
                    + label + ") is not wired correctly.");
                return false;
            }

            return true;
        }

        private static void ReportCurrentState()
        {
            Scene scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[IntroSceneSetup] Could not open " + BootstrapScenePath);
                return;
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("[IntroSceneSetup] Validate Intro Setup report:");

            ReportSpriteState(report, "Mark", MarkAssetPath);
            ReportSpriteState(report, "Wordmark", WordmarkAssetPath);

            GameObject canvasRoot = FindRoot(scene, CanvasName);
            Transform canvasTransform = canvasRoot != null ? canvasRoot.transform : null;

            report.AppendLine("- Legacy 'Logo' node present: "
                + (canvasTransform != null && canvasTransform.Find("Logo") != null));

            Transform logoGroup = canvasTransform != null ? canvasTransform.Find("LogoGroup") : null;
            if (logoGroup == null)
            {
                report.AppendLine("- LogoGroup: NOT FOUND");
            }
            else
            {
                RectTransform logoGroupRect = logoGroup.GetComponent<RectTransform>();
                report.AppendLine("- LogoGroup: anchoredPosition=" + logoGroupRect.anchoredPosition
                    + ", sizeDelta=" + logoGroupRect.sizeDelta);
                bool onlyKnownChildren = logoGroup.childCount == KnownLogoGroupChildren.Count;
                for (int i = 0; onlyKnownChildren && i < logoGroup.childCount; i++)
                {
                    onlyKnownChildren &= KnownLogoGroupChildren.Contains(logoGroup.GetChild(i).name);
                }

                report.AppendLine("- LogoGroup children: " + logoGroup.childCount
                    + (onlyKnownChildren
                        ? " (OK: MarkImage, WordmarkRevealRoot, RevealGlint)"
                        : " (expected exactly 3: MarkImage, WordmarkRevealRoot, RevealGlint)"));
                report.AppendLine("- Legacy 'WordmarkUnderlineGlow' node present: "
                    + (logoGroup.Find("WordmarkUnderlineGlow") != null));

                Transform markImage = logoGroup.Find("MarkImage");
                if (markImage == null)
                {
                    report.AppendLine("- MarkImage: NOT FOUND");
                }
                else
                {
                    RectTransform markRect = markImage.GetComponent<RectTransform>();
                    Image image = markImage.GetComponent<Image>();
                    report.AppendLine("- MarkImage: anchoredPosition=" + markRect.anchoredPosition
                        + ", sizeDelta=" + markRect.sizeDelta
                        + ", sprite=" + (image != null && image.sprite != null ? image.sprite.name : "NONE"));
                }

                Transform revealRoot = logoGroup.Find("WordmarkRevealRoot");
                if (revealRoot == null)
                {
                    report.AppendLine("- WordmarkRevealRoot: NOT FOUND");
                }
                else
                {
                    RectTransform rootRect = revealRoot.GetComponent<RectTransform>();
                    report.AppendLine("- WordmarkRevealRoot: anchoredPosition=" + rootRect.anchoredPosition
                        + ", sizeDelta=" + rootRect.sizeDelta);

                    Transform revealMask = revealRoot.Find("RevealMask");
                    if (revealMask == null)
                    {
                        report.AppendLine("- RevealMask: NOT FOUND");
                    }
                    else
                    {
                        RectTransform maskRect = revealMask.GetComponent<RectTransform>();
                        RectMask2D mask = revealMask.GetComponent<RectMask2D>();
                        report.AppendLine("- RevealMask: anchoredPosition=" + maskRect.anchoredPosition
                            + ", sizeDelta=" + maskRect.sizeDelta
                            + ", pivot=" + maskRect.pivot + ", anchorMin=" + maskRect.anchorMin
                            + ", hasRectMask2D=" + (mask != null));

                        Transform wordmarkImage = revealMask.Find("WordmarkImage");
                        if (wordmarkImage == null)
                        {
                            report.AppendLine("- WordmarkImage: NOT FOUND");
                        }
                        else
                        {
                            RectTransform wordmarkRect = wordmarkImage.GetComponent<RectTransform>();
                            Image image = wordmarkImage.GetComponent<Image>();
                            report.AppendLine("- WordmarkImage: sizeDelta=" + wordmarkRect.sizeDelta
                                + " (must stay constant at runtime), sprite="
                                + (image != null && image.sprite != null ? image.sprite.name : "NONE"));
                        }
                    }
                }

                Transform glint = logoGroup.Find("RevealGlint");
                if (glint == null)
                {
                    report.AppendLine("- RevealGlint: NOT FOUND");
                }
                else
                {
                    RectTransform glintRect = glint.GetComponent<RectTransform>();
                    bool hasGradient = glint.GetComponent<ProceduralHorizontalGradientGraphic>() != null;
                    report.AppendLine("- RevealGlint: anchoredPosition=" + glintRect.anchoredPosition
                        + ", sizeDelta=" + glintRect.sizeDelta + ", hasGradientGraphic=" + hasGradient);
                }
            }

            AudioCueLibrary reportLibrary =
                AssetDatabase.LoadAssetAtPath<AudioCueLibrary>(AudioCueLibrarySetup.LibraryPath);
            report.AppendLine("- AudioCueLibrary at " + AudioCueLibrarySetup.LibraryPath + ": "
                + (reportLibrary != null ? "found, " + reportLibrary.CueCount + " cue(s)" : "NOT FOUND"));

            GameObject introAudioObject = FindRoot(scene, IntroAudioName);
            if (introAudioObject == null)
            {
                report.AppendLine("- IntroAudio: NOT FOUND");
            }
            else
            {
                AudioSource introAudioSource = introAudioObject.GetComponent<AudioSource>();
                report.AppendLine("- IntroAudio.AudioSource: "
                    + (introAudioSource != null
                        ? "playOnAwake=" + introAudioSource.playOnAwake + ", loop=" + introAudioSource.loop
                        : "NOT FOUND"));
            }

            GameObject controllerObject = FindRoot(scene, BootstrapControllerName);
            BootstrapController controller = controllerObject != null
                ? controllerObject.GetComponent<BootstrapController>()
                : null;
            if (controller == null)
            {
                report.AppendLine("- BootstrapController: NOT FOUND");
            }
            else
            {
                SerializedObject serializedController = new SerializedObject(controller);
                SerializedProperty introProperty = serializedController.FindProperty("introSequence");
                Object introRef = introProperty != null ? introProperty.objectReferenceValue : null;
                report.AppendLine("- BootstrapController.introSequence: "
                    + (introRef != null ? "assigned" : "NULL"));
                report.AppendLine("- BootstrapController.audioService: "
                    + DescribeRef(serializedController, "audioService"));

                if (introRef is IntroSequenceController introController)
                {
                    SerializedObject serializedIntro = new SerializedObject(introController);
                    report.AppendLine("- IntroSequenceController.logoCanvasGroup: "
                        + DescribeRef(serializedIntro, "logoCanvasGroup"));
                    report.AppendLine("- IntroSequenceController.logoRectTransform: "
                        + DescribeRef(serializedIntro, "logoRectTransform"));
                    report.AppendLine("- IntroSequenceController.markImage: "
                        + DescribeRef(serializedIntro, "markImage"));
                    report.AppendLine("- IntroSequenceController.wordmarkImage: "
                        + DescribeRef(serializedIntro, "wordmarkImage"));
                    report.AppendLine("- IntroSequenceController.wordmarkRevealMaskRect: "
                        + DescribeRef(serializedIntro, "wordmarkRevealMaskRect"));
                    report.AppendLine("- IntroSequenceController.wordmarkGlintImage: "
                        + DescribeRef(serializedIntro, "wordmarkGlintImage"));
                    report.AppendLine("- IntroSequenceController.audioService: "
                        + DescribeRef(serializedIntro, "audioService"));
                }
            }

            Debug.Log(report.ToString());
        }

        private static void ReportSpriteState(StringBuilder report, string label, string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                report.AppendLine("- " + label + " importer: NOT FOUND at " + assetPath);
                return;
            }

            bool isSingle = importer.spriteImportMode == SpriteImportMode.Single;
            report.AppendLine("- " + label + " importer Sprite Mode: " + importer.spriteImportMode
                + (isSingle ? " (OK)" : " (SHOULD BE Single)"));

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            report.AppendLine(sprite != null
                ? "- " + label + " sprite: '" + sprite.name + "', rect " + sprite.rect.width + "x"
                    + sprite.rect.height
                : "- " + label + " sprite: NONE");
        }

        private static string DescribeRef(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return "FIELD NOT FOUND";
            }

            Object value = property.objectReferenceValue;
            return value != null ? value.name + " (" + value.GetType().Name + ")" : "NULL";
        }

        private static void WireBootstrapController(
            Scene scene, IntroSequenceController introController, AudioService introAudioService)
        {
            GameObject controllerObject = FindRoot(scene, BootstrapControllerName);
            if (controllerObject == null)
            {
                Debug.LogError(
                    "[IntroSceneSetup] '" + BootstrapControllerName + "' GameObject not found in "
                    + "Bootstrap.unity; the intro was created but not wired to Bootstrap.");
                return;
            }

            BootstrapController controller = controllerObject.GetComponent<BootstrapController>();
            if (controller == null)
            {
                Debug.LogError(
                    "[IntroSceneSetup] '" + BootstrapControllerName + "' has no BootstrapController "
                    + "component; the intro was created but not wired to Bootstrap.");
                return;
            }

            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty introProperty = serializedController.FindProperty("introSequence");
            if (introProperty == null)
            {
                Debug.LogError(
                    "[IntroSceneSetup] BootstrapController has no 'introSequence' field to wire.");
                return;
            }

            introProperty.objectReferenceValue = introController;

            // BootstrapController.ApplySettings only forwards master volume/mute to this field when
            // it is assigned; without it the intro would still be wired to play cues, just always
            // at whatever the AudioSource's own default volume happens to be.
            SerializedProperty audioProperty = serializedController.FindProperty("audioService");
            if (audioProperty != null)
            {
                audioProperty.objectReferenceValue = introAudioService;
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();
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
        /// Builds/updates a scene-root <c>IntroAudio</c> object carrying the <see cref="AudioSource"/>
        /// + <see cref="AudioService"/> pair the intro plays its three cues through, wired to the
        /// project's existing <see cref="AudioCueLibrary"/> (<see cref="AudioCueLibrarySetup.LibraryPath"/>)
        /// exactly like every other scene's audio setup — no bespoke intro-only audio plumbing.
        /// </summary>
        private static AudioService EnsureIntroAudio(Scene scene)
        {
            GameObject audioObject = FindRoot(scene, IntroAudioName) ?? new GameObject(IntroAudioName);

            AudioSource audioSource = EnsureComponent<AudioSource>(audioObject);
            Undo.RecordObject(audioSource, "Configure Intro audio source");
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;

            AudioService audioService = EnsureComponent<AudioService>(audioObject);
            AudioCueLibrary library =
                AssetDatabase.LoadAssetAtPath<AudioCueLibrary>(AudioCueLibrarySetup.LibraryPath);
            audioService.SetAuthoringReferences(audioSource, library);

            if (library == null)
            {
                Debug.LogWarning(
                    "[IntroSceneSetup] No AudioCueLibrary found at '" + AudioCueLibrarySetup.LibraryPath
                    + "'; the intro is wired but plays silently until it exists.");
            }

            return audioService;
        }

        private static GameObject EnsureIntroCanvas(Scene scene)
        {
            GameObject canvasObject = FindRoot(scene, CanvasName)
                ?? new GameObject(CanvasName, typeof(RectTransform));

            Canvas canvas = EnsureComponent<Canvas>(canvasObject);
            Undo.RecordObject(canvas, "Configure IntroCanvas");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = IntroCanvasSortingOrder;

            CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvasObject);
            Undo.RecordObject(scaler, "Configure IntroCanvas");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            EnsureComponent<GraphicRaycaster>(canvasObject);

            return canvasObject;
        }

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

        private static void ConfigureLogoGroupRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, LogoGroupVerticalOffset);
            // Pure positioning/animation node: MarkImage and WordmarkRevealRoot define their own
            // sizes and are centred within this, so LogoGroup itself needs no size.
            rect.sizeDelta = Vector2.zero;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
