using System;
using System.Runtime.CompilerServices;
using System.Text;
using RoyalDecisions.Composition;
using RoyalDecisions.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// EnsureComponent<T> below has no scene-file dependency (unlike SceneSetupAutomation's heavier
// ...ForTests(scene, ...) pattern for testing whole-scene composition) — it only needs a GameObject,
// so exposing it to the test assembly this way is proportionate to testing it directly rather than
// routing every case through a full Bootstrap.unity open/save cycle.
[assembly: InternalsVisibleTo("RoyalDecisions.Tests.EditMode")]

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Creates and wires the startup loading screen (<c>LoadingCanvas/LoadingRoot</c>,
    /// <see cref="StartupLoadingController"/>) inside <c>Bootstrap.unity</c>, and wires it into
    /// <see cref="BootstrapController.loadingSequence"/>, without hand-authored scene YAML.
    /// </summary>
    /// <remarks>
    /// Mirrors <see cref="IntroSceneSetup"/>: small, self-contained, every step finds-or-creates
    /// rather than duplicating, so re-running is always safe. Deliberately only ever touches its own
    /// known nodes (<c>LoadingCanvas</c> and its children) plus the single
    /// <c>loadingSequence</c> field on <c>BootstrapController</c> — it never renames, moves, or
    /// removes <c>IntroCanvas</c>, <c>EventSystem</c>, or any field <see cref="IntroSceneSetup"/>
    /// already wired, so running this after (or before) Apply Intro Setup is always safe in either
    /// order.
    /// </remarks>
    public static class StartupLoadingSetup
    {
        private const string BootstrapScenePath = "Assets/_Game/scenes/Bootstrap.unity";

        /// <summary>
        /// Where the Editor looks for the replaceable background artwork. Only this setup tool ever
        /// reads this path — <see cref="StartupLoadingController"/> only ever sees whatever Sprite is
        /// already assigned to its Artwork Image, so swapping this file for another later (or simply
        /// dragging a different Sprite onto Artwork's Source Image in the Inspector) needs no code
        /// change.
        /// </summary>
        public const string BackgroundArtPath = "Assets/_Game/Art/Branding/LoadingBackground.png";

        private const string CanvasName = "LoadingCanvas";
        private const string EventSystemName = "EventSystem";
        private const string RootName = "LoadingRoot";
        private const string SafeContentName = "SafeContent";
        private const string BootstrapControllerName = "BootstrapController";
        private const string IntroCanvasName = "IntroCanvas";

        // Screen Space Overlay canvases with equal sort order fall back to an unreliable draw order;
        // an explicit, higher sort order guarantees LoadingCanvas always paints over IntroCanvas
        // while it is visible, without ever touching IntroCanvas itself. Must stay above
        // IntroSceneSetup.IntroCanvasSortingOrder (20): IntroCanvas's own BlackBackground is a plain,
        // permanently opaque Image that IntroSequenceController never fades (only the logo group
        // fades, back to that same black, not away from it) — so LoadingCanvas can only ever become
        // visible by outranking IntroCanvas's sort order, never by anything on IntroCanvas's own side
        // becoming transparent. Previously 10, which is lower than IntroCanvas's 20 — that silently
        // painted IntroCanvas's opaque background over the entire loading screen (blood tube
        // included) for as long as both canvases existed in the scene, which is the whole loading
        // sequence. See ValidateAfterApply/ReportCurrentState below, which now actually check this
        // relationship instead of only checking sortingOrder > 0.
        private const int LoadingCanvasSortingOrder = 30;

        // Percentage sits below the tube; status sits above it. Both are margin-based (stretch
        // anchors, fixed inset), never a fixed absolute width anchored at a point, so neither
        // overflows the canvas on tall aspect ratios. With ScaleWithScreenSize matched fully to
        // height, a 1080x2520 device's *effective* canvas width shrinks to 1080 * 1920 / 2520 ~= 823
        // reference units — this is exactly why the tube itself (below) is sized as a fraction of its
        // parent's width rather than a fixed unit count too.
        private const float StatusBottomOffset = 360f;
        private const float PercentageBottomOffset = 130f;
        private const float ContentBandHeight = 460f;
        private const float StatusCharacterSpacing = 2f;

        // --- Blood tube -----------------------------------------------------------------------
        // Width as a fraction of the parent's own (already safe-area-correct) width, not a fixed
        // pixel/reference-unit count — the CanvasScaler math above means a fixed width that looks
        // right at 1080x1920 can overflow or under-fill at other aspect ratios; a fraction always
        // scales correctly. 0.74 sits in the middle of the requested 70-78% range.
        private const float TubeWidthFraction = 0.74f;
        private const float TubeBottomOffset = 240f;

        // Generated once from Assets/_Game/Art/Loading/BloodTube/BloodTubeGeneratedSheet.png via a
        // local, deterministic Python/Pillow pass (never shipped or referenced directly by any
        // Unity asset) — see the tool's report for the exact source-sheet regions used. All three
        // (four) runtime sprites below live only under Art/Loading/BloodTube/.
        private const string BloodTubeArtRoot = "Assets/_Game/Art/Loading/BloodTube";
        private const string BloodTubeFramePath = BloodTubeArtRoot + "/BloodTubeFrame.png";
        private const string BloodFillPath = BloodTubeArtRoot + "/BloodFill.png";
        private const string BloodLeadingEdgePath = BloodTubeArtRoot + "/BloodLeadingEdge.png";
        private const string GlassHighlightPath = BloodTubeArtRoot + "/GlassHighlight.png";

        // BloodTubeFrame.png is 1407x191 — its own aspect ratio, so BloodTube's height is always
        // derived from its (percentage-of-parent) width rather than a fixed reference-unit height,
        // the same "fraction, not fixed pixels" reasoning as TubeWidthFraction above.
        private const float FrameNaturalWidth = 1407f;
        private const float FrameNaturalHeight = 191f;
        private const float FrameAspectRatio = FrameNaturalWidth / FrameNaturalHeight;

        // The transparent liquid window baked into BloodTubeFrame.png sits at local pixel rect
        // x:210-1180, y:46-152 of its 1407x191 canvas (found by sampling the source sheet's fog
        // vignette, then hard-clearing that exact rectangle to alpha 0 — see the tool's report).
        // Expressed as fractions of the frame's own size so BloodWindow (below) always lines up
        // with the see-through part of the artwork at any screen size. Y is flipped from PIL's
        // top-down pixel rows to Unity's bottom-up anchor space.
        private const float WindowXMinFraction = 210f / FrameNaturalWidth;
        private const float WindowXMaxFraction = 1180f / FrameNaturalWidth;
        private const float WindowYMinFraction = 1f - (152f / FrameNaturalHeight);
        private const float WindowYMaxFraction = 1f - (46f / FrameNaturalHeight);

        // BloodFill.png (1020x106) and BloodLeadingEdge.png (147x106) natural aspect ratios, used
        // to size each sprite responsively via AspectRatioFitter instead of a hard-coded pixel size.
        private const float BloodLeadingEdgeAspectRatio = 147f / 106f;

        // GlassHighlight.png (517x98) natural aspect ratio, and where it sits within the window:
        // centred, spanning most of the window's width, in the window's upper portion.
        private const float GlassHighlightAspectRatio = 517f / 98f;
        private const float GlassHighlightWidthFractionOfWindow = 0.82f;
        private const float GlassHighlightVerticalFractionWithinWindow = 0.78f;

        // Small left/right inset for BloodMask within BloodWindow — mirrors the old
        // TubeContentPadding convention StartupLoadingController.ComputeTubeInnerWidth already
        // assumes (mask inset from its parent's left edge, mirrored on the right).
        private const float BloodMaskInset = 4f;

        // Bottom-weighted readability scrim stops (index 0 = bottom, last = top — see
        // ProceduralVerticalGradientGraphic). Alpha only, RGB lives in OverlayColour.
        private const float OverlayBottomAlpha = 0.6f;
        private const float OverlayMidAlpha = 0.22f;
        private const float OverlayTopAlpha = 0f;

        private static readonly Color OverlayColour = new Color(0.03f, 0.02f, 0.02f, 1f);
        private static readonly Color StatusTextColour = new Color32(0xF2, 0xE7, 0xCF, 0xFF);
        private static readonly Color PercentageTextColour = new Color32(0xD9, 0xC2, 0x8B, 0xFF);
        private static readonly Color TextShadowColour = new Color(0f, 0f, 0f, 0.45f);

        [MenuItem("Tools/Royal Decisions/Scene Setup/Loading/Apply Loading Setup")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play Mode before applying Loading Setup.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[StartupLoadingSetup] Cancelled: unsaved scenes.");
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
                // exception here means Bootstrap.unity on disk was never touched.
                Debug.LogError("[StartupLoadingSetup] Apply failed: " + exception);
            }
            finally
            {
                if (originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        [MenuItem("Tools/Royal Decisions/Scene Setup/Loading/Validate Loading Setup")]
        public static void ValidateMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play Mode before validating Loading Setup.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[StartupLoadingSetup] Validate cancelled: unsaved scenes.");
                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                ReportCurrentState();
            }
            catch (Exception exception)
            {
                Debug.LogError("[StartupLoadingSetup] Validate failed: " + exception);
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
                Debug.LogError("[StartupLoadingSetup] Could not open " + BootstrapScenePath);
                return;
            }

            // Reused, never duplicated: IntroSceneSetup already creates this root for the intro's
            // own input handling, and a scene must only ever have one EventSystem.
            EnsureEventSystem(scene);

            GameObject canvasObject = EnsureLoadingCanvas(scene);

            GameObject root = EnsureChild(canvasObject.transform, RootName);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Undo.RecordObject(rootRect, "Configure LoadingRoot");
            Stretch(rootRect);
            CanvasGroup rootGroup = EnsureComponent<CanvasGroup>(root);
            Undo.RecordObject(rootGroup, "Configure LoadingRoot");
            rootGroup.alpha = 1f;
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = false;

            (Image background, AspectRatioFitter backgroundFitter) = EnsureBackground(root.transform);
            EnsureDarkOverlay(root.transform);

            // Background/DarkOverlay stay full-bleed (the artwork is meant to draw under notches and
            // gesture bars, same as everywhere else in the project); only the actual text/bar content
            // needs to stay clear of unsafe edges, so only this wrapper gets a SafeAreaFitter — the
            // same component and pattern the Game scene's own "SafeArea" object already uses, not a
            // new safe-area architecture.
            GameObject safeContent = EnsureChild(root.transform, SafeContentName);
            RectTransform safeContentRect = safeContent.GetComponent<RectTransform>();
            Undo.RecordObject(safeContentRect, "Configure SafeContent");
            Stretch(safeContentRect);
            EnsureComponent<SafeAreaFitter>(safeContent);

            MigrateLegacyLoadingContent(root.transform, safeContent.transform);

            (TMP_Text statusText, TMP_Text percentageText, CanvasGroup contentGroup,
                RectTransform bloodMask, Graphic bloodFill, Graphic bloodLeadingEdge,
                RectTransform tubeInterior) = EnsureLoadingContent(safeContent.transform);

            StartupLoadingController controller = EnsureComponent<StartupLoadingController>(root);

            // Read back whatever showPercentage already is (true for a brand-new component, since
            // that is the field's own compile-time default) before SetAuthoringReferences touches it
            // — otherwise every re-run would silently reset a user's manual Inspector toggle back to
            // true, which is exactly the kind of clobbering an idempotent tool must never do.
            bool existingShowPercentage = new SerializedObject(controller)
                .FindProperty("showPercentage")?.boolValue ?? true;

            controller.SetAuthoringReferences(
                rootGroup, background, backgroundFitter, statusText, percentageText, contentGroup,
                existingShowPercentage);
            controller.SetBloodTubeAuthoringReferences(bloodMask, bloodFill, bloodLeadingEdge, tubeInterior);

            WireBootstrapController(scene, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, BootstrapScenePath))
            {
                Debug.LogError("[StartupLoadingSetup] Bootstrap scene could not be saved.");
                return;
            }

            Debug.Log("[StartupLoadingSetup] Bootstrap loading wiring applied.");
            ValidateAfterApply(scene);
        }

        /// <summary>
        /// Wires <see cref="StartupLoadingController"/> into <c>BootstrapController.loadingSequence</c>
        /// only — the same single-property pattern <c>IntroSceneSetup.WireBootstrapController</c>
        /// uses for <c>introSequence</c>/<c>audioService</c>, so the two tools can run in either order
        /// without either one clobbering the other's field.
        /// </summary>
        private static void WireBootstrapController(Scene scene, StartupLoadingController loadingController)
        {
            GameObject controllerObject = FindRoot(scene, BootstrapControllerName);
            if (controllerObject == null)
            {
                Debug.LogError(
                    "[StartupLoadingSetup] '" + BootstrapControllerName + "' GameObject not found in "
                    + "Bootstrap.unity; the loading screen was created but not wired to Bootstrap.");
                return;
            }

            BootstrapController controller = controllerObject.GetComponent<BootstrapController>();
            if (controller == null)
            {
                Debug.LogError(
                    "[StartupLoadingSetup] '" + BootstrapControllerName + "' has no BootstrapController "
                    + "component; the loading screen was created but not wired to Bootstrap.");
                return;
            }

            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty loadingProperty = serializedController.FindProperty("loadingSequence");
            if (loadingProperty == null)
            {
                Debug.LogError(
                    "[StartupLoadingSetup] BootstrapController has no 'loadingSequence' field to wire.");
                return;
            }

            loadingProperty.objectReferenceValue = loadingController;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Earlier revisions parented <c>LoadingContent</c> directly under <c>LoadingRoot</c>.
        /// Re-running Apply after <c>SafeContent</c> was introduced must not leave that old copy
        /// behind as an orphaned duplicate alongside a freshly created one under
        /// <c>SafeContent</c> — so if found at the old location, it is reparented (not destroyed and
        /// recreated) to preserve its existing component instances and any external references.
        /// </summary>
        private static void MigrateLegacyLoadingContent(Transform legacyParent, Transform newParent)
        {
            Transform legacyContent = legacyParent.Find("LoadingContent");
            if (legacyContent == null)
            {
                return;
            }

            Undo.SetTransformParent(legacyContent, newParent, "Migrate LoadingContent under SafeContent");
            legacyContent.SetAsLastSibling();
        }

        private static (Image artwork, AspectRatioFitter fitter) EnsureBackground(Transform parent)
        {
            GameObject surfaceObject = EnsureChild(parent, "Background");
            surfaceObject.transform.SetSiblingIndex(0);
            RectTransform surfaceRect = surfaceObject.GetComponent<RectTransform>();
            Undo.RecordObject(surfaceRect, "Configure Background");
            Stretch(surfaceRect);
            Image surfaceImage = EnsureComponent<Image>(surfaceObject);
            Undo.RecordObject(surfaceImage, "Configure Background");
            surfaceImage.sprite = null;
            surfaceImage.color = Color.black;
            surfaceImage.raycastTarget = false;

            GameObject artworkObject = EnsureChild(surfaceObject.transform, "Artwork");
            RectTransform artworkRect = artworkObject.GetComponent<RectTransform>();
            Undo.RecordObject(artworkRect, "Configure Artwork");
            // Cover-fit: fills the viewport and crops overflow instead of stretching or
            // letterboxing. EnvelopeParent needs the rect free to resize, so it is centred rather
            // than Stretch-anchored like Background/DarkOverlay.
            artworkRect.anchorMin = new Vector2(0.5f, 0.5f);
            artworkRect.anchorMax = new Vector2(0.5f, 0.5f);
            artworkRect.pivot = new Vector2(0.5f, 0.5f);
            artworkRect.anchoredPosition = Vector2.zero;
            artworkRect.sizeDelta = Vector2.zero;

            Image artworkImage = EnsureComponent<Image>(artworkObject);
            Undo.RecordObject(artworkImage, "Configure Artwork");
            artworkImage.raycastTarget = false;

            AspectRatioFitter fitter = EnsureComponent<AspectRatioFitter>(artworkObject);
            Undo.RecordObject(fitter, "Configure Artwork");

            Sprite backgroundSprite = EnsureBackgroundSprite();
            ApplyCoverFit(fitter, backgroundSprite);
            if (backgroundSprite != null)
            {
                artworkImage.sprite = backgroundSprite;
            }

            return (artworkImage, fitter);
        }

        /// <summary>
        /// Bakes a valid cover-fit ratio into the authored scene instead of leaving it for
        /// <see cref="StartupLoadingController"/> to compute on the first <c>Awake()</c>. Root cause
        /// of the "background not visible" bug this setup previously shipped: this method used to set
        /// only <see cref="AspectRatioFitter.aspectMode"/> and never <see cref="AspectRatioFitter.aspectRatio"/>,
        /// so the saved scene carried Unity's uninitialised value for that field. With no aspect ratio,
        /// <c>EnvelopeParent</c> resolves the Artwork RectTransform's size to <c>NaN</c>, which is
        /// exactly what a screenshot mostly-black background looks like — the opaque black fallback
        /// Image on the parent "Background" object is all that is left visible underneath a
        /// degenerate, invisible child. Mirrors <see cref="StartupLoadingController.ApplyBackgroundFallback"/>
        /// exactly so the Editor Game View (no Play Mode needed) and the runtime Awake() path always agree.
        /// </summary>
        private static void ApplyCoverFit(AspectRatioFitter fitter, Sprite sprite)
        {
            if (sprite == null || sprite.rect.height <= 0f)
            {
                fitter.enabled = false;
                return;
            }

            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            fitter.enabled = true;
        }

        /// <summary>
        /// Forces <see cref="BackgroundArtPath"/> to Sprite Mode "Single" and reimports it, mirroring
        /// <c>IntroSceneSetup.EnsureLogoIsSingleSprite</c>. Returns null (rather than throwing) if the
        /// PNG has not been imported by Unity yet — <see cref="StartupLoadingController"/> already
        /// shows its black fallback in that case, so Apply always leaves a usable loading screen.
        /// </summary>
        private static Sprite EnsureBackgroundSprite()
        {
            TextureImporter importer = AssetImporter.GetAtPath(BackgroundArtPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning(
                    "[StartupLoadingSetup] No importer found at '" + BackgroundArtPath + "' yet. The "
                    + "loading screen will show its black fallback until Unity has imported this PNG "
                    + "— re-run Apply Loading Setup afterwards, or assign a Sprite by hand to "
                    + "LoadingCanvas/LoadingRoot/Background/Artwork.");
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                Debug.Log(
                    "[StartupLoadingSetup] '" + BackgroundArtPath + "' set to Sprite Mode 'Single' "
                    + "and reimported.");
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundArtPath);
            if (sprite == null)
            {
                Debug.LogWarning("[StartupLoadingSetup] No sprite found at '" + BackgroundArtPath + "'.");
            }

            return sprite;
        }

        /// <summary>
        /// A bottom-weighted scrim instead of a flat full-screen tint: fully transparent at the top
        /// (the artwork's own darkest tones already read fine there) and only reaching moderate
        /// darkening behind <c>LoadingContent</c> at the bottom, so the background stays the dominant
        /// visual element instead of looking covered by "a large black layer".
        /// </summary>
        private static void EnsureDarkOverlay(Transform parent)
        {
            GameObject overlayObject = EnsureChild(parent, "DarkOverlay");
            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            Undo.RecordObject(overlayRect, "Configure DarkOverlay");
            Stretch(overlayRect);

            // Earlier revisions used a flat Image here. A GameObject may only drive one Graphic
            // through its single CanvasRenderer, so the stale one is removed before adding the
            // gradient — otherwise re-running Apply would leave two Graphics fighting over it.
            Image staleImage = overlayObject.GetComponent<Image>();
            if (staleImage != null)
            {
                Undo.DestroyObjectImmediate(staleImage);
            }

            ProceduralVerticalGradientGraphic overlayGraphic =
                EnsureComponent<ProceduralVerticalGradientGraphic>(overlayObject);
            Undo.RecordObject(overlayGraphic, "Configure DarkOverlay");
            overlayGraphic.color = OverlayColour;
            overlayGraphic.raycastTarget = false;
            // Index 0 is the bottom of the rect, last index is the top (see
            // ProceduralVerticalGradientGraphic.OnPopulateMesh).
            overlayGraphic.SetStops(OverlayBottomAlpha, OverlayMidAlpha, OverlayTopAlpha);
        }

        private static (TMP_Text status, TMP_Text percentage, CanvasGroup contentGroup,
            RectTransform bloodMask, Graphic bloodFill, Graphic bloodLeadingEdge,
            RectTransform tubeInterior) EnsureLoadingContent(Transform parent)
        {
            GameObject contentObject = EnsureChild(parent, "LoadingContent");
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            Undo.RecordObject(contentRect, "Configure LoadingContent");
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 0f);
            contentRect.pivot = new Vector2(0.5f, 0f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, ContentBandHeight);

            // Lets StartupLoadingController fade LoadingContent in from transparent when loading
            // begins; authored at alpha 1 so the Editor Game View (no Play Mode) always shows the
            // finished layout, exactly like LoadingRoot's own CanvasGroup.
            CanvasGroup contentGroup = EnsureComponent<CanvasGroup>(contentObject);
            Undo.RecordObject(contentGroup, "Configure LoadingContent");
            contentGroup.alpha = 1f;
            contentGroup.interactable = false;
            // CanvasGroup.blocksRaycasts ANDs together across every ancestor group, not just the
            // nearest one — leaving this true keeps LoadingRoot's own blocksRaycasts=true (which
            // deliberately blocks input to whatever is underneath while loading is visible) intact.
            contentGroup.blocksRaycasts = true;

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                TurkishGlyphValidator.FontAssetPath);

            TMP_Text statusText = EnsureText(
                contentObject.transform, "StatusText", "YÜKLENİYOR...",
                StatusTextColour, 42f, StatusBottomOffset, font,
                characterSpacing: StatusCharacterSpacing, withShadow: true);

            // Earlier revisions built a flat ProgressBarTrack/ProgressBarFill bar directly here; the
            // blood tube replaces it entirely, so any leftover copy is removed rather than left
            // behind as a duplicate, orphaned visual.
            RemoveLegacyProgressBar(contentObject.transform);

            (RectTransform bloodMask, Graphic bloodFill, Graphic bloodLeadingEdge,
                RectTransform tubeInterior) = EnsureBloodTube(contentObject.transform);

            TMP_Text percentageText = EnsureText(
                contentObject.transform, "PercentageText", "0%",
                PercentageTextColour, 34f, PercentageBottomOffset, font);

            return (statusText, percentageText, contentGroup, bloodMask, bloodFill, bloodLeadingEdge,
                tubeInterior);
        }

        /// <summary>Destroys the pre-blood-tube flat progress bar if a scene still has one, so
        /// re-running Apply never leaves it behind as a duplicate alongside the tube.</summary>
        private static void RemoveLegacyProgressBar(Transform contentParent)
        {
            Transform legacyTrack = contentParent.Find("ProgressBarTrack");
            if (legacyTrack != null)
            {
                Undo.DestroyObjectImmediate(legacyTrack.gameObject);
            }
        }

        /// <summary>
        /// Builds the asset-based blood-tube loading indicator: a plain <see cref="Image"/> showing
        /// <c>BloodFill.png</c>, revealed by an actual <see cref="RectMask2D"/> (never a horizontal
        /// scale of the artwork itself); <c>BloodTubeFrame.png</c> on top, whose baked-in transparent
        /// window is exactly where <c>BloodWindow</c> is anchored, so its opaque metal caps and glass
        /// borders contain the fill's rectangular clip edges; and a restrained
        /// <c>GlassHighlight.png</c> sheen rendered last, on top of everything. Every piece before the
        /// old procedural tube existed here is destroyed first (see
        /// <see cref="MigrateLegacyBloodTube"/>) so a repeat run never leaves both versions visible.
        /// BloodMask, BloodFill and BloodLeadingEdge are all parented under BloodWindow so they share
        /// one coordinate space — <see cref="StartupLoadingController.ComputeTubeInnerWidth"/> depends
        /// on that, exactly as it did for the old TubeInterior.
        /// </summary>
        private static (RectTransform mask, Graphic fill, Graphic leadingEdge, RectTransform interior)
            EnsureBloodTube(Transform parent)
        {
            GameObject tubeObject = EnsureChild(parent, "BloodTube");
            RectTransform tubeRect = tubeObject.GetComponent<RectTransform>();
            Undo.RecordObject(tubeRect, "Configure BloodTube");
            tubeRect.anchorMin = new Vector2(0.5f - (TubeWidthFraction * 0.5f), 0f);
            tubeRect.anchorMax = new Vector2(0.5f + (TubeWidthFraction * 0.5f), 0f);
            tubeRect.pivot = new Vector2(0.5f, 0f);
            tubeRect.anchoredPosition = new Vector2(0f, TubeBottomOffset);
            tubeRect.sizeDelta = Vector2.zero;

            AspectRatioFitter tubeFitter = EnsureComponent<AspectRatioFitter>(tubeObject);
            Undo.RecordObject(tubeFitter, "Configure BloodTube");
            tubeFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            tubeFitter.aspectRatio = FrameAspectRatio;
            tubeFitter.enabled = true;

            MigrateLegacyBloodTube(tubeObject.transform);

            RectTransform windowRect = EnsureBloodWindow(tubeObject.transform);
            (RectTransform bloodMask, Graphic bloodFill) = EnsureBloodMaskAndFill(windowRect);
            Graphic leadingEdge = EnsureBloodLeadingEdge(windowRect);

            EnsureBloodTubeFrame(tubeObject.transform);
            EnsureGlassHighlight(tubeObject.transform);

            return (bloodMask, bloodFill, leadingEdge, windowRect);
        }

        /// <summary>
        /// Destroys the pre-asset-based tube's procedural nodes if a scene still has them, so
        /// re-running Apply never leaves the old shadow/frame/interior graphics rendering underneath
        /// or alongside the new sprite-based ones. Destroying "TubeInterior" cascades to remove its
        /// old nested children (the old BloodMask/BloodFill/BloodLeadingEdge/GlassHighlight) too,
        /// since Unity destroys descendants automatically — none of those old names collide with the
        /// new hierarchy's "BloodWindow"-rooted nodes built after this runs. A no-op once a scene has
        /// already migrated, which is what keeps repeat runs idempotent.
        /// </summary>
        private static void MigrateLegacyBloodTube(Transform tubeParent)
        {
            DestroyChildIfPresent(tubeParent, "TubeShadow");
            DestroyChildIfPresent(tubeParent, "TubeFrame");
            DestroyChildIfPresent(tubeParent, "TubeInterior");
        }

        private static void DestroyChildIfPresent(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }
        }

        /// <summary>
        /// Invisible layout-only rect matching exactly the transparent liquid window baked into
        /// BloodTubeFrame.png (see <see cref="WindowXMinFraction"/> etc.) — this is the asset-based
        /// replacement for the old procedural "TubeInterior" backing plate: same role (the shared
        /// coordinate space and inner-width source for <c>StartupLoadingController</c>), but no
        /// Graphic of its own since the visible glass interior now comes from the artwork itself.
        /// </summary>
        private static RectTransform EnsureBloodWindow(Transform parent)
        {
            GameObject windowObject = EnsureChild(parent, "BloodWindow");
            windowObject.transform.SetAsFirstSibling();
            RectTransform windowRect = windowObject.GetComponent<RectTransform>();
            Undo.RecordObject(windowRect, "Configure BloodWindow");
            windowRect.anchorMin = new Vector2(WindowXMinFraction, WindowYMinFraction);
            windowRect.anchorMax = new Vector2(WindowXMaxFraction, WindowYMaxFraction);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = Vector2.zero;
            return windowRect;
        }

        /// <summary>
        /// BloodMask's width is the actual progress reveal (an honest RectMask2D clip); BloodFill
        /// underneath it is always authored — and kept, every re-run — at the window's full width
        /// (see <see cref="StartupLoadingController.ApplyBloodTube"/>: only BloodMask's width depends
        /// on displayed progress, BloodFill's never does).
        /// </summary>
        private static (RectTransform mask, Graphic fill) EnsureBloodMaskAndFill(RectTransform windowRect)
        {
            GameObject maskObject = EnsureChild(windowRect, "BloodMask");
            RectTransform maskRect = maskObject.GetComponent<RectTransform>();
            Undo.RecordObject(maskRect, "Configure BloodMask");
            maskRect.anchorMin = new Vector2(0f, 0f);
            maskRect.anchorMax = new Vector2(0f, 1f);
            maskRect.pivot = new Vector2(0f, 0.5f);
            maskRect.anchoredPosition = new Vector2(BloodMaskInset, 0f);
            maskRect.sizeDelta = new Vector2(0f, -2f * BloodMaskInset);

            EnsureComponent<RectMask2D>(maskObject);

            GameObject fillObject = EnsureChild(maskObject.transform, "BloodFill");
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            Undo.RecordObject(fillRect, "Configure BloodFill");
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;

            Image fillImage = EnsureComponent<Image>(fillObject);
            Undo.RecordObject(fillImage, "Configure BloodFill");
            fillImage.raycastTarget = false;
            fillImage.type = Image.Type.Simple;
            fillImage.preserveAspect = false;
            fillImage.color = Color.white;
            Sprite fillSprite = EnsureSprite(BloodFillPath);
            if (fillSprite != null)
            {
                fillImage.sprite = fillSprite;
            }

            return (maskRect, fillImage);
        }

        /// <summary>
        /// The blood strip's own smooth rounded meniscus (not the source sheet's separate, jagged
        /// "splash" element, which read as blood spilling outside the glass) sitting at the current
        /// fill boundary. Vertically stretched to match BloodFill's own height and sized by
        /// <see cref="AspectRatioFitter.AspectMode.HeightControlsWidth"/> so its width is always
        /// correct at any screen size without a hard-coded reference-unit value. Its x is overwritten
        /// every progress update by <c>StartupLoadingController</c> — this is only its resting
        /// position — using the same left inset as <see cref="EnsureBloodMaskAndFill"/>'s BloodMask,
        /// since both are children of the same BloodWindow.
        /// </summary>
        private static Graphic EnsureBloodLeadingEdge(RectTransform windowRect)
        {
            GameObject edgeObject = EnsureChild(windowRect, "BloodLeadingEdge");
            RectTransform edgeRect = edgeObject.GetComponent<RectTransform>();
            Undo.RecordObject(edgeRect, "Configure BloodLeadingEdge");
            edgeRect.anchorMin = new Vector2(0f, 0f);
            edgeRect.anchorMax = new Vector2(0f, 1f);
            edgeRect.pivot = new Vector2(0.5f, 0.5f);
            edgeRect.anchoredPosition = new Vector2(BloodMaskInset, 0f);
            edgeRect.sizeDelta = new Vector2(0f, -2f * BloodMaskInset);

            Image edgeImage = EnsureComponent<Image>(edgeObject);
            Undo.RecordObject(edgeImage, "Configure BloodLeadingEdge");
            edgeImage.raycastTarget = false;
            edgeImage.type = Image.Type.Simple;
            edgeImage.preserveAspect = false;
            edgeImage.color = Color.white;
            Sprite edgeSprite = EnsureSprite(BloodLeadingEdgePath);
            if (edgeSprite != null)
            {
                edgeImage.sprite = edgeSprite;
            }

            AspectRatioFitter edgeFitter = EnsureComponent<AspectRatioFitter>(edgeObject);
            Undo.RecordObject(edgeFitter, "Configure BloodLeadingEdge");
            edgeFitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            edgeFitter.aspectRatio = BloodLeadingEdgeAspectRatio;
            edgeFitter.enabled = true;

            return edgeImage;
        }

        /// <summary>
        /// The metal end caps and glass casing, with the transparent liquid window baked directly
        /// into the artwork. Stretched to fill BloodTube exactly and placed after BloodWindow in
        /// sibling order, so its opaque caps/borders sit in front of (and visually contain) the
        /// fill's rectangular clip edges, while its window stays transparent over BloodWindow.
        /// </summary>
        private static void EnsureBloodTubeFrame(Transform parent)
        {
            GameObject frameObject = EnsureChild(parent, "BloodTubeFrame");
            RectTransform frameRect = frameObject.GetComponent<RectTransform>();
            Undo.RecordObject(frameRect, "Configure BloodTubeFrame");
            Stretch(frameRect);

            Image frameImage = EnsureComponent<Image>(frameObject);
            Undo.RecordObject(frameImage, "Configure BloodTubeFrame");
            frameImage.raycastTarget = false;
            frameImage.type = Image.Type.Simple;
            frameImage.preserveAspect = false;
            frameImage.color = Color.white;
            Sprite frameSprite = EnsureSprite(BloodTubeFramePath);
            if (frameSprite != null)
            {
                frameImage.sprite = frameSprite;
            }
        }

        /// <summary>
        /// A restrained glass sheen, centred within the window and sized via
        /// <see cref="AspectRatioFitter.AspectMode.WidthControlsHeight"/> off a horizontal-stretch
        /// anchor fraction of the window — the same width-fraction/aspect-fitter combination BloodTube
        /// itself uses, so no piece of this hierarchy depends on a hard-coded reference-unit size.
        /// Last sibling under BloodTube, so it renders above BloodTubeFrame and stays visible
        /// regardless of fill level.
        /// </summary>
        private static void EnsureGlassHighlight(Transform parent)
        {
            GameObject highlightObject = EnsureChild(parent, "GlassHighlight");
            highlightObject.transform.SetAsLastSibling();
            RectTransform highlightRect = highlightObject.GetComponent<RectTransform>();
            Undo.RecordObject(highlightRect, "Configure GlassHighlight");

            float windowCenterX = (WindowXMinFraction + WindowXMaxFraction) * 0.5f;
            float windowWidthFraction = WindowXMaxFraction - WindowXMinFraction;
            float halfHighlightWidthFraction = windowWidthFraction * GlassHighlightWidthFractionOfWindow * 0.5f;
            float highlightY = Mathf.Lerp(
                WindowYMinFraction, WindowYMaxFraction, GlassHighlightVerticalFractionWithinWindow);

            highlightRect.anchorMin = new Vector2(windowCenterX - halfHighlightWidthFraction, highlightY);
            highlightRect.anchorMax = new Vector2(windowCenterX + halfHighlightWidthFraction, highlightY);
            highlightRect.pivot = new Vector2(0.5f, 0.5f);
            highlightRect.anchoredPosition = Vector2.zero;
            highlightRect.sizeDelta = Vector2.zero;

            Image highlightImage = EnsureComponent<Image>(highlightObject);
            Undo.RecordObject(highlightImage, "Configure GlassHighlight");
            highlightImage.raycastTarget = false;
            highlightImage.type = Image.Type.Simple;
            highlightImage.preserveAspect = false;
            highlightImage.color = Color.white;
            Sprite highlightSprite = EnsureSprite(GlassHighlightPath);
            if (highlightSprite != null)
            {
                highlightImage.sprite = highlightSprite;
            }

            AspectRatioFitter highlightFitter = EnsureComponent<AspectRatioFitter>(highlightObject);
            Undo.RecordObject(highlightFitter, "Configure GlassHighlight");
            highlightFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            highlightFitter.aspectRatio = GlassHighlightAspectRatio;
            highlightFitter.enabled = true;
        }

        /// <summary>
        /// Forces a generated BloodTube PNG to Sprite Mode "Single" and reimports it, generalising
        /// <see cref="EnsureBackgroundSprite"/>'s pattern for the three (four) runtime sprites under
        /// <see cref="BloodTubeArtRoot"/>. Returns null (rather than throwing) if the PNG has not been
        /// imported by Unity yet — every caller already leaves that Image without a sprite in that
        /// case rather than blocking the rest of Apply.
        /// </summary>
        private static Sprite EnsureSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning(
                    "[StartupLoadingSetup] No importer found at '" + path + "' yet. Re-run Apply "
                    + "Loading Setup once Unity has imported this PNG.");
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                Debug.Log("[StartupLoadingSetup] '" + path + "' set to Sprite Mode 'Single' and reimported.");
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning("[StartupLoadingSetup] No sprite found at '" + path + "'.");
            }

            return sprite;
        }

        private static TMP_Text EnsureText(
            Transform parent, string name, string defaultText, Color color, float fontSize,
            float bottomOffset, TMP_FontAsset font, float characterSpacing = 0f, bool withShadow = false)
        {
            GameObject textObject = EnsureChild(parent, name);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Configure " + name);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottomOffset);
            rect.sizeDelta = new Vector2(-80f, 70f);

            TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(textObject);
            Undo.RecordObject(text, "Configure " + name);
            text.text = defaultText;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.fontSize = fontSize;
            text.characterSpacing = characterSpacing;
            text.raycastTarget = false;
            if (font != null)
            {
                text.font = font;
            }

            if (withShadow)
            {
                Shadow shadow = EnsureComponent<Shadow>(textObject);
                Undo.RecordObject(shadow, "Configure " + name);
                shadow.effectColor = TextShadowColour;
                shadow.effectDistance = new Vector2(0f, -2f);
                shadow.useGraphicAlpha = true;
            }

            return text;
        }

        private static GameObject EnsureLoadingCanvas(Scene scene)
        {
            GameObject canvasObject = FindRoot(scene, CanvasName)
                ?? new GameObject(CanvasName, typeof(RectTransform));

            Canvas canvas = EnsureComponent<Canvas>(canvasObject);
            Undo.RecordObject(canvas, "Configure LoadingCanvas");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = LoadingCanvasSortingOrder;

            CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvasObject);
            Undo.RecordObject(scaler, "Configure LoadingCanvas");
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

        /// <summary>Logs a clear pass/fail summary immediately after Apply saves the scene.</summary>
        private static void ValidateAfterApply(Scene scene)
        {
            bool ok = true;

            GameObject introCanvasObject = FindRoot(scene, IntroCanvasName);
            if (introCanvasObject == null)
            {
                Debug.LogError(
                    "[StartupLoadingSetup] Validation: '" + IntroCanvasName + "' is missing after "
                    + "Apply — this tool must never remove it.");
                ok = false;
            }
            else
            {
                ok &= ValidateLoadingRendersAboveIntro(scene, introCanvasObject);
            }

            GameObject controllerObject = FindRoot(scene, BootstrapControllerName);
            BootstrapController controller = controllerObject != null
                ? controllerObject.GetComponent<BootstrapController>()
                : null;
            if (controller == null)
            {
                Debug.LogError("[StartupLoadingSetup] Validation: BootstrapController not found.");
                ok = false;
            }
            else
            {
                SerializedObject serializedController = new SerializedObject(controller);
                ok &= ValidateReference(serializedController, "loadingSequence", "BootstrapController.loadingSequence");
                // Read-only sanity check: confirms this tool's save did not clobber the intro wiring
                // IntroSceneSetup already applied. Never written to.
                SerializedProperty introProperty = serializedController.FindProperty("introSequence");
                if (introProperty != null && introProperty.objectReferenceValue == null)
                {
                    Debug.LogWarning(
                        "[StartupLoadingSetup] Validation: BootstrapController.introSequence is "
                        + "unassigned. If Apply Intro Setup was already run, this is unexpected — "
                        + "otherwise, run it too.");
                }
            }

            Debug.Log(ok
                ? "[StartupLoadingSetup] Validation passed: loading hierarchy and wiring are correct."
                : "[StartupLoadingSetup] Validation FAILED — see errors above.");
        }

        /// <summary>
        /// IntroCanvas carries a permanently opaque BlackBackground that IntroSequenceController
        /// never fades — the only way LoadingCanvas (and everything under it, including the blood
        /// tube) can ever actually be seen is by outranking IntroCanvas's own sort order. A sort
        /// order that merely happens to be positive is not sufficient, since IntroCanvas itself may
        /// be authored at any positive value too (see IntroSceneSetup.IntroCanvasSortingOrder).
        /// </summary>
        private static bool ValidateLoadingRendersAboveIntro(Scene scene, GameObject introCanvasObject)
        {
            Canvas introCanvas = introCanvasObject.GetComponent<Canvas>();
            GameObject loadingCanvasObject = FindRoot(scene, CanvasName);
            Canvas loadingCanvas = loadingCanvasObject != null ? loadingCanvasObject.GetComponent<Canvas>() : null;

            if (introCanvas == null || loadingCanvas == null)
            {
                Debug.LogError(
                    "[StartupLoadingSetup] Validation: could not compare sort order — IntroCanvas or "
                    + "LoadingCanvas has no Canvas component.");
                return false;
            }

            if (loadingCanvas.sortingOrder <= introCanvas.sortingOrder)
            {
                Debug.LogError(
                    "[StartupLoadingSetup] Validation: LoadingCanvas.sortingOrder ("
                    + loadingCanvas.sortingOrder + ") is not greater than IntroCanvas.sortingOrder ("
                    + introCanvas.sortingOrder + "). IntroCanvas's opaque BlackBackground will paint "
                    + "over the entire loading screen, including the blood tube, until this is fixed.");
                return false;
            }

            return true;
        }

        private static bool ValidateReference(SerializedObject serializedObject, string propertyName, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
            {
                Debug.LogError("[StartupLoadingSetup] Validation: " + label + " is not wired.");
                return false;
            }

            return true;
        }

        private static void ReportCurrentState()
        {
            Scene scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[StartupLoadingSetup] Could not open " + BootstrapScenePath);
                return;
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("[StartupLoadingSetup] Validate Loading Setup report:");
            bool ok = true;

            TextureImporter importer = AssetImporter.GetAtPath(BackgroundArtPath) as TextureImporter;
            if (importer == null)
            {
                report.AppendLine("- Background importer: NOT FOUND at " + BackgroundArtPath);
            }
            else
            {
                bool isSingle = importer.spriteImportMode == SpriteImportMode.Single;
                report.AppendLine("- Background importer Sprite Mode: " + importer.spriteImportMode
                    + (isSingle ? " (OK)" : " (SHOULD BE Single)"));
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundArtPath);
            report.AppendLine(sprite != null
                ? "- Background sprite: '" + sprite.name + "', " + sprite.rect.width + "x" + sprite.rect.height
                : "- Background sprite: NONE (loading screen will show its black fallback)");

            GameObject introCanvasRoot = FindRoot(scene, IntroCanvasName);
            report.AppendLine("- IntroCanvas still present: " + (introCanvasRoot != null ? "OK" : "MISSING"));

            GameObject canvasRoot = FindRoot(scene, CanvasName);
            Transform canvasTransform = canvasRoot != null ? canvasRoot.transform : null;
            Transform root = canvasTransform != null ? canvasTransform.Find(RootName) : null;

            if (canvasRoot != null)
            {
                Canvas canvas = canvasRoot.GetComponent<Canvas>();
                Canvas introCanvas = introCanvasRoot != null ? introCanvasRoot.GetComponent<Canvas>() : null;
                bool rendersAboveIntro = canvas != null && introCanvas != null
                    && canvas.sortingOrder > introCanvas.sortingOrder;
                report.AppendLine("- LoadingCanvas sortingOrder: " + (canvas != null ? canvas.sortingOrder.ToString() : "N/A")
                    + " vs IntroCanvas sortingOrder: " + (introCanvas != null ? introCanvas.sortingOrder.ToString() : "N/A")
                    + (rendersAboveIntro
                        ? " (OK, LoadingCanvas renders above IntroCanvas)"
                        : " (BROKEN — IntroCanvas's opaque BlackBackground will hide the entire loading "
                            + "screen, including the blood tube; re-run Apply Loading Setup)"));
            }

            if (root == null)
            {
                report.AppendLine("- LoadingRoot: NOT FOUND");
                Debug.Log(report.ToString());
                return;
            }

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            report.AppendLine("- LoadingRoot CanvasGroup: " + (group != null ? "OK" : "MISSING"));

            Transform artwork = root.Find("Background/Artwork");
            Image artworkImage = artwork != null ? artwork.GetComponent<Image>() : null;
            AspectRatioFitter artworkFitter = artwork != null ? artwork.GetComponent<AspectRatioFitter>() : null;
            report.AppendLine(artworkImage != null
                ? "- Background Image colour/alpha: " + artworkImage.color + " (white/opaque expected when a sprite is assigned)"
                : "- Background Image: MISSING (expected at LoadingRoot/Background/Artwork)");
            if (artworkFitter == null)
            {
                report.AppendLine("- Background cover-fit (AspectRatioFitter): MISSING");
            }
            else
            {
                bool ratioValid = !float.IsNaN(artworkFitter.aspectRatio) && artworkFitter.aspectRatio > 0f;
                report.AppendLine("- Background cover-fit: mode=" + artworkFitter.aspectMode
                    + ", aspectRatio=" + artworkFitter.aspectRatio
                    + ", enabled=" + artworkFitter.enabled
                    + (ratioValid ? " (OK)" : " (INVALID — background will not be visible; re-run Apply Loading Setup)"));
            }

            Transform overlay = root.Find("DarkOverlay");
            ProceduralVerticalGradientGraphic overlayGraphic =
                overlay != null ? overlay.GetComponent<ProceduralVerticalGradientGraphic>() : null;
            report.AppendLine(overlayGraphic != null
                ? "- DarkOverlay readability scrim: OK (bottom-weighted gradient, base colour "
                    + overlayGraphic.color + ")"
                : "- DarkOverlay readability scrim: MISSING or not a ProceduralVerticalGradientGraphic");

            Transform safeContent = root.Find(SafeContentName);
            SafeAreaFitter safeAreaFitter = safeContent != null ? safeContent.GetComponent<SafeAreaFitter>() : null;
            report.AppendLine(safeAreaFitter != null
                ? "- SafeContent SafeAreaFitter: OK"
                : "- SafeContent SafeAreaFitter: MISSING (expected at LoadingRoot/" + SafeContentName + ")");

            Transform contentTransform = safeContent != null ? safeContent.Find("LoadingContent") : null;
            CanvasGroup contentGroup = contentTransform != null ? contentTransform.GetComponent<CanvasGroup>() : null;
            report.AppendLine(contentGroup != null
                ? "- LoadingContent CanvasGroup (fade-in): OK, alpha=" + contentGroup.alpha
                : "- LoadingContent CanvasGroup: MISSING");

            Transform statusTransform = contentTransform != null ? contentTransform.Find("StatusText") : null;
            TMP_Text statusTmp = statusTransform != null ? statusTransform.GetComponent<TMP_Text>() : null;
            report.AppendLine(statusTmp != null
                ? "- StatusText TMP: OK, text='" + statusTmp.text + "', font=" + (statusTmp.font != null ? statusTmp.font.name : "default")
                : "- StatusText TMP: MISSING");

            // The flat bar this tool used to build must never linger alongside the blood tube.
            Transform legacyTrack = contentTransform != null ? contentTransform.Find("ProgressBarTrack") : null;
            if (legacyTrack == null)
            {
                report.AppendLine("- Legacy ProgressBarTrack: absent (OK)");
            }
            else
            {
                report.AppendLine("- Legacy ProgressBarTrack: STILL PRESENT — re-run Apply Loading Setup to remove it");
                Debug.LogError("[StartupLoadingSetup] Validation: legacy ProgressBarTrack is still present.");
                ok = false;
            }

            Transform bloodTubeTransform = contentTransform != null ? contentTransform.Find("BloodTube") : null;
            if (bloodTubeTransform != null)
            {
                report.AppendLine("- BloodTube: OK");
            }
            else
            {
                report.AppendLine("- BloodTube: MISSING");
                Debug.LogError("[StartupLoadingSetup] Validation: BloodTube is missing.");
                ok = false;
            }

            // The old procedural tube must never linger alongside the new asset-based one.
            bool legacyTubeNodeFound = false;
            foreach (string legacyName in new[] { "TubeShadow", "TubeFrame", "TubeInterior" })
            {
                if (bloodTubeTransform != null && bloodTubeTransform.Find(legacyName) != null)
                {
                    report.AppendLine("- Legacy " + legacyName + ": STILL PRESENT — re-run Apply Loading Setup to remove it");
                    Debug.LogError("[StartupLoadingSetup] Validation: legacy " + legacyName + " is still present.");
                    legacyTubeNodeFound = true;
                }
            }

            if (!legacyTubeNodeFound)
            {
                report.AppendLine("- Legacy procedural tube nodes (TubeShadow/TubeFrame/TubeInterior): absent (OK)");
            }
            else
            {
                ok = false;
            }

            Transform tubeFrameTransform = bloodTubeTransform != null ? bloodTubeTransform.Find("BloodTubeFrame") : null;
            ok &= CheckImageSpriteComponent(report, tubeFrameTransform, "BloodTubeFrame", BloodTubeFramePath);

            Transform windowTransform = bloodTubeTransform != null ? bloodTubeTransform.Find("BloodWindow") : null;
            if (windowTransform != null)
            {
                report.AppendLine("- BloodWindow width: " + windowTransform.GetComponent<RectTransform>().rect.width);
            }
            else
            {
                report.AppendLine("- BloodWindow: MISSING");
                Debug.LogError("[StartupLoadingSetup] Validation: BloodWindow is missing.");
                ok = false;
            }

            Transform bloodMaskTransform = windowTransform != null ? windowTransform.Find("BloodMask") : null;
            RectMask2D bloodMaskComponent = bloodMaskTransform != null ? bloodMaskTransform.GetComponent<RectMask2D>() : null;
            // RectMask2D is not a Graphic and needs no CanvasRenderer of its own — only its children
            // (BloodFill) are actually drawn.
            if (bloodMaskComponent != null)
            {
                report.AppendLine("- BloodMask: OK (RectMask2D), leftInset="
                    + bloodMaskTransform.GetComponent<RectTransform>().anchoredPosition.x
                    + ", left-anchored=" + (bloodMaskTransform.GetComponent<RectTransform>().anchorMin.x == 0f
                        && bloodMaskTransform.GetComponent<RectTransform>().anchorMax.x == 0f));
            }
            else
            {
                report.AppendLine("- BloodMask: MISSING or missing its RectMask2D");
                Debug.LogError("[StartupLoadingSetup] Validation: BloodMask is missing or has no RectMask2D.");
                ok = false;
            }

            Transform bloodFillTransform = bloodMaskTransform != null ? bloodMaskTransform.Find("BloodFill") : null;
            ok &= CheckImageSpriteComponent(report, bloodFillTransform, "BloodFill", BloodFillPath);
            if (tubeFrameTransform != null && bloodFillTransform != null
                && tubeFrameTransform.GetComponent<Image>() == bloodFillTransform.GetComponent<Image>())
            {
                report.AppendLine("- BloodTubeFrame/BloodFill independence: ERROR — sharing one Image component");
                Debug.LogError("[StartupLoadingSetup] Validation: BloodTubeFrame and BloodFill must be independent Images.");
                ok = false;
            }
            else
            {
                report.AppendLine("- BloodTubeFrame/BloodFill independence: OK (separate Image components)");
            }

            Transform leadingEdgeTransform = windowTransform != null
                ? windowTransform.Find("BloodLeadingEdge")
                : null;
            ok &= CheckImageSpriteComponent(report, leadingEdgeTransform, "BloodLeadingEdge", BloodLeadingEdgePath);

            Transform glassHighlightTransform = bloodTubeTransform != null
                ? bloodTubeTransform.Find("GlassHighlight")
                : null;
            ok &= CheckImageSpriteComponent(report, glassHighlightTransform, "GlassHighlight", GlassHighlightPath);

            Transform percentageTransform = contentTransform != null ? contentTransform.Find("PercentageText") : null;
            TMP_Text percentageTmp = percentageTransform != null ? percentageTransform.GetComponent<TMP_Text>() : null;
            report.AppendLine(percentageTmp != null
                ? "- PercentageText TMP: OK, text='" + percentageTmp.text + "', activeSelf="
                    + percentageTransform.gameObject.activeSelf
                : "- PercentageText TMP: MISSING");

            StartupLoadingController controller = root.GetComponent<StartupLoadingController>();
            if (controller == null)
            {
                report.AppendLine("- StartupLoadingController: MISSING");
            }
            else
            {
                SerializedObject serialized = new SerializedObject(controller);
                report.AppendLine("- StartupLoadingController.canvasGroup: "
                    + DescribeReference(serialized, "canvasGroup"));
                report.AppendLine("- StartupLoadingController.backgroundImage: "
                    + DescribeReference(serialized, "backgroundImage"));
                report.AppendLine("- StartupLoadingController.backgroundFitter: "
                    + DescribeReference(serialized, "backgroundFitter"));
                report.AppendLine("- StartupLoadingController.statusText: "
                    + DescribeReference(serialized, "statusText"));
                report.AppendLine("- StartupLoadingController.percentageText: "
                    + DescribeReference(serialized, "percentageText"));
                report.AppendLine("- StartupLoadingController.contentGroup: "
                    + DescribeReference(serialized, "contentGroup"));
                report.AppendLine("- StartupLoadingController.showPercentage: "
                    + (serialized.FindProperty("showPercentage")?.boolValue.ToString() ?? "FIELD NOT FOUND"));
                report.AppendLine("- StartupLoadingController.bloodMask: "
                    + DescribeReference(serialized, "bloodMask"));
                report.AppendLine("- StartupLoadingController.bloodFill: "
                    + DescribeReference(serialized, "bloodFill"));
                report.AppendLine("- StartupLoadingController.bloodLeadingEdge: "
                    + DescribeReference(serialized, "bloodLeadingEdge"));
                report.AppendLine("- StartupLoadingController.tubeInterior: "
                    + DescribeReference(serialized, "tubeInterior"));
            }

            GameObject controllerObject = FindRoot(scene, BootstrapControllerName);
            BootstrapController bootstrapController = controllerObject != null
                ? controllerObject.GetComponent<BootstrapController>()
                : null;
            if (bootstrapController == null)
            {
                report.AppendLine("- BootstrapController: NOT FOUND");
            }
            else
            {
                SerializedObject serializedController = new SerializedObject(bootstrapController);
                report.AppendLine("- BootstrapController.loadingSequence: "
                    + DescribeReference(serializedController, "loadingSequence"));
                report.AppendLine("- BootstrapController.introSequence: "
                    + DescribeReference(serializedController, "introSequence"));
            }

            // A wall of "OK"/"MISSING" lines is easy to skim past a single broken one in — this must
            // never be allowed to read as "Validation passed" while any required CanvasRenderer (or
            // other hard requirement checked above) is actually missing, which is exactly what
            // happened before this summary line and the CheckGraphicComponent errors above it existed.
            report.AppendLine(ok
                ? "[StartupLoadingSetup] Validation passed: loading hierarchy and required UI components are correct."
                : "[StartupLoadingSetup] Validation FAILED — see ERROR lines above.");

            Debug.Log(report.ToString());

            if (!ok)
            {
                Debug.LogError("[StartupLoadingSetup] Validate Loading Setup FAILED — see errors above.");
            }
        }

        /// <summary>
        /// Confirms <paramref name="transform"/> has both the expected Graphic-derived component and
        /// a CanvasRenderer — the exact pairing <see cref="EnsureComponent{T}"/> guarantees during
        /// Apply. A missing CanvasRenderer here means either Apply has not been (re-)run since that
        /// fix existed, or a future regression reintroduced the component-creation-order bug that
        /// caused <c>MissingComponentException: There is no 'CanvasRenderer' attached to ... BloodFill</c>
        /// — either way this must report ERROR and fail validation, never a bare "MISSING" line easy
        /// to mistake for a soft warning.
        /// </summary>
        private static bool CheckGraphicComponent<T>(StringBuilder report, Transform transform, string label)
            where T : Graphic
        {
            if (transform == null)
            {
                report.AppendLine("- " + label + ": MISSING (GameObject not found)");
                Debug.LogError("[StartupLoadingSetup] Validation: " + label + " GameObject not found.");
                return false;
            }

            T graphic = transform.GetComponent<T>();
            if (graphic == null)
            {
                report.AppendLine("- " + label + ": MISSING (" + typeof(T).Name + " component not found)");
                Debug.LogError("[StartupLoadingSetup] Validation: " + label + " has no " + typeof(T).Name + ".");
                return false;
            }

            if (transform.GetComponent<CanvasRenderer>() == null)
            {
                report.AppendLine("- " + label + ": ERROR — " + typeof(T).Name + " present but CanvasRenderer "
                    + "MISSING (re-run Apply Loading Setup to repair)");
                Debug.LogError("[StartupLoadingSetup] Validation: " + label + " is missing its CanvasRenderer.");
                return false;
            }

            report.AppendLine("- " + label + ": OK (" + typeof(T).Name + " + CanvasRenderer)");
            return true;
        }

        /// <summary>
        /// The asset-based tube's equivalent of <see cref="CheckGraphicComponent{T}"/>: confirms a
        /// plain <see cref="Image"/> (with its required CanvasRenderer) exists and has the expected
        /// generated sprite assigned — not just any sprite, so a leftover placeholder or a sprite
        /// dragged onto the wrong slot is still caught as an error.
        /// </summary>
        private static bool CheckImageSpriteComponent(StringBuilder report, Transform transform, string label, string expectedSpritePath)
        {
            if (!CheckGraphicComponent<Image>(report, transform, label))
            {
                return false;
            }

            Image image = transform.GetComponent<Image>();
            Sprite expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(expectedSpritePath);
            if (image.sprite == null)
            {
                report.AppendLine("- " + label + " sprite: MISSING (re-run Apply Loading Setup once "
                    + expectedSpritePath + " has been imported)");
                Debug.LogError("[StartupLoadingSetup] Validation: " + label + " has no sprite assigned.");
                return false;
            }

            if (expectedSprite != null && image.sprite != expectedSprite)
            {
                report.AppendLine("- " + label + " sprite: '" + image.sprite.name
                    + "' (expected '" + expectedSprite.name + "')");
                Debug.LogError("[StartupLoadingSetup] Validation: " + label + " sprite does not match " + expectedSpritePath + ".");
                return false;
            }

            report.AppendLine("- " + label + " sprite: OK ('" + image.sprite.name + "')");
            return true;
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

        /// <summary>
        /// Finds-or-adds <typeparamref name="T"/>, then — if <typeparamref name="T"/> is a
        /// <see cref="Graphic"/> — separately guarantees a <see cref="CanvasRenderer"/> exists too.
        /// <c>internal</c> (with <c>InternalsVisibleTo</c> above) rather than <c>private</c>,
        /// specifically so <c>StartupLoadingSetupCanvasRendererTests</c> can exercise the repair
        /// directly on a throwaway GameObject, without needing to open/save the real Bootstrap.unity
        /// scene for what is fundamentally a small, scene-agnostic helper.
        /// </summary>
        /// <remarks>
        /// <see cref="Graphic"/>'s own <c>[RequireComponent(typeof(CanvasRenderer))]</c> normally adds
        /// this automatically, but its auto-add can lose the race with a single-shot
        /// <c>-executeMethod</c>/<c>-quit</c> batch Editor run before <c>SaveScene</c> serializes the
        /// object — the exact "Gear icon didn't render" / "ApplyButton" <c>CanvasRenderer</c> bug
        /// already diagnosed and fixed the same way for MainMenu buttons (see
        /// <c>MANUAL_UNITY_STEPS.md</c>). Checking explicitly here, in the one helper every component
        /// in this file goes through, closes that whole class of bug for every Graphic-derived
        /// component this file creates — the blood tube's <see cref="Image"/> layers included — and
        /// repairs an existing malformed object left over from before this fix existed, not just ones
        /// freshly created this run.
        /// </remarks>
        internal static T EnsureComponent<T>(GameObject target) where T : Component
        {
            // CanvasRenderer must exist BEFORE a Graphic-derived T is added, not after. Adding T
            // first is what actually threw the "no CanvasRenderer" exception: Unity calls the new
            // component's OnEnable() synchronously as part of AddComponent<T>() itself, and
            // Graphic.OnEnable() immediately touches its own canvasRenderer property, which uses a
            // required-component fast-path accessor that throws MissingComponentException the
            // instant it is queried before CanvasRenderer is attached — regardless of whether
            // RequireComponent's own auto-add (or a follow-up AddComponent<CanvasRenderer>() call
            // placed after AddComponent<T>()) attaches one moments later. The scene still ends up
            // correctly serialized either way, which is why Apply/Validate reported success while the
            // Console kept logging the exception. Pre-attaching it first means it is already present
            // by the time the Graphic's own OnEnable runs, so this never fires at all.
            if (typeof(Graphic).IsAssignableFrom(typeof(T)) && target.GetComponent<CanvasRenderer>() == null)
            {
                target.AddComponent<CanvasRenderer>();
            }

            T component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.AddComponent<T>();
            }

            return component;
        }
    }
}
