using System;
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
    /// Deliberately simple: <c>IntroCanvas/LogoGroup/LogoImage</c>, one Image, the complete PNG
    /// (both the "AS" mark and the baked-in "ARILLA GAMES" wordmark), no masking, no generated
    /// text. An earlier pass tried to isolate just the mark via a RectMask2D crop plus a separate
    /// TMP wordmark; that added real complexity for no real benefit, since the source art already
    /// contains the whole composition — reverted in favour of this.
    /// </remarks>
    public static class IntroSceneSetup
    {
        private const string BootstrapScenePath = "Assets/_Game/scenes/Bootstrap.unity";

        /// <summary>Where the Editor looks for the logo sprite. Import the PNG here as a Sprite.</summary>
        public const string LogoAssetPath = "Assets/_Game/Art/Branding/ArillaGamesLogo.png";

        private const string CanvasName = "IntroCanvas";
        private const string EventSystemName = "EventSystem";
        private const string BootstrapControllerName = "BootstrapController";

        // 620-680 requested reference-unit range for the complete (mark + wordmark) composition;
        // the source PNG is a perfect 1254x1254 square, so a square box matches it with zero
        // letterboxing on either axis.
        private const float LogoDisplaySize = 650f;
        // "around screen centre, perhaps y=50 max".
        private const float LogoGroupVerticalOffset = 50f;

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

            (CanvasGroup logoGroupCanvasGroup, RectTransform logoGroupRect, Image logoImage) =
                EnsureLogoGroup(canvasTransform);

            // The click-catcher (BlackBackground's raycastable Image) and the controller that
            // reads the click must be the same GameObject — uGUI does not bubble pointer events
            // to parents on its own.
            IntroSequenceController introController = EnsureComponent<IntroSequenceController>(background);
            introController.SetAuthoringReferences(logoGroupCanvasGroup, logoGroupRect, logoImage);

            WireBootstrapController(scene, introController);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, BootstrapScenePath))
            {
                Debug.LogError("[IntroSceneSetup] Bootstrap scene could not be saved.");
                return;
            }

            Debug.Log("[IntroSceneSetup] Bootstrap intro wiring applied.");
            ValidateAfterApply(scene, canvasTransform, introController, logoGroupCanvasGroup, logoGroupRect, logoImage);
        }

        /// <summary>
        /// Removes anything left behind by earlier versions of this tool that the current, simpler
        /// layout no longer creates: the original single-node "Logo" object, and — from a since-
        /// reverted attempt to mask just the mark and regenerate the wordmark in TMP — any
        /// "LogoImage" children (e.g. a nested "LogoArt") and any "LogoGroup" children other than
        /// "LogoImage" itself (e.g. "BrandText"), plus a stray RectMask2D on LogoImage. LogoGroup
        /// and LogoImage themselves are reused, never destroyed, by <see cref="EnsureLogoGroup"/>
        /// below. Nothing outside this specific, known set of names is ever touched.
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
                if (child.name != "LogoImage")
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            Transform logoImage = logoGroup.Find("LogoImage");
            if (logoImage == null)
            {
                return;
            }

            for (int i = logoImage.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(logoImage.GetChild(i).gameObject);
            }

            RectMask2D leftoverMask = logoImage.GetComponent<RectMask2D>();
            if (leftoverMask != null)
            {
                Undo.DestroyObjectImmediate(leftoverMask);
            }
        }

        /// <summary>
        /// Builds/updates <c>LogoGroup/LogoImage</c> and returns the components
        /// <see cref="IntroSequenceController"/> animates: LogoGroup's own CanvasGroup and
        /// RectTransform (alpha + scale), and LogoImage's Image (the reveal's brightness pulse).
        /// </summary>
        private static (CanvasGroup, RectTransform, Image) EnsureLogoGroup(Transform canvasTransform)
        {
            GameObject logoGroup = EnsureChild(canvasTransform, "LogoGroup");
            logoGroup.transform.SetSiblingIndex(1);
            RectTransform logoGroupRect = logoGroup.GetComponent<RectTransform>();
            Undo.RecordObject(logoGroupRect, "Configure LogoGroup");
            ConfigureLogoGroupRect(logoGroupRect);
            CanvasGroup logoGroupCanvasGroup = EnsureComponent<CanvasGroup>(logoGroup);
            Undo.RecordObject(logoGroupCanvasGroup, "Configure LogoGroup");
            logoGroupCanvasGroup.alpha = 0f;

            GameObject logoImageObject = EnsureChild(logoGroup.transform, "LogoImage");
            RectTransform logoImageRect = logoImageObject.GetComponent<RectTransform>();
            Undo.RecordObject(logoImageRect, "Configure LogoImage");
            ConfigureLogoImageRect(logoImageRect);
            Image logoImage = EnsureComponent<Image>(logoImageObject);
            Undo.RecordObject(logoImage, "Configure LogoImage");
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;

            Sprite logoSprite = EnsureLogoIsSingleSprite();
            if (logoSprite != null)
            {
                logoImage.sprite = logoSprite;
            }
            else
            {
                logoImage.sprite = null;
                Debug.LogWarning(
                    "[IntroSceneSetup] Logo sprite was not assigned — see the error above. Until a "
                    + "valid, complete sprite is wired, the intro safely skips straight to MainMenu.");
            }

            return (logoGroupCanvasGroup, logoGroupRect, logoImage);
        }

        /// <summary>
        /// Forces <see cref="LogoAssetPath"/> to Sprite Mode "Single", synchronously reimports it,
        /// then loads and validates the result. Never returns a sprite loaded before the reimport,
        /// and never returns a sprite whose rect is smaller than the full source texture (a
        /// fragment left over from a "Multiple" auto-slice, exactly what caused the original bug —
        /// the importer was found set to "Multiple" with 13 auto-sliced fragments, and the loaded
        /// "sprite" was actually just fragment 0, a partial crop of the mark).
        /// </summary>
        private static Sprite EnsureLogoIsSingleSprite()
        {
            TextureImporter importer = AssetImporter.GetAtPath(LogoAssetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError(
                    "[IntroSceneSetup] No TextureImporter found at '" + LogoAssetPath + "'. Import "
                    + "the logo PNG there first.");
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
                    LogoAssetPath,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                Debug.Log(
                    "[IntroSceneSetup] '" + LogoAssetPath + "' was set to Sprite Mode 'Multiple' "
                    + "with an auto-slice; reset to 'Single' and reimported so the whole logo "
                    + "(mark + wordmark) loads as one sprite.");

                // SaveAndReimport can invalidate the importer instance above; re-fetch before
                // trusting its state.
                importer = AssetImporter.GetAtPath(LogoAssetPath) as TextureImporter;
            }

            if (importer == null || importer.spriteImportMode != SpriteImportMode.Single)
            {
                Debug.LogError(
                    "[IntroSceneSetup] '" + LogoAssetPath + "' is still not Sprite Mode 'Single' "
                    + "after reimport; refusing to wire a possibly-fragmented sprite. Fix Sprite "
                    + "Mode by hand in the Inspector and re-run Apply Intro Setup.");
                return null;
            }

            // Loaded only now, after the fix above — never a reference obtained before it.
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoAssetPath);
            if (sprite == null)
            {
                Debug.LogError(
                    "[IntroSceneSetup] No sprite found at '" + LogoAssetPath + "' after reimport.");
                return null;
            }

            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            if (sourceWidth <= 0 || sourceHeight <= 0)
            {
                Debug.LogError(
                    "[IntroSceneSetup] Could not read source dimensions for '" + LogoAssetPath + "'.");
                return null;
            }

            if (Mathf.RoundToInt(sprite.rect.width) != sourceWidth
                || Mathf.RoundToInt(sprite.rect.height) != sourceHeight)
            {
                Debug.LogError(
                    "[IntroSceneSetup] Loaded sprite rect (" + sprite.rect.width + "x"
                    + sprite.rect.height + ") does not match the full texture (" + sourceWidth + "x"
                    + sourceHeight + "); it looks like a leftover fragment. Refusing to wire it.");
                return null;
            }

            Debug.Log(
                "[IntroSceneSetup] Logo sprite validated: '" + sprite.name + "', " + sourceWidth
                + "x" + sourceHeight + ", Sprite Mode Single.");
            return sprite;
        }

        /// <summary>Logs a clear pass/fail summary immediately after Apply saves the scene.</summary>
        private static void ValidateAfterApply(
            Scene scene,
            Transform canvasTransform,
            IntroSequenceController introController,
            CanvasGroup expectedGroup,
            RectTransform expectedRect,
            Image expectedImage)
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
            }

            SerializedObject serializedIntro = new SerializedObject(introController);
            ok &= ValidateReference(serializedIntro, "logoCanvasGroup", expectedGroup, "LogoGroup CanvasGroup");
            ok &= ValidateReference(serializedIntro, "logoRectTransform", expectedRect, "LogoGroup RectTransform");
            ok &= ValidateReference(serializedIntro, "logoImage", expectedImage, "LogoImage Image");

            if (expectedImage.sprite == null)
            {
                Debug.LogError("[IntroSceneSetup] Validation: LogoImage has no sprite assigned.");
                ok = false;
            }

            if (canvasTransform.Find("Logo") != null)
            {
                Debug.LogError("[IntroSceneSetup] Validation: legacy 'Logo' node still present.");
                ok = false;
            }

            Transform logoGroup = canvasTransform.Find("LogoGroup");
            if (logoGroup != null)
            {
                for (int i = 0; i < logoGroup.childCount; i++)
                {
                    string childName = logoGroup.GetChild(i).name;
                    if (childName != "LogoImage")
                    {
                        Debug.LogError(
                            "[IntroSceneSetup] Validation: unexpected leftover node '" + childName
                            + "' under LogoGroup.");
                        ok = false;
                    }
                }

                Transform logoImageTransform = logoGroup.Find("LogoImage");
                if (logoImageTransform != null && logoImageTransform.childCount > 0)
                {
                    Debug.LogError(
                        "[IntroSceneSetup] Validation: LogoImage has "
                        + logoImageTransform.childCount + " leftover child object(s) (expected none).");
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

            TextureImporter importer = AssetImporter.GetAtPath(LogoAssetPath) as TextureImporter;
            if (importer == null)
            {
                report.AppendLine("- Importer: NOT FOUND at " + LogoAssetPath);
            }
            else
            {
                bool isSingle = importer.spriteImportMode == SpriteImportMode.Single;
                report.AppendLine("- Importer Sprite Mode: " + importer.spriteImportMode
                    + (isSingle ? " (OK)" : " (SHOULD BE Single)"));
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                report.AppendLine("- Source texture size: " + width + "x" + height
                    + (width <= 0 || height <= 0 ? " (could not be read)" : string.Empty));
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoAssetPath);
            report.AppendLine(sprite != null
                ? "- Loaded sprite: '" + sprite.name + "', rect " + sprite.rect.width + "x"
                    + sprite.rect.height
                : "- Loaded sprite: NONE");

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
                bool onlyLogoImage = logoGroup.childCount == 1 && logoGroup.GetChild(0).name == "LogoImage";
                report.AppendLine("- LogoGroup children: " + logoGroup.childCount
                    + (onlyLogoImage ? " (OK: only LogoImage)" : " (expected exactly 1: LogoImage)"));

                Transform logoImage = logoGroup.Find("LogoImage");
                if (logoImage == null)
                {
                    report.AppendLine("- LogoImage: NOT FOUND");
                }
                else
                {
                    RectTransform logoImageRect = logoImage.GetComponent<RectTransform>();
                    report.AppendLine("- LogoImage: anchoredPosition=" + logoImageRect.anchoredPosition
                        + ", sizeDelta=" + logoImageRect.sizeDelta);
                    report.AppendLine("- LogoImage children: " + logoImage.childCount
                        + (logoImage.childCount == 0 ? " (OK)" : " (expected 0)"));
                    bool hasMask = logoImage.GetComponent<RectMask2D>() != null;
                    report.AppendLine("- LogoImage has RectMask2D: " + hasMask
                        + (hasMask ? " (SHOULD BE none)" : " (OK)"));
                    Image image = logoImage.GetComponent<Image>();
                    report.AppendLine("- LogoImage.Image.sprite: "
                        + (image != null && image.sprite != null ? image.sprite.name : "NONE"));
                    report.AppendLine("- LogoImage.Image.preserveAspect: "
                        + (image != null && image.preserveAspect));
                }
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

                if (introRef is IntroSequenceController introController)
                {
                    SerializedObject serializedIntro = new SerializedObject(introController);
                    report.AppendLine("- IntroSequenceController.logoCanvasGroup: "
                        + DescribeRef(serializedIntro, "logoCanvasGroup"));
                    report.AppendLine("- IntroSequenceController.logoRectTransform: "
                        + DescribeRef(serializedIntro, "logoRectTransform"));
                    report.AppendLine("- IntroSequenceController.logoImage: "
                        + DescribeRef(serializedIntro, "logoImage"));
                }
            }

            Debug.Log(report.ToString());
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

        private static void WireBootstrapController(Scene scene, IntroSequenceController introController)
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

        private static GameObject EnsureIntroCanvas(Scene scene)
        {
            GameObject canvasObject = FindRoot(scene, CanvasName)
                ?? new GameObject(CanvasName, typeof(RectTransform));

            Canvas canvas = EnsureComponent<Canvas>(canvasObject);
            Undo.RecordObject(canvas, "Configure IntroCanvas");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

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
            // Pure positioning/animation node: LogoImage defines its own size and is centred (0,0)
            // within this, so LogoGroup itself needs no size.
            rect.sizeDelta = Vector2.zero;
        }

        private static void ConfigureLogoImageRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(LogoDisplaySize, LogoDisplaySize);
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
