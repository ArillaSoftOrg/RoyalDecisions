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
        // (default 0) while it is visible, without ever touching IntroCanvas itself.
        private const int LoadingCanvasSortingOrder = 10;

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
        private const float TubeHeight = 80f;
        private const float TubeBottomOffset = 240f;
        private const float TubeFrameBorderThickness = 6f;
        // Gap between TubeInterior's own edges and the liquid/leading-edge content inside it — a
        // visible glass rim around the blood, and the shared coordinate-space origin bloodMask,
        // bloodFill and bloodLeadingEdge all measure their left inset from (see
        // StartupLoadingController.ComputeTubeInnerWidth).
        private const float TubeContentPadding = 5f;
        private const float TubeShadowOffsetY = 5f;
        private const float TubeShadowExtraSize = 4f;
        private const float GlassHighlightTopInset = 10f;
        private const float GlassHighlightHeight = 10f;
        private const float GlassHighlightPeakAlpha = 0.16f;
        private const float BloodMidStopHeight = 0.62f;

        // Bottom-weighted readability scrim stops (index 0 = bottom, last = top — see
        // ProceduralVerticalGradientGraphic). Alpha only, RGB lives in OverlayColour.
        private const float OverlayBottomAlpha = 0.6f;
        private const float OverlayMidAlpha = 0.22f;
        private const float OverlayTopAlpha = 0f;

        private static readonly Color OverlayColour = new Color(0.03f, 0.02f, 0.02f, 1f);
        private static readonly Color StatusTextColour = new Color32(0xF2, 0xE7, 0xCF, 0xFF);
        private static readonly Color PercentageTextColour = new Color32(0xD9, 0xC2, 0x8B, 0xFF);
        private static readonly Color TextShadowColour = new Color(0f, 0f, 0f, 0.45f);

        // Dark burgundy / deep red throughout — deliberately never a flat pure #FF0000.
        private static readonly Color TubeShadowColour = new Color(0f, 0f, 0f, 0.4f);
        private static readonly Color32 TubeFrameColour = new Color32(0x3A, 0x30, 0x2A, 0xFF);
        private static readonly Color32 TubeInteriorColour = new Color32(0x14, 0x12, 0x16, 0xE0);
        private static readonly Color32 BloodBottomColour = new Color32(0x3D, 0x06, 0x09, 0xFF);
        private static readonly Color32 BloodMidColour = new Color32(0x9A, 0x18, 0x1C, 0xFF);
        private static readonly Color32 BloodTopColour = new Color32(0x5C, 0x0A, 0x0E, 0xFF);
        private static readonly Color32 LeadingEdgeColour = new Color32(0xB5, 0x22, 0x24, 0xFF);
        private static readonly Color32 GlassHighlightColour = new Color32(0xCF, 0xDD, 0xE6, 0xFF);

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
                RectTransform bloodMask, BloodFillGraphic bloodFill, Graphic bloodLeadingEdge,
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
            RectTransform bloodMask, BloodFillGraphic bloodFill, Graphic bloodLeadingEdge,
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

            (RectTransform bloodMask, BloodFillGraphic bloodFill, Graphic bloodLeadingEdge,
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
        /// Builds the blood-tube loading indicator: a shadow, a metallic/dark frame acting as a
        /// backing border, a dark glass interior, the blood liquid revealed by an actual
        /// <see cref="RectMask2D"/> (never a horizontal scale of the artwork), a small rounded
        /// leading-edge cap, and a restrained glass highlight on top. BloodMask, BloodFill and
        /// BloodLeadingEdge are all parented under TubeInterior so they share one coordinate space —
        /// <see cref="StartupLoadingController.ComputeTubeInnerWidth"/> depends on that.
        /// </summary>
        private static (RectTransform mask, BloodFillGraphic fill, Graphic leadingEdge, RectTransform interior)
            EnsureBloodTube(Transform parent)
        {
            GameObject tubeObject = EnsureChild(parent, "BloodTube");
            RectTransform tubeRect = tubeObject.GetComponent<RectTransform>();
            Undo.RecordObject(tubeRect, "Configure BloodTube");
            tubeRect.anchorMin = new Vector2(0.5f - (TubeWidthFraction * 0.5f), 0f);
            tubeRect.anchorMax = new Vector2(0.5f + (TubeWidthFraction * 0.5f), 0f);
            tubeRect.pivot = new Vector2(0.5f, 0f);
            tubeRect.anchoredPosition = new Vector2(0f, TubeBottomOffset);
            tubeRect.sizeDelta = new Vector2(0f, TubeHeight);

            EnsureTubeShadow(tubeObject.transform);
            EnsureTubeFrame(tubeObject.transform);
            RectTransform interiorRect = EnsureTubeInterior(tubeObject.transform);

            (RectTransform bloodMask, BloodFillGraphic bloodFill) = EnsureBloodMaskAndFill(interiorRect);
            Graphic leadingEdge = EnsureBloodLeadingEdge(interiorRect);
            EnsureGlassHighlight(interiorRect);

            return (bloodMask, bloodFill, leadingEdge, interiorRect);
        }

        private static void EnsureTubeShadow(Transform parent)
        {
            GameObject shadowObject = EnsureChild(parent, "TubeShadow");
            shadowObject.transform.SetAsFirstSibling();
            RectTransform shadowRect = shadowObject.GetComponent<RectTransform>();
            Undo.RecordObject(shadowRect, "Configure TubeShadow");
            shadowRect.anchorMin = Vector2.zero;
            shadowRect.anchorMax = Vector2.one;
            shadowRect.pivot = new Vector2(0.5f, 0.5f);
            shadowRect.anchoredPosition = new Vector2(0f, -TubeShadowOffsetY);
            shadowRect.sizeDelta = new Vector2(TubeShadowExtraSize * 2f, TubeShadowExtraSize * 2f);

            ProceduralRoundedRectGraphic shadowGraphic = EnsureComponent<ProceduralRoundedRectGraphic>(shadowObject);
            Undo.RecordObject(shadowGraphic, "Configure TubeShadow");
            shadowGraphic.color = TubeShadowColour;
            shadowGraphic.raycastTarget = false;
            shadowGraphic.SetCornerRadius((TubeHeight + (TubeShadowExtraSize * 2f)) * 0.5f);
        }

        /// <summary>
        /// A backing border rather than a top-level ring overlay: sized exactly to BloodTube's outer
        /// rect and placed behind the (smaller, inset) TubeInterior, so a thin band of this colour is
        /// all that shows around the edge — the classic texture-free "bordered rounded rect" trick,
        /// reusing <see cref="ProceduralRoundedRectGraphic"/> rather than a dedicated outline-mesh
        /// component purely for this one border.
        /// </summary>
        private static void EnsureTubeFrame(Transform parent)
        {
            GameObject frameObject = EnsureChild(parent, "TubeFrame");
            RectTransform frameRect = frameObject.GetComponent<RectTransform>();
            Undo.RecordObject(frameRect, "Configure TubeFrame");
            Stretch(frameRect);

            ProceduralRoundedRectGraphic frameGraphic = EnsureComponent<ProceduralRoundedRectGraphic>(frameObject);
            Undo.RecordObject(frameGraphic, "Configure TubeFrame");
            frameGraphic.color = TubeFrameColour;
            frameGraphic.raycastTarget = false;
            frameGraphic.SetCornerRadius(TubeHeight * 0.5f);
        }

        private static RectTransform EnsureTubeInterior(Transform parent)
        {
            GameObject interiorObject = EnsureChild(parent, "TubeInterior");
            RectTransform interiorRect = interiorObject.GetComponent<RectTransform>();
            Undo.RecordObject(interiorRect, "Configure TubeInterior");
            interiorRect.anchorMin = Vector2.zero;
            interiorRect.anchorMax = Vector2.one;
            interiorRect.pivot = new Vector2(0.5f, 0.5f);
            interiorRect.anchoredPosition = Vector2.zero;
            interiorRect.sizeDelta = new Vector2(
                -2f * TubeFrameBorderThickness, -2f * TubeFrameBorderThickness);

            ProceduralRoundedRectGraphic interiorGraphic = EnsureComponent<ProceduralRoundedRectGraphic>(interiorObject);
            Undo.RecordObject(interiorGraphic, "Configure TubeInterior");
            interiorGraphic.color = TubeInteriorColour;
            interiorGraphic.raycastTarget = false;
            interiorGraphic.SetCornerRadius((TubeHeight - (2f * TubeFrameBorderThickness)) * 0.5f);

            return interiorRect;
        }

        /// <summary>
        /// BloodMask's width is the actual progress reveal (an honest RectMask2D clip); BloodFill
        /// underneath it is authored at zero width to match the "0% empty tube" look before
        /// <see cref="StartupLoadingController"/> ever runs (its first Awake() immediately corrects
        /// BloodFill to the tube's full inner width regardless of progress — only BloodMask's width
        /// depends on displayed progress).
        /// </summary>
        private static (RectTransform mask, BloodFillGraphic fill) EnsureBloodMaskAndFill(RectTransform interiorRect)
        {
            GameObject maskObject = EnsureChild(interiorRect, "BloodMask");
            RectTransform maskRect = maskObject.GetComponent<RectTransform>();
            Undo.RecordObject(maskRect, "Configure BloodMask");
            maskRect.anchorMin = new Vector2(0f, 0f);
            maskRect.anchorMax = new Vector2(0f, 1f);
            maskRect.pivot = new Vector2(0f, 0.5f);
            maskRect.anchoredPosition = new Vector2(TubeContentPadding, 0f);
            maskRect.sizeDelta = new Vector2(0f, -2f * TubeContentPadding);

            EnsureComponent<RectMask2D>(maskObject);

            GameObject fillObject = EnsureChild(maskObject.transform, "BloodFill");
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            Undo.RecordObject(fillRect, "Configure BloodFill");
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;

            BloodFillGraphic fillGraphic = EnsureComponent<BloodFillGraphic>(fillObject);
            Undo.RecordObject(fillGraphic, "Configure BloodFill");
            fillGraphic.raycastTarget = false;
            fillGraphic.SetColors(BloodBottomColour, BloodMidColour, BloodTopColour, BloodMidStopHeight);

            return (maskRect, fillGraphic);
        }

        /// <summary>
        /// A small near-circular rounded rect sitting at the current fill boundary — reads as an
        /// organic liquid cap rather than a scan-line divider. Positioned with the same left inset as
        /// <see cref="EnsureBloodMaskAndFill"/>'s BloodMask (both are children of the same
        /// TubeInterior, so <c>StartupLoadingController</c> can measure both in one shared coordinate
        /// space) — its x is overwritten every progress update, this is only its resting position.
        /// </summary>
        private static Graphic EnsureBloodLeadingEdge(RectTransform interiorRect)
        {
            GameObject edgeObject = EnsureChild(interiorRect, "BloodLeadingEdge");
            RectTransform edgeRect = edgeObject.GetComponent<RectTransform>();
            Undo.RecordObject(edgeRect, "Configure BloodLeadingEdge");
            float edgeSize = TubeHeight - (2f * TubeFrameBorderThickness) - (2f * TubeContentPadding);
            edgeRect.anchorMin = new Vector2(0f, 0.5f);
            edgeRect.anchorMax = new Vector2(0f, 0.5f);
            edgeRect.pivot = new Vector2(0.5f, 0.5f);
            edgeRect.anchoredPosition = new Vector2(TubeContentPadding, 0f);
            edgeRect.sizeDelta = new Vector2(edgeSize, edgeSize);

            ProceduralRoundedRectGraphic edgeGraphic = EnsureComponent<ProceduralRoundedRectGraphic>(edgeObject);
            Undo.RecordObject(edgeGraphic, "Configure BloodLeadingEdge");
            edgeGraphic.color = LeadingEdgeColour;
            edgeGraphic.raycastTarget = false;
            edgeGraphic.SetCornerRadius(edgeSize * 0.5f);

            return edgeGraphic;
        }

        /// <summary>A thin, restrained, cool-toned sheen near the top of the glass — spans the full
        /// interior width (unlike the liquid, it is part of the tube itself) and sits above the blood
        /// in sibling order so it stays visible even where the tube is filled.</summary>
        private static void EnsureGlassHighlight(RectTransform interiorRect)
        {
            GameObject highlightObject = EnsureChild(interiorRect, "GlassHighlight");
            RectTransform highlightRect = highlightObject.GetComponent<RectTransform>();
            Undo.RecordObject(highlightRect, "Configure GlassHighlight");
            highlightRect.anchorMin = new Vector2(0f, 1f);
            highlightRect.anchorMax = new Vector2(1f, 1f);
            highlightRect.pivot = new Vector2(0.5f, 1f);
            highlightRect.anchoredPosition = new Vector2(0f, -GlassHighlightTopInset);
            highlightRect.sizeDelta = new Vector2(0f, GlassHighlightHeight);

            ProceduralHorizontalGradientGraphic highlightGraphic =
                EnsureComponent<ProceduralHorizontalGradientGraphic>(highlightObject);
            Undo.RecordObject(highlightGraphic, "Configure GlassHighlight");
            highlightGraphic.color = GlassHighlightColour;
            highlightGraphic.raycastTarget = false;
            highlightGraphic.SetStops(0f, GlassHighlightPeakAlpha, 0f);
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

            if (FindRoot(scene, IntroCanvasName) == null)
            {
                Debug.LogError(
                    "[StartupLoadingSetup] Validation: '" + IntroCanvasName + "' is missing after "
                    + "Apply — this tool must never remove it.");
                ok = false;
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

            report.AppendLine("- IntroCanvas still present: "
                + (FindRoot(scene, IntroCanvasName) != null ? "OK" : "MISSING"));

            GameObject canvasRoot = FindRoot(scene, CanvasName);
            Transform canvasTransform = canvasRoot != null ? canvasRoot.transform : null;
            Transform root = canvasTransform != null ? canvasTransform.Find(RootName) : null;

            if (canvasRoot != null)
            {
                Canvas canvas = canvasRoot.GetComponent<Canvas>();
                report.AppendLine("- LoadingCanvas sortingOrder: " + (canvas != null ? canvas.sortingOrder.ToString() : "N/A")
                    + (canvas != null && canvas.sortingOrder > 0 ? " (OK, renders above IntroCanvas)" : " (CHECK)"));
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

            Transform tubeFrameTransform = bloodTubeTransform != null ? bloodTubeTransform.Find("TubeFrame") : null;
            ok &= CheckGraphicComponent<ProceduralRoundedRectGraphic>(report, tubeFrameTransform, "TubeFrame");

            Transform tubeShadowTransform = bloodTubeTransform != null ? bloodTubeTransform.Find("TubeShadow") : null;
            ok &= CheckGraphicComponent<ProceduralRoundedRectGraphic>(report, tubeShadowTransform, "TubeShadow");

            Transform tubeInteriorTransform = bloodTubeTransform != null ? bloodTubeTransform.Find("TubeInterior") : null;
            ok &= CheckGraphicComponent<ProceduralRoundedRectGraphic>(report, tubeInteriorTransform, "TubeInterior");
            if (tubeInteriorTransform != null)
            {
                report.AppendLine("- TubeInterior width: " + tubeInteriorTransform.GetComponent<RectTransform>().rect.width);
            }

            Transform bloodMaskTransform = tubeInteriorTransform != null ? tubeInteriorTransform.Find("BloodMask") : null;
            RectMask2D bloodMaskComponent = bloodMaskTransform != null ? bloodMaskTransform.GetComponent<RectMask2D>() : null;
            // RectMask2D is not a Graphic and needs no CanvasRenderer of its own — only its children
            // (BloodFill) are actually drawn.
            if (bloodMaskComponent != null)
            {
                report.AppendLine("- BloodMask: OK (RectMask2D), leftInset="
                    + bloodMaskTransform.GetComponent<RectTransform>().anchoredPosition.x);
            }
            else
            {
                report.AppendLine("- BloodMask: MISSING or missing its RectMask2D");
                Debug.LogError("[StartupLoadingSetup] Validation: BloodMask is missing or has no RectMask2D.");
                ok = false;
            }

            Transform bloodFillTransform = bloodMaskTransform != null ? bloodMaskTransform.Find("BloodFill") : null;
            ok &= CheckGraphicComponent<BloodFillGraphic>(report, bloodFillTransform, "BloodFill");

            Transform leadingEdgeTransform = tubeInteriorTransform != null
                ? tubeInteriorTransform.Find("BloodLeadingEdge")
                : null;
            ok &= CheckGraphicComponent<Graphic>(report, leadingEdgeTransform, "BloodLeadingEdge");

            Transform glassHighlightTransform = tubeInteriorTransform != null
                ? tubeInteriorTransform.Find("GlassHighlight")
                : null;
            ok &= CheckGraphicComponent<ProceduralHorizontalGradientGraphic>(report, glassHighlightTransform, "GlassHighlight");

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
        /// in this file goes through, closes that whole class of bug for every procedural graphic the
        /// blood tube builds (BloodFill, TubeShadow/Frame/Interior, BloodLeadingEdge, GlassHighlight)
        /// and repairs an existing malformed object left over from before this fix existed, not just
        /// ones freshly created this run.
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
