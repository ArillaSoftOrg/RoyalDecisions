using System;
using System.Collections.Generic;
using System.IO;
using RoyalDecisions.Composition;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
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
using Object = UnityEngine.Object;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Repairs and creates the three MVP scenes without directly editing Unity YAML.
    /// </summary>
    public static class SceneSetupAutomation
    {
        public const string GameScenePath = "Assets/_Game/scenes/Game.unity";
        public const string BootstrapScenePath = "Assets/_Game/scenes/Bootstrap.unity";
        public const string MainMenuScenePath = "Assets/_Game/scenes/MainMenu.unity";
        public const string SessionIntentPath = "Assets/_Game/Content/SessionIntent.asset";
        public const string CataloguePath =
            "Assets/_Game/Content/Placeholder/PlaceholderContentCatalogue.asset";
        public const string InterfaceTextPath = TurkishInterfaceTextGenerator.AssetPath;
        public const string TurkishFontPath = TurkishGlyphValidator.FontAssetPath;
        public const string DefaultThemePath = "Assets/_Game/Content/UI/DefaultGameUITheme.asset";
        public const string DefaultFeedbackCueProfilePath =
            "Assets/_Game/Content/UI/DefaultFeedbackCueProfile.asset";

        // Supplied art (Assets/Tasarım/) — folder and file names contain Turkish characters, same
        // as the story content files already in this project; the source is UTF-8 throughout.
        private const string ArtFolder = "Assets/Tasarım/";
        private const string BackgroundArtPath = ArtFolder + "Background.png";
        // The active full-screen backdrop (Reigns-style final presentation). Background.png stays
        // imported but unused — the team may still want it, per "do not delete source assets."
        private const string Background2ArtPath = ArtFolder + "Background2.png";
        // Card-back art shown fixed behind the swipeable portrait, revealed as it is dragged away.
        private const string CardBackArtPath = ArtFolder + "Card.png";
        private const string CardFrameArtPath = ArtFolder + "KartÇerçevesi.png";
        private const string SituationPanelArtPath = ArtFolder + "Parşömen.png";
        private const string PeopleIconArtPath = ArtFolder + "People.png";
        private const string SecurityIconArtPath = ArtFolder + "Güvenlik.png";
        private const string AuthorityIconArtPath = ArtFolder + "Otorite.png";
        private const string WealthIconArtPath = ArtFolder + "Servet.png";
        private const string LeftBannerArtPath = ArtFolder + "solSwipeBanner.png";
        private const string RightBannerArtPath = ArtFolder + "SağSwipeBanner.png";

        // Character portraits (Assets/Tasarım/Characters/) — each maps to the exact `speaker`
        // string authored on real Story CardDefinitions, matched verbatim in
        // AssignCharacterPortraits so a card only ever gets a portrait its own content named.
        private const string CharacterArtFolder = ArtFolder + "Characters/";
        private const string OmerPortraitArtPath = CharacterArtFolder + "GözcüÖmer.png";
        private const string SabihaPortraitArtPath = CharacterArtFolder + "ErzakçıSabiha.png";
        private const string ZeynepPortraitArtPath = CharacterArtFolder + "SağlıkçıDoktorZeynep.png";
        // Currently pixel-identical to ZeynepPortraitArtPath (no story card requires a distinct
        // wounded/bandaged state yet) — imported and reported on, but deliberately not mapped to
        // any speaker below. See AssignCharacterPortraits.
        private const string ZeynepBandagedPortraitArtPath =
            CharacterArtFolder + "BandajlıSağlıkçıZeynep.png";
        private const string AtillaPortraitArtPath =
            CharacterArtFolder + "SığınakGörevlisiAtilla.png";
        private const string AzizPortraitArtPath = CharacterArtFolder + "TarımcıAziz.png";
        private const string IsmetPortraitArtPath = CharacterArtFolder + "Telsizciİsmet.png";
        private const string StoryCardsFolder = "Assets/_Game/Content/Story/Cards";
        // Portraits are large, screen-filling art (see the source portraits at 1024x1536) but
        // smaller canvases than the hero/background art (4096) and clearly above icon-scale
        // (1024); 2048 keeps every supplied portrait at full source resolution with headroom.
        private const int CharacterPortraitMaxSize = 2048;

        // Speaker string -> portrait art path. Only characters with supplied art appear here;
        // every other named speaker (and the narrator, "Anlatıcı") is left with no portrait,
        // which CardView/GraphicFallback already render correctly via the procedural silhouette.
        private static readonly (string Speaker, string ArtPath)[] CharacterPortraitMap =
        {
            ("Ömer (Gözcü)", OmerPortraitArtPath),
            ("Sabiha (Erzakçı)", SabihaPortraitArtPath),
            ("Zeynep (Doktor)", ZeynepPortraitArtPath),
            ("Atilla (Sığınak Görevlisi)", AtillaPortraitArtPath),
            ("Aziz (Tarımcı)", AzizPortraitArtPath),
            ("İsmet (Telsizci)", IsmetPortraitArtPath),
        };

        private const string ReportPath = "Logs/RoyalDecisionsSceneValidation.json";
        // Unity clears Temp during startup, so rollback data must live in the untracked Library.
        private const string BackupRelativePath = "Library/RoyalDecisionsSceneSetupBackup/Last";
        private const string BackupManifestName = "manifest.json";
        private const string CanvasName = "UICanvas";
        private const string LegacyCanvasName = "U\u0131Canvas";
        private const string BuiltInUiSpritePath = "UI/Skin/UISprite.psd";

        // Moderate, consistent rounding for every interactive button; the settings icon chip is
        // sized so the clamp in ProceduralRoundedRectGraphic yields a perfect circle.
        private const float StandardButtonCornerRadius = 26f;
        // Tabs read as a segmented-control/pill nav (distinct from action buttons): radius half
        // their floored 96px height so the ends are fully round, not just softened corners.
        private const float TabPillCornerRadius = 48f;
        private const float SettingsIconButtonSize = 112f;
        private const float SettingsIconButtonMargin = 28f;
        // A dedicated strip above the Game scene's HUD for Back/Ayarlar icon buttons, surfaced the
        // same as HUD so the two read as one continuous panel instead of icons floating loose over
        // the game background. Sized to the accessibility touch-target floor (96px, the same floor
        // ConfigureMinimumTouchTarget already enforces) rather than MainMenu's larger 112px chip —
        // this is a secondary, always-visible in-run control, not a primary menu action — so it sits
        // measurably tighter/denser than MainMenu's single lone settings icon.
        private const float GameTopBarIconSize = 96f;
        // Trimmed from 20 (was 136 combined with HUD below, ~17.9% of a 1920-tall SafeArea) so
        // TopBar+HUD together land inside the requested ~14-17% band instead of just over it.
        // The icon itself stays at the 96px accessibility touch-target floor.
        private const float GameTopBarIconMargin = 12f;
        // Symmetric top/bottom breathing room around the icon: margin + icon + margin.
        private const float GameTopBarHeight =
            GameTopBarIconMargin * 2f + GameTopBarIconSize;
        private const float GameHudHeight = 208f;
        // Where the fixed content column (SituationArea, ContentPanel) begins: directly below the
        // combined TopBar+HUD strip, which together read as one continuous dark panel.
        private const float GameContentTopInset = GameTopBarHeight + GameHudHeight;

        private static readonly Color OverallBackgroundColour = new Color32(0x07, 0x11, 0x1B, 0xFF);
        private static readonly Color SurfaceColour = new Color32(0x12, 0x16, 0x20, 0xFF);
        // Warm royal palette (MainMenu + Settings + About only), matching the game's own gold/navy
        // branding rather than a generic neutral theme. Applied to the camera clear colour, which
        // only these three screens ever actually show — the Game scene's own opaque Background
        // (ConfigureBackground) always covers its camera, so repainting this tone has no visible
        // effect there.
        private static readonly Color MainMenuBackgroundColour = new Color32(0x12, 0x10, 0x0C, 0xFF);
        private static readonly Color CardSurfaceColour = new Color32(0x21, 0x17, 0x1A, 0xFF);
        private static readonly Color BorderGoldColour = new Color32(0xB5, 0x8A, 0x4A, 0xFF);
        private static readonly Color StatBackgroundColour = new Color32(0x2A, 0x2F, 0x3A, 0xFF);
        // Game/GameOver gold — deliberately hardcoded rather than reading SettingsPanelTheme (as it
        // did before the zombie re-theme below), so the Settings-only palette change below cannot
        // change the Game scene's restart button or any other default-coloured menu button there.
        private static readonly Color ButtonColour = new Color(0.78f, 0.58f, 0.18f, 1f);
        // TopBar + HUD panel — a single dark warm brown so the two read as one continuous bar
        // (there is no illustrated background left behind them to blend with any more).
        private static readonly Color HudPanelColour = new Color32(0x2E, 0x1D, 0x12, 0xFF);
        // Dark ink, not gold: the name band now sits on the same aged-paper surface as
        // SituationText (NameScrimColour below, ContentPanel), so it reads as paper-column
        // text rather than a highlight over raw background art. Literal duplicate of
        // SituationTextColour rather than a reference to it — static field initializers run in
        // declaration order, and that field is declared after this one.
        private static readonly Color SpeakerTextColour = new Color32(0x2A, 0x1E, 0x14, 0xFF);
        private static readonly Color BodyTextColour = new Color32(0xF2, 0xE7, 0xCF, 0xFF);
        private static readonly Color SecondaryTextColour = new Color32(0xB9, 0xAA, 0x90, 0xFF);
        // The situation panel sits above the card as light parchment, not the card's dark
        // surface, so it needs its own light background and dark ink text colours.
        private static readonly Color SituationPanelColour = new Color32(0xD9, 0xC7, 0x9E, 0xFF);
        private static readonly Color SituationTextColour = new Color32(0x2A, 0x1E, 0x14, 0xFF);
        // Paper backing behind the fixed name band — literal duplicate of SituationPanelColour
        // (same reason as SpeakerTextColour above) so Speaker reads as sitting on the same
        // paper column as SituationText, not a dark scrim over raw background art.
        private static readonly Color NameScrimColour = new Color32(0xD9, 0xC7, 0x9E, 0xFF);
        // Restrained and mostly dark, with real alpha so the choice preview panel reads as a
        // translucent scrim over the moving portrait rather than an opaque banner.
        // Alpha raised from 0xC0 (~75%) to 0xE0 (~88%) for stronger contrast against the portrait
        // while dragging — within the requested ~82-90% maximum-opacity band.
        private static readonly Color ChoicePreviewLeftTint = new Color32(0x3A, 0x14, 0x18, 0xE0);
        private static readonly Color ChoicePreviewRightTint = new Color32(0x2E, 0x35, 0x14, 0xE0);
        // A quarter-opacity version of BorderGoldColour: still signals "no frame art yet" without
        // reading as a solid debug/bounding-box rectangle around the card.
        private static readonly Color TemporaryCardBorderColour = new Color32(0xB5, 0x8A, 0x4A, 0x40);
        // Same gold as the card's temporary border, but more opaque — the stat bar is small enough
        // that the card's 0x40 alpha nearly disappears, so this needs more contrast to read as a
        // deliberate frame rather than another rendering glitch.
        private static readonly Color StatBarBorderColour = new Color32(0xB5, 0x8A, 0x4A, 0x99);
        private static readonly Color[] StatFillColours =
        {
            new Color32(0x8A, 0x41, 0x4B, 0xFF),
            new Color32(0x68, 0x70, 0x3D, 0xFF),
            new Color32(0x3E, 0x56, 0x7D, 0xFF),
            new Color32(0xB3, 0x8A, 0x3D, 0xFF)
        };
        // Settings/About-only text tones — deliberately separate from SpeakerTextColour/
        // SecondaryTextColour above, which the Game/Card scene still uses as-is (even though the
        // values are close; kept decoupled per this file's existing pattern for Settings colours).
        private static readonly Color MenuTitleTextColour = new Color32(0xF2, 0xE7, 0xCF, 0xFF);
        private static readonly Color MenuMutedTextColour = new Color32(0xB9, 0xAA, 0x90, 0xFF);
        // A shade darker than SettingsPanelTheme.InactiveTabColour (the row card's own background)
        // so the slider/toggle track reads as a sunken groove inside its card instead of blending
        // into the card fill behind it.
        private static readonly Color MenuTrackGrooveColour = new Color32(0x12, 0x0E, 0x0A, 0xFF);

        [MenuItem("Tools/Royal Decisions/Scene Setup/Audit")]
        public static void AuditMenu()
        {
            WriteAndLog(ValidateProject("Audit"));
        }

        [MenuItem("Tools/Royal Decisions/Scene Setup/Apply Remaining Setup")]
        public static void ApplyMenu()
        {
            WriteAndLog(ApplyProject(false));
        }

        [MenuItem("Tools/Royal Decisions/Scene Setup/Validate")]
        public static void ValidateMenu()
        {
            WriteAndLog(ValidateProject("Validate"));
        }

        [MenuItem("Tools/Royal Decisions/Content/Assign Character Portraits")]
        public static void AssignCharacterPortraitsMenu()
        {
            WriteAndLog(AssignCharacterPortraits());
        }

        public static void AssignCharacterPortraitsBatch()
        {
            SceneSetupReport report = AssignCharacterPortraits();
            WriteAndLog(report);

            if (!report.Succeeded)
            {
                throw new InvalidOperationException(
                    "Character portrait assignment failed. See " + ReportPath + ".");
            }
        }

        [MenuItem("Tools/Royal Decisions/Scene Setup/Restore Last Backup")]
        public static void RestoreLastBackupMenu()
        {
            SceneSetupReport report = new SceneSetupReport("Restore Last Backup");

            if (!EditorUtility.DisplayDialog(
                    "Restore Royal Decisions scene setup",
                    "Restore the last scene-setup backup and remove assets created by that run?",
                    "Restore",
                    "Cancel"))
            {
                report.Add(SceneSetupIssueSeverity.Info, "RESTORE_CANCELLED", "Rollback",
                    string.Empty, string.Empty, "Restore was cancelled.");
                WriteAndLog(report);
                return;
            }

            RestoreBackup(report);
            WriteAndLog(report);
        }

        public static void ApplyBatch()
        {
            SceneSetupReport report = ApplyProject(true);
            WriteAndLog(report);

            if (!report.Succeeded)
            {
                throw new InvalidOperationException(
                    "Royal Decisions scene setup failed. See " + ReportPath + ".");
            }
        }

        public static void ValidateBatch()
        {
            SceneSetupReport report = ValidateProject("Validate Batch");
            WriteAndLog(report);

            if (!report.Succeeded)
            {
                throw new InvalidOperationException(
                    "Royal Decisions scene validation failed. See " + ReportPath + ".");
            }
        }

        public static void RestoreLastBackupBatch()
        {
            SceneSetupReport report = new SceneSetupReport("Restore Last Backup Batch");
            RestoreBackup(report);
            WriteAndLog(report);

            if (!report.Succeeded)
            {
                throw new InvalidOperationException(
                    "Royal Decisions scene backup restore failed. See " + ReportPath + ".");
            }
        }

        /// <summary>Test seam: applies Game-scene authoring without saving an asset.</summary>
        public static SceneSetupReport ApplyGameSceneForTests(
            Scene scene,
            ContentCatalogue catalogue,
            SessionIntent sessionIntent)
        {
            SceneSetupReport report = new SceneSetupReport("Apply Game Scene For Tests");
            ApplyGameScene(scene, catalogue, sessionIntent, report);
            return report;
        }

        /// <summary>Test seam: validates a loaded Game scene without changing it.</summary>
        public static SceneSetupReport ValidateGameSceneForTests(
            Scene scene,
            ContentCatalogue catalogue,
            SessionIntent sessionIntent)
        {
            SceneSetupReport report = new SceneSetupReport("Validate Game Scene For Tests");
            ValidateGameScene(scene, catalogue, sessionIntent, report);
            return report;
        }

        private static SceneSetupReport ApplyProject(bool batchMode)
        {
            SceneSetupReport report = new SceneSetupReport("Apply Remaining Setup");

            if (!batchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                report.Add(SceneSetupIssueSeverity.Error, "UNSAVED_SCENES", "Safety",
                    string.Empty, string.Empty,
                    "Scene setup was cancelled because modified scenes were not saved.");
                return report;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            BackupManifest manifest = CreateBackup(report);

            if (!report.Succeeded)
            {
                return report;
            }

            try
            {
                ConfigureArtTextureImportSettings(report);

                SessionIntent intent = EnsureSessionIntent(report);
                InterfaceTextDefinition interfaceText =
                    AssetDatabase.LoadAssetAtPath<InterfaceTextDefinition>(InterfaceTextPath);
                TMP_FontAsset turkishFont =
                    AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TurkishFontPath);
                GameUITheme theme = EnsureDefaultTheme(report);
                FeedbackCueProfile feedback = EnsureDefaultFeedbackCueProfile(report);
                // The real Game scene is canonically wired to the Story catalogue (see
                // StorySceneWiring) — Apply must wire and validate against the same asset, or its
                // own post-apply validation (which already checks the Game scene against
                // StorySceneWiring.StoryCataloguePath) fails against whatever Apply just wrote.
                ContentCatalogue catalogue = AssetDatabase.LoadAssetAtPath<ContentCatalogue>(
                    StorySceneWiring.StoryCataloguePath);

                if (catalogue == null)
                {
                    report.Add(SceneSetupIssueSeverity.Error, "CATALOGUE_MISSING", "Assets",
                        StorySceneWiring.StoryCataloguePath, string.Empty,
                        "The story catalogue is missing or has the wrong type.");
                    throw new InvalidOperationException("Required catalogue is unavailable.");
                }
                if (interfaceText == null || turkishFont == null)
                {
                    report.Add(SceneSetupIssueSeverity.Error, "TURKISH_TEXT_ASSET_MISSING", "Assets",
                        interfaceText == null ? InterfaceTextPath : TurkishFontPath, string.Empty,
                        "The Turkish interface text and project-owned TMP font must be generated first.");
                    throw new InvalidOperationException("Required Turkish text assets are unavailable.");
                }

                AssetDatabase.SaveAssets();

                Scene game = OpenRequiredScene(GameScenePath, report);
                if (!game.IsValid())
                {
                    throw new InvalidOperationException("Game scene could not be opened.");
                }

                // Prefer the story catalogue here specifically: ValidateProjectLoadedState's
                // post-apply check (below) validates the Game scene's GameSceneController.catalogue
                // against StorySceneWiring.StoryCataloguePath unconditionally, because "the
                // committed Game scene is wired to the story catalogue" (see that method's own
                // comment). Writing the placeholder catalogue here — the pre-story-content default —
                // guaranteed a mismatch on every run: apply, save, then immediately fail its own
                // post-apply validation and roll back via RestoreBackup, silently discarding every
                // change this pass made (including unrelated ones, e.g. HUD stat bar sizing) even
                // though nothing was actually wrong with them. Falls back to the placeholder
                // catalogue only if the story one has not been generated yet, preserving this tool's
                // original placeholder-only behaviour for a project with no story content.
                catalogue = AssetDatabase.LoadAssetAtPath<ContentCatalogue>(
                    StorySceneWiring.StoryCataloguePath);
                if (catalogue == null)
                {
                    catalogue = AssetDatabase.LoadAssetAtPath<ContentCatalogue>(CataloguePath);
                }
                intent = AssetDatabase.LoadAssetAtPath<SessionIntent>(SessionIntentPath);
                interfaceText = AssetDatabase.LoadAssetAtPath<InterfaceTextDefinition>(
                    InterfaceTextPath);
                turkishFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TurkishFontPath);
                theme = AssetDatabase.LoadAssetAtPath<GameUITheme>(DefaultThemePath);
                feedback = AssetDatabase.LoadAssetAtPath<FeedbackCueProfile>(
                    DefaultFeedbackCueProfilePath);
                AssignSuppliedArt(theme, report);

                ApplyGameScene(
                    game, catalogue, intent, interfaceText, turkishFont, theme, feedback, report);
                if (!report.Succeeded)
                {
                    throw new InvalidOperationException("Game scene contains blocking ambiguity.");
                }

                EditorSceneManager.MarkSceneDirty(game);
                if (!EditorSceneManager.SaveScene(game, GameScenePath))
                {
                    throw new InvalidOperationException("Game scene could not be saved.");
                }

                Scene bootstrap = OpenOrCreateEmptyScene(BootstrapScenePath);
                ApplyBootstrapScene(bootstrap, report);
                EditorSceneManager.MarkSceneDirty(bootstrap);
                if (!EditorSceneManager.SaveScene(bootstrap, BootstrapScenePath))
                {
                    throw new InvalidOperationException("Bootstrap scene could not be saved.");
                }

                Scene mainMenu = OpenOrCreateEmptyScene(MainMenuScenePath);
                // Opening/saving multiple scenes can release asset object instances that are no
                // longer referenced by the active scene. Resolve project assets immediately before
                // wiring the menu so the serialized references always use the current instances.
                intent = AssetDatabase.LoadAssetAtPath<SessionIntent>(SessionIntentPath);
                interfaceText = AssetDatabase.LoadAssetAtPath<InterfaceTextDefinition>(
                    InterfaceTextPath);
                turkishFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TurkishFontPath);
                FeedbackCueProfile menuFeedback =
                    AssetDatabase.LoadAssetAtPath<FeedbackCueProfile>(DefaultFeedbackCueProfilePath);
                ApplyMainMenuScene(mainMenu, intent, interfaceText, turkishFont, menuFeedback, report);
                EditorSceneManager.MarkSceneDirty(mainMenu);
                if (!EditorSceneManager.SaveScene(mainMenu, MainMenuScenePath))
                {
                    throw new InvalidOperationException("MainMenu scene could not be saved.");
                }

                ApplyBuildScenes();

                AssetDatabase.SaveAssets();

                SceneSetupReport validation = ValidateProjectLoadedState(
                    "Post-apply Validation", catalogue, intent);
                report.Merge(validation);

                if (!validation.Succeeded)
                {
                    throw new InvalidOperationException("Post-apply validation failed.");
                }

                report.Add(SceneSetupIssueSeverity.Info, "APPLY_COMPLETE", "Summary",
                    string.Empty, string.Empty,
                    "Game UI foundation and theme were applied; supporting scenes and build order are valid.");
            }
            catch (Exception exception)
            {
                report.Add(SceneSetupIssueSeverity.Error, "APPLY_EXCEPTION", "Safety",
                    string.Empty, string.Empty, exception.Message);
                RestoreBackup(report, manifest);
            }
            finally
            {
                if (!batchMode && originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            return report;
        }

        private static SceneSetupReport ValidateProject(string operation)
        {
            SceneSetupReport report = new SceneSetupReport(operation);

            if (!UnityEngine.Application.isBatchMode
                && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                report.Add(SceneSetupIssueSeverity.Error, "UNSAVED_SCENES", "Safety",
                    string.Empty, string.Empty,
                    "Validation was cancelled because modified scenes were not saved.");
                return report;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                ContentCatalogue catalogue = AssetDatabase.LoadAssetAtPath<ContentCatalogue>(
                    StorySceneWiring.StoryCataloguePath);
                SessionIntent intent = AssetDatabase.LoadAssetAtPath<SessionIntent>(SessionIntentPath);
                report.Merge(ValidateProjectLoadedState(operation, catalogue, intent));
            }
            finally
            {
                if (!UnityEngine.Application.isBatchMode && originalSetup != null && originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }

            return report;
        }

        private static SceneSetupReport ValidateProjectLoadedState(
            string operation,
            ContentCatalogue catalogue,
            SessionIntent intent)
        {
            SceneSetupReport report = new SceneSetupReport(operation);

            // Opening scenes with Single mode may unload the managed wrappers supplied by the
            // caller. Reload stable asset references before comparing serialized fields.
            if (catalogue == null)
            {
                catalogue = AssetDatabase.LoadAssetAtPath<ContentCatalogue>(
                    StorySceneWiring.StoryCataloguePath);
            }
            if (intent == null)
            {
                intent = AssetDatabase.LoadAssetAtPath<SessionIntent>(SessionIntentPath);
            }

            GameUITheme theme = AssetDatabase.LoadAssetAtPath<GameUITheme>(DefaultThemePath);
            if (theme == null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "UI_THEME_MISSING", "Assets",
                    DefaultThemePath, string.Empty, "Default GameUITheme is missing or invalid.");
            }
            else
            {
                ValidateTheme(theme, report);
            }
            if (AssetDatabase.LoadAssetAtPath<FeedbackCueProfile>(
                    DefaultFeedbackCueProfilePath) == null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "FEEDBACK_PROFILE_MISSING", "Assets",
                    DefaultFeedbackCueProfilePath, string.Empty,
                    "Default feedback cue profile is missing or invalid.");
            }

            if (catalogue == null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "CATALOGUE_MISSING", "Assets",
                    StorySceneWiring.StoryCataloguePath, string.Empty,
                    "ContentCatalogue is missing or invalid.");
            }

            if (intent == null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "SESSION_INTENT_MISSING", "Assets",
                    SessionIntentPath, string.Empty, "SessionIntent is missing or invalid.");
            }

            // Reloaded independently here (rather than reusing the catalogue captured above)
            // because this runs inside ValidateSceneAsset's callback, after the Game scene has
            // been opened Single — which can unload asset references nothing else currently
            // holds live, per the same caution StorySceneWiring documents.
            ValidateSceneAsset(GameScenePath, report,
                scene => ValidateGameScene(
                    scene,
                    AssetDatabase.LoadAssetAtPath<ContentCatalogue>(
                        StorySceneWiring.StoryCataloguePath),
                    AssetDatabase.LoadAssetAtPath<SessionIntent>(SessionIntentPath),
                    report));
            ValidateSceneAsset(BootstrapScenePath, report,
                scene => ValidateBootstrapScene(scene, report));
            ValidateSceneAsset(MainMenuScenePath, report,
                scene => ValidateMainMenuScene(
                    scene,
                    AssetDatabase.LoadAssetAtPath<SessionIntent>(SessionIntentPath),
                    report));
            ValidateBuildScenes(report);

            if (report.Succeeded)
            {
                report.Add(SceneSetupIssueSeverity.Info, "VALIDATION_OK", "Summary",
                    string.Empty, string.Empty, "All managed scene setup checks passed.");
            }

            return report;
        }

        private static void ValidateSceneAsset(
            string path,
            SceneSetupReport report,
            Action<Scene> validator)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "SCENE_MISSING", "Scenes",
                    path, string.Empty, "Required scene is missing.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            validator(scene);
        }

        // Game scene -----------------------------------------------------------------

        private static void ApplyGameScene(
            Scene scene,
            ContentCatalogue catalogue,
            SessionIntent intent,
            SceneSetupReport report)
        {
            ApplyGameScene(
                scene,
                catalogue,
                intent,
                AssetDatabase.LoadAssetAtPath<InterfaceTextDefinition>(InterfaceTextPath),
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TurkishFontPath),
                AssetDatabase.LoadAssetAtPath<GameUITheme>(DefaultThemePath),
                AssetDatabase.LoadAssetAtPath<FeedbackCueProfile>(DefaultFeedbackCueProfilePath),
                report);
        }

        private static void ApplyGameScene(
            Scene scene,
            ContentCatalogue catalogue,
            SessionIntent intent,
            InterfaceTextDefinition interfaceText,
            TMP_FontAsset font,
            GameUITheme theme,
            FeedbackCueProfile feedback,
            SceneSetupReport report)
        {
            if (!PreflightGameScene(scene, report))
            {
                return;
            }

            EnsureCamera(scene, report);
            EnsureEventSystem(scene, report);

            GameObject canvasObject = EnsureGameCanvas(scene, report);
            if (canvasObject == null)
            {
                return;
            }

            RectTransform safeArea = EnsureUiChild(canvasObject.transform, "SafeArea", report);
            Stretch(safeArea);
            EnsureSingleComponent<SafeAreaFitter>(safeArea.gameObject, report);

            BackgroundView background = ConfigureBackground(canvasObject.transform, report);

            // A top strip above HUD for Geri (back to MainMenu) and Ayarlar (Settings) — HUD's own
            // row is already edge-to-edge with the four stat bars, so these need their own space.
            // Surfaced the same as HUD (zero gap between the two) so together they read as one
            // continuous dark warm-brown panel — there is no illustrated background left behind
            // them on portrait mobile to blend with any more.
            RectTransform topBar = EnsureUiChild(safeArea, "TopBar", report);
            SetRect(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
                new Vector2(0f, GameTopBarHeight), new Vector2(0.5f, 1f));
            Image topBarSurface = EnsureSingleComponent<Image>(topBar.gameObject, report);
            ConfigureSimpleImage(topBarSurface, LoadBuiltInUiSprite(report), HudPanelColour, false);
            Button backButton = EnsureBackIconButton(
                topBar, report, GameTopBarIconSize, GameTopBarIconMargin);
            Button settingsButton = EnsureSettingsIconButton(
                topBar, report, GameTopBarIconSize, GameTopBarIconMargin);
            ConfigureMinimumTouchTarget(backButton, report);
            ConfigureMinimumTouchTarget(settingsButton, report);

            HUDView hud = ConfigureHud(safeArea, interfaceText, font, report);
            if (hud != null)
            {
                // Shift HUD down by GameTopBarHeight so it sits below the new top bar instead of
                // flush with SafeArea's own top edge.
                SetRect(hud.transform as RectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0f, -GameTopBarHeight), new Vector2(0f, GameHudHeight),
                    new Vector2(0.5f, 1f));
            }
            RectTransform contentPanel = ConfigureContentPanel(safeArea, report);
            FooterParts footer = ConfigureFooter(safeArea, interfaceText, font, report);
            // Kept (hidden, not deleted) for its layout slot and in case it's ever needed again —
            // see ConfigureSituationArea. bodyText is wired inside ConfigureCard now instead.
            SituationAreaParts situationArea = ConfigureSituationArea(safeArea, font, report);
            CardParts card = ConfigureCard(safeArea, contentPanel, font, report);
            SetObjectProperty(card.View, "bodyText", situationArea.Text, report);
            TapChoiceButtonsParts tapChoices = ConfigureTapChoiceButtons(
                safeArea, font, card.Swipe, report);
            TutorialParts tutorial = ConfigureTutorial(safeArea, font, report);
            GameOverParts gameOver = ConfigureGameOver(
                canvasObject, safeArea, interfaceText, font, report);
            AudioService audio = ConfigureAudio(scene, report);
            // Starts opaque (unlike every other panel in this file) — see the method's own remarks.
            PanelFadeAnimator transitionOverlay = ConfigureTransitionOverlay(
                canvasObject.transform, report, startVisible: true);

            GameObject controllerObject = EnsureRoot(scene, "GameSceneController", report);
            GameSceneController controller = EnsureSingleComponent<GameSceneController>(
                controllerObject, report);

            if (controller != null && card.View != null && card.Swipe != null)
            {
                SetObjectProperty(controller, "catalogue", catalogue, report);
                SetObjectProperty(controller, "cardView", card.View, report);
                SetObjectProperty(controller, "hudView", hud, report);
                SetObjectProperty(controller, "gameOverView", gameOver.View, report);
                SetObjectProperty(controller, "swipeController", card.Swipe, report);
                SetObjectProperty(controller, "tapChoiceButtonsView", tapChoices.View, report);
                SetObjectProperty(controller, "runStatusView", footer.RunStatus, report);
                SetObjectProperty(controller, "footerView", footer.Footer, report);
                SetObjectProperty(controller, "audioService", audio, report);
                SetObjectProperty(controller, "sessionIntent", intent, report);
                SetObjectProperty(controller, "tutorialCoordinator", tutorial.Coordinator, report);
                SetObjectProperty(controller, "transitionOverlay", transitionOverlay, report);
                SetEnumProperty(controller, "fallbackStartMode", (int)SessionStartMode.NewGame, report);
            }

            // The same full Settings/About panel MainMenu has, reachable mid-run via the Ayarlar
            // icon above — reuses ConfigureSettingsPanel/ConfigureAboutPanel as-is (both are scene-
            // agnostic already) rather than building a second, different settings UI.
            SettingsParts gameSettings = ConfigureSettingsPanel(
                canvasObject.transform, font, audio, feedback, report);
            AboutPanelView gameAboutPanel = ConfigureAboutPanel(canvasObject.transform, font, report);
            SetObjectProperty(gameSettings.Controller, "aboutPanel", gameAboutPanel, report);
            // Hides the whole gameplay screen (TopBar/HUD/Card/Footer) while Settings or About is
            // open, exactly like MainMenu hides its own MainMenuPanel behind the same panel.
            SetObjectProperty(gameSettings.Controller, "mainMenuRoot", safeArea.gameObject, report);
            SetObjectProperty(gameSettings.Controller, "gameSceneController", controller, report);

            GameObject gameResetProgressObject = EnsureRoot(scene, "ResetProgressController", report);
            ResetProgressController gameResetProgressController =
                EnsureSingleComponent<ResetProgressController>(gameResetProgressObject, report);
            SetObjectProperty(gameResetProgressController, "view", gameSettings.View, report);

            AccessibilityPresentationController accessibility =
                EnsureSingleComponent<AccessibilityPresentationController>(controllerObject, report);
            TextMeshProUGUI[] accessibleText = FindComponentsInScene<TextMeshProUGUI>(scene);
            SetObjectArrayProperty(accessibility, "scalableText", accessibleText, report);
            SetObjectArrayProperty(accessibility, "secondaryText", new[]
            {
                card.View != null ? card.View.GetComponentInChildren<TextMeshProUGUI>(true) : null,
                footer.Root != null ? footer.Root.GetComponentInChildren<TextMeshProUGUI>(true) : null
            }, report);
            SetObjectProperty(accessibility, "swipeController", card.Swipe, report);
            SetObjectArrayProperty(accessibility, "statItems",
                hud != null ? hud.GetComponentsInChildren<StatItemView>(true) : Array.Empty<StatItemView>(),
                report);
            // Reduced Motion shortens every fade/scale transition in this scene: the entry overlay,
            // the newly-added Settings panel's open/close and tab crossfade, and About's open/close.
            PanelFadeAnimator gameAboutPanelTransition = gameAboutPanel != null
                ? gameAboutPanel.GetComponent<PanelFadeAnimator>() : null;
            List<PanelFadeAnimator> gamePanelAnimators =
                new List<PanelFadeAnimator> { transitionOverlay };
            gamePanelAnimators.AddRange(gameSettings.PanelAnimators);
            if (gameAboutPanelTransition != null)
            {
                gamePanelAnimators.Add(gameAboutPanelTransition);
            }
            SetObjectArrayProperty(accessibility, "panelAnimators", gamePanelAnimators.ToArray(), report);
            if (controller != null)
            {
                SetObjectProperty(controller, "accessibility", accessibility, report);
            }
            SetObjectProperty(gameSettings.Controller, "accessibility", accessibility, report);

            GameFeedbackController feedbackController =
                EnsureSingleComponent<GameFeedbackController>(controllerObject, report);
            SetObjectProperty(feedbackController, "gameSceneController", controller, report);
            SetObjectProperty(feedbackController, "swipeController", card.Swipe, report);
            SetObjectProperty(feedbackController, "audioService", audio, report);
            SetObjectProperty(feedbackController, "cues", feedback, report);

            ApplicationLifecycleController lifecycle =
                EnsureSingleComponent<ApplicationLifecycleController>(controllerObject, report);
            SetObjectProperty(lifecycle, "gameSceneController", controller, report);
            SetObjectProperty(lifecycle, "tutorialCoordinator", tutorial.Coordinator, report);
            SetObjectProperty(lifecycle, "settingsController", gameSettings.Controller, report);
            SetStringProperty(lifecycle, "mainMenuSceneName", "MainMenu", report);
            SetBoolProperty(lifecycle, "mainMenuMode", false, report);

            EnsureExpectedListener(backButton, lifecycle,
                nameof(ApplicationLifecycleController.HandleBackRequested),
                lifecycle != null ? lifecycle.HandleBackRequested : null, report);
            EnsureExpectedListener(settingsButton, gameSettings.Controller,
                nameof(SettingsController.Open),
                gameSettings.Controller != null ? gameSettings.Controller.Open : null, report);

            GameUIThemeController themeController = EnsureSingleComponent<GameUIThemeController>(
                canvasObject, report);
            if (themeController != null)
            {
                if (GetObjectProperty(themeController, "theme") == null)
                {
                    SetObjectProperty(themeController, "theme", theme, report);
                }
                SetObjectProperty(themeController, "backgroundView", background, report);
                SetObjectProperty(themeController, "hudView", hud, report);
                SetObjectProperty(themeController, "cardView", card.View, report);
                SetObjectProperty(themeController, "footerView", footer.Footer, report);
                SetObjectProperty(themeController, "gameOverView", gameOver.View, report);
                SetObjectProperty(themeController, "situationPanelImage", situationArea.Artwork, report);
                SetObjectProperty(
                    themeController, "situationPanelFallback", situationArea.Fallback, report);
                themeController.ApplyTheme();
            }

            if (background != null)
            {
                SetSiblingIndex(background.transform, 0);
                SetSiblingIndex(safeArea, 1);
            }

            if (hud != null && contentPanel != null && situationArea.Root != null
                && card.Area != null && tapChoices.Root != null && footer.Root != null
                && tutorial.Root != null && gameOver.Root != null)
            {
                SetSiblingIndex(topBar, 0);
                SetSiblingIndex(hud.transform, 1);
                SetSiblingIndex(contentPanel, 2);
                SetSiblingIndex(situationArea.Root, 3);
                SetSiblingIndex(card.Area, 4);
                SetSiblingIndex(tapChoices.Root, 5);
                SetSiblingIndex(footer.Root, 6);
                SetSiblingIndex(tutorial.Root, 7);
                SetSiblingIndex(gameOver.Root, 8);
            }

            // TMP auto-sizing stores both its configured base size and its last calculated size.
            // Force the calculation before every save so a newly-created scene and a repair pass
            // serialize the same value instead of converging only on the second run.
            Canvas.ForceUpdateCanvases();
            TextMeshProUGUI[] managedText = FindComponentsInScene<TextMeshProUGUI>(scene);
            for (int i = 0; i < managedText.Length; i++)
            {
                if (font != null && managedText[i].font != font)
                {
                    Undo.RecordObject(managedText[i], "Assign Turkish TMP font");
                    managedText[i].font = font;
                }
                managedText[i].ForceMeshUpdate(true, true);
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static bool PreflightGameScene(Scene scene, SceneSetupReport report)
        {
            bool valid = true;
            valid &= CheckRootDuplicates(scene, CanvasName, report);
            valid &= CheckRootDuplicates(scene, "Main Camera", report);
            valid &= CheckRootDuplicates(scene, "EventSystem", report);
            valid &= CheckRootDuplicates(scene, "AudioService", report);
            valid &= CheckRootDuplicates(scene, "GameSceneController", report);
            valid &= CheckRootDuplicates(scene, "SettingsController", report);

            GameObject canvas = FindUniqueRoot(scene, CanvasName, null);
            if (canvas == null)
            {
                GameObject legacy = FindUniqueRoot(scene, LegacyCanvasName, null);
                Canvas[] canvases = FindComponentsInScene<Canvas>(scene);

                if (legacy == null && canvases.Length > 1)
                {
                    report.Add(SceneSetupIssueSeverity.Error, "AMBIGUOUS_CANVAS", "Hierarchy",
                        scene.path, "/", "Multiple root Canvases exist and none is /UICanvas.");
                    valid = false;
                }
            }

            return valid;
        }

        private static GameObject EnsureGameCanvas(Scene scene, SceneSetupReport report)
        {
            GameObject canvasObject = FindUniqueRoot(scene, CanvasName, report);

            if (canvasObject == null)
            {
                GameObject legacy = FindUniqueRoot(scene, LegacyCanvasName, report);
                if (legacy != null && legacy.GetComponent<Canvas>() != null)
                {
                    Undo.RecordObject(legacy, "Repair UICanvas name");
                    legacy.name = CanvasName;
                    canvasObject = legacy;
                }
            }

            canvasObject ??= EnsureRoot(scene, CanvasName, report, true);
            ConfigureCanvas(canvasObject, report);
            return canvasObject;
        }

        private static BackgroundView ConfigureBackground(
            Transform canvas,
            SceneSetupReport report)
        {
            RectTransform root = EnsureUiChild(canvas, "Background", report);
            Stretch(root);
            Image surface = EnsureSingleComponent<Image>(root.gameObject, report);
            ConfigureSimpleImage(surface, LoadBuiltInUiSprite(report), OverallBackgroundColour, false);

            RectTransform artworkTransform = EnsureUiChild(root, "Artwork", report);
            RectTransform overlayTransform = EnsureUiChild(root, "DarkOverlay", report);
            RectTransform vignetteTransform = EnsureUiChild(root, "Vignette", report);
            RectTransform proceduralTransform = EnsureUiChild(root, "ProceduralVignette", report);
            Stretch(overlayTransform);
            Stretch(vignetteTransform);
            Stretch(proceduralTransform);
            Image artwork = EnsureSingleComponent<Image>(artworkTransform.gameObject, report);
            // Cover-fit: fills the viewport and crops overflow instead of stretching or
            // letterboxing. EnvelopeParent needs the rect free to resize, so it is not Stretch-
            // anchored like its siblings; BackgroundView sets the actual aspect ratio at runtime
            // from the assigned sprite.
            SetRect(artworkTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, Center);
            AspectRatioFitter artworkFitter =
                EnsureSingleComponent<AspectRatioFitter>(artworkTransform.gameObject, report);
            Image overlay = EnsureSingleComponent<Image>(overlayTransform.gameObject, report);
            Image vignette = EnsureSingleComponent<Image>(vignetteTransform.gameObject, report);
            ConfigureSimpleImage(artwork, null, Color.white, false, false);
            // Background2 is already dark, so only a light scrim is needed; BackgroundView.
            // ApplyTheme sets the same value at runtime.
            ConfigureSimpleImage(overlay, null, new Color(0f, 0f, 0f, 0.12f), false);
            ConfigureSimpleImage(vignette, null, Color.white, false, false);
            ProceduralVignetteGraphic procedural =
                EnsureSingleComponent<ProceduralVignetteGraphic>(proceduralTransform.gameObject, report);
            if (procedural != null)
            {
                Undo.RecordObject(procedural, "Configure procedural vignette");
                procedural.raycastTarget = false;
                procedural.SetStyle(Color.black, 0.22f, 0.42f);
            }

            BackgroundView view = EnsureSingleComponent<BackgroundView>(root.gameObject, report);
            SetObjectProperty(view, "fallbackSurface", surface, report);
            SetObjectProperty(view, "artwork", artwork, report);
            SetObjectProperty(view, "artworkFitter", artworkFitter, report);
            SetObjectProperty(view, "darkOverlay", overlay, report);
            SetObjectProperty(view, "vignette", vignette, report);
            SetObjectProperty(view, "proceduralVignette", procedural, report);
            return view;
        }

        private static HUDView ConfigureHud(
            RectTransform safeArea,
            InterfaceTextDefinition interfaceText,
            TMP_FontAsset font,
            SceneSetupReport report)
        {
            RectTransform hudTransform = EnsureUiChild(safeArea, "HUD", report);
            SetRect(hudTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(0f, GameHudHeight), new Vector2(0.5f, 1f));

            HUDView hud = EnsureSingleComponent<HUDView>(hudTransform.gameObject, report);
            // Dark warm brown, same as TopBar above it — the two read as one continuous panel
            // (there is no illustrated background left behind them to blend with any more).
            Image hudSurface = EnsureSingleComponent<Image>(hudTransform.gameObject, report);
            ConfigureSimpleImage(hudSurface, LoadBuiltInUiSprite(report), HudPanelColour, false);
            HorizontalLayoutGroup layout = EnsureSingleComponent<HorizontalLayoutGroup>(
                hudTransform.gameObject, report);
            if (layout != null)
            {
                Undo.RecordObject(layout, "Configure HUD layout");
                layout.childAlignment = TextAnchor.MiddleCenter;
                // Larger side padding and tighter spacing pull the four equally-expanding slots
                // (and the icons centered in them) closer together as a group — with
                // childForceExpandWidth on, a slot's own width already absorbs most of a spacing
                // change, so padding is the stronger lever here.
                layout.padding = new RectOffset(64, 64, 12, 12);
                layout.spacing = 0f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
            }

            string[] statNames =
            {
                "StatItem_People", "StatItem_Security", "StatItem_Authority", "StatItem_Wealth"
            };
            string[] slotNames =
            {
                "StatSlot_People", "StatSlot_Security", "StatSlot_Authority", "StatSlot_Wealth"
            };
            StatType[] stats =
            {
                StatType.People, StatType.Security, StatType.Authority, StatType.Wealth
            };
            // The four supplied icon PNGs have very different amounts of transparent padding
            // around their visible content (People.png ~70% fill, Güvenlik.png ~88%, Otorite.png
            // ~80%, Servet.png ~76%, at canvas aspects 1.21/1.02/1.47/1.11), so assigning them the
            // same RectTransform with Preserve Aspect renders very different apparent icon sizes.
            // These per-stat multipliers (measured from each PNG's actual alpha-channel content
            // bounding box) normalize perceived icon height to a consistent ~72% of the icon slot,
            // in the same People/Security/Authority/Wealth order as the stats array above.
            float[] iconScales = { 1.14f, 0.79f, 1.17f, 1.05f };
            Sprite uiSprite = LoadBuiltInUiSprite(report);
            StatItemView[] items = new StatItemView[statNames.Length];

            for (int i = 0; i < statNames.Length; i++)
            {
                RectTransform slot = EnsureUiChild(hudTransform, slotNames[i], report);
                LayoutElement slotLayout = EnsureSingleComponent<LayoutElement>(slot.gameObject, report);
                if (slotLayout != null)
                {
                    Undo.RecordObject(slotLayout, "Configure stat slot layout");
                    slotLayout.flexibleWidth = 1f;
                    slotLayout.minWidth = 0f;
                }

                RectTransform itemTransform = FindDirectChild(slot, statNames[i], report);
                RectTransform legacyItem = FindDirectChild(hudTransform, statNames[i], report);
                if (itemTransform == null && legacyItem != null)
                {
                    Undo.SetTransformParent(legacyItem, slot, "Move stat bar into semantic slot");
                    itemTransform = legacyItem;
                }
                itemTransform ??= EnsureUiChild(slot, statNames[i], report);
                // A stat gauge beneath the icon/value stack. Went through two failed sizings
                // before this one: 3 units tall at 36% slot width (a 2026-08-25 pass shrunk an
                // original, approved 24-unit bar down to a "faint accent underline" — see
                // MANUAL_UNITY_STEPS.md) read as a near-invisible hairline; bumping height alone to
                // 24 barely helped, since width was the bigger problem. Now 84% of the slot's width
                // (was 36%) and a gold Outline frame (see below) so it reads as a themed gauge
                // rather than a stray colour bar.
                SetRect(itemTransform, new Vector2(0.08f, 0f), new Vector2(0.92f, 0f),
                    new Vector2(0f, 20f), new Vector2(0f, 20f), new Vector2(0.5f, 0f));

                Image background = EnsureSingleComponent<Image>(itemTransform.gameObject, report);
                StatItemView item = EnsureSingleComponent<StatItemView>(
                    itemTransform.gameObject, report);
                Outline barOutline = EnsureSingleComponent<Outline>(itemTransform.gameObject, report);
                if (barOutline != null)
                {
                    Undo.RecordObject(barOutline, "Configure stat bar outline");
                    // Frames the background+fill pair (both exactly itemTransform's own rect) in
                    // gold, the same device used for the card's own temporary border, so the gauge
                    // reads as an intentional, on-theme element instead of an unstyled rectangle.
                    barOutline.effectColor = StatBarBorderColour;
                    barOutline.effectDistance = new Vector2(1.5f, -1.5f);
                    barOutline.useGraphicAlpha = false;
                }
                RectTransform fillTransform = FindDirectChild(itemTransform, "Fill", report);
                fillTransform ??= EnsureUiChild(itemTransform, "Fill", report);
                Stretch(fillTransform);
                Image fill = EnsureSingleComponent<Image>(fillTransform.gameObject, report);
                // Centered in its slot (the value number beside it is currently hidden — see
                // below) so the four icons sit as close together as the slots allow, instead of
                // each one hugging the left edge of its slot with dead space to the right.
                // Box scaled per-stat around its own center (0.5, 0.60) by iconScales[i] so all
                // four icons read at a visually consistent size despite differing source padding.
                // Center lowered again, from 0.68 to 0.62 to 0.60, each time to buy headroom for
                // another size increase (0.23/0.25 -> 0.26/0.29 -> 0.29/0.32) without the tallest
                // icon (Authority, iconScale 1.17) clipping the slot's top edge or dipping into the
                // stat bar now anchored at the slot's bottom (see ConfigureHud's itemTransform
                // SetRect above): at 0.32 half-height and 1.17 scale, Authority's box still leaves
                // ~0.02 clear at the top and ~1.5 reference units clear above the bar.
                RectTransform iconTransform = EnsureUiChild(slot, "Icon", report);
                float iconScale = iconScales[i];
                float iconHalfWidth = 0.29f * iconScale;
                float iconHalfHeight = 0.32f * iconScale;
                SetRect(iconTransform,
                    new Vector2(0.5f - iconHalfWidth, 0.60f - iconHalfHeight),
                    new Vector2(0.5f + iconHalfWidth, 0.60f + iconHalfHeight),
                    Vector2.zero, Vector2.zero, Center);
                Image icon = EnsureSingleComponent<Image>(iconTransform.gameObject, report);
                if (icon != null)
                {
                    Undo.RecordObject(icon, "Configure stat icon slot");
                    icon.raycastTarget = false;
                    icon.preserveAspect = true;
                    icon.enabled = false;
                }

                RectTransform fallbackTransform = EnsureUiChild(slot, "IconFallback", report);
                SetRect(fallbackTransform, iconTransform.anchorMin, iconTransform.anchorMax,
                    Vector2.zero, Vector2.zero, Center);
                TextMeshProUGUI fallback = EnsureSingleComponent<TextMeshProUGUI>(
                    fallbackTransform.gameObject, report);
                ConfigureReadableText(fallback, font, 30f, 24f, 34f, true, false, 0f);

                RectTransform labelTransform = FindDirectChild(slot, "Name", report);
                RectTransform legacyLabel = FindDirectChild(itemTransform, "Label", report);
                if (labelTransform == null && legacyLabel != null)
                {
                    Undo.SetTransformParent(legacyLabel, slot, "Move stat name into semantic slot");
                    Undo.RecordObject(legacyLabel.gameObject, "Rename stat label");
                    legacyLabel.gameObject.name = "Name";
                    labelTransform = legacyLabel;
                }
                labelTransform ??= EnsureUiChild(slot, "Name", report);
                // Hidden in the normal gameplay composition — showing icon + value alone reads as
                // "P 72" rather than "People / P 72". The object, component, and StatItemView/
                // HUDView wiring all stay intact (SetLabel still runs; it just writes to inactive
                // text), so nothing about the label's functionality is removed, only its default
                // visibility.
                SetRect(labelTransform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 1f),
                    Vector2.zero, Vector2.zero, Center);
                TextMeshProUGUI label = EnsureSingleComponent<TextMeshProUGUI>(
                    labelTransform.gameObject, report);
                ConfigureReadableText(label, font, 16f, 14f, 18f, true, false, 0f);
                SetActiveIfNeeded(labelTransform.gameObject, false);

                RectTransform valueTransform = FindDirectChild(slot, "Value", report);
                RectTransform legacyValue = FindDirectChild(itemTransform, "Value", report);
                if (valueTransform == null && legacyValue != null)
                {
                    Undo.SetTransformParent(legacyValue, slot, "Move stat value into semantic slot");
                    valueTransform = legacyValue;
                }
                valueTransform ??= EnsureUiChild(slot, "Value", report);
                // Right of the icon column, vertically centered to match it.
                SetRect(valueTransform, new Vector2(0.52f, 0.25f), new Vector2(0.98f, 0.75f),
                    Vector2.zero, Vector2.zero, Center);
                TextMeshProUGUI value = EnsureSingleComponent<TextMeshProUGUI>(
                    valueTransform.gameObject, report);
                ConfigureReadableText(value, font, 44f, 40f, 48f, true, false, 0f);
                // Hidden at the user's request. Wiring stays intact (SetValue still runs; it just
                // writes to an inactive object), matching the Name label's treatment above.
                SetActiveIfNeeded(valueTransform.gameObject, false);

                // A badge near the icon's corner, matching the reference's transient +/-/++ glyph
                // that flashes while dragging toward a choice affecting this stat.
                RectTransform impactTransform = EnsureUiChild(slot, "Impact", report);
                SetRect(impactTransform, new Vector2(0.60f, 0.62f), new Vector2(0.98f, 0.92f),
                    Vector2.zero, Vector2.zero, Center);
                TextMeshProUGUI impact = EnsureSingleComponent<TextMeshProUGUI>(
                    impactTransform.gameObject, report);
                CanvasGroup impactGroup = EnsureSingleComponent<CanvasGroup>(
                    impactTransform.gameObject, report);
                ConfigureReadableText(impact, font, 27f, 24f, 30f, true, false, 0f);
                impact.text = string.Empty;
                impactGroup.alpha = 0f;

                RectTransform criticalTransform = EnsureUiChild(slot, "Critical", report);
                SetRect(criticalTransform, new Vector2(0.02f, 0.62f), new Vector2(0.30f, 0.92f),
                    Vector2.zero, Vector2.zero, Center);
                TextMeshProUGUI critical = EnsureSingleComponent<TextMeshProUGUI>(
                    criticalTransform.gameObject, report);
                ConfigureReadableText(critical, font, 27f, 24f, 30f, true, false, 0f);
                critical.text = "!";
                critical.gameObject.SetActive(false);

                ConfigureStatBackground(background, uiSprite);
                ConfigureStatFill(fill, uiSprite, StatFillColours[i]);

                if (item != null)
                {
                    SetEnumProperty(item, "stat", (int)stats[i], report);
                    SetObjectProperty(item, "fillImage", fill, report);
                    SetObjectProperty(item, "iconImage", icon, report);
                    SetObjectProperty(item, "label", label, report);
                    SetObjectProperty(item, "valueText", value, report);
                    SetObjectProperty(item, "iconFallbackLabel", fallback, report);
                    SetObjectProperty(item, "impactLabel", impact, report);
                    SetObjectProperty(item, "impactGroup", impactGroup, report);
                    SetObjectProperty(item, "criticalLabel", critical, report);
                }

                label.text = interfaceText != null ? interfaceText.GetStatLabel(stats[i]) : stats[i].ToString();
                value.text = StatBounds.Initial.ToString();

                items[i] = item;
                SetSiblingIndex(slot, i);
                SetSiblingIndex(iconTransform, 0);
                SetSiblingIndex(fallbackTransform, 1);
                SetSiblingIndex(labelTransform, 2);
                SetSiblingIndex(valueTransform, 3);
                SetSiblingIndex(impactTransform, 4);
                SetSiblingIndex(criticalTransform, 5);
                SetSiblingIndex(itemTransform, 6);
            }

            if (hud != null)
            {
                SetObjectArrayProperty(hud, "statItems", items, report);
                SetObjectProperty(hud, "interfaceText", interfaceText, report);
            }

            return hud;
        }

        private static FooterParts ConfigureFooter(
            RectTransform safeArea,
            InterfaceTextDefinition interfaceText,
            TMP_FontAsset font,
            SceneSetupReport report)
        {
            RectTransform root = FindDirectChild(safeArea, "Footer", report);
            RectTransform legacyRoot = FindDirectChild(safeArea, "RunStatus", report);
            if (root == null && legacyRoot != null)
            {
                Undo.RecordObject(legacyRoot.gameObject, "Rename run status as footer");
                legacyRoot.gameObject.name = "Footer";
                root = legacyRoot;
            }
            root ??= EnsureUiChild(safeArea, "Footer", report);
            SetRect(root, new Vector2(0f, 0f), new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, 96f), new Vector2(0.5f, 0f));

            HorizontalLayoutGroup layout = EnsureSingleComponent<HorizontalLayoutGroup>(
                root.gameObject, report);
            if (layout != null)
            {
                Undo.RecordObject(layout, "Configure footer layout");
                layout.padding = new RectOffset(16, 16, 8, 8);
                layout.spacing = 12f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
            }

            RectTransform reignTransform = FindDirectChild(root, "Reign", report);
            RectTransform legacyTurn = FindDirectChild(root, "Turn", report);
            if (reignTransform == null && legacyTurn != null)
            {
                Undo.RecordObject(legacyTurn.gameObject, "Rename footer reign label");
                legacyTurn.gameObject.name = "Reign";
                reignTransform = legacyTurn;
            }
            reignTransform ??= EnsureUiChild(root, "Reign", report);
            RectTransform rulerTransform = EnsureUiChild(root, "Ruler", report);
            RectTransform progressTransform = EnsureUiChild(root, "Progress", report);
            RectTransform sealTransform = EnsureUiChild(root, "Seal", report);

            TextMeshProUGUI reign = EnsureSingleComponent<TextMeshProUGUI>(
                reignTransform.gameObject, report);
            TextMeshProUGUI ruler = EnsureSingleComponent<TextMeshProUGUI>(
                rulerTransform.gameObject, report);
            TextMeshProUGUI progress = EnsureSingleComponent<TextMeshProUGUI>(
                progressTransform.gameObject, report);
            Image seal = EnsureSingleComponent<Image>(sealTransform.gameObject, report);
            ConfigureReadableText(reign, font, 30f, 26f, 34f, true, false, 0f);
            ConfigureReadableText(ruler, font, 26f, 22f, 30f, true, false, 0f);
            ConfigureReadableText(progress, font, 26f, 22f, 30f, true, false, 0f);
            reign.text = string.Format("{0} 1", interfaceText != null ? interfaceText.Turn : "Tur");
            ruler.text = "Royal Decisions";
            progress.text = string.Empty;
            progress.gameObject.SetActive(false);
            if (seal != null)
            {
                Undo.RecordObject(seal, "Configure footer seal slot");
                seal.raycastTarget = false;
                seal.preserveAspect = true;
                seal.enabled = false;
            }

            LayoutElement sealLayout = EnsureSingleComponent<LayoutElement>(sealTransform.gameObject, report);
            if (sealLayout != null)
            {
                Undo.RecordObject(sealLayout, "Configure footer seal layout");
                sealLayout.minWidth = 56f;
                sealLayout.preferredWidth = 56f;
                sealLayout.flexibleWidth = 0f;
            }

            RunStatusView runStatus = EnsureSingleComponent<RunStatusView>(root.gameObject, report);
            SetObjectProperty(runStatus, "interfaceText", interfaceText, report);
            SetObjectProperty(runStatus, "turnText", reign, report);

            FooterView footer = EnsureSingleComponent<FooterView>(root.gameObject, report);
            SetObjectProperty(footer, "interfaceText", interfaceText, report);
            SetObjectProperty(footer, "reignText", reign, report);
            SetObjectProperty(footer, "rulerText", ruler, report);
            SetObjectProperty(footer, "progressText", progress, report);
            SetObjectProperty(footer, "sealImage", seal, report);

            SetSiblingIndex(reignTransform, 0);
            SetSiblingIndex(rulerTransform, 1);
            SetSiblingIndex(progressTransform, 2);
            SetSiblingIndex(sealTransform, 3);

            // Hidden in the Reigns-inspired gameplay composition — purely decorative/informational
            // (turn count, static ruler-name flavour text) and not part of the target vertical
            // hierarchy. FooterView/RunStatusView and their wiring are untouched: RenderTurn/
            // ShowTurn still run every turn and still write real text, it just isn't shown.
            SetActiveIfNeeded(root.gameObject, false);

            return new FooterParts(root, runStatus, footer);
        }

        private static Sprite LoadBuiltInUiSprite(SceneSetupReport report)
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(BuiltInUiSpritePath);
            if (sprite == null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "BUILTIN_UI_SPRITE_MISSING", "Assets",
                    BuiltInUiSpritePath, string.Empty,
                    "Unity's built-in UISprite could not be loaded for serializable UI Images.");
            }
            return sprite;
        }

        private static void ConfigureStatBackground(Image background, Sprite sprite)
        {
            if (background == null || (background.sprite == sprite
                && background.type == Image.Type.Simple
                && !background.raycastTarget
                && ColoursMatch(background.color, StatBackgroundColour)))
            {
                return;
            }

            Undo.RecordObject(background, "Configure stat background");
            background.sprite = sprite;
            background.type = Image.Type.Simple;
            background.raycastTarget = false;
            background.color = StatBackgroundColour;
        }

        private static void ConfigureSimpleImage(
            Image image,
            Sprite sprite,
            Color colour,
            bool raycast,
            bool enabled = true)
        {
            if (image == null)
            {
                return;
            }

            Undo.RecordObject(image, "Configure UI Image");
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = raycast;
            image.color = colour;
            image.enabled = enabled;
        }

        /// <summary>
        /// For art with unique, non-repeating detail at every edge (ornate frame corners, torn
        /// parchment edges) where 9-slicing would stretch and deform that detail. Fits by
        /// preserved aspect instead of border-based slicing.
        /// </summary>
        private static void ConfigureOptionalSimpleImage(Image image, Sprite sprite, Color colour)
        {
            if (image == null)
            {
                return;
            }

            Undo.RecordObject(image, "Configure optional simple Image");
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = sprite != null;
            image.raycastTarget = false;
            image.color = colour;
            image.enabled = sprite != null;
        }

        private static void ConfigureLayoutElement(
            GameObject target,
            float preferredHeight,
            SceneSetupReport report)
        {
            LayoutElement element = EnsureSingleComponent<LayoutElement>(target, report);
            if (element == null)
            {
                return;
            }

            Undo.RecordObject(element, "Configure layout element");
            element.preferredHeight = preferredHeight;
            element.flexibleHeight = 0f;
        }

        private static void ConfigureMinimumTouchTarget(
            Button button,
            SceneSetupReport report)
        {
            if (button == null)
            {
                return;
            }
            RectTransform rect = button.transform as RectTransform;
            if (rect != null && (rect.sizeDelta.x < 96f || rect.sizeDelta.y < 96f))
            {
                Undo.RecordObject(rect, "Configure minimum touch target");
                rect.sizeDelta = new Vector2(
                    Mathf.Max(96f, rect.sizeDelta.x),
                    Mathf.Max(96f, rect.sizeDelta.y));
            }
            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout != null)
            {
                Undo.RecordObject(layout, "Configure minimum touch target layout");
                layout.minWidth = Mathf.Max(96f, layout.minWidth);
                layout.minHeight = Mathf.Max(96f, layout.minHeight);
            }
        }

        private static void ConfigureStatFill(Image fill, Sprite sprite, Color colour)
        {
            if (fill == null || (fill.sprite == sprite
                && fill.type == Image.Type.Filled
                && fill.fillMethod == Image.FillMethod.Horizontal
                && fill.fillOrigin == (int)Image.OriginHorizontal.Left
                && !fill.preserveAspect
                && !fill.raycastTarget
                && ColoursMatch(fill.color, colour)))
            {
                return;
            }

            Undo.RecordObject(fill, "Configure stat fill");
            fill.sprite = sprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.preserveAspect = false;
            fill.raycastTarget = false;
            fill.color = colour;
        }

        /// <summary>
        /// The fixed flat aged-beige surface filling everything below TopBar+HUD — full SafeArea
        /// width, no side margins, no illustrated background behind it. Never moves. A sibling of
        /// SituationArea and CardArea, drawn behind both, so SituationText and the card's fixed
        /// name band read as sitting directly on this one continuous paper surface.
        /// </summary>
        private static RectTransform ConfigureContentPanel(
            RectTransform safeArea, SceneSetupReport report)
        {
            // Retired: an earlier pass of this presentation used a narrower "DecisionColumn"
            // (72-78% width, Background2 visible on the sides) in this same role. Superseded by
            // this full-width ContentPanel now that portrait mobile has no background art at all.
            RemoveObsoleteChild(safeArea, "DecisionColumn");

            RectTransform panel = EnsureUiChild(safeArea, "ContentPanel", report);
            // Top inset matches SituationArea's own top offset below TopBar+HUD; bottom inset is
            // zero — the panel reaches the very bottom of SafeArea, so no background art (or the
            // flat navy Background fallback behind it) is ever exposed on portrait mobile.
            SetRect(panel, Vector2.zero, Vector2.one,
                new Vector2(0f, -GameContentTopInset / 2f), new Vector2(0f, -GameContentTopInset),
                Center);
            // Flat (0 radius) restrained aged-beige surface — no ornate frame, no floating box;
            // SituationPanel above shares this exact colour so the two read as one surface.
            ConfigureRoundedFill(panel.gameObject, SituationPanelColour, 0f, false, report);
            return panel;
        }

        /// <summary>
        /// The situation/question panel sitting above the swipe card, outside <c>Card</c> so it
        /// never moves with the drag. A sibling of <c>CardArea</c>, flush below <c>HUD</c>.
        /// </summary>
        private static SituationAreaParts ConfigureSituationArea(
            RectTransform safeArea,
            TMP_FontAsset font,
            SceneSetupReport report)
        {
            // Full SafeArea width, no side margins — same horizontal span as ContentPanel (see
            // ConfigureContentPanel) so its edges sit flush with it instead of floating as a
            // separately-sized box. A short fixed height (room for 2-4 lines) directly below
            // TopBar+HUD (see ConfigureCard's top margin, which is derived from this height).
            RectTransform root = EnsureUiChild(safeArea, "SituationArea", report);
            SetRect(root, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -GameContentTopInset), new Vector2(0f, 160f), new Vector2(0.5f, 1f));
            // Final sibling position is pinned by ApplyGameScene's canonical SafeArea ordering
            // block once every sibling exists, so this new object's position here is provisional.

            // Hidden at the user's explicit request — the situation/body text now renders directly
            // on the card (see ConfigureCard's BodyScrim/Body) instead of this parchment panel
            // above it. Kept in the hierarchy rather than destroyed: cheap to bring back, and its
            // theme wiring (SituationPanelImage/Fallback) still runs harmlessly while inactive.
            SetActiveIfNeeded(root.gameObject, false);

            RectTransform panelTransform = EnsureUiChild(root, "SituationPanel", report);
            Stretch(panelTransform);
            // Flat (0 radius), same colour as ContentPanel immediately behind it: the two
            // surfaces read as one continuous paper strip instead of a separate floating rounded
            // box sitting on top of it.
            ProceduralRoundedRectGraphic panelFallback = ConfigureRoundedFill(
                panelTransform.gameObject, SituationPanelColour, 0f, false, report);

            // Sprite-driven surface, sibling of the procedural fallback rather than the same
            // object (two Graphics cannot safely share one CanvasRenderer). Starts disabled —
            // GameUIThemeController.ApplyTheme enables exactly one of the two based on whether
            // GameUITheme.SituationPanelSprite is assigned (it is not: the flat paper-column look
            // is the target presentation, not Parşömen.png).
            RectTransform artworkTransform = EnsureUiChild(panelTransform, "Artwork", report);
            Stretch(artworkTransform);
            Image artwork = EnsureSingleComponent<Image>(artworkTransform.gameObject, report);
            ConfigureOptionalSimpleImage(artwork, null, Color.white);
            SetSiblingIndex(artworkTransform, 0);

            // Generous fixed pixel margins (not percentage) so text never reads edge-to-edge on
            // the now full-width panel: ~90px left/right at the 1080-reference width, ~26px
            // top/bottom. See SituationTextLayoutPlayModeTests, which reproduces this exact box
            // against real 1-, 3- and 4-line authored story text.
            RectTransform textTransform = EnsureUiChild(panelTransform, "SituationText", report);
            SetRect(textTransform, Vector2.zero, Vector2.one,
                Vector2.zero, new Vector2(-180f, -52f), Center);
            TextMeshProUGUI text = EnsureSingleComponent<TextMeshProUGUI>(
                textTransform.gameObject, report);
            // Sized up ~10-12% (32/20-36 -> 36/22-40) now that the full-width panel and wider
            // margins give more room than the old narrower column did.
            ConfigureReadableText(text, font, 36f, 22f, 40f, true, true, 2f);
            SetTextColour(text, SituationTextColour);
            SetSiblingIndex(textTransform, 1);

            return new SituationAreaParts(root, text, artwork, panelFallback);
        }

        // Fraction of Card's height reserved for the fixed CharacterName band at the bottom;
        // the portrait area (CardBack + PortraitSwipeRoot) occupies the remainder above it.
        private const float NameBandHeightFraction = 0.13f;

        // Restrained rounding for the portrait card (PortraitMask) and CardBack, which share
        // identical bounds and so must share this exact radius too — otherwise CardBack's sharp
        // corners would peek out from behind the portrait's rounded ones at rest.
        private const float PortraitCornerRadius = 11f;

        private static CardParts ConfigureCard(
            RectTransform safeArea,
            RectTransform contentPanel,
            TMP_FontAsset font,
            SceneSetupReport report)
        {
            RectTransform area = EnsureUiChild(safeArea, "CardArea", report);
            // Top margin clears TopBar+HUD (328) + SituationArea (160) + a small 12-unit gap =
            // 500; bottom margin (80) is tight since Footer is hidden and TapChoiceButtons are
            // now visually subtle — the card is the dominant object on screen (see the polish
            // pass). anchoredPosition/sizeDelta below encode that 500/80 split directly (half of
            // the 580 total minus/plus the offset) — see ConfigureContentPanel for the same trick.
            SetRect(area, Vector2.zero, Vector2.one, new Vector2(0f, -210f),
                new Vector2(-40f, -580f), Center);

            // Retired: the dimmed "next card in the deck" peek. Superseded by CardBack, which
            // reveals the fixed card-back art directly behind the swiped portrait instead.
            RemoveObsoleteChild(area, "NextCard");

            RectTransform card = EnsureUiChild(area, "Card", report);
            // Near-square (portrait area) plus a name band beneath it, rather than the old tall
            // 2:3 frame shape — see widthToHeightRatio below, which drives the real size.
            SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(880f, 978f), new Vector2(0.5f, 0.5f));
            Image cardImage = EnsureSingleComponent<Image>(card.gameObject, report);
            // Card's own full-bounds Image is the drag/tap raycast surface for the whole decision
            // area; it sits under CardBack and PortraitSwipeRoot, which cover it visually.
            ConfigureSimpleImage(cardImage, LoadBuiltInUiSprite(report), CardSurfaceColour, true);

            // Retired: the picture-frame overlay, its temporary-border fallback, and the corner
            // decorations that stood in for it. The final presentation uses CardBack (Card.png)
            // plus a near-square masked portrait instead of a framed card shell.
            RemoveObsoleteChild(card, "Frame");
            RemoveObsoleteChild(card, "TemporaryBorder");
            RemoveObsoleteChild(card, "CornerTopLeft");
            RemoveObsoleteChild(card, "CornerTopRight");
            RemoveObsoleteChild(card, "CornerBottomLeft");
            RemoveObsoleteChild(card, "CornerBottomRight");
            // Retired along with the old in-card Body text (see RemoveLegacyCardBody): the dark
            // scrim that used to sit behind it for legibility over painted art.
            RemoveObsoleteChild(card, "BodyScrim");
            // Retired along with Frame: the sharp gold fallback outline drawn at the card's exact
            // bounds when no frame art was assigned.
            RemoveStaleComponents<Outline>(card.gameObject);
            // Retired: the old always-fixed portrait window with its own gold frame ring. Its
            // reusable children (PortraitMask/Portrait) move under PortraitSwipeRoot below instead
            // of being rebuilt from scratch.
            RectTransform legacyPortraitRegion = FindDirectChild(card, "PortraitRegion", report);

            Vector2 portraitAreaAnchorMin = new Vector2(0f, NameBandHeightFraction);
            Vector2 portraitAreaAnchorMax = Vector2.one;

            // CardBack: fixed, sits directly behind the portrait at EXACTLY the same visual
            // bounds as PortraitSwipeRoot (zero inset either side) — never moves during a swipe;
            // only PortraitSwipeRoot does. At rest PortraitSwipeRoot fully covers it, so Card.png
            // reads as a fixed backdrop revealed by the drag, never as an outer frame.
            RectTransform cardBackTransform = EnsureUiChild(card, "CardBack", report);
            SetRect(cardBackTransform, portraitAreaAnchorMin, portraitAreaAnchorMax,
                Vector2.zero, Vector2.zero, Center);
            // A procedural rounded-rect mask (exact radius, shared with PortraitMask below) rather
            // than a 9-sliced sprite border — the two must line up exactly, not approximately, or
            // CardBack's corners would peek out from behind the portrait's at rest.
            ConfigureRoundedFill(cardBackTransform.gameObject, Color.white, PortraitCornerRadius, false, report);
            Mask cardBackMask = EnsureSingleComponent<Mask>(cardBackTransform.gameObject, report);
            if (cardBackMask != null)
            {
                Undo.RecordObject(cardBackMask, "Configure card back mask");
                cardBackMask.showMaskGraphic = false;
            }
            RectTransform cardBackArtTransform = EnsureUiChild(cardBackTransform, "CardBackArt", report);
            Stretch(cardBackArtTransform);
            Image cardBack = EnsureSingleComponent<Image>(cardBackArtTransform.gameObject, report);
            ConfigureSimpleImage(cardBack, LoadBuiltInUiSprite(report), CardSurfaceColour, false);

            // PortraitSwipeRoot: the only part of the decision card that moves/rotates during a
            // swipe, at EXACTLY CardBack's bounds (see above).
            RectTransform portraitSwipeRoot = EnsureUiChild(card, "PortraitSwipeRoot", report);
            SetRect(portraitSwipeRoot, portraitAreaAnchorMin, portraitAreaAnchorMax,
                Vector2.zero, Vector2.zero, Center);
            // Clips the portrait and both choice-preview panels to PortraitSwipeRoot's own
            // bounds, so neither can render past the moving card's edge during a drag.
            EnsureSingleComponent<RectMask2D>(portraitSwipeRoot.gameObject, report);

            RectTransform portraitMask = EnsureUiChild(portraitSwipeRoot, "PortraitMask", report);
            Stretch(portraitMask);
            // Same procedural rounded-rect radius as CardBack above — see PortraitCornerRadius.
            ProceduralRoundedRectGraphic maskGraphic = ConfigureRoundedFill(
                portraitMask.gameObject, Color.white, PortraitCornerRadius, false, report);
            Mask mask = EnsureSingleComponent<Mask>(portraitMask.gameObject, report);
            if (mask != null)
            {
                Undo.RecordObject(mask, "Configure portrait mask");
                mask.showMaskGraphic = false;
            }

            RectTransform portraitTransform = FindDirectChild(portraitMask, "Portrait", report);
            if (portraitTransform == null && legacyPortraitRegion != null)
            {
                RectTransform legacyMask = FindDirectChild(legacyPortraitRegion, "PortraitMask", report);
                RectTransform legacyPortrait = legacyMask != null
                    ? FindDirectChild(legacyMask, "Portrait", report)
                    : null;
                if (legacyPortrait != null)
                {
                    Undo.SetTransformParent(
                        legacyPortrait, portraitMask, "Move portrait into card mask");
                    portraitTransform = legacyPortrait;
                }
            }
            portraitTransform ??= EnsureUiChild(portraitMask, "Portrait", report);
            Stretch(portraitTransform);
            Image portrait = EnsureSingleComponent<Image>(portraitTransform.gameObject, report);
            if (portrait != null)
            {
                Undo.RecordObject(portrait, "Configure portrait");
                portrait.raycastTarget = false;
                portrait.preserveAspect = false;
            }

            PortraitFallbackView portraitFallback = ConfigurePortraitFallback(
                portraitMask, portraitTransform, report);

            // The old fixed portrait window is now fully superseded by PortraitSwipeRoot; remove
            // it once its reusable children have been migrated above.
            if (legacyPortraitRegion != null)
            {
                Undo.DestroyObjectImmediate(legacyPortraitRegion.gameObject);
            }

            ChoicePreviewView left = ConfigurePreview(
                portraitSwipeRoot, "PreviewLeft", ChoiceSide.Left, font, report);
            ChoicePreviewView right = ConfigurePreview(
                portraitSwipeRoot, "PreviewRight", ChoiceSide.Right, font, report);

            // The situation text ("Body") moved to SituationArea above the card; remove a
            // leftover in-card Body object from a scene built by the previous layout.
            RemoveLegacyCardBody(card, report);

            // Opaque paper-toned backing behind the fixed name band, matching ContentPanel and
            // SituationPanel so Speaker reads as sitting inside the same paper column, not
            // floating over background art. Flat (0 radius) procedural fill, not a sprite Image —
            // the built-in UISprite stretched across this short, wide rect used to read as a
            // rounded "pill" instead of a plain paper backing.
            RectTransform nameScrimTransform = EnsureUiChild(card, "NameScrim", report);
            SetRect(nameScrimTransform, Vector2.zero, new Vector2(1f, NameBandHeightFraction),
                Vector2.zero, Vector2.zero, Center);
            ProceduralRoundedRectGraphic nameScrim = ConfigureRoundedFill(
                nameScrimTransform.gameObject, NameScrimColour, 0f, false, report);

            RectTransform speakerTransform = EnsureUiChild(card, "Speaker", report);
            SetRect(speakerTransform, new Vector2(0.06f, 0.01f), new Vector2(0.94f, 0.115f),
                Vector2.zero, Vector2.zero, Center);
            TextMeshProUGUI speaker = EnsureSingleComponent<TextMeshProUGUI>(
                speakerTransform.gameObject, report);
            // Sized up ~10% (38/32-42 -> 42/35-46), same visual family as SituationText.
            ConfigureReadableText(speaker, font, 42f, 35f, 46f, true, false, 3f);
            SetTextColour(speaker, SpeakerTextColour);

            CardView view = EnsureSingleComponent<CardView>(card.gameObject, report);
            CardSwipeController swipe = EnsureSingleComponent<CardSwipeController>(
                card.gameObject, report);

            if (view != null)
            {
                // The dragged/rotated root is PortraitSwipeRoot, not Card itself — HUD,
                // SituationText, CardBack, and CharacterName all stay fixed.
                SetObjectProperty(view, "cardRoot", portraitSwipeRoot, report);
                SetObjectProperty(view, "speakerText", speaker, report);
                // bodyText (the situation text) is wired by the caller to SituationArea's text.
                SetObjectProperty(view, "portraitImage", portrait, report);
                SetObjectProperty(view, "leftPreview", left, report);
                SetObjectProperty(view, "rightPreview", right, report);
                SetObjectProperty(view, "visualRoot", card.gameObject, report);
                SetObjectProperty(view, "surfaceImage", cardImage, report);
                SetObjectProperty(view, "portraitMaskImage", maskGraphic, report);
                SetObjectProperty(view, "nameScrimImage", nameScrim, report);
                SetObjectProperty(view, "cardBackImage", cardBack, report);
                SetObjectProperty(view, "portraitFallbackView", portraitFallback, report);
            }

            if (swipe != null)
            {
                SetObjectProperty(swipe, "cardView", view, report);
                SetObjectProperty(swipe, "dragParent", area, report);
                // Restrained Reigns-style tilt (was 12°) — presentation only; does not change the
                // confirm threshold or resolution logic in SwipeMath.
                SetFloatProperty(swipe, "maxRotationDegrees", 7f, report);
                // Nonlinear drag tilt, a small vertical arc, and a subtle scale response — motion
                // feel only, applied to the same displacement SwipeMath.Rotation already reads.
                SetFloatProperty(swipe, "rotationEaseExponent", 0.85f, report);
                SetFloatProperty(swipe, "maxDragLift", 18f, report);
                SetFloatProperty(swipe, "maxDragScale", 1.02f, report);
                // Spring-like snap-back (fast return, slight overshoot, settle) within the
                // requested ~0.20-0.35s feel, replacing the old 0.18s linear EaseInOut return.
                SetFloatProperty(swipe, "snapBackDuration", 0.28f, report);
                SetAnimationCurveProperty(
                    swipe, "snapBackEase", CardSwipeController.BuildSnapBackSpringCurve(), report);
                // Committed exit reads as a thrown card: continues rotating past the drag-time
                // max, arcs up slightly, and scales up a touch — visual only, does not delay or
                // otherwise change when/how the decision itself resolves.
                SetFloatProperty(swipe, "exitRotationDegrees", 12f, report);
                SetFloatProperty(swipe, "exitArcHeight", 36f, report);
                SetFloatProperty(swipe, "exitScale", 1.04f, report);
            }

            ResponsiveCardSizer sizer = EnsureSingleComponent<ResponsiveCardSizer>(
                area.gameObject, report);
            SetObjectProperty(sizer, "card", card, report);
            SetObjectProperty(sizer, "nextCard", null, report);
            // ContentPanel is full SafeArea width now, so this is equivalently ~82-86% of either.
            SetObjectProperty(sizer, "widthReference", contentPanel, report);
            SetFloatProperty(sizer, "preferredWidthRatio", 0.84f, report);
            SetFloatProperty(sizer, "maximumWidth", 960f, report);
            // Near-square portrait area (NameBandHeightFraction reserved beneath it for the fixed
            // name band) — 1:1 once the name band is subtracted (0.87 / (1 - 0.13) = 1.0), not
            // KartÇerçevesi's old tall 1024/1536 frame aspect.
            SetFloatProperty(sizer, "widthToHeightRatio", 0.87f, report);
            // Top-aligned close under SituationArea (12px here + CardArea's own 12px top margin
            // component = ~24px total gap) instead of centred in CardArea's remaining height —
            // closes the empty beige space that centring left above the portrait.
            SetFloatProperty(sizer, "topPadding", 12f, report);
            sizer?.RecalculateLayout();

            SetSiblingIndex(card, 0);
            SetSiblingIndex(cardBackTransform, 0);
            SetSiblingIndex(portraitSwipeRoot, 1);
            SetSiblingIndex(nameScrimTransform, 2);
            SetSiblingIndex(speakerTransform, 3);

            return new CardParts(area, view, swipe);
        }

        /// <summary>Destroys <paramref name="name"/> under <paramref name="parent"/> if present —
        /// used to retire structural children from an earlier layout. Idempotent: a no-op once the
        /// child is gone.</summary>
        private static void RemoveObsoleteChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return;
            }

            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }
        }

        /// <summary>
        /// Removes an in-card "Body" text object left over from the layout that rendered the
        /// situation text inside the card. Only removes it when it is unambiguously that legacy
        /// object (a plain text leaf with no children of its own), leaving anything unexpected
        /// in place rather than guessing.
        /// </summary>
        private static void RemoveLegacyCardBody(RectTransform card, SceneSetupReport report)
        {
            RectTransform legacy = FindDirectChild(card, "Body", report);
            if (legacy == null)
            {
                return;
            }

            bool isPlainTextLeaf = legacy.childCount == 0
                && legacy.GetComponent<TextMeshProUGUI>() != null;
            if (!isPlainTextLeaf)
            {
                AddInvalid(report, card.gameObject.scene.path, HierarchyPath(legacy),
                    "Obsolete Card/Body child is not a plain text leaf and was preserved.");
                return;
            }

            Undo.DestroyObjectImmediate(legacy.gameObject);
        }

        /// <summary>
        /// Two floating tap targets — an alternate to the drag gesture, toggled by the Controls
        /// settings tab. Anchored to SafeArea's own bottom corners (not CardArea's layout, and not
        /// Footer's HorizontalLayoutGroup) so they cannot perturb either one's carefully tuned
        /// sizing, and sit in the gap CardArea already reserves above Footer.
        /// </summary>
        private static TapChoiceButtonsParts ConfigureTapChoiceButtons(
            RectTransform safeArea,
            TMP_FontAsset font,
            CardSwipeController swipe,
            SceneSetupReport report)
        {
            RectTransform root = EnsureUiChild(safeArea, "TapChoiceButtons", report);
            Stretch(root);

            // Invisible by default (normal swipe-first gameplay) but always fully tappable —
            // GameSceneController.ApplySettings calls TapChoiceButtonsView.SetProminent(true) only
            // when Settings.DisableSwipe makes tapping the sole way to play; SetVisible (existence/
            // hit-target) is still driven by TapButtonsEnabled/DisableSwipe exactly as before, and
            // is untouched by prominence. This authored value is the pre-ApplySettings default.
            CanvasGroup group = EnsureSingleComponent<CanvasGroup>(root.gameObject, report);
            if (group != null)
            {
                Undo.RecordObject(group, "Configure tap choice buttons prominence");
                group.alpha = 0f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            const float buttonSize = 112f;
            const float bottomOffset = 216f;
            const float sideInset = 32f;

            Button left = ConfigureTapChoiceButton(
                root, "LeftChoiceButton", pointsRight: false, Vector2.zero,
                new Vector2(sideInset, bottomOffset), buttonSize, report);
            Button right = ConfigureTapChoiceButton(
                root, "RightChoiceButton", pointsRight: true, Vector2.right,
                new Vector2(-sideInset, bottomOffset), buttonSize, report);

            TapChoiceButtonsView view =
                EnsureSingleComponent<TapChoiceButtonsView>(root.gameObject, report);
            SetObjectProperty(view, "swipeController", swipe, report);
            SetObjectProperty(view, "leftButton", left, report);
            SetObjectProperty(view, "rightButton", right, report);
            SetObjectProperty(view, "canvasGroup", group, report);
            return new TapChoiceButtonsParts(root, view);
        }

        private static Button ConfigureTapChoiceButton(
            RectTransform parent,
            string name,
            bool pointsRight,
            Vector2 anchor,
            Vector2 anchoredPosition,
            float size,
            SceneSetupReport report)
        {
            RectTransform transform = EnsureUiChild(parent, name, report);
            SetRect(transform, anchor, anchor, anchoredPosition, new Vector2(size, size), anchor);

            // Game scene only: uses the Game palette's own neutral chip tone directly (not
            // SettingsPanelTheme), so the Settings-only zombie re-theme cannot change this button.
            ProceduralRoundedRectGraphic graphic = ConfigureRoundedButtonGraphic(
                transform.gameObject, StatBackgroundColour, size * 0.5f, report);
            Button button = EnsureSingleComponent<Button>(transform.gameObject, report);
            if (button != null && graphic != null)
            {
                Undo.RecordObject(button, "Wire tap choice button target graphic");
                button.targetGraphic = graphic;
            }

            // A prior authoring pass rendered this as a TMP "◀"/"▶" glyph; drop the stale label —
            // those characters fall outside the project's Turkish SDF atlas and rendered as the
            // missing-glyph fallback box.
            TextMeshProUGUI staleGlyph = transform.GetComponentInChildren<TextMeshProUGUI>(true);
            if (staleGlyph != null)
            {
                Undo.DestroyObjectImmediate(staleGlyph.gameObject);
            }

            RectTransform icon = EnsureUiChild(transform, "Icon", report);
            float iconSize = size * 0.4f;
            SetRect(icon, Center, Center, Vector2.zero, new Vector2(iconSize, iconSize), Center);
            EnsureSingleComponent<CanvasRenderer>(icon.gameObject, report);
            ProceduralTriangleIconGraphic triangle =
                EnsureSingleComponent<ProceduralTriangleIconGraphic>(icon.gameObject, report);
            if (triangle != null)
            {
                Undo.RecordObject(triangle, "Configure tap choice arrow");
                triangle.color = Color.white;
                triangle.raycastTarget = false;
                triangle.PointsRight = pointsRight;
            }

            ConfigureMinimumTouchTarget(button, report);
            return button;
        }

        private static PortraitFallbackView ConfigurePortraitFallback(
            RectTransform portraitMask,
            RectTransform portrait,
            SceneSetupReport report)
        {
            RectTransform root = EnsureUiChild(portraitMask, "FallbackSilhouette", report);
            Stretch(root);
            PortraitFallbackView view = EnsureSingleComponent<PortraitFallbackView>(
                root.gameObject, report);
            Sprite sprite = LoadBuiltInUiSprite(report);
            Image backdrop = ConfigureFallbackShape(
                root, "Backdrop", Vector2.zero, Vector2.one, OverallBackgroundColour, sprite, report);
            Image head = ConfigureFallbackShape(
                root, "Head", new Vector2(0.36f, 0.55f), new Vector2(0.64f, 0.80f),
                SecondaryTextColour, sprite, report);
            Image shoulders = ConfigureFallbackShape(
                root, "Shoulders", new Vector2(0.225f, 0.25f), new Vector2(0.775f, 0.57f),
                SecondaryTextColour, sprite, report);
            Image torso = ConfigureFallbackShape(
                root, "Torso", new Vector2(0.34f, 0.175f), new Vector2(0.66f, 0.45f),
                SecondaryTextColour, sprite, report);
            SetObjectProperty(view, "visualRoot", root.gameObject, report);
            SetObjectProperty(view, "backdrop", backdrop, report);
            SetObjectProperty(view, "head", head, report);
            SetObjectProperty(view, "shoulders", shoulders, report);
            SetObjectProperty(view, "torso", torso, report);
            SetSiblingIndex(root, 0);
            SetSiblingIndex(portrait, 1);
            return view;
        }

        private static Image ConfigureFallbackShape(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color colour,
            Sprite sprite,
            SceneSetupReport report)
        {
            RectTransform transform = EnsureUiChild(parent, name, report);
            SetRect(transform, anchorMin, anchorMax, Vector2.zero, Vector2.zero, Center);
            Image image = EnsureSingleComponent<Image>(transform.gameObject, report);
            ConfigureSimpleImage(image, sprite, colour, false);
            return image;
        }

        // Upper band of PortraitSwipeRoot the choice panel occupies (top ~27%, within the
        // requested 24-30% band height).
        private const float ChoicePreviewBandStart = 0.73f;

        private static ChoicePreviewView ConfigurePreview(
            RectTransform portraitSwipeRoot,
            string name,
            ChoiceSide side,
            TMP_FontAsset font,
            SceneSetupReport report)
        {
            RectTransform preview = EnsureUiChild(portraitSwipeRoot, name, report);
            bool left = side == ChoiceSide.Left;
            Stretch(preview);

            Image image = EnsureSingleComponent<Image>(preview.gameObject, report);
            CanvasGroup group = EnsureSingleComponent<CanvasGroup>(preview.gameObject, report);
            ChoicePreviewView view = EnsureSingleComponent<ChoicePreviewView>(
                preview.gameObject, report);
            if (image != null)
            {
                Undo.RecordObject(image, "Configure choice preview");
                image.raycastTarget = false;
                image.enabled = false;
            }

            if (group != null)
            {
                Undo.RecordObject(group, "Reset choice preview visibility");
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }

            // Unity UI only, no banner sprite: a dark translucent panel spanning the upper 35% of
            // PortraitSwipeRoot, full width. Moves and rotates with the portrait since it is a
            // child of PortraitSwipeRoot; ChoicePreviewView.ApplyTheme tints it burgundy (left) or
            // olive (right) via theme.LeftChoice/RightChoice once no banner sprite is supplied.
            RectTransform edgeTransform = EnsureUiChild(preview, "EdgeHighlight", report);
            SetRect(edgeTransform,
                new Vector2(0f, ChoicePreviewBandStart),
                Vector2.one,
                Vector2.zero, Vector2.zero, Center);
            Image edge = EnsureSingleComponent<Image>(edgeTransform.gameObject, report);
            ConfigureSimpleImage(edge, LoadBuiltInUiSprite(report),
                left ? StatFillColours[0] : StatFillColours[3], false);

            RectTransform markerTransform = EnsureUiChild(preview, "CommitMarker", report);
            SetRect(markerTransform,
                new Vector2(left ? 0.02f : 0.94f, 0.80f),
                new Vector2(left ? 0.06f : 0.98f, 0.86f),
                Vector2.zero, Vector2.zero, Center);
            Image markerImage = EnsureSingleComponent<Image>(markerTransform.gameObject, report);
            ConfigureSimpleImage(markerImage, LoadBuiltInUiSprite(report), BodyTextColour, false);
            CanvasGroup marker = EnsureSingleComponent<CanvasGroup>(markerTransform.gameObject, report);
            if (marker != null)
            {
                Undo.RecordObject(marker, "Reset choice commit marker");
                marker.alpha = 0f;
                marker.blocksRaycasts = false;
                marker.interactable = false;
            }

            // The choice label sits centered within the panel band, identical for both sides.
            RectTransform labelTransform = EnsureUiChild(preview, "Label", report);
            SetRect(labelTransform,
                new Vector2(0.08f, ChoicePreviewBandStart + 0.03f),
                new Vector2(0.92f, 0.97f),
                Vector2.zero, Vector2.zero, Center);
            TextMeshProUGUI label = EnsureSingleComponent<TextMeshProUGUI>(
                labelTransform.gameObject, report);
            // Noticeably larger and bolder than before (30/26-34 -> 38/32-44) so it stays readable
            // while the portrait is moving; warm cream, not stark white.
            ConfigureReadableText(label, font, 38f, 32f, 44f, true, true, 4f);
            Undo.RecordObject(label, "Configure choice label weight");
            label.fontStyle = FontStyles.Bold;
            SetTextColour(label, BodyTextColour);
            // Stronger dark shadow (was 0.75 alpha / 1.5px) so the label stays readable over both
            // bright and dark portraits while dragging.
            Shadow labelShadow = EnsureSingleComponent<Shadow>(labelTransform.gameObject, report);
            if (labelShadow != null)
            {
                Undo.RecordObject(labelShadow, "Configure choice label shadow");
                labelShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
                labelShadow.effectDistance = new Vector2(2f, -2f);
                labelShadow.useGraphicAlpha = true;
            }

            if (view != null)
            {
                SetEnumProperty(view, "side", (int)side, report);
                SetObjectProperty(view, "label", label, report);
                SetObjectProperty(view, "canvasGroup", group, report);
                SetObjectProperty(view, "edgeHighlight", edge, report);
                SetObjectProperty(view, "commitMarker", marker, report);
            }

            SetSiblingIndex(edgeTransform, 0);
            SetSiblingIndex(markerTransform, 1);
            SetSiblingIndex(labelTransform, 2);

            return view;
        }

        private static TutorialParts ConfigureTutorial(
            RectTransform safeArea,
            TMP_FontAsset font,
            SceneSetupReport report)
        {
            RectTransform root = EnsureUiChild(safeArea, "TutorialOverlay", report);
            Stretch(root);
            Image surface = EnsureSingleComponent<Image>(root.gameObject, report);
            ConfigureSimpleImage(surface, LoadBuiltInUiSprite(report), OverallBackgroundColour, true);

            RectTransform content = EnsureUiChild(root, "Content", report);
            SetRect(content, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.80f),
                Vector2.zero, Vector2.zero, Center);
            TextMeshProUGUI title = EnsureText(content, "Title", new Vector2(0f, 260f),
                new Vector2(860f, 140f), 54f, report);
            TextMeshProUGUI body = EnsureText(content, "Body", new Vector2(0f, 40f),
                new Vector2(860f, 300f), 38f, report);
            ConfigureReadableText(title, font, 54f, 44f, 58f, true, true, 3f);
            ConfigureReadableText(body, font, 38f, 32f, 42f, true, true, 6f);
            Button next = EnsureMenuButton(content, "NextButton", "İleri", -190f, report);
            Button skip = EnsureMenuButton(content, "SkipButton", "Atla", -330f, report);
            ConfigureMinimumTouchTarget(next, report);
            ConfigureMinimumTouchTarget(skip, report);

            TutorialOverlayView view = EnsureSingleComponent<TutorialOverlayView>(
                root.gameObject, report);
            SetObjectProperty(view, "panelRoot", root.gameObject, report);
            SetObjectProperty(view, "titleText", title, report);
            SetObjectProperty(view, "bodyText", body, report);
            SetObjectProperty(view, "nextButton", next, report);
            SetObjectProperty(view, "skipButton", skip, report);
            TutorialCoordinator coordinator = EnsureSingleComponent<TutorialCoordinator>(
                root.gameObject, report);
            SetObjectProperty(coordinator, "view", view, report);

            if (root.gameObject.activeSelf)
            {
                Undo.RecordObject(root.gameObject, "Deactivate tutorial overlay");
                root.gameObject.SetActive(false);
            }
            return new TutorialParts(root, view, coordinator);
        }

        private static GameOverParts ConfigureGameOver(
            GameObject canvasObject,
            RectTransform safeArea,
            InterfaceTextDefinition interfaceText,
            TMP_FontAsset font,
            SceneSetupReport report)
        {
            RectTransform panel = EnsureUiChild(safeArea, "GameOverPanel", report);
            Stretch(panel);
            Image panelImage = EnsureSingleComponent<Image>(panel.gameObject, report);
            GameOverView view = EnsureSingleComponent<GameOverView>(panel.gameObject, report);

            if (panelImage != null)
            {
                Undo.RecordObject(panelImage, "Configure game-over panel");
                panelImage.color = OverallBackgroundColour;
            }

            RectTransform content = EnsureUiChild(panel, "Content", report);
            SetRect(content, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.82f),
                Vector2.zero, Vector2.zero, Center);
            VerticalLayoutGroup contentLayout = EnsureSingleComponent<VerticalLayoutGroup>(
                content.gameObject, report);
            if (contentLayout != null)
            {
                Undo.RecordObject(contentLayout, "Configure game-over content layout");
                contentLayout.padding = new RectOffset(24, 24, 24, 24);
                contentLayout.spacing = 20f;
                contentLayout.childAlignment = TextAnchor.MiddleCenter;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = true;
                contentLayout.childForceExpandHeight = false;
            }

            RectTransform illustrationTransform = RepairOrCreateGameOverChild(
                canvasObject.transform, content, view, "illustrationImage", "Illustration", report);
            Image illustration = EnsureSingleComponent<Image>(
                illustrationTransform.gameObject, report);
            if (illustration != null)
            {
                Undo.RecordObject(illustration, "Configure ending illustration");
                illustration.raycastTarget = false;
            }

            RectTransform titleTransform = RepairOrCreateGameOverChild(
                canvasObject.transform, content, view, "titleText", "Title", report);
            TextMeshProUGUI title = EnsureSingleComponent<TextMeshProUGUI>(
                titleTransform.gameObject, report);
            ConfigureReadableText(title, font, 56f, 48f, 60f, true, true, 3f);

            RectTransform bodyTransform = RepairOrCreateGameOverChild(
                canvasObject.transform, content, view, "bodyText", "Body", report, "BODY");
            TextMeshProUGUI body = EnsureSingleComponent<TextMeshProUGUI>(
                bodyTransform.gameObject, report);
            ConfigureReadableText(body, font, 38f, 34f, 42f, true, true, 5f);

            RectTransform restartTransform = RepairOrCreateGameOverChild(
                canvasObject.transform, content, view, "restartButton", "RestartButton", report);
            Image restartImage = EnsureSingleComponent<Image>(restartTransform.gameObject, report);
            Button restart = EnsureSingleComponent<Button>(restartTransform.gameObject, report);
            if (restartImage != null)
            {
                Undo.RecordObject(restartImage, "Configure restart button");
                restartImage.color = ButtonColour;
                restartImage.raycastTarget = true;
            }

            TextMeshProUGUI restartText = EnsureButtonText(
                restartTransform, "Restart", report);
            ConfigureReadableText(restartText, font, 40f, 34f, 42f, true, true, 2f);
            restartText.text = interfaceText != null ? interfaceText.Restart : "Yeniden Başlat";
            EnsureExpectedListener(restart, view, nameof(GameOverView.HandleRestartButton),
                view != null ? view.HandleRestartButton : null, report);

            if (view != null)
            {
                SetObjectProperty(view, "panelRoot", panel.gameObject, report);
                SetObjectProperty(view, "titleText", title, report);
                SetObjectProperty(view, "bodyText", body, report);
                SetObjectProperty(view, "illustrationImage", illustration, report);
                SetObjectProperty(view, "restartButton", restart, report);
                SetObjectProperty(view, "restartButtonText", restartText, report);
                SetObjectProperty(view, "interfaceText", interfaceText, report);
                SetObjectProperty(view, "panelImage", panelImage, report);
            }

            ConfigureLayoutElement(illustrationTransform.gameObject, 260f, report);
            ConfigureLayoutElement(titleTransform.gameObject, 96f, report);
            ConfigureLayoutElement(bodyTransform.gameObject, 220f, report);
            ConfigureLayoutElement(restartTransform.gameObject, 112f, report);

            SetSiblingIndex(illustrationTransform, 0);
            SetSiblingIndex(titleTransform, 1);
            SetSiblingIndex(bodyTransform, 2);
            SetSiblingIndex(restartTransform, 3);
            SetSiblingIndex(restartText.transform, 0);
            SetSiblingIndex(content, 0);

            RemoveSafeLegacyGameOverChild(panel, "Illustration", illustrationTransform, report);
            RemoveSafeLegacyGameOverChild(panel, "Title", titleTransform, report);
            RemoveSafeLegacyGameOverChild(panel, "Body", bodyTransform, report);
            RemoveSafeLegacyGameOverChild(panel, "BODY", bodyTransform, report);
            RemoveSafeLegacyGameOverChild(panel, "RestartButton", restartTransform, report);

            if (panel.gameObject.activeSelf)
            {
                Undo.RecordObject(panel.gameObject, "Deactivate game-over panel");
                panel.gameObject.SetActive(false);
            }

            return new GameOverParts(panel, view);
        }

        private static void RemoveSafeLegacyGameOverChild(
            RectTransform panel,
            string childName,
            RectTransform activeReplacement,
            SceneSetupReport report)
        {
            Transform legacy = panel != null ? panel.Find(childName) : null;
            if (legacy == null || legacy == activeReplacement)
            {
                return;
            }
            if (!CanRemoveLegacyGameOverObject(legacy.gameObject, activeReplacement, out string reason))
            {
                AddInvalid(report, panel.gameObject.scene.path, HierarchyPath(legacy),
                    "Obsolete GameOver child is ambiguous and was preserved: " + reason);
                return;
            }
            Undo.DestroyObjectImmediate(legacy.gameObject);
        }

        private static bool CanRemoveLegacyGameOverObject(
            GameObject legacy,
            RectTransform replacement,
            out string reason)
        {
            if (legacy.activeSelf)
            {
                reason = "object is active";
                return false;
            }
            if (replacement == null || replacement.parent == legacy.transform.parent)
            {
                reason = "the managed Content replacement is missing";
                return false;
            }
            Component required = legacy.name switch
            {
                "Illustration" => legacy.GetComponent<Image>(),
                "Title" => legacy.GetComponent<TextMeshProUGUI>(),
                "Body" => legacy.GetComponent<TextMeshProUGUI>(),
                "BODY" => legacy.GetComponent<TextMeshProUGUI>(),
                "RestartButton" => legacy.GetComponent<Button>(),
                _ => null
            };
            if (required == null)
            {
                reason = "component signature is not the known legacy signature";
                return false;
            }

            Component[] components = legacy.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                {
                    reason = "object contains a missing-script component";
                    return false;
                }
                if (component is Transform || component is CanvasRenderer || component is Image
                    || component is Button || component is TextMeshProUGUI)
                {
                    continue;
                }
                reason = "object contains unexpected component " + component.GetType().Name;
                return false;
            }

            Button[] buttons = legacy.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].onClick.GetPersistentEventCount() != 0
                    && !HasOnlyMatchingManagedRestartListener(
                        buttons[i], replacement.GetComponent<Button>()))
                {
                    reason = "object contains a persistent button listener that does not exactly "
                        + "match the managed replacement";
                    return false;
                }
            }

            if (HasExternalSerializedReference(legacy))
            {
                reason = "another scene component serializes a reference to it";
                return false;
            }
            reason = string.Empty;
            return true;
        }

        private static bool HasOnlyMatchingManagedRestartListener(
            Button legacy,
            Button replacement)
        {
            if (legacy == null || replacement == null
                || legacy.onClick.GetPersistentEventCount() != 1
                || replacement.onClick.GetPersistentEventCount() != 1)
            {
                return false;
            }

            Object legacyTarget = legacy.onClick.GetPersistentTarget(0);
            string legacyMethod = legacy.onClick.GetPersistentMethodName(0);
            return legacyTarget is GameOverView
                && legacyMethod == nameof(GameOverView.HandleRestartButton)
                && replacement.onClick.GetPersistentTarget(0) == legacyTarget
                && replacement.onClick.GetPersistentMethodName(0) == legacyMethod
                && replacement.onClick.GetPersistentListenerState(0)
                    == legacy.onClick.GetPersistentListenerState(0);
        }

        private static bool HasExternalSerializedReference(GameObject candidate)
        {
            HashSet<Object> owned = new HashSet<Object> { candidate };
            Component[] ownedComponents = candidate.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < ownedComponents.Length; i++)
            {
                if (ownedComponents[i] != null)
                {
                    owned.Add(ownedComponents[i]);
                    owned.Add(ownedComponents[i].gameObject);
                }
            }

            Component[] sceneComponents = FindComponentsInScene<Component>(candidate.scene);
            for (int i = 0; i < sceneComponents.Length; i++)
            {
                Component owner = sceneComponents[i];
                if (owner == null || owned.Contains(owner))
                {
                    continue;
                }
                SerializedObject serialized = new SerializedObject(owner);
                SerializedProperty iterator = serialized.GetIterator();
                if (!iterator.Next(true))
                {
                    continue;
                }
                do
                {
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference
                        && owned.Contains(iterator.objectReferenceValue))
                    {
                        return true;
                    }
                }
                while (iterator.NextVisible(true));
            }
            return false;
        }

        private static AudioService ConfigureAudio(Scene scene, SceneSetupReport report)
        {
            GameObject audioObject = EnsureRoot(scene, "AudioService", report);
            AudioSource source = EnsureSingleComponent<AudioSource>(audioObject, report);
            Transform musicTransform = audioObject.transform.Find("MusicSource");
            GameObject musicObject;
            if (musicTransform == null)
            {
                musicObject = new GameObject("MusicSource");
                Undo.RegisterCreatedObjectUndo(musicObject, "Create music AudioSource");
                musicObject.transform.SetParent(audioObject.transform, false);
            }
            else
            {
                musicObject = musicTransform.gameObject;
            }
            AudioSource music = EnsureSingleComponent<AudioSource>(musicObject, report);
            AudioService service = EnsureSingleComponent<AudioService>(audioObject, report);
            if (source != null)
            {
                Undo.RecordObject(source, "Configure audio source");
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
            }
            if (music != null)
            {
                Undo.RecordObject(music, "Configure music audio source");
                music.playOnAwake = false;
                music.loop = true;
                music.spatialBlend = 0f;
            }

            if (service != null)
            {
                SetObjectProperty(service, "audioSource", source, report);
                SetObjectProperty(service, "musicSource", music, report);
            }

            return service;
        }

        // Bootstrap and menu ---------------------------------------------------------

        private static void ApplyBootstrapScene(Scene scene, SceneSetupReport report)
        {
            if (!CheckRootDuplicates(scene, "BootstrapController", report))
            {
                return;
            }

            GameObject root = EnsureRoot(scene, "BootstrapController", report);
            BootstrapController controller = EnsureSingleComponent<BootstrapController>(root, report);
            SetStringProperty(controller, "mainMenuSceneName", "MainMenu", report);
        }

        private static void ApplyMainMenuScene(
            Scene scene,
            SessionIntent intent,
            InterfaceTextDefinition interfaceText,
            TMP_FontAsset font,
            FeedbackCueProfile feedback,
            SceneSetupReport report)
        {
            if (!CheckRootDuplicates(scene, CanvasName, report)
                || !CheckRootDuplicates(scene, "MainMenuController", report)
                || !CheckRootDuplicates(scene, "AudioService", report)
                || !CheckRootDuplicates(scene, "SettingsController", report))
            {
                return;
            }

            EnsureCamera(scene, report);
            EnsureEventSystem(scene, report);
            GameObject canvasObject = EnsureRoot(scene, CanvasName, report, true);
            ConfigureCanvas(canvasObject, report);

            RectTransform safeArea = EnsureUiChild(canvasObject.transform, "SafeArea", report);
            Stretch(safeArea);
            EnsureSingleComponent<SafeAreaFitter>(safeArea.gameObject, report);
            // A prior pass added a full-Canvas Background here; the Main Menu screen itself was
            // explicitly asked to stay untouched, so remove it if a previous Apply created it.
            // SettingsPanel/AboutPanel are legitimate direct Canvas children too (full-screen
            // overlays, siblings of SafeArea) — they're migrated into place further down.
            RemoveUnexpectedChildren(
                canvasObject.transform, report,
                "SafeArea", "SettingsPanel", "AboutPanel", "TransitionOverlay");

            RectTransform panel = EnsureUiChild(safeArea, "MainMenuPanel", report);
            Stretch(panel);
            TextMeshProUGUI title = EnsureText(panel, "Title", new Vector2(0f, 280f),
                new Vector2(850f, 160f), 64f, report);
            ConfigureReadableText(title, font, 64f, 52f, 68f, true, true, 2f);
            title.text = interfaceText != null ? interfaceText.MainMenuTitle : "Royal Decisions";

            Button newGame = EnsureMenuButton(panel, "NewGameButton", "Yeni Oyun", 40f, report,
                colourOverride: SettingsPanelTheme.ActiveTabColour);
            Button continueButton = EnsureMenuButton(
                panel, "ContinueButton", "Devam Et", -120f, report,
                colourOverride: SettingsPanelTheme.ActiveTabColour);
            Button settingsButton = EnsureSettingsIconButton(
                panel, report, iconColourOverride: SettingsPanelTheme.ActiveTabColour);
            TextMeshProUGUI saveError = EnsureText(panel, "SaveError", new Vector2(0f, -430f),
                new Vector2(850f, 150f), 30f, report);
            TextMeshProUGUI newGameText = newGame != null
                ? newGame.GetComponentInChildren<TextMeshProUGUI>(true)
                : null;
            TextMeshProUGUI continueText = continueButton != null
                ? continueButton.GetComponentInChildren<TextMeshProUGUI>(true)
                : null;
            ConfigureReadableText(newGameText, font, 40f, 34f, 42f, true, true, 2f);
            ConfigureReadableText(continueText, font, 40f, 34f, 42f, true, true, 2f);
            ConfigureReadableText(saveError, font, 30f, 28f, 32f, true, true, 2f);
            saveError.text = string.Empty;
            saveError.gameObject.SetActive(false);
            if (interfaceText != null)
            {
                newGameText.text = interfaceText.NewGame;
                continueText.text = interfaceText.ContinueGame;
            }
            MainMenuTextView textView = EnsureSingleComponent<MainMenuTextView>(
                panel.gameObject, report);
            SetObjectProperty(textView, "interfaceText", interfaceText, report);
            SetObjectProperty(textView, "titleText", title, report);
            SetObjectProperty(textView, "newGameText", newGameText, report);
            SetObjectProperty(textView, "continueText", continueText, report);
            SetObjectProperty(textView, "saveErrorText", saveError, report);

            // A prior pass blocked input to the menu with a CanvasGroup while Settings was open;
            // replaced with real SetActive state management on this GameObject (wired onto
            // SettingsController below), so remove that leftover component if present.
            RemoveStaleComponents<CanvasGroup>(panel.gameObject);

            // Starts hidden, like every other panel — only appears for the moment of leaving.
            PanelFadeAnimator transitionOverlay = ConfigureTransitionOverlay(
                canvasObject.transform, report, startVisible: false);

            GameObject controllerObject = EnsureRoot(scene, "MainMenuController", report);
            MainMenuController controller = EnsureSingleComponent<MainMenuController>(
                controllerObject, report);
            SetStringProperty(controller, "gameSceneName", "Game", report);
            SetObjectProperty(controller, "sessionIntent", intent, report);
            SetObjectProperty(controller, "continueButton", continueButton, report);
            SetObjectProperty(controller, "interfaceText", interfaceText, report);
            SetObjectProperty(controller, "mainMenuTextView", textView, report);
            SetObjectProperty(controller, "transitionOverlay", transitionOverlay, report);

            EnsureExpectedListener(newGame, controller, nameof(MainMenuController.OnNewGamePressed),
                controller != null ? controller.OnNewGamePressed : null, report);
            EnsureExpectedListener(continueButton, controller,
                nameof(MainMenuController.OnContinuePressed),
                controller != null ? controller.OnContinuePressed : null, report);

            AudioService audio = ConfigureAudio(scene, report);
            // SettingsPanel/AboutPanel are full-screen overlays: siblings of SafeArea directly
            // under the Canvas (not nested inside it), so their background can cover the whole
            // physical screen instead of just the safe-area inset. Migrate them out of the old
            // SafeArea-nested location the first time this runs against an older scene.
            MigrateChildIfNeeded(canvasObject.transform, safeArea, "SettingsPanel", report);
            MigrateChildIfNeeded(canvasObject.transform, safeArea, "AboutPanel", report);
            SettingsParts settings = ConfigureSettingsPanel(
                canvasObject.transform, font, audio, feedback, report);
            AboutPanelView aboutPanel = ConfigureAboutPanel(canvasObject.transform, font, report);
            SetObjectProperty(settings.Controller, "aboutPanel", aboutPanel, report);
            SetObjectProperty(settings.Controller, "mainMenuRoot", panel.gameObject, report);
            EnsureExpectedListener(settingsButton, settings.Controller,
                nameof(SettingsController.Open),
                settings.Controller != null ? settings.Controller.Open : null, report);
            ConfigureMinimumTouchTarget(newGame, report);
            ConfigureMinimumTouchTarget(continueButton, report);
            ConfigureMinimumTouchTarget(settingsButton, report);

            // One accessibility controller for the whole MainMenu scene (menu, every Settings tab,
            // and About), mirroring exactly how the Game scene's own AccessibilityPresentationController
            // scales its whole scene's text — Reduced Motion and Text Size were previously wired only
            // as far as GameSettings and never actually applied to anything in this scene.
            AccessibilityPresentationController accessibility =
                EnsureSingleComponent<AccessibilityPresentationController>(
                    settings.Controller != null ? settings.Controller.gameObject : controllerObject,
                    report);
            TextMeshProUGUI[] mainMenuAccessibleText = FindComponentsInScene<TextMeshProUGUI>(scene);
            SetObjectArrayProperty(accessibility, "scalableText", mainMenuAccessibleText, report);
            PanelFadeAnimator aboutPanelTransition = aboutPanel != null
                ? aboutPanel.GetComponent<PanelFadeAnimator>() : null;
            List<PanelFadeAnimator> mainMenuPanelAnimators =
                new List<PanelFadeAnimator> { transitionOverlay };
            mainMenuPanelAnimators.AddRange(settings.PanelAnimators);
            if (aboutPanelTransition != null)
            {
                mainMenuPanelAnimators.Add(aboutPanelTransition);
            }
            SetObjectArrayProperty(accessibility, "panelAnimators", mainMenuPanelAnimators.ToArray(), report);
            if (settings.Controller != null)
            {
                SetObjectProperty(settings.Controller, "accessibility", accessibility, report);
            }

            GameObject resetProgressObject = EnsureRoot(scene, "ResetProgressController", report);
            ResetProgressController resetProgressController =
                EnsureSingleComponent<ResetProgressController>(resetProgressObject, report);
            SetObjectProperty(resetProgressController, "view", settings.View, report);

            ApplicationLifecycleController lifecycle =
                EnsureSingleComponent<ApplicationLifecycleController>(controllerObject, report);
            SetBoolProperty(lifecycle, "mainMenuMode", true, report);
            SetStringProperty(lifecycle, "mainMenuSceneName", "MainMenu", report);
            SetObjectProperty(lifecycle, "settingsController", settings.Controller, report);
        }

        private static SettingsParts ConfigureSettingsPanel(
            Transform canvasTransform,
            TMP_FontAsset font,
            AudioService audio,
            FeedbackCueProfile cues,
            SceneSetupReport report)
        {
            // Stretches the full Canvas (not SafeArea) so the background genuinely covers the
            // whole physical screen, independent of any safe-area inset.
            RectTransform root = EnsureUiChild(canvasTransform, "SettingsPanel", report);
            Stretch(root);
            Image surface = EnsureSingleComponent<Image>(root.gameObject, report);
            // Flat colour, no sprite: the built-in UISprite bakes a subtle bevel/shadow into its
            // pixels, which reads as a thin dark rectangle when stretched to fill the whole screen.
            ConfigureSimpleImage(surface, null, MainMenuBackgroundColour, true);

            // The panel's own content still respects the safe area, independent of the full-bleed
            // background above — mirrors the top-level SafeArea/MainMenuPanel pattern.
            RectTransform safeContent = EnsureUiChild(root, "SafeArea", report);
            Stretch(safeContent);
            EnsureSingleComponent<SafeAreaFitter>(safeContent.gameObject, report);
            MigrateChildIfNeeded(safeContent, root, "Content", report);
            // Sweeps a leftover "HeaderBackdrop" from an earlier pass — it existed only to bleed
            // Header's own card colour behind the notch, and Header no longer has a card colour
            // (every row is transparent now), so root's own flat MainMenuBackgroundColour already
            // covers that area uniformly on its own.
            RemoveUnexpectedChildren(root, report, "SafeArea");

            // Full-bleed to the safe area's own edges (was inset 8%/6%, leaving the root's flat
            // colour visible as a margin) — at the user's request, the panel now fills the screen
            // edge-to-edge instead of floating as a smaller centred card.
            RectTransform content = EnsureUiChild(safeContent, "Content", report);
            Stretch(content);
            VerticalLayoutGroup contentLayout =
                EnsureSingleComponent<VerticalLayoutGroup>(content.gameObject, report);
            if (contentLayout != null)
            {
                Undo.RecordObject(contentLayout, "Configure settings content layout");
                contentLayout.padding = new RectOffset(0, 0, 0, 0);
                contentLayout.spacing = 10f;
                contentLayout.childAlignment = TextAnchor.UpperCenter;
                contentLayout.childControlWidth = true;
                contentLayout.childForceExpandWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandHeight = false;
            }

            // Header -----------------------------------------------------------------
            RectTransform header = EnsureUiChild(content, "Header", report);
            SetRect(header, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
                new Vector2(0f, 84f), new Vector2(0.5f, 1f));
            ConfigureLayoutElement(header.gameObject, 84f, report);
            // No card background, matching every other row — RemoveStaleComponents drops the
            // Image/Outline a previous pass's ConfigureRowCard call left behind.
            RemoveStaleComponents<Image>(header.gameObject);
            RemoveStaleComponents<Outline>(header.gameObject);
            MigrateChildIfNeeded(header, content, "Title", report);
            RectTransform titleTransform = EnsureUiChild(header, "Title", report);
            SetRect(titleTransform, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f),
                Vector2.zero, Vector2.zero, Center);
            TextMeshProUGUI title = EnsureSingleComponent<TextMeshProUGUI>(
                titleTransform.gameObject, report);
            ConfigureReadableText(title, font, 36f, 30f, 40f, true, false, 2f);
            title.text = "Ayarlar";
            // Sweeps leftovers from earlier authoring passes (e.g. a since-removed decorative
            // tagline, or the since-removed diamond icon) — Header previously had no allowlist, so
            // anything an older pass added and a later pass stopped creating was never cleaned up
            // automatically.
            RemoveUnexpectedChildren(header, report, "Title");

            // Tab bar ------------------------------------------------------------------
            RectTransform tabBar = EnsureUiChild(content, "TabBar", report);
            SetRect(tabBar, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
                new Vector2(0f, 96f), new Vector2(0.5f, 1f));
            ConfigureLayoutElement(tabBar.gameObject, 96f, report);
            HorizontalLayoutGroup tabBarLayout =
                EnsureSingleComponent<HorizontalLayoutGroup>(tabBar.gameObject, report);
            if (tabBarLayout != null)
            {
                Undo.RecordObject(tabBarLayout, "Configure settings tab bar layout");
                tabBarLayout.padding = new RectOffset(0, 0, 0, 0);
                tabBarLayout.spacing = 12f;
                tabBarLayout.childAlignment = TextAnchor.MiddleCenter;
                tabBarLayout.childControlWidth = true;
                tabBarLayout.childForceExpandWidth = true;
                tabBarLayout.childControlHeight = true;
                tabBarLayout.childForceExpandHeight = true;
            }
            MigrateChildIfNeeded(tabBar, content, "AudioTabButton", report);
            MigrateChildIfNeeded(tabBar, content, "GraphicsTabButton", report);
            MigrateChildIfNeeded(tabBar, content, "ControlsTabButton", report);
            MigrateChildIfNeeded(tabBar, content, "GeneralTabButton", report);

            // Tab switcher: one row of four flexible buttons above four mutually-exclusive bodies.
            // Audio starts active (Show() opens on it), so it is authored with the active tint.
            Button audioTabButton = EnsureMenuButton(tabBar, "AudioTabButton", "Ses", 0f, report,
                width: 172f, height: 96f, colourOverride: SettingsPanelTheme.ActiveTabColour,
                cornerRadius: TabPillCornerRadius);
            Button graphicsTabButton = EnsureMenuButton(
                tabBar, "GraphicsTabButton", "Grafik", 0f, report,
                width: 172f, height: 96f, colourOverride: SettingsPanelTheme.InactiveTabColour,
                cornerRadius: TabPillCornerRadius);
            Button controlsTabButton = EnsureMenuButton(
                tabBar, "ControlsTabButton", "Kontroller", 0f, report,
                width: 172f, height: 96f, colourOverride: SettingsPanelTheme.InactiveTabColour,
                cornerRadius: TabPillCornerRadius);
            Button generalTabButton = EnsureMenuButton(
                tabBar, "GeneralTabButton", "Genel", 0f, report,
                width: 172f, height: 96f, colourOverride: SettingsPanelTheme.InactiveTabColour,
                cornerRadius: TabPillCornerRadius);
            ConfigureButtonFont(audioTabButton, font, 27f, 20f, 31f);
            ConfigureButtonFont(graphicsTabButton, font, 27f, 20f, 31f);
            ConfigureButtonFont(controlsTabButton, font, 27f, 20f, 31f);
            ConfigureButtonFont(generalTabButton, font, 27f, 20f, 31f);
            // Ses starts active; give each tab's label the matching initial text contrast so the
            // very first paint (before any runtime SetActiveTab call) already looks correct.
            SetTextColour(audioTabButton.GetComponentInChildren<TextMeshProUGUI>(true),
                SettingsPanelTheme.ActiveTabTextColour);
            SetTextColour(graphicsTabButton.GetComponentInChildren<TextMeshProUGUI>(true),
                SettingsPanelTheme.InactiveTabTextColour);
            SetTextColour(controlsTabButton.GetComponentInChildren<TextMeshProUGUI>(true),
                SettingsPanelTheme.InactiveTabTextColour);
            SetTextColour(generalTabButton.GetComponentInChildren<TextMeshProUGUI>(true),
                SettingsPanelTheme.InactiveTabTextColour);
            ConfigureMinimumTouchTarget(audioTabButton, report);
            ConfigureMinimumTouchTarget(graphicsTabButton, report);
            ConfigureMinimumTouchTarget(controlsTabButton, report);
            ConfigureMinimumTouchTarget(generalTabButton, report);

            // Scrollable content: only the active tab's controls are visible at a time, so the
            // scroll content's height always matches whichever tab is currently shown.
            RectTransform contentViewport = EnsureUiChild(content, "ContentViewport", report);
            SetRect(contentViewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Center);
            LayoutElement viewportElement =
                EnsureSingleComponent<LayoutElement>(contentViewport.gameObject, report);
            if (viewportElement != null)
            {
                Undo.RecordObject(viewportElement, "Configure settings content viewport layout");
                viewportElement.flexibleHeight = 1f;
                viewportElement.flexibleWidth = 1f;
            }
            EnsureSingleComponent<RectMask2D>(contentViewport.gameObject, report);
            ScrollRect scrollRect = EnsureSingleComponent<ScrollRect>(contentViewport.gameObject, report);

            RectTransform scrollContent = EnsureUiChild(contentViewport, "ScrollContent", report);
            SetRect(scrollContent, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
                Vector2.zero, new Vector2(0.5f, 1f));
            VerticalLayoutGroup scrollContentLayout =
                EnsureSingleComponent<VerticalLayoutGroup>(scrollContent.gameObject, report);
            if (scrollContentLayout != null)
            {
                Undo.RecordObject(scrollContentLayout, "Configure settings scroll content layout");
                scrollContentLayout.padding = new RectOffset(0, 0, 0, 0);
                scrollContentLayout.spacing = 0f;
                scrollContentLayout.childAlignment = TextAnchor.UpperCenter;
                scrollContentLayout.childControlWidth = true;
                scrollContentLayout.childForceExpandWidth = true;
                // Height stays child-controlled (each tab sizes itself via its own
                // ContentSizeFitter below) so the two fitters never fight over the same axis.
                scrollContentLayout.childControlHeight = false;
                scrollContentLayout.childForceExpandHeight = false;
            }
            ContentSizeFitter scrollContentFitter =
                EnsureSingleComponent<ContentSizeFitter>(scrollContent.gameObject, report);
            if (scrollContentFitter != null)
            {
                Undo.RecordObject(scrollContentFitter, "Configure settings scroll content fitter");
                scrollContentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                scrollContentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            if (scrollRect != null)
            {
                Undo.RecordObject(scrollRect, "Configure settings scroll rect");
                scrollRect.content = scrollContent;
                scrollRect.viewport = null;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.inertia = true;
                scrollRect.scrollSensitivity = 28f;
                scrollRect.verticalScrollbar = null;
                scrollRect.horizontalScrollbar = null;
            }
            MigrateChildIfNeeded(scrollContent, content, "AudioTab", report);
            MigrateChildIfNeeded(scrollContent, content, "GraphicsTab", report);
            MigrateChildIfNeeded(scrollContent, content, "ControlsTab", report);
            MigrateChildIfNeeded(scrollContent, content, "GeneralTab", report);

            AudioSettingsPanelView audioPanel = ConfigureAudioSettingsTab(scrollContent, font, report);
            GraphicsSettingsPanelView graphicsPanel =
                ConfigureGraphicsSettingsTab(scrollContent, font, report);
            ControlsSettingsPanelView controlsPanel =
                ConfigureControlsSettingsTab(scrollContent, font, report);
            GeneralSettingsPanelView generalPanel =
                ConfigureGeneralSettingsTab(scrollContent, font, report, out Button resetToDefaults);

            // Only Audio is visible at rest, matching SettingsPanelView.Show() -> ShowAudioTab().
            // Authoring all four active would otherwise render every tab's rows stacked on top of
            // one another until the panel is opened once at runtime.
            SetActiveIfNeeded(audioPanel != null ? audioPanel.gameObject : null, true);
            SetActiveIfNeeded(graphicsPanel != null ? graphicsPanel.gameObject : null, false);
            SetActiveIfNeeded(controlsPanel != null ? controlsPanel.gameObject : null, false);
            SetActiveIfNeeded(generalPanel != null ? generalPanel.gameObject : null, false);

            // Thin divider so the fixed Apply/Cancel bar reads as a distinct footer rather than
            // blurring into whatever tab content happens to sit right above it.
            RectTransform bottomDivider = EnsureUiChild(content, "BottomDivider", report);
            SetRect(bottomDivider, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
                new Vector2(0f, 2f), new Vector2(0.5f, 1f));
            ConfigureLayoutElement(bottomDivider.gameObject, 2f, report);
            Image dividerImage = EnsureSingleComponent<Image>(bottomDivider.gameObject, report);
            ConfigureSimpleImage(
                dividerImage, LoadBuiltInUiSprite(report), new Color(1f, 1f, 1f, 0.18f), false);

            // Bottom actions: pinned outside the ScrollRect so Apply/Cancel never scroll away.
            // Varsayılanlara Dön used to live here as a third button with the same visual weight
            // as Uygula/İptal; it is now an ordinary settings action row inside the Genel tab
            // (see ConfigureGeneralSettingsTab) — İptal/Uygula are the only two draft-state
            // actions left, so they are the only two buttons left in the footer.
            RectTransform bottomActions = EnsureUiChild(content, "BottomActions", report);
            SetRect(bottomActions, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
                new Vector2(0f, 104f), new Vector2(0.5f, 1f));
            ConfigureLayoutElement(bottomActions.gameObject, 104f, report);
            HorizontalLayoutGroup bottomActionsLayout =
                EnsureSingleComponent<HorizontalLayoutGroup>(bottomActions.gameObject, report);
            if (bottomActionsLayout != null)
            {
                Undo.RecordObject(bottomActionsLayout, "Configure settings bottom action layout");
                bottomActionsLayout.padding = new RectOffset(0, 0, 0, 0);
                bottomActionsLayout.spacing = 16f;
                bottomActionsLayout.childAlignment = TextAnchor.MiddleCenter;
                bottomActionsLayout.childControlWidth = true;
                bottomActionsLayout.childForceExpandWidth = true;
                bottomActionsLayout.childControlHeight = true;
                bottomActionsLayout.childForceExpandHeight = true;
            }
            MigrateChildIfNeeded(bottomActions, content, "ApplyButton", report);
            MigrateChildIfNeeded(bottomActions, content, "CancelButton", report);

            // İptal (secondary/neutral) on the left, Uygula (primary) on the right.
            Button cancel = EnsureMenuButton(bottomActions, "CancelButton", "İptal", 0f, report,
                colourOverride: SettingsPanelTheme.ActiveTabColour);
            Button apply = EnsureMenuButton(bottomActions, "ApplyButton", "Uygula", 0f, report,
                colourOverride: SettingsPanelTheme.ActiveTabColour);
            ConfigureButtonFont(cancel, font, 36f, 30f, 40f);
            ConfigureButtonFont(apply, font, 36f, 30f, 40f);
            // Apply is the primary action; a heavier weight distinguishes it from İptal without
            // needing a second colour or size.
            TextMeshProUGUI applyLabel = apply != null
                ? apply.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (applyLabel != null)
            {
                Undo.RecordObject(applyLabel, "Configure apply button weight");
                applyLabel.fontStyle = FontStyles.Bold;
            }
            ConfigureMinimumTouchTarget(apply, report);
            ConfigureMinimumTouchTarget(cancel, report);
            SetSiblingIndex(cancel.transform, 0);
            SetSiblingIndex(apply.transform, 1);

            // Sweeps the old ResetButton left behind in the footer by an earlier authoring pass —
            // it now lives inside the Genel tab as a normal action row instead.
            RemoveUnexpectedChildren(bottomActions, report, "CancelButton", "ApplyButton");

            SetSiblingIndex(bottomDivider, 3);
            SetSiblingIndex(bottomActions, 4);

            RemoveUnexpectedChildren(content, report,
                "Header", "TabBar", "ContentViewport", "BottomDivider", "BottomActions");

            SettingsPanelView view = EnsureSingleComponent<SettingsPanelView>(root.gameObject, report);
            SetObjectProperty(view, "panelRoot", root.gameObject, report);
            SetObjectProperty(view, "audioPanel", audioPanel, report);
            SetObjectProperty(view, "graphicsPanel", graphicsPanel, report);
            SetObjectProperty(view, "controlsPanel", controlsPanel, report);
            SetObjectProperty(view, "generalPanel", generalPanel, report);
            SetObjectProperty(view, "audioTabButton", audioTabButton, report);
            SetObjectProperty(view, "graphicsTabButton", graphicsTabButton, report);
            SetObjectProperty(view, "controlsTabButton", controlsTabButton, report);
            SetObjectProperty(view, "generalTabButton", generalTabButton, report);
            SetObjectProperty(view, "applyButton", apply, report);
            SetObjectProperty(view, "cancelButton", cancel, report);
            SetObjectProperty(view, "resetButton", resetToDefaults, report);

            // Panel-level open/close reads as a screen transition (longer); the tab crossfade is an
            // in-place content swap (kept at the animator's shorter defaults) and never scales, since
            // ContentViewport clips via RectMask2D and a scale pulse would visibly distort that clip.
            PanelFadeAnimator settingsPanelTransition = ConfigurePanelFadeAnimator(
                root.gameObject, report, showDuration: 0.22f, hideDuration: 0.18f);
            PanelFadeAnimator tabCrossfadeTransition = ConfigurePanelFadeAnimator(
                contentViewport.gameObject, report, animateScale: false);
            SetObjectProperty(view, "panelAnimator", settingsPanelTransition, report);
            SetObjectProperty(view, "tabCrossfadeAnimator", tabCrossfadeTransition, report);

            GameObject controllerObject = EnsureRoot(
                root.gameObject.scene, "SettingsController", report);
            SettingsController controller = EnsureSingleComponent<SettingsController>(
                controllerObject, report);
            SetObjectProperty(controller, "view", view, report);
            SetObjectProperty(controller, "audioService", audio, report);
            SetObjectProperty(controller, "cues", cues, report);

            // Last sibling under the Canvas so it always renders above SafeArea (MainMenu)
            // regardless of authoring-time ordering; SettingsPanelView.Show() re-asserts this
            // at runtime too, since AboutPanel may since have taken the last slot.
            SetSiblingIndex(root, canvasTransform.childCount - 1);

            if (root.gameObject.activeSelf)
            {
                Undo.RecordObject(root.gameObject, "Deactivate settings panel");
                root.gameObject.SetActive(false);
            }
            return new SettingsParts(
                root, view, controller,
                new[] { settingsPanelTransition, tabCrossfadeTransition });
        }

        private static AboutPanelView ConfigureAboutPanel(
            Transform canvasTransform, TMP_FontAsset font, SceneSetupReport report)
        {
            RectTransform root = EnsureUiChild(canvasTransform, "AboutPanel", report);
            Stretch(root);
            Image surface = EnsureSingleComponent<Image>(root.gameObject, report);
            // Matches SettingsPanel's background since About is now reached only from within
            // Settings and should read as the same flow, not a visually distinct screen. Flat
            // colour, no sprite: the built-in UISprite bakes a subtle bevel/shadow into its
            // pixels, which reads as a thin dark rectangle when stretched to fill the whole
            // screen (see ConfigureSettingsPanel).
            ConfigureSimpleImage(surface, null, MainMenuBackgroundColour, true);

            RectTransform safeContent = EnsureUiChild(root, "SafeArea", report);
            Stretch(safeContent);
            EnsureSingleComponent<SafeAreaFitter>(safeContent.gameObject, report);
            MigrateChildIfNeeded(safeContent, root, "Content", report);

            // Full-bleed, matching ConfigureSettingsPanel's Content (was inset 8%/6%).
            RectTransform content = EnsureUiChild(safeContent, "Content", report);
            Stretch(content);

            TextMeshProUGUI title = EnsureText(content, "Title", new Vector2(0f, 700f),
                new Vector2(840f, 110f), 52f, report);
            title.text = "Hakkında";
            ConfigureReadableText(title, font, 52f, 44f, 56f, true, true, 2f);

            TextMeshProUGUI body = EnsureText(content, "Body", new Vector2(0f, 100f),
                new Vector2(840f, 900f), 32f, report);
            body.alignment = TextAlignmentOptions.TopLeft;
            if (string.IsNullOrEmpty(body.text))
            {
                Undo.RecordObject(body, "Set about body placeholder text");
                body.text = "Royal Decisions\nSürüm: geliştirme\n\nBu içerik yer tutucudur.";
            }
            ConfigureReadableText(body, font, 32f, 26f, 36f, true, true, 2f);

            Button close = EnsureMenuButton(content, "CloseButton", "Kapat", -710f, report,
                colourOverride: SettingsPanelTheme.ActiveTabColour);
            ConfigureButtonFont(close, font, 40f, 34f, 42f);
            ConfigureMinimumTouchTarget(close, report);

            // Sweeps leftovers from earlier authoring passes (e.g. a since-removed decorative
            // "HazardTop" band) — Content here previously had no allowlist, so anything an older
            // pass added and a later pass stopped creating was never cleaned up automatically.
            RemoveUnexpectedChildren(content, report, "Title", "Body", "CloseButton");

            AboutPanelView view = EnsureSingleComponent<AboutPanelView>(root.gameObject, report);
            SetObjectProperty(view, "panelRoot", root.gameObject, report);
            SetObjectProperty(view, "closeButton", close, report);

            PanelFadeAnimator aboutPanelTransition = ConfigurePanelFadeAnimator(
                root.gameObject, report, showDuration: 0.22f, hideDuration: 0.18f);
            SetObjectProperty(view, "panelAnimator", aboutPanelTransition, report);

            // Last sibling under the Canvas: About opens on top of both MainMenu and Settings.
            SetSiblingIndex(root, canvasTransform.childCount - 1);

            if (root.gameObject.activeSelf)
            {
                Undo.RecordObject(root.gameObject, "Deactivate about panel");
                root.gameObject.SetActive(false);
            }
            return view;
        }

        private static AudioSettingsPanelView ConfigureAudioSettingsTab(
            RectTransform scrollContent, TMP_FontAsset font, SceneSetupReport report)
        {
            RectTransform tab = EnsureUiChild(scrollContent, "AudioTab", report);
            ConfigureTabLayout(tab, report, spacing: 24f);
            EnsureTabSectionHeader(tab, "Ses ve Müzik",
                "Müzik ve efekt seviyelerini ayarlayın.", font, report);

            // The three volume sliders share one grouped card, matching the reference layout,
            // instead of sitting directly in the tab as separate rows. MigrateChildIfNeeded moves
            // each slider out of its pre-grouping location directly under `tab` so repeated Apply
            // runs on an older scene stay idempotent instead of leaving an orphaned duplicate.
            // rowSpacing (rather than a divider line between rows) keeps the three rows visually
            // separated without a rendered element that can go wrong the way the old Divider1/
            // Divider2 hairlines did — see EnsureSettingsGroupPanel's childControlHeight = false.
            RectTransform sliderGroup = EnsureSettingsGroupPanel(tab, "VolumeGroup", report,
                rowSpacing: 8f);
            MigrateChildIfNeeded(sliderGroup, tab, "MasterVolume", report);
            MigrateChildIfNeeded(sliderGroup, tab, "MusicVolume", report);
            MigrateChildIfNeeded(sliderGroup, tab, "SfxVolume", report);

            Slider master = EnsureSliderControl(sliderGroup, "MasterVolume", "Ana Ses", font, report,
                out TMP_Text masterLabel, defaultValue: GameSettings.MaxVolume);
            SetSiblingIndex(master.transform, 0);
            Slider music = EnsureSliderControl(sliderGroup, "MusicVolume", "Müzik", font, report,
                out TMP_Text musicLabel, defaultValue: GameSettings.DefaultVolume);
            SetSiblingIndex(music.transform, 1);
            Slider sfx = EnsureSliderControl(sliderGroup, "SfxVolume", "Ses Efektleri", font, report,
                out TMP_Text sfxLabel, defaultValue: GameSettings.DefaultVolume);
            SetSiblingIndex(sfx.transform, 2);
            // Also prunes any leftover Divider1/Divider2 objects a previous Apply pass created.
            RemoveUnexpectedChildren(sliderGroup, report,
                "MasterVolume", "MusicVolume", "SfxVolume");

            Toggle mute = EnsureToggleControl(tab, "MasterMute", "Sessiz Mod",
                "Tüm oyun seslerini kapatır.", font, report);

            AudioSettingsPanelView audioPanel =
                EnsureSingleComponent<AudioSettingsPanelView>(tab.gameObject, report);
            SetObjectProperty(audioPanel, "masterVolume", master, report);
            SetObjectProperty(audioPanel, "musicVolume", music, report);
            SetObjectProperty(audioPanel, "sfxVolume", sfx, report);
            SetObjectProperty(audioPanel, "masterMute", mute, report);
            SetObjectProperty(audioPanel, "masterVolumeValueLabel", masterLabel, report);
            SetObjectProperty(audioPanel, "musicVolumeValueLabel", musicLabel, report);
            SetObjectProperty(audioPanel, "sfxVolumeValueLabel", sfxLabel, report);

            SetSiblingIndex(sliderGroup, 2);
            SetSiblingIndex(mute.transform, 3);
            RemoveUnexpectedChildren(tab, report,
                "SectionTitle", "SectionDescription", "VolumeGroup", "MasterMute");
            // VolumeGroup is a brand-new nested ContentSizeFitter this pass — without an explicit
            // rebuild here its height stays at the zero it was created with until something else
            // happens to trigger Unity's own layout pass, which briefly renders it collapsed (see
            // the TMP auto-size fix above this function's sibling tabs for the same class of bug).
            LayoutRebuilder.ForceRebuildLayoutImmediate(tab);
            return audioPanel;
        }

        private static GraphicsSettingsPanelView ConfigureGraphicsSettingsTab(
            RectTransform scrollContent, TMP_FontAsset font, SceneSetupReport report)
        {
            RectTransform tab = EnsureUiChild(scrollContent, "GraphicsTab", report);
            ConfigureTabLayout(tab, report);
            EnsureTabSectionHeader(tab, "Grafik",
                "Kare hızı ve pil tasarrufu tercihlerini yönetin.", font, report);

            // A single slider snapping across three whole-number steps (30 FPS / 60 FPS /
            // Otomatik) — the same slider control and visual language as the volume sliders on
            // the Ses tab, rather than three separate toggle buttons. Still gets its own single-row
            // group card so every control on the tab sits inside a frame, matching the grouped Ses
            // tab and the individually-carded toggle below it.
            RectTransform frameRateGroup = EnsureSettingsGroupPanel(tab, "FrameRateGroup", report);
            MigrateChildIfNeeded(frameRateGroup, tab, "FrameRate", report);
            Slider frameRateSlider = EnsureSliderControl(
                frameRateGroup, "FrameRate", "Kare Hızı", font, report,
                out TMP_Text frameRateLabel,
                minValue: 0f, maxValue: 2f, defaultValue: 1f,
                wholeNumbers: true, initialValueText: "60 FPS",
                // "Otomatik" is far longer than the "100%" the default anchors were sized for —
                // a shorter track and a wider value-label region keep the word (and its font,
                // auto-sized within the same range as the row's own name label) from being
                // squeezed down disproportionately small.
                trackEndAnchor: 0.66f, valueLabelStartAnchor: 0.68f);
            RemoveUnexpectedChildren(frameRateGroup, report, "FrameRate");

            Toggle batterySaver = EnsureToggleControl(
                tab, "BatterySaver", "Pil Tasarrufu",
                "Kare hızını düşürerek pil ömrünü uzatır.", font, report);

            GraphicsSettingsPanelView graphicsPanel =
                EnsureSingleComponent<GraphicsSettingsPanelView>(tab.gameObject, report);
            SetObjectProperty(graphicsPanel, "frameRateSlider", frameRateSlider, report);
            SetObjectProperty(graphicsPanel, "frameRateValueLabel", frameRateLabel, report);
            SetObjectProperty(graphicsPanel, "batterySaver", batterySaver, report);

            SetSiblingIndex(frameRateGroup, 2);
            SetSiblingIndex(batterySaver.transform, 3);

            RemoveUnexpectedChildren(tab, report,
                "SectionTitle", "SectionDescription", "FrameRateGroup", "BatterySaver");
            // See ConfigureAudioSettingsTab's matching call: FrameRateGroup is a brand-new nested
            // ContentSizeFitter and needs an explicit rebuild to avoid rendering collapsed.
            LayoutRebuilder.ForceRebuildLayoutImmediate(tab);
            return graphicsPanel;
        }

        private static ControlsSettingsPanelView ConfigureControlsSettingsTab(
            RectTransform scrollContent, TMP_FontAsset font, SceneSetupReport report)
        {
            RectTransform tab = EnsureUiChild(scrollContent, "ControlsTab", report);
            ConfigureTabLayout(tab, report);
            EnsureTabSectionHeader(tab, "Kontroller",
                "Dokunma butonlarını, kaydırma hassasiyetini ve titreşimi ayarlayın.", font, report);

            RectTransform sensitivityGroup = EnsureSettingsGroupPanel(tab, "SensitivityGroup", report);
            MigrateChildIfNeeded(sensitivityGroup, tab, "SwipeSensitivity", report);
            Slider sensitivity = EnsureSliderControl(
                sensitivityGroup, "SwipeSensitivity", "Kaydırma Hassasiyeti", font, report,
                out TMP_Text sensitivityLabel, defaultValue: GameSettings.DefaultSwipeSensitivity,
                // "Kaydırma Hassasiyeti" is far longer than every other row's name ("Ana Ses",
                // "Kare Hızı", ...), so it needs more of the row's width to render at the same
                // font-size range as those labels instead of shrinking to the ellipsis floor.
                labelEndAnchor: 0.56f, trackStartAnchor: 0.58f,
                trackEndAnchor: 0.80f, valueLabelStartAnchor: 0.82f);
            RemoveUnexpectedChildren(sensitivityGroup, report, "SwipeSensitivity");
            Toggle tapButtons = EnsureToggleControl(
                tab, "TapButtonsEnabled", "Dokunma ile Karar Butonları",
                "Kararları kaydırma yerine dokunmatik butonlarla verin.", font, report);
            Toggle invert = EnsureToggleControl(
                tab, "InvertSwipeRotation", "Kaydırma Yönünü Ters Çevir",
                "Kartın eğim yönünü tersine çevirir.", font, report);
            Toggle disableSwipe = EnsureToggleControl(
                tab, "DisableSwipe", "Kaydırmayı Devre Dışı Bırak",
                "Kararları yalnızca dokunma butonlarıyla verin.", font, report);
            Toggle haptics = EnsureToggleControl(tab, "Haptics", "Titreşim",
                "Kart seçimlerinde titreşim.", font, report);

            ControlsSettingsPanelView controlsPanel =
                EnsureSingleComponent<ControlsSettingsPanelView>(tab.gameObject, report);
            SetObjectProperty(controlsPanel, "swipeSensitivity", sensitivity, report);
            SetObjectProperty(controlsPanel, "swipeSensitivityValueLabel", sensitivityLabel, report);
            SetObjectProperty(controlsPanel, "tapButtonsEnabled", tapButtons, report);
            SetObjectProperty(controlsPanel, "invertSwipeRotation", invert, report);
            SetObjectProperty(controlsPanel, "disableSwipe", disableSwipe, report);
            SetObjectProperty(controlsPanel, "haptics", haptics, report);

            SetSiblingIndex(sensitivityGroup, 2);
            SetSiblingIndex(tapButtons.transform, 3);
            SetSiblingIndex(invert.transform, 4);
            SetSiblingIndex(disableSwipe.transform, 5);
            SetSiblingIndex(haptics.transform, 6);

            RemoveUnexpectedChildren(tab, report,
                "SectionTitle", "SectionDescription", "SensitivityGroup",
                "TapButtonsEnabled", "InvertSwipeRotation", "DisableSwipe", "Haptics");
            // See ConfigureAudioSettingsTab's matching call: SensitivityGroup is a brand-new nested
            // ContentSizeFitter and needs an explicit rebuild to avoid rendering collapsed.
            LayoutRebuilder.ForceRebuildLayoutImmediate(tab);
            return controlsPanel;
        }

        private static GeneralSettingsPanelView ConfigureGeneralSettingsTab(
            RectTransform scrollContent, TMP_FontAsset font, SceneSetupReport report,
            out Button resetToDefaultsButton)
        {
            RectTransform tab = EnsureUiChild(scrollContent, "GeneralTab", report);
            ConfigureTabLayout(tab, report);
            EnsureTabSectionHeader(tab, "Genel",
                "Erişilebilirlik seçenekleri ve ilerleme yönetimi.", font, report);

            Toggle reduced = EnsureToggleControl(tab, "ReducedMotion", "Azaltılmış Hareket",
                "Animasyonları ve geçişleri sadeleştirir.", font, report);

            // A three-step slider, same pattern as the Graphics tab's frame-rate picker:
            // 0 = Small, 1 = Normal (default), 2 = Large. Replaces the old three-way
            // Small/Normal/Large toggle row with a single draggable control.
            RectTransform textSizeGroup = EnsureSettingsGroupPanel(tab, "TextSizeGroup", report);
            MigrateChildIfNeeded(textSizeGroup, tab, "TextSize", report);
            Slider textSizeSlider = EnsureSliderControl(
                textSizeGroup, "TextSize", "Metin Boyutu", font, report,
                out TMP_Text textSizeValueLabel,
                minValue: 0f, maxValue: 2f, defaultValue: 1f,
                wholeNumbers: true, initialValueText: "Normal");
            RemoveUnexpectedChildren(textSizeGroup, report, "TextSize");

            Toggle contrast = EnsureToggleControl(tab, "HighContrast", "Yüksek Kontrast",
                "Metin ve arayüz kontrastını artırır.", font, report);

            // Read-only: no in-app localization system exists yet, so this shows the current
            // (only) supported language rather than a non-functional picker.
            RectTransform languageRow = EnsureUiChild(tab, "Language", report);
            SetRect(languageRow, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
                new Vector2(0f, 60f), new Vector2(0.5f, 1f));
            ConfigureLayoutElement(languageRow.gameObject, 60f, report);
            RectTransform languageNameTransform = EnsureUiChild(languageRow, "Label", report);
            SetRect(languageNameTransform, new Vector2(0.05f, 0f), new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero, Center);
            TextMeshProUGUI languageNameLabel = EnsureSingleComponent<TextMeshProUGUI>(
                languageNameTransform.gameObject, report);
            ConfigureReadableText(languageNameLabel, font, 28f, 22f, 30f, true, false, 2f);
            languageNameLabel.alignment = TextAlignmentOptions.MidlineLeft;
            languageNameLabel.text = "Dil";
            RectTransform languageValueTransform = EnsureUiChild(languageRow, "Value", report);
            SetRect(languageValueTransform, new Vector2(0.5f, 0f), new Vector2(0.97f, 1f),
                Vector2.zero, Vector2.zero, Center);
            TextMeshProUGUI languageValue = EnsureSingleComponent<TextMeshProUGUI>(
                languageValueTransform.gameObject, report);
            ConfigureReadableText(languageValue, font, 28f, 22f, 30f, true, false, 2f);
            languageValue.alignment = TextAlignmentOptions.MidlineRight;
            if (string.IsNullOrEmpty(languageValue.text))
            {
                Undo.RecordObject(languageValue, "Set language value placeholder");
                languageValue.text = "Türkçe";
            }

            // "Diğer" groups the settings actions (tutorial replay, resetting preferences to
            // defaults, About, and Reset Progress) as plain navigation rows — same visual weight
            // as any other settings row, not big CTA buttons — so they read as routine actions
            // rather than competing with Uygula for attention.
            EnsureActionSectionLabel(
                tab, "OtherSectionLabel", "Diğer", MenuTitleTextColour, font, report);

            Button resetTutorial = EnsureActionRow(
                tab, "ResetTutorialButton", "Öğreticiyi Tekrar Göster", font, report);

            // Moved here from the panel's bottom action bar: resetting preferences to defaults is
            // an ordinary settings action (and — unlike Uygula/İptal — already saves immediately
            // via SettingsController.ResetToDefaults, so it was never actually part of the
            // Apply/Cancel draft state machine), so it belongs with the other normal actions
            // rather than sitting between Uygula and İptal with equal visual weight.
            resetToDefaultsButton = EnsureActionRow(
                tab, "ResetToDefaultsButton", "Varsayılanlara Dön", font, report);

            Button about = EnsureActionRow(tab, "AboutButton", "Hakkında", font, report);

            // Reset Progress is still destructive and irreversible (the two-tap confirmation
            // below is unchanged, still enforced in GeneralSettingsPanelView), but it no longer
            // gets a separate "Tehlikeli İşlemler" section or a distinct red CTA style — it now
            // sits as an ordinary row directly under Hakkında, identical in shape to the three
            // rows above it. The idle label swaps out for an "armed" confirmation overlay on the
            // first tap exactly as before; only the resting visual shell changed.
            Button resetProgress = EnsureActionRow(
                tab, "ResetProgressButton", "İlerlemeyi Sıfırla", font, report);

            RectTransform resetProgressRoot = resetProgress != null
                ? resetProgress.transform as RectTransform : tab;
            RectTransform resetProgressLabel = FindDirectChild(resetProgressRoot, "Label", report);
            GameObject resetProgressIdleTextObject =
                resetProgressLabel != null ? resetProgressLabel.gameObject : null;

            RectTransform resetProgressArmedText = EnsureUiChild(
                resetProgressRoot, "ArmedText (TMP)", report);
            Stretch(resetProgressArmedText);
            TextMeshProUGUI armedLabel = EnsureSingleComponent<TextMeshProUGUI>(
                resetProgressArmedText.gameObject, report);
            ConfigureText(armedLabel, 30f);
            if (armedLabel != null && string.IsNullOrEmpty(armedLabel.text))
            {
                Undo.RecordObject(armedLabel, "Set armed label text");
                armedLabel.text = "Onaylamak için tekrar dokun";
            }
            ConfigureReadableText(armedLabel, font, 30f, 24f, 34f, true, true, 2f);
            resetProgressArmedText.gameObject.SetActive(false);

            ConfigureMinimumTouchTarget(resetProgress, report);
            ConfigureMinimumTouchTarget(resetTutorial, report);
            ConfigureMinimumTouchTarget(resetToDefaultsButton, report);
            ConfigureMinimumTouchTarget(about, report);

            GeneralSettingsPanelView generalPanel =
                EnsureSingleComponent<GeneralSettingsPanelView>(tab.gameObject, report);
            SetObjectProperty(generalPanel, "reducedMotion", reduced, report);
            SetObjectProperty(generalPanel, "textSizeSlider", textSizeSlider, report);
            SetObjectProperty(generalPanel, "textSizeValueLabel", textSizeValueLabel, report);
            SetObjectProperty(generalPanel, "highContrast", contrast, report);
            SetObjectProperty(generalPanel, "languageValueLabel", languageValue, report);
            SetObjectProperty(generalPanel, "resetProgressButton", resetProgress, report);
            SetObjectProperty(generalPanel, "resetProgressIdleLabel", resetProgressIdleTextObject, report);
            SetObjectProperty(
                generalPanel, "resetProgressArmedLabel", resetProgressArmedText.gameObject, report);
            SetObjectProperty(generalPanel, "resetTutorialButton", resetTutorial, report);
            SetObjectProperty(generalPanel, "aboutButton", about, report);

            // A scene authored across several passes can leave these at whatever sibling index
            // they first got created at, independent of where this method now logically places
            // them (EnsureUiChild reuses an existing child in place rather than moving it). Pin
            // the intended reading order explicitly: every accessibility/general control first,
            // then all four "Diğer" action rows in order, Reset Progress last among them.
            SetSiblingIndex(reduced.transform, 2);
            SetSiblingIndex(textSizeGroup, 3);
            SetSiblingIndex(contrast.transform, 4);
            SetSiblingIndex(languageRow, 5);
            SetSiblingIndex(FindDirectChild(tab, "OtherSectionLabel", report), 6);
            SetSiblingIndex(resetTutorial.transform, 7);
            SetSiblingIndex(resetToDefaultsButton.transform, 8);
            SetSiblingIndex(about.transform, 9);
            SetSiblingIndex(resetProgress.transform, 10);

            RemoveUnexpectedChildren(tab, report,
                "SectionTitle", "SectionDescription", "ReducedMotion", "TextSizeGroup",
                "HighContrast", "Language",
                "OtherSectionLabel", "ResetTutorialButton", "ResetToDefaultsButton", "AboutButton",
                "ResetProgressButton");
            // See ConfigureAudioSettingsTab's matching call: TextSizeGroup is a brand-new nested
            // ContentSizeFitter and needs an explicit rebuild to avoid rendering collapsed.
            LayoutRebuilder.ForceRebuildLayoutImmediate(tab);
            return generalPanel;
        }

        /// <summary>Finds the "Text (TMP)" child <see cref="EnsureButtonText"/> creates.</summary>
        private static GameObject FindChildText(Transform parent, SceneSetupReport report)
        {
            RectTransform text = FindDirectChild(parent, "Text (TMP)", report);
            return text != null ? text.gameObject : null;
        }

        private static Slider EnsureSliderControl(
            RectTransform parent,
            string name,
            string labelText,
            TMP_FontAsset font,
            SceneSetupReport report,
            out TMP_Text valueLabel,
            float minValue = 0f,
            float maxValue = 1f,
            float defaultValue = GameSettings.DefaultVolume,
            bool wholeNumbers = false,
            string initialValueText = null,
            float labelEndAnchor = 0.36f,
            float trackStartAnchor = 0.40f,
            float trackEndAnchor = 0.78f,
            float valueLabelStartAnchor = 0.80f)
        {
            RectTransform root = EnsureUiChild(parent, name, report);
            SetRect(root, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
                new Vector2(0f, 108f), new Vector2(0.5f, 1f));
            ConfigureLayoutElement(root.gameObject, 108f, report);
            // Transparent but still raycastable, so the whole row remains a drag target without
            // painting a card background behind it — removes the boxed-row look at the user's
            // request, leaving the panel's own background visible between rows. RemoveStaleComponents
            // drops the Outline a previous pass's ConfigureRowCard call left behind.
            RemoveStaleComponents<Outline>(root.gameObject);
            Image background = EnsureSingleComponent<Image>(root.gameObject, report);
            ConfigureSimpleImage(background, LoadBuiltInUiSprite(report), Color.clear, true);
            Slider slider = EnsureSingleComponent<Slider>(root.gameObject, report);
            // Label, track and the trailing value readout are proportional splits of the row (not
            // fixed pixel offsets) so none of them can run off-screen on a narrower-than-reference-
            // width safe area.
            RectTransform track = EnsureUiChild(root, "Track", report);
            SetRect(track, new Vector2(trackStartAnchor, 0.30f), new Vector2(trackEndAnchor, 0.70f),
                Vector2.zero, Vector2.zero, Center);
            ConfigureRoundedFill(
                track.gameObject, MenuTrackGrooveColour, 40f, false, report);
            RectTransform fillArea = EnsureUiChild(root, "FillArea", report);
            SetRect(fillArea, new Vector2(trackStartAnchor, 0.30f), new Vector2(trackEndAnchor, 0.70f),
                Vector2.zero, Vector2.zero, Center);
            RectTransform fillTransform = EnsureUiChild(fillArea, "Fill", report);
            Stretch(fillTransform);
            // Same single accent colour as the active tab — a single-accent theme has no reason to
            // introduce a second highlight tone for the slider fill.
            ConfigureRoundedFill(
                fillTransform.gameObject, SettingsPanelTheme.ActiveTabColour, 40f, false, report);
            // Inset 48 units (the handle's own width) from the track's ends: Slider moves the
            // handle's *centre* across this area's full width, so without the inset the handle
            // would overhang half its own width past the track at 0%/100% — visibly overlapping
            // the value label at full volume, exactly the "unprofessional" edge case being fixed.
            RectTransform handleArea = EnsureUiChild(root, "HandleArea", report);
            SetRect(handleArea, new Vector2(trackStartAnchor, 0.15f), new Vector2(trackEndAnchor, 0.85f),
                Vector2.zero, new Vector2(-48f, 0f), Center);
            RectTransform handleTransform = EnsureUiChild(handleArea, "Handle", report);
            SetRect(handleTransform, Center, Center, Vector2.zero, new Vector2(48f, 48f), Center);
            ProceduralRoundedRectGraphic handle = ConfigureRoundedFill(
                handleTransform.gameObject, SettingsPanelTheme.InactiveTabTextColour, 40f, true,
                report);
            // A thin gold ring so the handle reads as a distinct, grabbable control against
            // whichever colour (groove or accent fill) happens to sit behind it at a given value,
            // instead of just a flat tan dot with no edge definition.
            Outline handleOutline = EnsureSingleComponent<Outline>(handleTransform.gameObject, report);
            if (handleOutline != null)
            {
                Undo.RecordObject(handleOutline, "Configure slider handle outline");
                handleOutline.effectColor = SettingsPanelTheme.BorderGoldColour;
                handleOutline.effectDistance = new Vector2(1.5f, -1.5f);
                handleOutline.useGraphicAlpha = false;
            }
            SetSiblingIndex(track, 0);
            SetSiblingIndex(fillArea, 1);
            SetSiblingIndex(handleArea, 2);
            RectTransform labelTransform = EnsureUiChild(root, "Label", report);
            SetRect(labelTransform, new Vector2(0.05f, 0f), new Vector2(labelEndAnchor, 1f),
                Vector2.zero, Vector2.zero, Center);
            TextMeshProUGUI label = EnsureSingleComponent<TextMeshProUGUI>(
                labelTransform.gameObject, report);
            ConfigureReadableText(label, font, 32f, 26f, 34f, true, false, 2f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.text = labelText;
            // Explicit, not left on whatever default/stale colour the TMP component happened to
            // carry — the same class of oversight already found and fixed on the toggle's track.
            SetTextColour(label, MenuTitleTextColour);

            // Trailing percentage readout, e.g. "80%" — updated at runtime by the owning panel
            // view's Render()/onValueChanged, never written here beyond an initial placeholder.
            RectTransform valueLabelTransform = EnsureUiChild(root, "ValueLabel", report);
            SetRect(valueLabelTransform, new Vector2(valueLabelStartAnchor, 0f), new Vector2(0.98f, 1f),
                Vector2.zero, Vector2.zero, Center);
            TextMeshProUGUI valueText = EnsureSingleComponent<TextMeshProUGUI>(
                valueLabelTransform.gameObject, report);
            ConfigureReadableText(valueText, font, 32f, 24f, 34f, true, false, 2f);
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            if (string.IsNullOrEmpty(valueText.text))
            {
                Undo.RecordObject(valueText, "Set slider value label placeholder");
                valueText.text = initialValueText
                    ?? Mathf.RoundToInt(Mathf.Clamp01(defaultValue) * 100f) + "%";
            }
            SetTextColour(valueText, MenuTitleTextColour);
            valueLabel = valueText;
            // Sweeps the now-removed diamond icon (and any other stale leftovers) — this row never
            // had an allowlist before, so an old pass's orphan would otherwise persist forever.
            RemoveUnexpectedChildren(root, report, "Track", "FillArea", "HandleArea", "Label", "ValueLabel");

            if (slider != null)
            {
                Undo.RecordObject(slider, "Configure settings slider");
                slider.minValue = minValue;
                slider.maxValue = maxValue;
                slider.wholeNumbers = wholeNumbers;
                slider.value = defaultValue;
                slider.fillRect = fillTransform;
                slider.handleRect = handleTransform;
                slider.targetGraphic = handle;
                slider.direction = Slider.Direction.LeftToRight;
                // Explicit, not left implicit — Unity's own built-in ColorBlock defaults (a gentle
                // dim on press/highlight) already read fine against the handle's tan fill, so
                // there's nothing to override here; this just states that on purpose rather than
                // leaving it to whatever Selectable happened to default to.
                slider.transition = Selectable.Transition.ColorTint;
            }
            return slider;
        }

        private static Toggle EnsureToggleControl(
            RectTransform parent,
            string name,
            string labelText,
            string description,
            TMP_FontAsset font,
            SceneSetupReport report)
        {
            RectTransform root = EnsureUiChild(parent, name, report);
            SetRect(root, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
                new Vector2(0f, 132f), new Vector2(0.5f, 1f));
            ConfigureLayoutElement(root.gameObject, 132f, report);
            // Each toggle is its own single-row group card — a warm card fill and gold border,
            // matching the reference's individually-framed Sessiz Mod/Titreşim rows — rather than
            // the plain transparent row every slider/action row uses. ConfigureRoundedFill drops
            // the plain raycast-only Image an earlier pass left here.
            ConfigureRoundedFill(root.gameObject, SettingsPanelTheme.InactiveTabColour, 28f, true,
                report);
            Outline cardOutline = EnsureSingleComponent<Outline>(root.gameObject, report);
            if (cardOutline != null)
            {
                Undo.RecordObject(cardOutline, "Configure toggle card border");
                cardOutline.effectColor = SettingsPanelTheme.BorderGoldColour;
                cardOutline.effectDistance = new Vector2(1.5f, -1.5f);
                cardOutline.useGraphicAlpha = false;
            }
            Toggle toggle = EnsureSingleComponent<Toggle>(root.gameObject, report);

            // A real sliding pill switch (track + knob), moved to the row's right edge (was the
            // left) to match title+description reading left-to-right with the switch as the
            // trailing control, the same reading order as every slider row.
            RectTransform track = EnsureUiChild(root, "Track", report);
            SetRect(track, new Vector2(0.80f, 0.32f), new Vector2(0.94f, 0.68f),
                Vector2.zero, Vector2.zero, Center);
            ProceduralRoundedRectGraphic trackGraphic = ConfigureRoundedFill(
                track.gameObject, MenuTrackGrooveColour, 40f, false, report);

            RectTransform knob = EnsureUiChild(track, "Knob", report);
            SetRect(knob, Center, Center, Vector2.zero, new Vector2(40f, 40f), Center);
            ConfigureRoundedFill(
                knob.gameObject, SettingsPanelTheme.InactiveTabTextColour, 40f, false, report);

            RemoveUnexpectedChildren(root, report, "Track", "Title", "Description");

            // Title (bold, upper half) + a muted description line below it (matching the section
            // header's title/description pairing) — proportional splits, not fixed pixel offsets,
            // so neither can run off-screen on a narrower-than-reference-width safe area.
            RectTransform titleTransform = EnsureUiChild(root, "Title", report);
            SetRect(titleTransform, new Vector2(0.05f, 0.52f), new Vector2(0.76f, 1f),
                Vector2.zero, Vector2.zero, Center);
            TextMeshProUGUI title = EnsureSingleComponent<TextMeshProUGUI>(
                titleTransform.gameObject, report);
            ConfigureReadableText(title, font, 28f, 22f, 30f, true, false, 2f);
            title.alignment = TextAlignmentOptions.BottomLeft;
            title.fontStyle = FontStyles.Bold;
            title.text = labelText;

            RectTransform descriptionTransform = EnsureUiChild(root, "Description", report);
            SetRect(descriptionTransform, new Vector2(0.05f, 0f), new Vector2(0.76f, 0.46f),
                Vector2.zero, Vector2.zero, Center);
            TextMeshProUGUI descriptionText = EnsureSingleComponent<TextMeshProUGUI>(
                descriptionTransform.gameObject, report);
            ConfigureReadableText(descriptionText, font, 20f, 16f, 22f, true, true, 2f);
            descriptionText.alignment = TextAlignmentOptions.TopLeft;
            descriptionText.text = description;
            SetTextColour(descriptionText, MenuMutedTextColour);

            ToggleSwitchVisual visual = EnsureSingleComponent<ToggleSwitchVisual>(
                root.gameObject, report);
            SetObjectProperty(visual, "track", trackGraphic, report);
            SetObjectProperty(visual, "knob", knob, report);
            // The component's own [SerializeField] defaults are stale gold/navy from before this
            // re-theme; without setting these explicitly every toggle would still flash the old
            // gold the instant it's switched on. offColour is MenuTrackGrooveColour, not
            // InactiveTabColour, for the same reason the slider track uses it: a dark, distinct
            // "sunken groove" tone the knob visibly sits inside, rather than an off toggle reading
            // as a bare knob with no visible track at all.
            SetColorProperty(visual, "onColour", SettingsPanelTheme.ActiveTabColour, report);
            SetColorProperty(visual, "offColour", MenuTrackGrooveColour, report);

            if (toggle != null)
            {
                Undo.RecordObject(toggle, "Configure settings toggle");
                toggle.targetGraphic = trackGraphic;
                toggle.graphic = null;
                toggle.isOn = false;
            }
            return toggle;
        }

        /// <summary>
        /// A low-emphasis, tappable settings row: label on the left, a trailing chevron on the
        /// right — same transparent row shell (proportional splits, no card) as
        /// <see cref="EnsureSliderControl"/>, but for a normal navigation/action item (Öğreticiyi
        /// Tekrar Göster, Varsayılanlara Dön, Hakkında) instead of a big filled CTA button, so it
        /// reads as "an ordinary settings action" rather than competing visually with Uygula/İptal
        /// or the destructive İlerlemeyi Sıfırla button.
        /// </summary>
        private static Button EnsureActionRow(
            RectTransform parent, string name, string labelText, TMP_FontAsset font,
            SceneSetupReport report)
        {
            RectTransform root = EnsureUiChild(parent, name, report);
            SetRect(root, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
                new Vector2(0f, 92f), new Vector2(0.5f, 1f));
            ConfigureLayoutElement(root.gameObject, 92f, report);
            // This object may already exist from an earlier authoring pass as a big filled CTA
            // button (a ProceduralRoundedRectGraphic fill, via EnsureMenuButton); strip that down
            // to the plain row shell below instead of layering the new look on top of the old one.
            RemoveStaleComponents<ProceduralRoundedRectGraphic>(root.gameObject);
            // No card background, matching every other row. A faint (not fully transparent) fill
            // so Button's built-in pressed/highlighted colour multiply still has something non-zero
            // to darken — this row's only touch feedback, since there's no separate switch/pill
            // graphic like the toggle rows have.
            RemoveStaleComponents<Outline>(root.gameObject);
            Image background = EnsureSingleComponent<Image>(root.gameObject, report);
            ConfigureSimpleImage(background, null, new Color(1f, 1f, 1f, 0.04f), true);
            Button button = EnsureSingleComponent<Button>(root.gameObject, report);

            RectTransform labelTransform = EnsureUiChild(root, "Label", report);
            SetRect(labelTransform, new Vector2(0.05f, 0f), new Vector2(0.85f, 1f),
                Vector2.zero, Vector2.zero, Center);
            TextMeshProUGUI label = EnsureSingleComponent<TextMeshProUGUI>(
                labelTransform.gameObject, report);
            ConfigureReadableText(label, font, 30f, 24f, 32f, true, false, 2f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.text = labelText;

            RectTransform chevronTransform = EnsureUiChild(root, "Chevron", report);
            SetRect(chevronTransform, new Vector2(0.88f, 0f), new Vector2(0.97f, 1f),
                Vector2.zero, Vector2.zero, Center);
            TextMeshProUGUI chevron = EnsureSingleComponent<TextMeshProUGUI>(
                chevronTransform.gameObject, report);
            ConfigureText(chevron, 30f);
            if (font != null)
            {
                Undo.RecordObject(chevron, "Set action row chevron font");
                chevron.font = font;
            }
            chevron.alignment = TextAlignmentOptions.MidlineRight;
            if (string.IsNullOrEmpty(chevron.text))
            {
                Undo.RecordObject(chevron, "Set action row chevron");
                chevron.text = ">";
            }
            SetTextColour(chevron, MenuMutedTextColour);

            // Drops the old "Text (TMP)" child EnsureMenuButton left behind when this object used
            // to be a big filled CTA button, if it was ever authored as one.
            RemoveUnexpectedChildren(root, report, "Label", "Chevron");

            if (button != null)
            {
                Undo.RecordObject(button, "Configure settings action row");
                button.targetGraphic = background;
            }
            return button;
        }

        /// <summary>A small left-aligned caption introducing a group of action rows below it.</summary>
        private static void EnsureActionSectionLabel(
            RectTransform parent, string name, string labelText, Color tint, TMP_FontAsset font,
            SceneSetupReport report)
        {
            RectTransform row = EnsureUiChild(parent, name, report);
            SetRect(row, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
                new Vector2(0f, 40f), new Vector2(0.5f, 1f));
            ConfigureLayoutElement(row.gameObject, 40f, report);
            TextMeshProUGUI text = EnsureSingleComponent<TextMeshProUGUI>(row.gameObject, report);
            ConfigureReadableText(text, font, 24f, 20f, 26f, true, false, 2f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.text = labelText;
            SetTextColour(text, tint);
        }

        // Validation -----------------------------------------------------------------

        private static void ValidateGameScene(
            Scene scene,
            ContentCatalogue catalogue,
            SessionIntent intent,
            SceneSetupReport report)
        {
            InterfaceTextDefinition interfaceText =
                AssetDatabase.LoadAssetAtPath<InterfaceTextDefinition>(InterfaceTextPath);
            TMP_FontAsset turkishFont =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TurkishFontPath);
            ValidateTurkishTextAssets(scene, turkishFont, report);

            GameObject canvas = RequirePath(scene, "/UICanvas", report);
            RequirePath(scene, "/Main Camera", report);
            GameObject eventSystem = RequirePath(scene, "/EventSystem", report);
            GameObject safeArea = RequirePath(scene, "/UICanvas/SafeArea", report);
            GameObject backgroundObject = RequirePath(scene, "/UICanvas/Background", report);
            GameObject transitionOverlayObject = RequirePath(
                scene, "/UICanvas/TransitionOverlay", report);
            GameObject hudObject = RequirePath(scene, "/UICanvas/SafeArea/HUD", report);
            GameObject contentPanelObject = RequirePath(
                scene, "/UICanvas/SafeArea/ContentPanel", report);
            GameObject cardArea = RequirePath(scene, "/UICanvas/SafeArea/CardArea", report);
            GameObject cardObject = RequirePath(scene, "/UICanvas/SafeArea/CardArea/Card", report);
            GameObject panel = RequirePath(scene, "/UICanvas/SafeArea/GameOverPanel", report);
            GameObject audioObject = RequirePath(scene, "/AudioService", report);
            GameObject controllerObject = RequirePath(scene, "/GameSceneController", report);
            GameObject footerObject = RequirePath(scene, "/UICanvas/SafeArea/Footer", report);
            GameObject tapChoicesObject = RequirePath(
                scene, "/UICanvas/SafeArea/TapChoiceButtons", report);
            GameObject tapChoicesLeftObject = RequirePath(
                scene, "/UICanvas/SafeArea/TapChoiceButtons/LeftChoiceButton", report);
            GameObject tapChoicesRightObject = RequirePath(
                scene, "/UICanvas/SafeArea/TapChoiceButtons/RightChoiceButton", report);
            GameObject tutorialObject = RequirePath(
                scene, "/UICanvas/SafeArea/TutorialOverlay", report);

            if (canvas != null)
            {
                CanvasScaler scaler = RequireSingleComponent<CanvasScaler>(canvas, scene.path, report);
                Canvas canvasComponent = RequireSingleComponent<Canvas>(canvas, scene.path, report);
                RequireSingleComponent<GraphicRaycaster>(canvas, scene.path, report);
                if (canvasComponent != null
                    && (canvasComponent.renderMode != RenderMode.ScreenSpaceOverlay
                        || canvasComponent.pixelPerfect))
                {
                    AddInvalid(report, scene.path, "/UICanvas", "Canvas settings are incorrect.");
                }
                if (scaler != null
                    && (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
                        || scaler.referenceResolution != new Vector2(1080f, 1920f)
                        || !Mathf.Approximately(scaler.matchWidthOrHeight, 1f)))
                {
                    AddInvalid(report, scene.path, "/UICanvas", "CanvasScaler settings are incorrect.");
                }
            }

            if (eventSystem != null)
            {
                RequireSingleComponent<EventSystem>(eventSystem, scene.path, report);
                RequireSingleComponent<InputSystemUIInputModule>(eventSystem, scene.path, report);
            }

            if (safeArea != null)
            {
                RequireSingleComponent<SafeAreaFitter>(safeArea, scene.path, report);
            }
            if (hudObject != null)
            {
                RectTransform rect = hudObject.transform as RectTransform;
                HorizontalLayoutGroup layout = RequireSingleComponent<HorizontalLayoutGroup>(
                    hudObject, scene.path, report);
                if (rect == null || !Mathf.Approximately(rect.sizeDelta.y, 208f)
                    || layout == null || !Mathf.Approximately(layout.spacing, 0f)
                    || layout.padding.left != 64 || layout.padding.right != 64)
                {
                    AddInvalid(report, scene.path, "/UICanvas/SafeArea/HUD",
                        "HUD height, padding, or spacing differs from the managed phone layout.");
                }
            }

            if (backgroundObject != null)
            {
                BackgroundView background = RequireSingleComponent<BackgroundView>(
                    backgroundObject, scene.path, report);
                ValidateNonRaycastImage(scene, "/UICanvas/Background", report);
                ValidateNonRaycastImage(scene, "/UICanvas/Background/Artwork", report);
                ValidateNonRaycastImage(scene, "/UICanvas/Background/DarkOverlay", report);
                ValidateNonRaycastImage(scene, "/UICanvas/Background/Vignette", report);
                GameObject proceduralObject = RequirePath(
                    scene, "/UICanvas/Background/ProceduralVignette", report);
                ProceduralVignetteGraphic procedural = proceduralObject != null
                    ? RequireSingleComponent<ProceduralVignetteGraphic>(
                        proceduralObject, scene.path, report)
                    : null;
                if (procedural != null && procedural.raycastTarget)
                {
                    AddInvalid(report, scene.path,
                        "/UICanvas/Background/ProceduralVignette",
                        "Procedural vignette must not block raycasts.");
                }
                ValidateReference(background, "proceduralVignette", procedural, scene.path,
                    "/UICanvas/Background", report);
            }

            StatItemView[] statItems = new StatItemView[4];
            string[] itemNames =
            {
                "StatItem_People", "StatItem_Security", "StatItem_Authority", "StatItem_Wealth"
            };
            string[] slotNames =
            {
                "StatSlot_People", "StatSlot_Security", "StatSlot_Authority", "StatSlot_Wealth"
            };
            StatType[] stats =
            {
                StatType.People, StatType.Security, StatType.Authority, StatType.Wealth
            };
            Sprite uiSprite = LoadBuiltInUiSprite(report);
            for (int i = 0; i < itemNames.Length; i++)
            {
                string slotPath = "/UICanvas/SafeArea/HUD/" + slotNames[i];
                GameObject slotObject = RequirePath(scene, slotPath, report);
                string itemPath = slotPath + "/" + itemNames[i];
                GameObject itemObject = RequirePath(scene, itemPath, report);
                GameObject fillObject = RequirePath(scene, itemPath + "/Fill", report);
                GameObject iconObject = RequirePath(scene, slotPath + "/Icon", report);
                GameObject iconFallbackObject = RequirePath(scene, slotPath + "/IconFallback", report);
                GameObject nameObject = RequirePath(scene, slotPath + "/Name", report);
                GameObject valueObject = RequirePath(scene, slotPath + "/Value", report);
                GameObject impactObject = RequirePath(scene, slotPath + "/Impact", report);
                GameObject criticalObject = RequirePath(scene, slotPath + "/Critical", report);
                if (slotObject == null || itemObject == null || fillObject == null)
                {
                    continue;
                }

                StatItemView item = RequireSingleComponent<StatItemView>(
                    itemObject, scene.path, report);
                Image background = RequireSingleComponent<Image>(
                    itemObject, scene.path, report);
                Image fill = RequireSingleComponent<Image>(fillObject, scene.path, report);
                statItems[i] = item;
                if (item != null && item.Stat != stats[i])
                {
                    AddInvalid(report, scene.path, itemPath,
                        "Stat type is incorrect.");
                }
                if (item != null && GetObjectProperty(item, "fillImage") != fill)
                {
                    AddInvalid(report, scene.path, itemPath,
                        "Fill reference must point to this item's own child Fill Image.");
                }
                if (item != null && (GetObjectProperty(item, "iconImage")
                        != (iconObject != null ? iconObject.GetComponent<Image>() : null)
                    || GetObjectProperty(item, "iconFallbackLabel")
                        != (iconFallbackObject != null ? iconFallbackObject.GetComponent<TMP_Text>() : null)
                    || GetObjectProperty(item, "label")
                        != (nameObject != null ? nameObject.GetComponent<TMP_Text>() : null)
                    || GetObjectProperty(item, "valueText")
                        != (valueObject != null ? valueObject.GetComponent<TMP_Text>() : null)
                    || GetObjectProperty(item, "impactLabel")
                        != (impactObject != null ? impactObject.GetComponent<TMP_Text>() : null)
                    || GetObjectProperty(item, "criticalLabel")
                        != (criticalObject != null ? criticalObject.GetComponent<TMP_Text>() : null)))
                {
                    AddInvalid(report, scene.path, itemPath,
                        "Semantic stat references are incomplete or point outside their slot.");
                }
                if (slotObject.transform.GetSiblingIndex() != i)
                {
                    AddInvalid(report, scene.path, slotPath,
                        "HUD semantic visual order must be People, Security, Authority, Wealth.");
                }
                RectTransform itemRect = itemObject.transform as RectTransform;
                if (itemRect == null || !Mathf.Approximately(itemRect.sizeDelta.y, 20f))
                {
                    AddInvalid(report, scene.path, itemPath,
                        "HUD stat bar height must be 20 reference units.");
                }
                Outline itemOutline = itemObject.GetComponent<Outline>();
                if (itemOutline == null || !ColoursMatch(itemOutline.effectColor, StatBarBorderColour))
                {
                    AddInvalid(report, scene.path, itemPath,
                        "HUD stat bar must have a gold Outline frame.");
                }
                if (background != null && (background.sprite != uiSprite
                    || background.type != Image.Type.Simple
                    || background.raycastTarget
                    || !ColoursMatch(background.color, StatBackgroundColour)))
                {
                    AddInvalid(report, scene.path, itemPath,
                        "Stat background must use the built-in UISprite, Simple type, no raycast, "
                        + "and the managed background colour.");
                }
                if (fill == null)
                {
                    continue;
                }
                if (fill.sprite == null)
                {
                    AddInvalid(report, scene.path, itemPath + "/Fill",
                        "Fill Image sprite must not be null.");
                }
                else if (uiSprite != null && fill.sprite != uiSprite)
                {
                    AddInvalid(report, scene.path, itemPath + "/Fill",
                        "Fill Image must use Unity's built-in UISprite.");
                }
                if (fill.type != Image.Type.Filled)
                {
                    AddInvalid(report, scene.path, itemPath + "/Fill",
                        "Fill Image Type must be Filled.");
                }
                if (fill.fillMethod != Image.FillMethod.Horizontal)
                {
                    AddInvalid(report, scene.path, itemPath + "/Fill",
                        "Fill Image method must be Horizontal.");
                }
                if (fill.fillOrigin != (int)Image.OriginHorizontal.Left)
                {
                    AddInvalid(report, scene.path, itemPath + "/Fill",
                        "Fill Image origin must be Left.");
                }
                if (fill.preserveAspect)
                {
                    AddInvalid(report, scene.path, itemPath + "/Fill",
                        "Fill Image Preserve Aspect must be disabled.");
                }
                if (fill.raycastTarget)
                {
                    AddInvalid(report, scene.path, itemPath + "/Fill",
                        "Fill Image Raycast Target must be disabled.");
                }
                if (!ColoursMatch(fill.color, StatFillColours[i]))
                {
                    AddInvalid(report, scene.path, itemPath + "/Fill",
                        "Fill Image colour does not match its managed stat colour.");
                }
                RectTransform fillTransform = fill.transform as RectTransform;
                if (fillTransform == null || fillTransform.anchorMin != Vector2.zero
                    || fillTransform.anchorMax != Vector2.one
                    || fillTransform.offsetMin != Vector2.zero
                    || fillTransform.offsetMax != Vector2.zero)
                {
                    AddInvalid(report, scene.path, itemPath + "/Fill",
                        "Fill RectTransform must be fully stretched with zero offsets.");
                }
            }

            HUDView hud = hudObject != null
                ? RequireSingleComponent<HUDView>(hudObject, scene.path, report)
                : null;
            ValidateReference(hud, "interfaceText", interfaceText, scene.path,
                "/UICanvas/SafeArea/HUD", report);
            if (hud != null && !hud.TryValidate(out string hudMessage))
            {
                AddInvalid(report, scene.path, "/UICanvas/SafeArea/HUD", hudMessage);
            }

            CardView card = cardObject != null
                ? RequireSingleComponent<CardView>(cardObject, scene.path, report)
                : null;
            CardSwipeController swipe = cardObject != null
                ? RequireSingleComponent<CardSwipeController>(cardObject, scene.path, report)
                : null;
            Image cardImage = cardObject != null ? cardObject.GetComponent<Image>() : null;
            if (cardObject != null && (cardImage == null || !cardImage.raycastTarget))
            {
                AddInvalid(report, scene.path, "/UICanvas/SafeArea/CardArea/Card",
                    "Card needs a raycast-enabled Image.");
            }
            if (cardArea != null)
            {
                ResponsiveCardSizer sizer = RequireSingleComponent<ResponsiveCardSizer>(
                    cardArea, scene.path, report);
                if (sizer != null
                    && (GetObjectProperty(sizer, "widthReference")
                            != (contentPanelObject != null ? contentPanelObject.transform : null)
                        || !Mathf.Approximately(GetFloatProperty(sizer, "preferredWidthRatio"), 0.84f)
                        || !Mathf.Approximately(GetFloatProperty(sizer, "maximumWidth"), 960f)
                        || !Mathf.Approximately(GetFloatProperty(sizer, "topPadding"), 12f)))
                {
                    AddInvalid(report, scene.path, "/UICanvas/SafeArea/CardArea",
                        "Responsive card sizing reference, phone ratio, tablet cap, or top padding "
                        + "is incorrect.");
                }
                RectTransform areaRect = cardArea.transform as RectTransform;
                if (areaRect == null || areaRect.anchoredPosition != new Vector2(0f, -210f)
                    || areaRect.sizeDelta != new Vector2(-40f, -580f))
                {
                    AddInvalid(report, scene.path, "/UICanvas/SafeArea/CardArea",
                        "CardArea margins or HUD/SituationArea/footer reservations are incorrect.");
                }
            }
            // CardBack is fixed behind PortraitSwipeRoot, at the same bounds it occupies at rest;
            // PortraitSwipeRoot is the only part of the decision card that moves during a swipe.
            // CardBack itself is now the rounded Mask container; the actual Card.png Image is one
            // level deeper, on the CardBackArt child it clips.
            GameObject cardBackObject = RequirePath(
                scene, "/UICanvas/SafeArea/CardArea/Card/CardBack", report);
            GameObject cardBackArtObject = RequirePath(
                scene, "/UICanvas/SafeArea/CardArea/Card/CardBack/CardBackArt", report);
            if (cardBackObject != null)
            {
                RequireSingleComponent<Mask>(cardBackObject, scene.path, report);
                RequireSingleComponent<ProceduralRoundedRectGraphic>(cardBackObject, scene.path, report);
            }
            GameObject portraitSwipeRootObject = RequirePath(
                scene, "/UICanvas/SafeArea/CardArea/Card/PortraitSwipeRoot", report);
            if (portraitSwipeRootObject != null)
            {
                RequireSingleComponent<RectMask2D>(portraitSwipeRootObject, scene.path, report);
            }
            GameObject portraitMaskObject = RequirePath(
                scene, "/UICanvas/SafeArea/CardArea/Card/PortraitSwipeRoot/PortraitMask", report);
            if (portraitMaskObject != null)
            {
                RequireSingleComponent<Mask>(portraitMaskObject, scene.path, report);
                RequireSingleComponent<ProceduralRoundedRectGraphic>(
                    portraitMaskObject, scene.path, report);
            }
            RequirePath(
                scene,
                "/UICanvas/SafeArea/CardArea/Card/PortraitSwipeRoot/PortraitMask/Portrait",
                report);
            GameObject fallbackObject = RequirePath(scene,
                "/UICanvas/SafeArea/CardArea/Card/PortraitSwipeRoot/PortraitMask/FallbackSilhouette",
                report);
            PortraitFallbackView portraitFallback = fallbackObject != null
                ? RequireSingleComponent<PortraitFallbackView>(fallbackObject, scene.path, report)
                : null;
            if (card != null && GetObjectProperty(card, "portraitFallbackView") != portraitFallback)
            {
                AddInvalid(report, scene.path, "/UICanvas/SafeArea/CardArea/Card",
                    "Card portrait fallback reference is incorrect.");
            }
            if (card != null
                && (GetObjectProperty(card, "cardRoot") != portraitSwipeRootObject?.transform
                    || GetObjectProperty(card, "cardBackImage")
                        != (cardBackArtObject != null ? cardBackArtObject.GetComponent<Image>() : null)))
            {
                AddInvalid(report, scene.path, "/UICanvas/SafeArea/CardArea/Card",
                    "CardView must drag PortraitSwipeRoot, not the fixed Card shell, and must be "
                    + "wired to CardBack.");
            }
            // The old picture-frame shell, its temporary-border fallback, and the sharp-outline
            // fallback it used are retired in favour of CardBack + a masked near-square portrait.
            if (cardObject != null)
            {
                string[] retiredChildren =
                {
                    "Frame", "TemporaryBorder", "CornerTopLeft", "CornerTopRight",
                    "CornerBottomLeft", "CornerBottomRight", "BodyScrim"
                };
                for (int i = 0; i < retiredChildren.Length; i++)
                {
                    if (FindDirectChild(cardObject.transform, retiredChildren[i], report) != null)
                    {
                        AddInvalid(report, scene.path,
                            "/UICanvas/SafeArea/CardArea/Card/" + retiredChildren[i],
                            "Retired frame-shell child must not remain on Card.");
                    }
                }
                if (cardObject.GetComponent<Outline>() != null)
                {
                    AddInvalid(report, scene.path, "/UICanvas/SafeArea/CardArea/Card",
                        "Retired temporary card outline must not remain on Card.");
                }
            }
            ValidateTextColour(scene, "/UICanvas/SafeArea/CardArea/Card/Speaker",
                SpeakerTextColour, report);
            RequirePath(scene, "/UICanvas/SafeArea/CardArea/Card/NameScrim", report);
            if (cardObject != null && FindDirectChild(cardObject.transform, "Body", report) != null)
            {
                AddInvalid(report, scene.path, "/UICanvas/SafeArea/CardArea/Card/Body",
                    "The situation text moved to SituationArea; Card/Body must not remain.");
            }
            ValidateTextColour(scene, "/UICanvas/SafeArea/SituationArea/SituationPanel/SituationText",
                SituationTextColour, report);
            GameObject situationPanelObject = RequirePath(
                scene, "/UICanvas/SafeArea/SituationArea/SituationPanel", report);
            ProceduralRoundedRectGraphic situationFallback = situationPanelObject != null
                ? RequireSingleComponent<ProceduralRoundedRectGraphic>(
                    situationPanelObject, scene.path, report)
                : null;
            GameObject situationArtworkObject = RequirePath(
                scene, "/UICanvas/SafeArea/SituationArea/SituationPanel/Artwork", report);
            Image situationArtwork = situationArtworkObject != null
                ? RequireSingleComponent<Image>(situationArtworkObject, scene.path, report)
                : null;
            if (swipe != null && (GetObjectProperty(swipe, "cardView") != card
                || GetObjectProperty(swipe, "dragParent")
                    != (cardArea != null ? cardArea.transform : null)))
            {
                AddInvalid(report, scene.path, "/UICanvas/SafeArea/CardArea/Card",
                    "CardSwipeController references are incorrect.");
            }

            TapChoiceButtonsView tapChoices = tapChoicesObject != null
                ? RequireSingleComponent<TapChoiceButtonsView>(tapChoicesObject, scene.path, report)
                : null;
            Button tapChoicesLeft = tapChoicesLeftObject != null
                ? RequireSingleComponent<Button>(tapChoicesLeftObject, scene.path, report)
                : null;
            Button tapChoicesRight = tapChoicesRightObject != null
                ? RequireSingleComponent<Button>(tapChoicesRightObject, scene.path, report)
                : null;
            ValidateReference(tapChoices, "swipeController", swipe, scene.path,
                "/UICanvas/SafeArea/TapChoiceButtons", report);
            ValidateReference(tapChoices, "leftButton", tapChoicesLeft, scene.path,
                "/UICanvas/SafeArea/TapChoiceButtons", report);
            ValidateReference(tapChoices, "rightButton", tapChoicesRight, scene.path,
                "/UICanvas/SafeArea/TapChoiceButtons", report);

            ValidatePreview(scene, ChoiceSide.Left, report);
            ValidatePreview(scene, ChoiceSide.Right, report);

            GameOverView gameOver = panel != null
                ? RequireSingleComponent<GameOverView>(panel, scene.path, report)
                : null;
            ValidateReference(gameOver, "interfaceText", interfaceText, scene.path,
                "/UICanvas/SafeArea/GameOverPanel", report);
            GameObject restartObject = RequirePath(
                scene, "/UICanvas/SafeArea/GameOverPanel/Content/RestartButton", report);
            Button restart = restartObject != null ? restartObject.GetComponent<Button>() : null;
            RequirePath(scene, "/UICanvas/SafeArea/GameOverPanel/Content/Illustration", report);
            RequirePath(scene, "/UICanvas/SafeArea/GameOverPanel/Content/Title", report);
            RequirePath(scene, "/UICanvas/SafeArea/GameOverPanel/Content/Body", report);
            RequirePath(scene, "/UICanvas/SafeArea/GameOverPanel/Content/RestartButton/Text (TMP)", report);
            string[] obsoleteNames = { "Illustration", "Title", "Body", "BODY", "RestartButton" };
            for (int i = 0; panel != null && i < obsoleteNames.Length; i++)
            {
                Transform obsolete = panel.transform.Find(obsoleteNames[i]);
                if (obsolete != null)
                {
                    AddInvalid(report, scene.path, HierarchyPath(obsolete),
                        "Obsolete managed GameOver child must not remain beside Content.");
                }
            }
            if (panel != null && panel.activeSelf)
            {
                AddInvalid(report, scene.path, "/UICanvas/SafeArea/GameOverPanel",
                    "GameOverPanel must start inactive.");
            }
            if (panel != null && safeArea != null
                && panel.transform.GetSiblingIndex() != safeArea.transform.childCount - 1)
            {
                AddInvalid(report, scene.path, "/UICanvas/SafeArea/GameOverPanel",
                    "GameOverPanel must remain the last SafeArea sibling.");
            }
            ValidateExpectedListener(restart, gameOver,
                nameof(GameOverView.HandleRestartButton), scene.path,
                "/UICanvas/SafeArea/GameOverPanel/Content/RestartButton", report);

            FooterView footer = footerObject != null
                ? RequireSingleComponent<FooterView>(footerObject, scene.path, report)
                : null;
            RunStatusView runStatus = footerObject != null
                ? RequireSingleComponent<RunStatusView>(footerObject, scene.path, report)
                : null;
            ValidateReference(footer, "interfaceText", interfaceText, scene.path,
                "/UICanvas/SafeArea/Footer", report);
            ValidateReference(runStatus, "interfaceText", interfaceText, scene.path,
                "/UICanvas/SafeArea/Footer", report);
            RequirePath(scene, "/UICanvas/SafeArea/Footer/Reign", report);
            RequirePath(scene, "/UICanvas/SafeArea/Footer/Ruler", report);
            RequirePath(scene, "/UICanvas/SafeArea/Footer/Progress", report);
            RequirePath(scene, "/UICanvas/SafeArea/Footer/Seal", report);
            RectTransform footerRect = footerObject != null
                ? footerObject.transform as RectTransform : null;
            if (footerRect == null || !Mathf.Approximately(footerRect.sizeDelta.y, 96f))
            {
                AddInvalid(report, scene.path, "/UICanvas/SafeArea/Footer",
                    "Footer height must be 96 reference units.");
            }

            TutorialOverlayView tutorialView = tutorialObject != null
                ? RequireSingleComponent<TutorialOverlayView>(tutorialObject, scene.path, report)
                : null;
            TutorialCoordinator tutorial = tutorialObject != null
                ? RequireSingleComponent<TutorialCoordinator>(tutorialObject, scene.path, report)
                : null;
            RequirePath(scene, "/UICanvas/SafeArea/TutorialOverlay/Content/Title", report);
            RequirePath(scene, "/UICanvas/SafeArea/TutorialOverlay/Content/Body", report);
            RequirePath(scene, "/UICanvas/SafeArea/TutorialOverlay/Content/NextButton", report);
            RequirePath(scene, "/UICanvas/SafeArea/TutorialOverlay/Content/SkipButton", report);
            if (tutorialObject != null && tutorialObject.activeSelf)
            {
                AddInvalid(report, scene.path, "/UICanvas/SafeArea/TutorialOverlay",
                    "Tutorial overlay must start inactive.");
            }
            ValidateReference(tutorial, "view", tutorialView, scene.path,
                "/UICanvas/SafeArea/TutorialOverlay", report);

            AudioService audio = audioObject != null
                ? RequireSingleComponent<AudioService>(audioObject, scene.path, report)
                : null;
            AudioSource source = audioObject != null
                ? RequireSingleComponent<AudioSource>(audioObject, scene.path, report)
                : null;
            GameObject musicObject = RequirePath(scene, "/AudioService/MusicSource", report);
            AudioSource music = musicObject != null
                ? RequireSingleComponent<AudioSource>(musicObject, scene.path, report)
                : null;
            if (source != null && (source.playOnAwake || source.loop
                || !Mathf.Approximately(source.spatialBlend, 0f)))
            {
                AddInvalid(report, scene.path, "/AudioService", "AudioSource settings are incorrect.");
            }
            if (audio != null && GetObjectProperty(audio, "audioSource") != source)
            {
                AddInvalid(report, scene.path, "/AudioService", "AudioSource reference is incorrect.");
            }
            if (music != null && (music.playOnAwake || !music.loop
                || !Mathf.Approximately(music.spatialBlend, 0f)))
            {
                AddInvalid(report, scene.path, "/AudioService/MusicSource",
                    "Music AudioSource settings are incorrect.");
            }
            ValidateReference(audio, "musicSource", music, scene.path, "/AudioService", report);

            GameSceneController controller = controllerObject != null
                ? RequireSingleComponent<GameSceneController>(controllerObject, scene.path, report)
                : null;
            ValidateReference(controller, "catalogue", catalogue, scene.path,
                "/GameSceneController", report);
            ValidateReference(controller, "cardView", card, scene.path,
                "/GameSceneController", report);
            ValidateReference(controller, "hudView", hud, scene.path,
                "/GameSceneController", report);
            ValidateReference(controller, "gameOverView", gameOver, scene.path,
                "/GameSceneController", report);
            ValidateReference(controller, "swipeController", swipe, scene.path,
                "/GameSceneController", report);
            ValidateReference(controller, "tapChoiceButtonsView", tapChoices, scene.path,
                "/GameSceneController", report);
            ValidateReference(controller, "audioService", audio, scene.path,
                "/GameSceneController", report);
            ValidateReference(controller, "sessionIntent", intent, scene.path,
                "/GameSceneController", report);
            ValidateReference(controller, "runStatusView", runStatus, scene.path,
                "/GameSceneController", report);
            ValidateReference(controller, "footerView", footer, scene.path,
                "/GameSceneController", report);
            ValidateReference(controller, "tutorialCoordinator", tutorial, scene.path,
                "/GameSceneController", report);
            PanelFadeAnimator transitionOverlay = ValidatePanelFadeAnimator(
                transitionOverlayObject, scene.path, "/UICanvas/TransitionOverlay", report);
            ValidateReference(controller, "transitionOverlay", transitionOverlay, scene.path,
                "/GameSceneController", report);
            if (transitionOverlayObject != null && !transitionOverlayObject.activeSelf)
            {
                AddInvalid(report, scene.path, "/UICanvas/TransitionOverlay",
                    "TransitionOverlay must start active — it covers the first frame until "
                    + "GameSceneController reveals it.");
            }
            AccessibilityPresentationController accessibility = controllerObject != null
                ? RequireSingleComponent<AccessibilityPresentationController>(
                    controllerObject, scene.path, report)
                : null;
            GameFeedbackController feedback = controllerObject != null
                ? RequireSingleComponent<GameFeedbackController>(controllerObject, scene.path, report)
                : null;
            ApplicationLifecycleController lifecycle = controllerObject != null
                ? RequireSingleComponent<ApplicationLifecycleController>(
                    controllerObject, scene.path, report)
                : null;
            ValidateReference(accessibility, "swipeController", swipe, scene.path,
                "/GameSceneController", report);
            ValidateReference(controller, "accessibility", accessibility, scene.path,
                "/GameSceneController", report);
            ValidateReference(feedback, "gameSceneController", controller, scene.path,
                "/GameSceneController", report);
            ValidateReference(feedback, "cues",
                AssetDatabase.LoadAssetAtPath<FeedbackCueProfile>(DefaultFeedbackCueProfilePath),
                scene.path, "/GameSceneController", report);
            ValidateReference(lifecycle, "gameSceneController", controller, scene.path,
                "/GameSceneController", report);
            ValidateReference(lifecycle, "tutorialCoordinator", tutorial, scene.path,
                "/GameSceneController", report);

            // Geri (back to MainMenu) and Ayarlar — reachable from inside a run, not only MainMenu.
            RequirePath(scene, "/UICanvas/SafeArea/TopBar", report);
            GameObject backButtonObject = RequirePath(
                scene, "/UICanvas/SafeArea/TopBar/BackButton", report);
            GameObject gameSettingsButtonObject = RequirePath(
                scene, "/UICanvas/SafeArea/TopBar/SettingsButton", report);
            Button backButton = backButtonObject != null
                ? RequireSingleComponent<Button>(backButtonObject, scene.path, report) : null;
            ValidateExpectedListener(backButton, lifecycle,
                nameof(ApplicationLifecycleController.HandleBackRequested), scene.path,
                "/UICanvas/SafeArea/TopBar/BackButton", report);

            // The same full Settings/About panel as MainMenu's, duplicated into this scene so it
            // can be opened mid-run without leaving Game.
            GameObject gameSettingsPanelObject = RequirePath(scene, "/UICanvas/SettingsPanel", report);
            SettingsPanelView gameSettingsView = gameSettingsPanelObject != null
                ? RequireSingleComponent<SettingsPanelView>(gameSettingsPanelObject, scene.path, report)
                : null;
            GameObject gameSettingsControllerObject = RequirePath(
                scene, "/SettingsController", report);
            SettingsController gameSettingsController = gameSettingsControllerObject != null
                ? RequireSingleComponent<SettingsController>(
                    gameSettingsControllerObject, scene.path, report)
                : null;
            ValidateReference(gameSettingsController, "view", gameSettingsView, scene.path,
                "/SettingsController", report);
            ValidateReference(gameSettingsController, "cues",
                AssetDatabase.LoadAssetAtPath<FeedbackCueProfile>(DefaultFeedbackCueProfilePath),
                scene.path, "/SettingsController", report);
            Button gameSettingsButton = gameSettingsButtonObject != null
                ? RequireSingleComponent<Button>(gameSettingsButtonObject, scene.path, report) : null;
            ValidateExpectedListener(gameSettingsButton, gameSettingsController,
                nameof(SettingsController.Open), scene.path,
                "/UICanvas/SafeArea/TopBar/SettingsButton", report);
            ValidateReference(lifecycle, "settingsController", gameSettingsController, scene.path,
                "/GameSceneController", report);
            ValidateReference(gameSettingsController, "gameSceneController", controller, scene.path,
                "/SettingsController", report);
            if (gameSettingsPanelObject != null && gameSettingsPanelObject.activeSelf)
            {
                AddInvalid(report, scene.path, "/UICanvas/SettingsPanel",
                    "SettingsPanel must start inactive.");
            }

            GameObject gameAboutPanelObject = RequirePath(scene, "/UICanvas/AboutPanel", report);
            AboutPanelView gameAboutPanel = gameAboutPanelObject != null
                ? RequireSingleComponent<AboutPanelView>(gameAboutPanelObject, scene.path, report)
                : null;
            ValidateReference(gameSettingsController, "aboutPanel", gameAboutPanel, scene.path,
                "/SettingsController", report);
            ValidateReference(gameSettingsController, "mainMenuRoot",
                safeArea, scene.path, "/SettingsController", report);
            if (gameAboutPanelObject != null && gameAboutPanelObject.activeSelf)
            {
                AddInvalid(report, scene.path, "/UICanvas/AboutPanel",
                    "AboutPanel must start inactive.");
            }

            GameObject gameResetProgressObject = RequirePath(
                scene, "/ResetProgressController", report);
            ResetProgressController gameResetProgressController = gameResetProgressObject != null
                ? RequireSingleComponent<ResetProgressController>(
                    gameResetProgressObject, scene.path, report)
                : null;
            ValidateReference(gameResetProgressController, "view", gameSettingsView, scene.path,
                "/ResetProgressController", report);

            if (FindComponentsInScene<DevelopmentDebugPanel>(scene).Length != 0)
            {
                AddInvalid(report, scene.path, string.Empty,
                    "DevelopmentDebugPanel must never be serialized into a release scene.");
            }

            if (canvas != null)
            {
                GameUIThemeController themeController = RequireSingleComponent<GameUIThemeController>(
                    canvas, scene.path, report);
                GameUITheme assignedTheme = GetObjectProperty(themeController, "theme") as GameUITheme;
                if (assignedTheme == null)
                {
                    AddInvalid(report, scene.path, "/UICanvas",
                        "GameUIThemeController needs a serialized GameUITheme.");
                }
                else if (!UIContrastMath.MeetsNormalText(
                    assignedTheme.PrimaryText, assignedTheme.CardSurface))
                {
                    AddInvalid(report, scene.path, "/UICanvas",
                        "Assigned GameUITheme does not meet normal-text contrast on the card.");
                }
                ValidateReference(themeController, "hudView", hud, scene.path,
                    "/UICanvas", report);
                ValidateReference(themeController, "cardView", card, scene.path,
                    "/UICanvas", report);
                ValidateReference(themeController, "footerView", footer, scene.path,
                    "/UICanvas", report);
                ValidateReference(themeController, "situationPanelImage", situationArtwork,
                    scene.path, "/UICanvas", report);
                ValidateReference(themeController, "situationPanelFallback", situationFallback,
                    scene.path, "/UICanvas", report);
            }
        }

        private static void ValidatePreview(Scene scene, ChoiceSide side, SceneSetupReport report)
        {
            string name = side == ChoiceSide.Left ? "PreviewLeft" : "PreviewRight";
            string path = "/UICanvas/SafeArea/CardArea/Card/PortraitSwipeRoot/" + name;
            GameObject previewObject = RequirePath(scene, path, report);
            GameObject labelObject = RequirePath(scene, path + "/Label", report);
            GameObject edgeObject = RequirePath(scene, path + "/EdgeHighlight", report);
            GameObject markerObject = RequirePath(scene, path + "/CommitMarker", report);
            if (previewObject == null || labelObject == null
                || edgeObject == null || markerObject == null)
            {
                return;
            }

            ChoicePreviewView view = RequireSingleComponent<ChoicePreviewView>(
                previewObject, scene.path, report);
            CanvasGroup group = RequireSingleComponent<CanvasGroup>(
                previewObject, scene.path, report);
            TMP_Text label = RequireSingleComponent<TextMeshProUGUI>(
                labelObject, scene.path, report);
            Image edge = RequireSingleComponent<Image>(edgeObject, scene.path, report);
            CanvasGroup marker = RequireSingleComponent<CanvasGroup>(
                markerObject, scene.path, report);
            Image legacyOverlay = previewObject.GetComponent<Image>();
            if (view != null && (view.Side != side
                || GetObjectProperty(view, "label") != label
                || GetObjectProperty(view, "canvasGroup") != group
                || GetObjectProperty(view, "edgeHighlight") != edge
                || GetObjectProperty(view, "commitMarker") != marker))
            {
                AddInvalid(report, scene.path, path, "Choice preview references are incorrect.");
            }
            if (edge != null && edge.raycastTarget)
            {
                AddInvalid(report, scene.path, path + "/EdgeHighlight",
                    "Choice edge highlight must not block raycasts.");
            }
            if (legacyOverlay != null && legacyOverlay.enabled)
            {
                AddInvalid(report, scene.path, path,
                    "Legacy full-area choice overlay must be disabled.");
            }
        }

        private static void ValidateNonRaycastImage(
            Scene scene,
            string path,
            SceneSetupReport report)
        {
            GameObject imageObject = RequirePath(scene, path, report);
            if (imageObject == null)
            {
                return;
            }

            Image image = RequireSingleComponent<Image>(imageObject, scene.path, report);
            if (image != null && image.raycastTarget)
            {
                AddInvalid(report, scene.path, path, "Background Image must not block raycasts.");
            }
        }

        private static void ValidateTheme(GameUITheme theme, SceneSetupReport report)
        {
            if (!ColoursMatch(theme.OverallBackground, OverallBackgroundColour)
                || !ColoursMatch(theme.UISurface, SurfaceColour)
                || !ColoursMatch(theme.CardSurface, CardSurfaceColour)
                || !ColoursMatch(theme.BorderGold, BorderGoldColour)
                || !ColoursMatch(theme.EmptyBar, StatBackgroundColour)
                || !ColoursMatch(theme.GetStatColor(StatType.People), StatFillColours[0])
                || !ColoursMatch(theme.GetStatColor(StatType.Security), StatFillColours[1])
                || !ColoursMatch(theme.GetStatColor(StatType.Authority), StatFillColours[2])
                || !ColoursMatch(theme.GetStatColor(StatType.Wealth), StatFillColours[3])
                || !ColoursMatch(theme.PortraitFallbackBackground, SurfaceColour)
                || !ColoursMatch(theme.PortraitFallbackForeground, SecondaryTextColour))
            {
                AddInvalid(report, DefaultThemePath, string.Empty,
                    "Default GameUITheme palette differs from the managed neutral baseline.");
            }

            if (!UIContrastMath.MeetsNormalText(theme.PrimaryText, theme.CardSurface)
                || !UIContrastMath.MeetsNormalText(theme.SecondaryText, theme.UISurface)
                || !UIContrastMath.MeetsNormalText(theme.HighlightGold, theme.CardSurface))
            {
                AddInvalid(report, DefaultThemePath, string.Empty,
                    "Default GameUITheme normal-text contrast must be at least 4.5:1.");
            }
        }

        private static void ValidateTurkishTextAssets(
            Scene scene,
            TMP_FontAsset expectedFont,
            SceneSetupReport report)
        {
            if (!TurkishGlyphValidator.TryValidate(expectedFont, out string fontMessage))
            {
                AddInvalid(report, scene.path, string.Empty,
                    "The project-owned Turkish TMP font is invalid: " + fontMessage);
                return;
            }

            TextMeshProUGUI[] textObjects = FindComponentsInScene<TextMeshProUGUI>(scene);
            for (int i = 0; i < textObjects.Length; i++)
            {
                TextMeshProUGUI text = textObjects[i];
                TMP_FontAsset resolvedFont = text.font != null
                    ? text.font
                    : TMP_Settings.defaultFontAsset;
                if (resolvedFont == null
                    || !resolvedFont.HasCharacters(
                        TurkishGlyphValidator.RequiredTurkishCharacters,
                        out _,
                        true,
                        false))
                {
                    AddInvalid(report, scene.path, HierarchyPath(text.transform),
                        "The resolved TMP font must cover all required Turkish glyphs.");
                }
            }
        }

        private static void ValidateTextColour(
            Scene scene,
            string path,
            Color expected,
            SceneSetupReport report)
        {
            GameObject textObject = RequirePath(scene, path, report);
            if (textObject == null)
            {
                return;
            }

            TextMeshProUGUI text = RequireSingleComponent<TextMeshProUGUI>(
                textObject, scene.path, report);
            if (text != null && !ColoursMatch(text.color, expected))
            {
                AddInvalid(report, scene.path, path, "TMP text colour is incorrect.");
            }
        }

        private static void ValidateBootstrapScene(Scene scene, SceneSetupReport report)
        {
            GameObject root = RequirePath(scene, "/BootstrapController", report);
            BootstrapController controller = root != null
                ? RequireSingleComponent<BootstrapController>(root, scene.path, report)
                : null;
            if (controller != null && GetStringProperty(controller, "mainMenuSceneName") != "MainMenu")
            {
                AddInvalid(report, scene.path, "/BootstrapController",
                    "Main menu scene name must be MainMenu.");
            }
        }

        private static void ValidateMainMenuScene(
            Scene scene,
            SessionIntent intent,
            SceneSetupReport report)
        {
            InterfaceTextDefinition interfaceText =
                AssetDatabase.LoadAssetAtPath<InterfaceTextDefinition>(InterfaceTextPath);
            TMP_FontAsset turkishFont =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TurkishFontPath);
            RequirePath(scene, "/Main Camera", report);
            GameObject eventObject = RequirePath(scene, "/EventSystem", report);
            RequirePath(scene, "/UICanvas/SafeArea/MainMenuPanel/Title", report);
            GameObject newObject = RequirePath(
                scene, "/UICanvas/SafeArea/MainMenuPanel/NewGameButton", report);
            GameObject continueObject = RequirePath(
                scene, "/UICanvas/SafeArea/MainMenuPanel/ContinueButton", report);
            GameObject settingsButtonObject = RequirePath(
                scene, "/UICanvas/SafeArea/MainMenuPanel/SettingsButton", report);
            GameObject controllerObject = RequirePath(scene, "/MainMenuController", report);
            GameObject panelObject = RequirePath(
                scene, "/UICanvas/SafeArea/MainMenuPanel", report);
            GameObject settingsPanelObject = RequirePath(
                scene, "/UICanvas/SettingsPanel", report);
            GameObject settingsControllerObject = RequirePath(scene, "/SettingsController", report);
            GameObject audioObject = RequirePath(scene, "/AudioService", report);
            GameObject transitionOverlayObject = RequirePath(
                scene, "/UICanvas/TransitionOverlay", report);

            // The Game UI foundation deliberately leaves the existing MainMenu scene untouched.
            // Validate the Phase-F localization wiring only when that separately managed migration
            // has begun; a legacy menu without either marker remains a supported baseline here.
            bool hasLocalizedMenu = panelObject != null
                && (panelObject.GetComponent<MainMenuTextView>() != null
                    || panelObject.transform.Find("SaveError") != null);
            if (hasLocalizedMenu)
            {
                ValidateTurkishTextAssets(scene, turkishFont, report);
                RequirePath(scene, "/UICanvas/SafeArea/MainMenuPanel/SaveError", report);
            }

            if (eventObject != null)
            {
                RequireSingleComponent<InputSystemUIInputModule>(eventObject, scene.path, report);
            }

            MainMenuController controller = controllerObject != null
                ? RequireSingleComponent<MainMenuController>(controllerObject, scene.path, report)
                : null;
            Button newButton = newObject != null ? newObject.GetComponent<Button>() : null;
            Button continueButton = continueObject != null ? continueObject.GetComponent<Button>() : null;
            Button settingsButton = settingsButtonObject != null
                ? settingsButtonObject.GetComponent<Button>() : null;
            ValidateReference(controller, "sessionIntent", intent, scene.path,
                "/MainMenuController", report);
            ValidateReference(controller, "continueButton", continueButton, scene.path,
                "/MainMenuController", report);
            PanelFadeAnimator transitionOverlay = ValidatePanelFadeAnimator(
                transitionOverlayObject, scene.path, "/UICanvas/TransitionOverlay", report);
            ValidateReference(controller, "transitionOverlay", transitionOverlay, scene.path,
                "/MainMenuController", report);
            if (transitionOverlayObject != null && transitionOverlayObject.activeSelf)
            {
                AddInvalid(report, scene.path, "/UICanvas/TransitionOverlay",
                    "TransitionOverlay must start inactive.");
            }
            if (hasLocalizedMenu)
            {
                ValidateReference(controller, "interfaceText", interfaceText, scene.path,
                    "/MainMenuController", report);
                MainMenuTextView textView = RequireSingleComponent<MainMenuTextView>(
                    panelObject, scene.path, report);
                ValidateReference(textView, "interfaceText", interfaceText, scene.path,
                    "/UICanvas/SafeArea/MainMenuPanel", report);
                ValidateReference(controller, "mainMenuTextView", textView, scene.path,
                    "/MainMenuController", report);
            }
            if (controller != null && GetStringProperty(controller, "gameSceneName") != "Game")
            {
                AddInvalid(report, scene.path, "/MainMenuController",
                    "Game scene name must be Game.");
            }
            ValidateExpectedListener(newButton, controller,
                nameof(MainMenuController.OnNewGamePressed), scene.path,
                "/UICanvas/SafeArea/MainMenuPanel/NewGameButton", report);
            ValidateExpectedListener(continueButton, controller,
                nameof(MainMenuController.OnContinuePressed), scene.path,
                "/UICanvas/SafeArea/MainMenuPanel/ContinueButton", report);
            SettingsPanelView settingsView = settingsPanelObject != null
                ? RequireSingleComponent<SettingsPanelView>(settingsPanelObject, scene.path, report)
                : null;
            SettingsController settingsController = settingsControllerObject != null
                ? RequireSingleComponent<SettingsController>(
                    settingsControllerObject, scene.path, report)
                : null;
            ValidateReference(settingsController, "view", settingsView, scene.path,
                "/SettingsController", report);
            ValidateExpectedListener(settingsButton, settingsController,
                nameof(SettingsController.Open), scene.path,
                "/UICanvas/SafeArea/MainMenuPanel/SettingsButton", report);
            const string settingsScrollContent = "SafeArea/Content/ContentViewport/ScrollContent/";
            string[] settingsPaths =
            {
                "SafeArea/Content/Header/Title",
                "SafeArea/Content/TabBar/AudioTabButton", "SafeArea/Content/TabBar/GraphicsTabButton",
                "SafeArea/Content/TabBar/ControlsTabButton", "SafeArea/Content/TabBar/GeneralTabButton",
                settingsScrollContent + "AudioTab/VolumeGroup/MasterVolume",
                settingsScrollContent + "AudioTab/VolumeGroup/MusicVolume",
                settingsScrollContent + "AudioTab/VolumeGroup/SfxVolume",
                settingsScrollContent + "AudioTab/MasterMute",
                settingsScrollContent + "GraphicsTab/FrameRateGroup/FrameRate",
                settingsScrollContent + "GraphicsTab/BatterySaver",
                settingsScrollContent + "ControlsTab/SensitivityGroup/SwipeSensitivity",
                settingsScrollContent + "ControlsTab/TapButtonsEnabled",
                settingsScrollContent + "ControlsTab/InvertSwipeRotation",
                settingsScrollContent + "ControlsTab/DisableSwipe",
                settingsScrollContent + "ControlsTab/Haptics",
                settingsScrollContent + "GeneralTab/ReducedMotion",
                settingsScrollContent + "GeneralTab/TextSizeGroup/TextSize",
                settingsScrollContent + "GeneralTab/HighContrast",
                settingsScrollContent + "GeneralTab/Language",
                settingsScrollContent + "GeneralTab/ResetTutorialButton",
                settingsScrollContent + "GeneralTab/ResetToDefaultsButton",
                settingsScrollContent + "GeneralTab/AboutButton",
                settingsScrollContent + "GeneralTab/ResetProgressButton",
                "SafeArea/Content/BottomActions/ApplyButton",
                "SafeArea/Content/BottomActions/CancelButton"
            };
            for (int i = 0; i < settingsPaths.Length; i++)
            {
                RequirePath(scene, "/UICanvas/SettingsPanel/" + settingsPaths[i], report);
            }
            if (settingsPanelObject != null && settingsPanelObject.activeSelf)
            {
                AddInvalid(report, scene.path, "/UICanvas/SettingsPanel",
                    "SettingsPanel must start inactive.");
            }
            PanelFadeAnimator settingsPanelTransition = ValidatePanelFadeAnimator(
                settingsPanelObject, scene.path, "/UICanvas/SettingsPanel", report);
            ValidateReference(settingsView, "panelAnimator", settingsPanelTransition, scene.path,
                "/UICanvas/SettingsPanel", report);
            GameObject settingsContentViewport = RequirePath(
                scene, "/UICanvas/SettingsPanel/SafeArea/Content/ContentViewport", report);
            PanelFadeAnimator tabCrossfadeTransition = ValidatePanelFadeAnimator(
                settingsContentViewport, scene.path,
                "/UICanvas/SettingsPanel/SafeArea/Content/ContentViewport", report);
            ValidateReference(settingsView, "tabCrossfadeAnimator", tabCrossfadeTransition, scene.path,
                "/UICanvas/SettingsPanel", report);
            AudioService audio = audioObject != null
                ? RequireSingleComponent<AudioService>(audioObject, scene.path, report)
                : null;
            ValidateReference(settingsController, "audioService", audio, scene.path,
                "/SettingsController", report);
            ValidateReference(settingsController, "cues",
                AssetDatabase.LoadAssetAtPath<FeedbackCueProfile>(DefaultFeedbackCueProfilePath),
                scene.path, "/SettingsController", report);
            AccessibilityPresentationController mainMenuAccessibility = settingsControllerObject != null
                ? RequireSingleComponent<AccessibilityPresentationController>(
                    settingsControllerObject, scene.path, report)
                : null;
            ValidateReference(settingsController, "accessibility", mainMenuAccessibility, scene.path,
                "/SettingsController", report);

            GameObject aboutPanelObject = RequirePath(
                scene, "/UICanvas/AboutPanel", report);
            RequirePath(scene, "/UICanvas/AboutPanel/SafeArea/Content/CloseButton", report);
            AboutPanelView aboutPanel = aboutPanelObject != null
                ? RequireSingleComponent<AboutPanelView>(aboutPanelObject, scene.path, report)
                : null;
            ValidateReference(settingsController, "aboutPanel", aboutPanel, scene.path,
                "/SettingsController", report);
            if (aboutPanelObject != null && aboutPanelObject.activeSelf)
            {
                AddInvalid(report, scene.path, "/UICanvas/AboutPanel",
                    "AboutPanel must start inactive.");
            }
            PanelFadeAnimator aboutPanelTransition = ValidatePanelFadeAnimator(
                aboutPanelObject, scene.path, "/UICanvas/AboutPanel", report);
            ValidateReference(aboutPanel, "panelAnimator", aboutPanelTransition, scene.path,
                "/UICanvas/AboutPanel", report);

            GameObject resetProgressObject = RequirePath(scene, "/ResetProgressController", report);
            ResetProgressController resetProgressController = resetProgressObject != null
                ? RequireSingleComponent<ResetProgressController>(
                    resetProgressObject, scene.path, report)
                : null;
            ValidateReference(resetProgressController, "view", settingsView, scene.path,
                "/ResetProgressController", report);

            ApplicationLifecycleController lifecycle = controllerObject != null
                ? RequireSingleComponent<ApplicationLifecycleController>(
                    controllerObject, scene.path, report)
                : null;
            ValidateReference(lifecycle, "settingsController", settingsController, scene.path,
                "/MainMenuController", report);
            if (lifecycle != null && !GetBoolProperty(lifecycle, "mainMenuMode"))
            {
                AddInvalid(report, scene.path, "/MainMenuController",
                    "Main-menu lifecycle controller must quit on Back.");
            }
        }

        private static void ValidateBuildScenes(SceneSetupReport report)
        {
            string[] expected = { BootstrapScenePath, MainMenuScenePath, GameScenePath };
            EditorBuildSettingsScene[] actual = EditorBuildSettings.scenes;
            if (actual.Length != expected.Length)
            {
                report.Add(SceneSetupIssueSeverity.Error, "BUILD_SCENES", "Build",
                    "ProjectSettings/EditorBuildSettings.asset", string.Empty,
                    "Build scene list must contain exactly Bootstrap, MainMenu, and Game.");
                return;
            }

            for (int i = 0; i < expected.Length; i++)
            {
                if (!actual[i].enabled || actual[i].path != expected[i])
                {
                    report.Add(SceneSetupIssueSeverity.Error, "BUILD_SCENE_ORDER", "Build",
                        "ProjectSettings/EditorBuildSettings.asset", string.Empty,
                        "Build scene index " + i + " must be " + expected[i] + " and enabled.");
                }
            }
        }

        // Authoring primitives -------------------------------------------------------

        private static void EnsureCamera(Scene scene, SceneSetupReport report)
        {
            GameObject cameraObject = EnsureRoot(scene, "Main Camera", report);
            Camera camera = EnsureSingleComponent<Camera>(cameraObject, report);
            EnsureSingleComponent<AudioListener>(cameraObject, report);
            if (camera != null)
            {
                Undo.RecordObject(camera, "Configure camera");
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                // Was previously left at Unity's implicit default (the same colour numerically —
                // see MainMenuBackgroundColour). Naming it explicitly gives MainMenu/Settings/
                // About a single real source instead of each guessing at a matching tone. The
                // Game scene's own opaque Background always covers this, so it has no visible
                // effect there.
                camera.backgroundColor = MainMenuBackgroundColour;
            }
            if (cameraObject != null)
            {
                Undo.RecordObject(cameraObject, "Configure camera tag");
                cameraObject.tag = "MainCamera";
            }
        }

        private static void EnsureEventSystem(Scene scene, SceneSetupReport report)
        {
            GameObject eventObject = EnsureRoot(scene, "EventSystem", report);
            EnsureSingleComponent<EventSystem>(eventObject, report);
            InputSystemUIInputModule module = EnsureSingleComponent<InputSystemUIInputModule>(
                eventObject, report);
            StandaloneInputModule legacy = eventObject != null
                ? eventObject.GetComponent<StandaloneInputModule>()
                : null;
            if (legacy != null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "LEGACY_INPUT_MODULE", "Components",
                    scene.path, "/EventSystem",
                    "StandaloneInputModule is ambiguous and will not be deleted automatically.");
            }
            if (module != null && module.actionsAsset == null)
            {
                Undo.RecordObject(module, "Assign default UI actions");
                module.AssignDefaultActions();
            }
        }

        private static void ConfigureCanvas(GameObject canvasObject, SceneSetupReport report)
        {
            if (canvasObject == null)
            {
                return;
            }
            Canvas canvas = EnsureSingleComponent<Canvas>(canvasObject, report);
            CanvasScaler scaler = EnsureSingleComponent<CanvasScaler>(canvasObject, report);
            EnsureSingleComponent<GraphicRaycaster>(canvasObject, report);
            if (canvas != null)
            {
                Undo.RecordObject(canvas, "Configure Canvas");
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.pixelPerfect = false;
            }
            if (scaler != null)
            {
                Undo.RecordObject(scaler, "Configure CanvasScaler");
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
            }
        }

        private static Button EnsureMenuButton(
            RectTransform parent,
            string name,
            string label,
            float y,
            SceneSetupReport report,
            float x = 0f,
            float width = 600f,
            float height = 120f,
            Color? colourOverride = null,
            float cornerRadius = StandardButtonCornerRadius)
        {
            RectTransform transform = EnsureUiChild(parent, name, report);
            LayoutGroup parentLayout = parent != null ? parent.GetComponent<LayoutGroup>() : null;
            if (parentLayout != null)
            {
                // Flow layouts own position and size; only hand them a size hint via LayoutElement.
                SetRect(transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(width, height), Center);
                LayoutElement element = EnsureSingleComponent<LayoutElement>(transform.gameObject, report);
                if (element != null)
                {
                    Undo.RecordObject(element, "Configure menu button layout");
                    element.preferredWidth = width;
                    element.preferredHeight = height;
                    // Also a floor, not just a hint — belt-and-suspenders against a sibling in the
                    // same VerticalLayoutGroup ever compressing this button below its authored size.
                    element.minHeight = height;
                    element.flexibleWidth = parentLayout is HorizontalLayoutGroup ? 1f : 0f;
                    element.flexibleHeight = 0f;
                }
            }
            else
            {
                SetRect(transform, Center, Center, new Vector2(x, y), new Vector2(width, height), Center);
            }

            ProceduralRoundedRectGraphic graphic = ConfigureRoundedButtonGraphic(
                transform.gameObject, colourOverride ?? ButtonColour, cornerRadius, report);
            Button button = EnsureSingleComponent<Button>(transform.gameObject, report);
            if (button != null && graphic != null)
            {
                Undo.RecordObject(button, "Wire menu button target graphic");
                button.targetGraphic = graphic;
            }
            EnsureButtonText(transform, label, report);
            return button;
        }

        /// <summary>
        /// Replaces a flat <see cref="Image"/> with a tinted, corner-rounded procedural graphic and
        /// removes any stale <see cref="Image"/> left behind by an older authoring pass, so the two
        /// never render on top of one another. Buttons always raycast; non-interactive fills
        /// (slider track/fill, toggle switch) can opt out via <see cref="ConfigureRoundedFill"/>.
        /// </summary>
        private static ProceduralRoundedRectGraphic ConfigureRoundedButtonGraphic(
            GameObject target, Color colour, float cornerRadius, SceneSetupReport report)
        {
            return ConfigureRoundedFill(target, colour, cornerRadius, true, report);
        }

        private static ProceduralRoundedRectGraphic ConfigureRoundedFill(
            GameObject target, Color colour, float cornerRadius, bool raycastTarget,
            SceneSetupReport report)
        {
            if (target == null)
            {
                return null;
            }
            RemoveStaleComponents<Image>(target);
            // A brand-new GameObject's [RequireComponent(typeof(CanvasRenderer))] add-on can lose
            // the race with a single-shot batch -executeMethod/-quit before SaveScene serializes
            // it (see the settings gear icon fix for the first occurrence of this). Every rounded
            // fill routes through here, so fixing it once here covers all of them permanently.
            EnsureSingleComponent<CanvasRenderer>(target, report);
            ProceduralRoundedRectGraphic graphic =
                EnsureSingleComponent<ProceduralRoundedRectGraphic>(target, report);
            if (graphic != null)
            {
                Undo.RecordObject(graphic, "Configure rounded fill graphic");
                graphic.color = colour;
                graphic.raycastTarget = raycastTarget;
                graphic.SetCornerRadius(cornerRadius);
            }
            return graphic;
        }

        private static void RemoveStaleComponents<T>(GameObject target) where T : Component
        {
            if (target == null)
            {
                return;
            }
            T[] stale = target.GetComponents<T>();
            for (int i = 0; i < stale.Length; i++)
            {
                Undo.DestroyObjectImmediate(stale[i]);
            }
        }

        /// <summary>
        /// Destroys any direct child of <paramref name="parent"/> not in <paramref name="expectedNames"/>.
        /// A pre-tab-restructuring authoring pass left individual control rows (MusicVolume,
        /// SfxVolume, MasterMute, Haptics, ReducedMotion, LargerText, HighContrast) parented
        /// directly under Settings' Content instead of inside their tab body; the new per-tab rows
        /// EnsureSliderControl/EnsureToggleControl create are unrelated duplicates, so those seven
        /// stayed active and rendered stacked on top of Header/TabBar. Sweep them here.
        /// </summary>
        private static void RemoveUnexpectedChildren(
            Transform parent, SceneSetupReport report, params string[] expectedNames)
        {
            if (parent == null)
            {
                return;
            }
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (Array.IndexOf(expectedNames, child.name) >= 0)
                {
                    continue;
                }
                report.Add(SceneSetupIssueSeverity.Info, "ORPHAN_REMOVED", "Hierarchy",
                    parent.gameObject.scene.path, HierarchyPath(child),
                    "Removed a leftover child from an earlier authoring pass.");
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        /// <summary>
        /// Reparents an existing child from its pre-restructure location so repeated Apply runs stay
        /// idempotent instead of leaving an orphaned duplicate behind.
        /// </summary>
        private static void MigrateChildIfNeeded(
            Transform newParent, Transform legacyParent, string name, SceneSetupReport report)
        {
            if (newParent == null || legacyParent == null || newParent == legacyParent)
            {
                return;
            }
            if (FindDirectChild(newParent, name, report) != null)
            {
                return;
            }
            RectTransform legacy = FindDirectChild(legacyParent, name, report);
            if (legacy != null)
            {
                Undo.SetTransformParent(legacy, newParent,
                    "Migrate " + name + " into restructured settings layout");
            }
        }

        /// <summary>Configures a settings tab body to auto-size vertically from its own children.</summary>
        private static void ConfigureTabLayout(
            RectTransform tab, SceneSetupReport report, float spacing = 20f)
        {
            SetRect(tab, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero,
                new Vector2(0.5f, 1f));
            VerticalLayoutGroup layout = EnsureSingleComponent<VerticalLayoutGroup>(tab.gameObject, report);
            if (layout != null)
            {
                Undo.RecordObject(layout, "Configure settings tab layout");
                layout.padding = new RectOffset(4, 4, 6, 12);
                layout.spacing = spacing;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandHeight = false;
            }
            ContentSizeFitter fitter = EnsureSingleComponent<ContentSizeFitter>(tab.gameObject, report);
            if (fitter != null)
            {
                Undo.RecordObject(fitter, "Configure settings tab fitter");
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        /// <summary>
        /// A bordered card that visually groups a cluster of rows into one section — e.g. wrapping
        /// all three volume sliders under "Ses ve Müzik" in a single frame, matching the reference
        /// layout's grouping. Auto-sizes to whatever rows are parented into it via the same
        /// VerticalLayoutGroup + ContentSizeFitter recipe <see cref="ConfigureTabLayout"/> uses for
        /// a whole tab, nested one level deeper.
        /// </summary>
        private static RectTransform EnsureSettingsGroupPanel(
            RectTransform parent, string name, SceneSetupReport report, float rowSpacing = 0f)
        {
            RectTransform group = EnsureUiChild(parent, name, report);
            ConfigureRoundedFill(group.gameObject, SettingsPanelTheme.InactiveTabColour, 28f, false,
                report);
            Outline groupOutline = EnsureSingleComponent<Outline>(group.gameObject, report);
            if (groupOutline != null)
            {
                Undo.RecordObject(groupOutline, "Configure settings group border");
                groupOutline.effectColor = SettingsPanelTheme.BorderGoldColour;
                groupOutline.effectDistance = new Vector2(1.5f, -1.5f);
                groupOutline.useGraphicAlpha = false;
            }
            SetRect(group, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero,
                new Vector2(0.5f, 1f));
            VerticalLayoutGroup layout =
                EnsureSingleComponent<VerticalLayoutGroup>(group.gameObject, report);
            if (layout != null)
            {
                Undo.RecordObject(layout, "Configure settings group layout");
                layout.padding = new RectOffset(28, 28, 6, 6);
                layout.spacing = rowSpacing;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandHeight = false;
            }
            ContentSizeFitter fitter = EnsureSingleComponent<ContentSizeFitter>(group.gameObject, report);
            if (fitter != null)
            {
                Undo.RecordObject(fitter, "Configure settings group fitter");
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            return group;
        }

        /// <summary>
        /// Section title + one-line description at the top of a settings tab. Purely
        /// presentational (no new GameSettings field, no persistence) — gives each tab real
        /// vertical weight and a typography hierarchy above the raw controls.
        /// </summary>
        private static void EnsureTabSectionHeader(
            RectTransform tab,
            string title,
            string description,
            TMP_FontAsset font,
            SceneSetupReport report)
        {
            RectTransform titleTransform = EnsureUiChild(tab, "SectionTitle", report);
            SetRect(titleTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
                new Vector2(0f, 76f), new Vector2(0.5f, 1f));
            ConfigureLayoutElement(titleTransform.gameObject, 76f, report);
            // SectionTitle used to hold its TMP text directly; it's now a plain container instead,
            // with the actual text moved into a child "Text" object below — remove stale components
            // an earlier authoring pass may have left directly on this object (its own TMP text,
            // and the card Image/Outline from when this row had a background).
            RemoveStaleComponents<TextMeshProUGUI>(titleTransform.gameObject);
            RemoveStaleComponents<Image>(titleTransform.gameObject);
            RemoveStaleComponents<Outline>(titleTransform.gameObject);
            RectTransform titleTextTransform = EnsureUiChild(titleTransform, "Text", report);
            SetRect(titleTextTransform, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f),
                Vector2.zero, Vector2.zero, Center);
            TextMeshProUGUI titleText = EnsureSingleComponent<TextMeshProUGUI>(
                titleTextTransform.gameObject, report);
            ConfigureReadableText(titleText, font, 32f, 26f, 36f, true, false, 2f);
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.text = title;
            SetTextColour(titleText, MenuTitleTextColour);
            RemoveUnexpectedChildren(titleTransform, report, "Text");

            RectTransform descriptionTransform = EnsureUiChild(tab, "SectionDescription", report);
            SetRect(descriptionTransform, new Vector2(0.05f, 1f), new Vector2(1f, 1f), Vector2.zero,
                new Vector2(0f, 40f), new Vector2(0.5f, 1f));
            ConfigureLayoutElement(descriptionTransform.gameObject, 40f, report);
            TextMeshProUGUI descriptionText = EnsureSingleComponent<TextMeshProUGUI>(
                descriptionTransform.gameObject, report);
            ConfigureReadableText(descriptionText, font, 24f, 20f, 26f, true, true, 2f);
            descriptionText.alignment = TextAlignmentOptions.TopLeft;
            descriptionText.text = description;
            SetTextColour(descriptionText, MenuMutedTextColour);

            SetSiblingIndex(titleTransform, 0);
            SetSiblingIndex(descriptionTransform, 1);
        }

        /// <summary>Compact top-right icon-only button that opens Settings from MainMenu.</summary>
        private static Button EnsureSettingsIconButton(
            RectTransform parent, SceneSetupReport report,
            float size = SettingsIconButtonSize, float margin = SettingsIconButtonMargin,
            Color? iconColourOverride = null)
        {
            RectTransform transform = EnsureUiChild(parent, "SettingsButton", report);
            Vector2 topRight = new Vector2(1f, 1f);
            SetRect(transform, topRight, topRight,
                new Vector2(-margin, -margin),
                new Vector2(size, size), topRight);

            // A lower-priority, secondary chip: dark like the HUD surfaces, not the gold CTA fill.
            ProceduralRoundedRectGraphic graphic = ConfigureRoundedButtonGraphic(
                transform.gameObject, StatBackgroundColour, size * 0.5f, report);
            Button button = EnsureSingleComponent<Button>(transform.gameObject, report);
            if (button != null && graphic != null)
            {
                Undo.RecordObject(button, "Wire settings icon button target graphic");
                button.targetGraphic = graphic;
            }

            // A prior authoring pass may have created this as a text button; drop the stale label.
            RectTransform staleText = FindDirectChild(transform, "Text (TMP)", report);
            if (staleText != null)
            {
                Undo.DestroyObjectImmediate(staleText.gameObject);
            }

            RectTransform icon = EnsureUiChild(transform, "Icon", report);
            float iconSize = size * 0.56f;
            SetRect(icon, Center, Center, Vector2.zero, new Vector2(iconSize, iconSize), Center);
            // A brand-new GameObject's [RequireComponent(typeof(CanvasRenderer))] add-on can lose
            // the race with a single-shot batch -executeMethod/-quit before SaveScene serializes
            // it (unlike every other button graphic here, which inherits its CanvasRenderer from
            // the pre-existing Image it replaces). Add it explicitly so the gear always renders.
            EnsureSingleComponent<CanvasRenderer>(icon.gameObject, report);
            ProceduralGearIconGraphic gear =
                EnsureSingleComponent<ProceduralGearIconGraphic>(icon.gameObject, report);
            if (gear != null)
            {
                Undo.RecordObject(gear, "Configure settings gear icon");
                gear.color = iconColourOverride ?? BorderGoldColour;
                gear.raycastTarget = false;
            }

            return button;
        }

        /// <summary>
        /// A top-left icon-only chip matching <see cref="EnsureSettingsIconButton"/>'s size, margin
        /// and dark-chip style — a plain TMP arrow glyph instead of a procedural mesh, since a
        /// single reusable back affordance doesn't warrant its own icon-shape class.
        /// </summary>
        private static Button EnsureBackIconButton(
            RectTransform parent, SceneSetupReport report,
            float size = SettingsIconButtonSize, float margin = SettingsIconButtonMargin)
        {
            RectTransform transform = EnsureUiChild(parent, "BackButton", report);
            Vector2 topLeft = new Vector2(0f, 1f);
            SetRect(transform, topLeft, topLeft,
                new Vector2(margin, -margin),
                new Vector2(size, size), topLeft);

            ProceduralRoundedRectGraphic graphic = ConfigureRoundedButtonGraphic(
                transform.gameObject, StatBackgroundColour, size * 0.5f, report);
            Button button = EnsureSingleComponent<Button>(transform.gameObject, report);
            if (button != null && graphic != null)
            {
                Undo.RecordObject(button, "Wire back icon button target graphic");
                button.targetGraphic = graphic;
            }

            RectTransform icon = EnsureUiChild(transform, "Icon", report);
            float iconSize = size * 0.56f;
            SetRect(icon, Center, Center, Vector2.zero, new Vector2(iconSize, iconSize), Center);

            // A prior authoring pass rendered this as a TMP "<" glyph; drop the stale label so it
            // doesn't linger alongside the procedural arrow below.
            TextMeshProUGUI staleGlyph = icon.GetComponent<TextMeshProUGUI>();
            if (staleGlyph != null)
            {
                Undo.DestroyObjectImmediate(staleGlyph);
            }

            // A procedural mesh rather than a font glyph, matching the settings gear: guaranteed to
            // render regardless of which characters the project's custom Turkish SDF atlas contains.
            EnsureSingleComponent<CanvasRenderer>(icon.gameObject, report);
            ProceduralArrowIconGraphic arrow =
                EnsureSingleComponent<ProceduralArrowIconGraphic>(icon.gameObject, report);
            if (arrow != null)
            {
                Undo.RecordObject(arrow, "Configure back icon arrow");
                arrow.color = BorderGoldColour;
                arrow.raycastTarget = false;
            }

            return button;
        }

        private static TextMeshProUGUI EnsureButtonText(
            RectTransform parent,
            string text,
            SceneSetupReport report)
        {
            RectTransform textTransform = EnsureUiChild(parent, "Text (TMP)", report);
            Stretch(textTransform);
            TextMeshProUGUI label = EnsureSingleComponent<TextMeshProUGUI>(
                textTransform.gameObject, report);
            ConfigureText(label, 40f);
            if (label != null && (string.IsNullOrEmpty(label.text)
                || label.text == "New Text" || label.text == "Button"))
            {
                Undo.RecordObject(label, "Set button text");
                label.text = text;
            }
            return label;
        }

        private static TextMeshProUGUI EnsureText(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size,
            float fontSize,
            SceneSetupReport report)
        {
            RectTransform transform = EnsureUiChild(parent, name, report);
            SetRect(transform, Center, Center, position, size, Center);
            TextMeshProUGUI text = EnsureSingleComponent<TextMeshProUGUI>(
                transform.gameObject, report);
            ConfigureText(text, fontSize);
            return text;
        }

        private static void ConfigureText(TextMeshProUGUI text, float fontSize)
        {
            if (text == null)
            {
                return;
            }
            Undo.RecordObject(text, "Configure TMP text");
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            if (text.font == null && TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }
        }

        private static void ConfigureReadableText(
            TextMeshProUGUI text,
            TMP_FontAsset font,
            float fontSize,
            float minimum,
            float maximum,
            bool autoSize,
            bool wrap,
            float lineSpacing)
        {
            if (text == null)
            {
                return;
            }

            ConfigureText(text, fontSize);
            Undo.RecordObject(text, "Configure readable TMP text");
            if (font != null)
            {
                text.font = font;
            }
            text.enableAutoSizing = autoSize;
            text.fontSizeMin = minimum;
            text.fontSizeMax = maximum;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.lineSpacing = lineSpacing;
        }

        private static void ConfigureButtonFont(
            Button button,
            TMP_FontAsset font,
            float fontSize,
            float minimum,
            float maximum)
        {
            ConfigureReadableText(
                button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null,
                font, fontSize, minimum, maximum, true, true, 2f);
        }

        private static void SetTextColour(TextMeshProUGUI text, Color colour)
        {
            if (text == null || ColoursMatch(text.color, colour))
            {
                return;
            }

            Undo.RecordObject(text, "Configure TMP text colour");
            text.color = colour;
        }

        private static bool ColoursMatch(Color left, Color right)
        {
            return Mathf.Approximately(left.r, right.r)
                && Mathf.Approximately(left.g, right.g)
                && Mathf.Approximately(left.b, right.b)
                && Mathf.Approximately(left.a, right.a);
        }

        private static RectTransform RepairOrCreateGameOverChild(
            Transform canvas,
            RectTransform panel,
            GameOverView view,
            string referenceProperty,
            string expectedName,
            SceneSetupReport report,
            string legacyName = null)
        {
            RectTransform existing = FindDirectChild(panel, expectedName, report);
            if (existing != null)
            {
                return existing;
            }

            GameObject referenced = GetReferencedGameObject(view, referenceProperty);
            if (referenced != null && referenced.transform.parent == canvas
                && (referenced.name == expectedName || referenced.name == legacyName))
            {
                Undo.SetTransformParent(referenced.transform, panel, "Repair game-over hierarchy");
                Undo.RecordObject(referenced, "Repair game-over name");
                referenced.name = expectedName;
                return referenced.transform as RectTransform;
            }

            RectTransform legacy = FindDirectChild(canvas, expectedName, report);
            if (legacy == null && !string.IsNullOrEmpty(legacyName))
            {
                legacy = FindDirectChild(canvas, legacyName, report);
            }
            if (legacy != null)
            {
                Undo.SetTransformParent(legacy, panel, "Repair game-over hierarchy");
                Undo.RecordObject(legacy.gameObject, "Repair game-over name");
                legacy.gameObject.name = expectedName;
                return legacy;
            }

            return EnsureUiChild(panel, expectedName, report);
        }

        private static GameObject GetReferencedGameObject(Object target, string propertyName)
        {
            Object referenced = GetObjectProperty(target, propertyName);
            if (referenced is Component component)
            {
                return component.gameObject;
            }
            return referenced as GameObject;
        }

        private static GameObject EnsureRoot(
            Scene scene,
            string name,
            SceneSetupReport report,
            bool rectTransform = false)
        {
            GameObject existing = FindUniqueRoot(scene, name, report);
            if (existing != null)
            {
                return existing;
            }

            GameObject created = rectTransform
                ? new GameObject(name, typeof(RectTransform))
                : new GameObject(name);
            Undo.RegisterCreatedObjectUndo(created, "Create " + name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static RectTransform EnsureUiChild(
            Transform parent,
            string name,
            SceneSetupReport report)
        {
            RectTransform existing = FindDirectChild(parent, name, report);
            if (existing != null)
            {
                return existing;
            }
            GameObject created = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(created, "Create " + name);
            created.transform.SetParent(parent, false);
            return (RectTransform)created.transform;
        }

        private static RectTransform FindDirectChild(
            Transform parent,
            string name,
            SceneSetupReport report)
        {
            if (parent == null)
            {
                return null;
            }
            RectTransform found = null;
            int count = 0;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name != name)
                {
                    continue;
                }
                count++;
                found = child as RectTransform;
            }
            if (count > 1)
            {
                report.Add(SceneSetupIssueSeverity.Error, "DUPLICATE_PATH", "Hierarchy",
                    parent.gameObject.scene.path, HierarchyPath(parent) + "/" + name,
                    "Multiple direct children occupy this managed path.");
            }
            return count == 1 ? found : null;
        }

        private static T EnsureSingleComponent<T>(GameObject gameObject, SceneSetupReport report)
            where T : Component
        {
            if (gameObject == null)
            {
                return null;
            }
            T[] components = gameObject.GetComponents<T>();
            if (components.Length > 1)
            {
                report.Add(SceneSetupIssueSeverity.Error, "DUPLICATE_COMPONENT", "Components",
                    gameObject.scene.path, HierarchyPath(gameObject.transform),
                    "Multiple " + typeof(T).Name + " components exist; none were deleted.");
                return components[0];
            }
            return components.Length == 1 ? components[0] : Undo.AddComponent<T>(gameObject);
        }

        private static T RequireSingleComponent<T>(
            GameObject gameObject,
            string scenePath,
            SceneSetupReport report) where T : Component
        {
            T[] components = gameObject.GetComponents<T>();
            if (components.Length != 1)
            {
                report.Add(SceneSetupIssueSeverity.Error, "COMPONENT_COUNT", "Components",
                    scenePath, HierarchyPath(gameObject.transform),
                    "Expected exactly one " + typeof(T).Name + "; found " + components.Length + ".");
                return components.Length > 0 ? components[0] : null;
            }
            return components[0];
        }

        private static void SetRect(
            RectTransform transform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Vector2 pivot)
        {
            if (transform == null)
            {
                return;
            }
            Undo.RecordObject(transform, "Configure RectTransform");
            transform.anchorMin = anchorMin;
            transform.anchorMax = anchorMax;
            transform.anchoredPosition = position;
            transform.sizeDelta = size;
            transform.pivot = pivot;
            transform.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform transform)
        {
            SetRect(transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Center);
        }

        private static void SetSiblingIndex(Transform transform, int index)
        {
            if (transform == null || transform.GetSiblingIndex() == index)
            {
                return;
            }
            Undo.RecordObject(transform, "Set managed sibling order");
            transform.SetSiblingIndex(index);
        }

        private static void SetActiveIfNeeded(GameObject gameObject, bool active)
        {
            if (gameObject == null || gameObject.activeSelf == active)
            {
                return;
            }
            Undo.RecordObject(gameObject, "Set managed active state");
            gameObject.SetActive(active);
        }

        private static void SetObjectProperty(
            Object target,
            string propertyName,
            Object value,
            SceneSetupReport report)
        {
            SerializedProperty property = FindProperty(target, propertyName, report);
            if (property == null || property.objectReferenceValue == value)
            {
                return;
            }
            Undo.RecordObject(target, "Wire " + propertyName);
            property.objectReferenceValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }

        private static void SetObjectArrayProperty<T>(
            Object target,
            string propertyName,
            T[] values,
            SceneSetupReport report) where T : Object
        {
            SerializedProperty property = FindProperty(target, propertyName, report);
            if (property == null || !property.isArray)
            {
                return;
            }
            Undo.RecordObject(target, "Wire " + propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            property.serializedObject.ApplyModifiedProperties();
        }

        private static void SetEnumProperty(
            Object target,
            string propertyName,
            int value,
            SceneSetupReport report)
        {
            SerializedProperty property = FindProperty(target, propertyName, report);
            if (property == null || property.intValue == value)
            {
                return;
            }
            Undo.RecordObject(target, "Set " + propertyName);
            property.intValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }

        private static void SetStringProperty(
            Object target,
            string propertyName,
            string value,
            SceneSetupReport report)
        {
            SerializedProperty property = FindProperty(target, propertyName, report);
            if (property == null || property.stringValue == value)
            {
                return;
            }
            Undo.RecordObject(target, "Set " + propertyName);
            property.stringValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }

        private static void SetFloatProperty(
            Object target,
            string propertyName,
            float value,
            SceneSetupReport report)
        {
            SerializedProperty property = FindProperty(target, propertyName, report);
            if (property == null || Mathf.Approximately(property.floatValue, value))
            {
                return;
            }
            Undo.RecordObject(target, "Set " + propertyName);
            property.floatValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }

        private static void SetAnimationCurveProperty(
            Object target,
            string propertyName,
            AnimationCurve curve,
            SceneSetupReport report)
        {
            SerializedProperty property = FindProperty(target, propertyName, report);
            if (property == null)
            {
                return;
            }
            Undo.RecordObject(target, "Set " + propertyName);
            property.animationCurveValue = curve;
            property.serializedObject.ApplyModifiedProperties();
        }

        private static void SetBoolProperty(
            Object target,
            string propertyName,
            bool value,
            SceneSetupReport report)
        {
            SerializedProperty property = FindProperty(target, propertyName, report);
            if (property == null || property.boolValue == value)
            {
                return;
            }
            Undo.RecordObject(target, "Set " + propertyName);
            property.boolValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }

        private static void SetColorProperty(
            Object target,
            string propertyName,
            Color value,
            SceneSetupReport report)
        {
            SerializedProperty property = FindProperty(target, propertyName, report);
            if (property == null || ColoursMatch(property.colorValue, value))
            {
                return;
            }
            Undo.RecordObject(target, "Set " + propertyName);
            property.colorValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Ensures a <see cref="CanvasGroup"/> + <see cref="PanelFadeAnimator"/> pair on
        /// <paramref name="target"/>, wired to fade that same object. <paramref name="animateScale"/>
        /// is off for in-place content swaps (settings tabs) where a scale pulse would visibly
        /// distort a clipped ScrollRect viewport; durations are left at the animator's own defaults
        /// unless overridden, so every screen-level panel shares one timing without repeating it here.
        /// </summary>
        private static PanelFadeAnimator ConfigurePanelFadeAnimator(
            GameObject target,
            SceneSetupReport report,
            bool animateScale = true,
            float? showDuration = null,
            float? hideDuration = null)
        {
            CanvasGroup group = EnsureSingleComponent<CanvasGroup>(target, report);
            PanelFadeAnimator animator = EnsureSingleComponent<PanelFadeAnimator>(target, report);
            SetObjectProperty(animator, "panelRoot", target, report);
            SetObjectProperty(animator, "canvasGroup", group, report);
            SetBoolProperty(animator, "animateScale", animateScale, report);
            if (showDuration.HasValue)
            {
                SetFloatProperty(animator, "showDuration", showDuration.Value, report);
            }
            if (hideDuration.HasValue)
            {
                SetFloatProperty(animator, "hideDuration", hideDuration.Value, report);
            }
            return animator;
        }

        /// <summary>
        /// Full-screen solid-colour cover used only as a MainMenu-to-Game scene transition wipe —
        /// distinct from <c>Background</c>, which is a permanent decorative layer. Always the last
        /// child of <paramref name="canvas"/> so it renders above every other Canvas-level object
        /// (SafeArea, and on MainMenu, Settings/About too), including ones added after it by a
        /// later Configure* call in the same Apply pass.
        /// </summary>
        /// <param name="startVisible">
        /// MainMenu's overlay starts hidden, like every other panel — it only appears for the
        /// moment of leaving. Game's starts the opposite way, already opaque: the very first
        /// rendered frame of a freshly loaded scene must never show unstyled/unsettled layout
        /// before <see cref="GameSceneController"/> reveals it in <c>Start()</c>.
        /// </param>
        private static PanelFadeAnimator ConfigureTransitionOverlay(
            Transform canvas,
            SceneSetupReport report,
            bool startVisible)
        {
            RectTransform root = EnsureUiChild(canvas, "TransitionOverlay", report);
            Stretch(root);
            Image surface = EnsureSingleComponent<Image>(root.gameObject, report);
            ConfigureSimpleImage(surface, null, OverallBackgroundColour, true);

            PanelFadeAnimator animator = ConfigurePanelFadeAnimator(
                root.gameObject, report, animateScale: false, showDuration: 0.28f, hideDuration: 0.28f);

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            if (group != null)
            {
                Undo.RecordObject(group, "Configure transition overlay resting state");
                group.alpha = startVisible ? 1f : 0f;
                group.interactable = false;
                group.blocksRaycasts = startVisible;
            }
            SetActiveIfNeeded(root.gameObject, startVisible);
            SetSiblingIndex(root, canvas.childCount - 1);

            return animator;
        }

        private static SerializedProperty FindProperty(
            Object target,
            string propertyName,
            SceneSetupReport report)
        {
            if (target == null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "NULL_SERIALIZED_TARGET", "References",
                    string.Empty, string.Empty,
                    "Cannot assign " + propertyName + " because its component is missing.");
                return null;
            }
            SerializedObject serialized = new SerializedObject(target);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "SERIALIZED_PROPERTY_MISSING", "References",
                    AssetDatabase.GetAssetPath(target),
                    target is Component component ? HierarchyPath(component.transform) : target.name,
                    target.GetType().Name + "." + propertyName + " was not found.");
            }
            return property;
        }

        private static Object GetObjectProperty(Object target, string propertyName)
        {
            if (target == null)
            {
                return null;
            }
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
            return property != null ? property.objectReferenceValue : null;
        }

        private static float GetFloatProperty(Object target, string propertyName)
        {
            if (target == null)
            {
                return 0f;
            }
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
            return property != null ? property.floatValue : 0f;
        }

        private static bool GetBoolProperty(Object target, string propertyName)
        {
            if (target == null)
            {
                return false;
            }
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
            return property != null && property.boolValue;
        }

        private static string GetStringProperty(Object target, string propertyName)
        {
            if (target == null)
            {
                return string.Empty;
            }
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
            return property != null ? property.stringValue : string.Empty;
        }

        private static void EnsureExpectedListener(
            Button button,
            Object target,
            string method,
            UnityEngine.Events.UnityAction action,
            SceneSetupReport report)
        {
            if (button == null || target == null || action == null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "BUTTON_WIRING_TARGET", "Events",
                    button != null ? button.gameObject.scene.path : string.Empty,
                    button != null ? HierarchyPath(button.transform) : string.Empty,
                    "Button or expected listener target is missing.");
                return;
            }
            int expectedCount = 0;
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                Object listenerTarget = button.onClick.GetPersistentTarget(i);
                string listenerMethod = button.onClick.GetPersistentMethodName(i);
                if (listenerTarget == target && listenerMethod == method)
                {
                    expectedCount++;
                    continue;
                }
                report.Add(SceneSetupIssueSeverity.Error, "UNEXPECTED_BUTTON_LISTENER", "Events",
                    button.gameObject.scene.path, HierarchyPath(button.transform),
                    "An unexpected persistent listener was preserved; reconcile it manually.");
            }
            if (report.ErrorCount > 0 && expectedCount == 0
                && button.onClick.GetPersistentEventCount() > 0)
            {
                return;
            }
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0 && expectedCount > 1; i--)
            {
                if (button.onClick.GetPersistentTarget(i) == target
                    && button.onClick.GetPersistentMethodName(i) == method)
                {
                    UnityEventTools.RemovePersistentListener(button.onClick, i);
                    expectedCount--;
                }
            }
            if (expectedCount == 0)
            {
                Undo.RecordObject(button, "Wire button listener");
                UnityEventTools.AddPersistentListener(button.onClick, action);
                EditorUtility.SetDirty(button);
            }
        }

        private static void ValidateExpectedListener(
            Button button,
            Object target,
            string method,
            string scenePath,
            string hierarchyPath,
            SceneSetupReport report)
        {
            if (button == null || target == null)
            {
                AddInvalid(report, scenePath, hierarchyPath, "Button or listener target is missing.");
                return;
            }
            int expected = 0;
            int unexpected = 0;
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) == target
                    && button.onClick.GetPersistentMethodName(i) == method)
                {
                    expected++;
                }
                else
                {
                    unexpected++;
                }
            }
            if (expected != 1 || unexpected != 0)
            {
                AddInvalid(report, scenePath, hierarchyPath,
                    "Expected exactly one " + method + " listener and no unexpected listeners.");
            }
        }

        // Project paths, backup, and reporting ---------------------------------------

        /// <summary>
        /// Reimports the supplied art with UI-appropriate settings. Idempotent: only touches an
        /// importer (and therefore its .meta) when a setting actually differs from the target.
        /// </summary>
        private static void ConfigureArtTextureImportSettings(SceneSetupReport report)
        {
            // Ornate/detailed hero art keeps full resolution up to 4096; small HUD icons are
            // capped lower since nothing renders them larger than a few dozen reference units.
            ConfigureArtTexture(BackgroundArtPath, 4096, report);
            ConfigureArtTexture(Background2ArtPath, 4096, report);
            ConfigureArtTexture(CardBackArtPath, 4096, report);
            ConfigureArtTexture(CardFrameArtPath, 4096, report);
            ConfigureArtTexture(SituationPanelArtPath, 4096, report);
            ConfigureArtTexture(LeftBannerArtPath, 4096, report);
            ConfigureArtTexture(RightBannerArtPath, 4096, report);
            ConfigureArtTexture(PeopleIconArtPath, 1024, report);
            ConfigureArtTexture(SecurityIconArtPath, 1024, report);
            ConfigureArtTexture(AuthorityIconArtPath, 1024, report);
            ConfigureArtTexture(WealthIconArtPath, 1024, report);
        }

        private static void ConfigureArtTexture(string path, int maxSize, SceneSetupReport report)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                report.Add(SceneSetupIssueSeverity.Warning, "ART_ASSET_MISSING", "Assets",
                    path, string.Empty, "Supplied art asset not found at the expected path.");
                return;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                changed = true;
            }
            if (importer.maxTextureSize != maxSize)
            {
                importer.maxTextureSize = maxSize;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// Wires the supplied art into the default theme. A missing file degrades to a warning and
        /// that theme field stays whatever it already was (usually null, i.e. the existing
        /// procedural/flat/letter fallback keeps working) rather than blocking the whole apply.
        /// </summary>
        private static void AssignSuppliedArt(GameUITheme theme, SceneSetupReport report)
        {
            if (theme == null)
            {
                return;
            }

            // Final mobile Reigns-style presentation: no illustrated background behind the flat
            // dark HUD / aged-beige ContentPanel composition — Card.png is the only supplied art
            // still wired in, as the fixed card-back revealed behind the swipeable portrait. The
            // old frame/parchment/swipe-banner art (KartÇerçevesi, Parşömen, sol/SağSwipeBanner)
            // and both background paintings (Background, Background2) are left imported but
            // deliberately not wired here — backgroundSprite stays null so BackgroundView falls
            // back to its flat OverallBackgroundColour, which is itself only ever visible behind
            // TopBar/HUD/ContentPanel's own opaque surfaces, i.e. never on screen. Source files
            // are not deleted; only unused.
            SetObjectProperty(theme, "backgroundSprite", null, report);
            SetObjectProperty(theme, "cardBackSprite", LoadArtSprite(CardBackArtPath, report), report);
            SetObjectProperty(theme, "cardFrameSprite", null, report);
            SetObjectProperty(theme, "situationPanelSprite", null, report);
            SetObjectProperty(theme, "leftEdgeSprite", null, report);
            SetObjectProperty(theme, "rightEdgeSprite", null, report);
            SetObjectProperty(theme, "peopleIcon", LoadArtSprite(PeopleIconArtPath, report), report);
            SetObjectProperty(theme, "securityIcon", LoadArtSprite(SecurityIconArtPath, report), report);
            SetObjectProperty(theme, "authorityIcon", LoadArtSprite(AuthorityIconArtPath, report), report);
            SetObjectProperty(theme, "wealthIcon", LoadArtSprite(WealthIconArtPath, report), report);
            SetColorProperty(theme, "leftChoice", ChoicePreviewLeftTint, report);
            SetColorProperty(theme, "rightChoice", ChoicePreviewRightTint, report);
        }

        /// <summary>
        /// Imports the supplied character portraits and wires each onto every real Story
        /// CardDefinition whose authored <c>speaker</c> field matches it exactly. Only the card's
        /// top-level speaker is considered — a <c>CardVariant</c> that overrides the speaker to a
        /// different character is a pre-existing schema limitation (CardVariant has no portrait
        /// field of its own) and is reported, not guessed around. Idempotent: re-running with the
        /// same art and cards changes nothing.
        /// </summary>
        private static SceneSetupReport AssignCharacterPortraits()
        {
            SceneSetupReport report = new SceneSetupReport("Assign Character Portraits");

            ConfigureArtTexture(OmerPortraitArtPath, CharacterPortraitMaxSize, report);
            ConfigureArtTexture(SabihaPortraitArtPath, CharacterPortraitMaxSize, report);
            ConfigureArtTexture(ZeynepPortraitArtPath, CharacterPortraitMaxSize, report);
            ConfigureArtTexture(ZeynepBandagedPortraitArtPath, CharacterPortraitMaxSize, report);
            ConfigureArtTexture(AtillaPortraitArtPath, CharacterPortraitMaxSize, report);
            ConfigureArtTexture(AzizPortraitArtPath, CharacterPortraitMaxSize, report);
            ConfigureArtTexture(IsmetPortraitArtPath, CharacterPortraitMaxSize, report);

            Dictionary<string, Sprite> spritesBySpeaker = new Dictionary<string, Sprite>();
            for (int i = 0; i < CharacterPortraitMap.Length; i++)
            {
                (string speaker, string artPath) = CharacterPortraitMap[i];
                Sprite sprite = LoadArtSprite(artPath, report);
                if (sprite != null)
                {
                    spritesBySpeaker[speaker] = sprite;
                }
            }

            string[] guids = AssetDatabase.FindAssets("t:CardDefinition", new[] { StoryCardsFolder });
            int assignedCount = 0;
            int alreadyCorrectCount = 0;
            int noArtCount = 0;
            SortedSet<string> speakersWithNoArt = new SortedSet<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CardDefinition card = AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
                if (card == null)
                {
                    continue;
                }

                if (spritesBySpeaker.TryGetValue(card.Speaker, out Sprite sprite))
                {
                    bool alreadySet = card.Portrait == sprite;
                    SetObjectProperty(card, "portrait", sprite, report);
                    if (alreadySet)
                    {
                        alreadyCorrectCount++;
                    }
                    else
                    {
                        assignedCount++;
                    }
                }
                else if (!string.IsNullOrEmpty(card.Speaker) && card.Speaker != "Anlatıcı")
                {
                    noArtCount++;
                    speakersWithNoArt.Add(card.Speaker);
                }
            }

            AssetDatabase.SaveAssets();

            report.Add(SceneSetupIssueSeverity.Info, "CHARACTER_PORTRAITS_ASSIGNED", "Content",
                StoryCardsFolder, string.Empty,
                assignedCount + " card(s) newly assigned a portrait; " + alreadyCorrectCount
                    + " already correct; " + noArtCount
                    + " card(s) have a named speaker with no supplied art yet ("
                    + string.Join(", ", speakersWithNoArt) + ").");

            return report;
        }

        private static Sprite LoadArtSprite(string path, SceneSetupReport report)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                report.Add(SceneSetupIssueSeverity.Warning, "ART_ASSET_MISSING", "Assets",
                    path, string.Empty, "Supplied art asset not found or not importing as a Sprite.");
            }
            return sprite;
        }

        private static GameUITheme EnsureDefaultTheme(SceneSetupReport report)
        {
            Object existing = AssetDatabase.LoadMainAssetAtPath(DefaultThemePath);
            if (existing != null && !(existing is GameUITheme))
            {
                report.Add(SceneSetupIssueSeverity.Error, "ASSET_TYPE_CONFLICT", "Assets",
                    DefaultThemePath, string.Empty,
                    "The default UI theme path is occupied by " + existing.GetType().Name + ".");
                return null;
            }

            if (existing is GameUITheme theme)
            {
                return theme;
            }

            EnsureAssetFolder("Assets/_Game/Content/UI");
            GameUITheme created = ScriptableObject.CreateInstance<GameUITheme>();
            AssetDatabase.CreateAsset(created, DefaultThemePath);
            return created;
        }

        private static FeedbackCueProfile EnsureDefaultFeedbackCueProfile(
            SceneSetupReport report)
        {
            Object existing = AssetDatabase.LoadMainAssetAtPath(DefaultFeedbackCueProfilePath);
            if (existing != null && !(existing is FeedbackCueProfile))
            {
                report.Add(SceneSetupIssueSeverity.Error, "ASSET_TYPE_CONFLICT", "Assets",
                    DefaultFeedbackCueProfilePath, string.Empty,
                    "The feedback profile path is occupied by " + existing.GetType().Name + ".");
                return null;
            }
            if (existing is FeedbackCueProfile profile)
            {
                return profile;
            }
            EnsureAssetFolder("Assets/_Game/Content/UI");
            FeedbackCueProfile created = ScriptableObject.CreateInstance<FeedbackCueProfile>();
            AssetDatabase.CreateAsset(created, DefaultFeedbackCueProfilePath);
            return created;
        }

        private static void EnsureAssetFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static SessionIntent EnsureSessionIntent(SceneSetupReport report)
        {
            Object existing = AssetDatabase.LoadMainAssetAtPath(SessionIntentPath);
            if (existing != null && !(existing is SessionIntent))
            {
                report.Add(SceneSetupIssueSeverity.Error, "ASSET_TYPE_CONFLICT", "Assets",
                    SessionIntentPath, string.Empty,
                    "The SessionIntent path is occupied by " + existing.GetType().Name + ".");
                return null;
            }
            if (existing is SessionIntent intent)
            {
                return intent;
            }
            SessionIntent created = ScriptableObject.CreateInstance<SessionIntent>();
            AssetDatabase.CreateAsset(created, SessionIntentPath);
            return created;
        }

        private static Scene OpenRequiredScene(string path, SceneSetupReport report)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "SCENE_MISSING", "Scenes",
                    path, string.Empty, "Required existing scene is missing.");
                return default;
            }
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        private static Scene OpenOrCreateEmptyScene(string path)
        {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null
                ? EditorSceneManager.OpenScene(path, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void ApplyBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
        }

        private static BackupManifest CreateBackup(SceneSetupReport report)
        {
            string backupRoot = BackupAbsolutePath;
            if (Directory.Exists(backupRoot))
            {
                FileUtil.DeleteFileOrDirectory(backupRoot);
            }
            Directory.CreateDirectory(backupRoot);
            BackupManifest manifest = new BackupManifest
            {
                buildScenes = BuildSceneRecords(EditorBuildSettings.scenes)
            };
            string[] managedAssets =
            {
                GameScenePath, BootstrapScenePath, MainMenuScenePath, SessionIntentPath,
                DefaultThemePath, DefaultFeedbackCueProfilePath
            };
            for (int i = 0; i < managedAssets.Length; i++)
            {
                string path = managedAssets[i];
                string absolute = AbsoluteProjectPath(path);
                if (File.Exists(absolute))
                {
                    string backup = Path.Combine(backupRoot, Path.GetFileName(path));
                    FileUtil.CopyFileOrDirectory(absolute, backup);
                    manifest.backups.Add(new BackupFileRecord { assetPath = path, backupPath = backup });
                }
                else
                {
                    manifest.createdAssetPaths.Add(path);
                }
            }
            File.WriteAllText(Path.Combine(backupRoot, BackupManifestName),
                JsonUtility.ToJson(manifest, true));
            report.Add(SceneSetupIssueSeverity.Info, "BACKUP_CREATED", "Rollback",
                backupRoot, string.Empty, "Pre-apply backup created.");
            return manifest;
        }

        private static void RestoreBackup(SceneSetupReport report)
        {
            string manifestPath = Path.Combine(BackupAbsolutePath, BackupManifestName);
            if (!File.Exists(manifestPath))
            {
                report.Add(SceneSetupIssueSeverity.Error, "BACKUP_MISSING", "Rollback",
                    manifestPath, string.Empty, "No scene-setup backup manifest exists.");
                return;
            }
            BackupManifest manifest = JsonUtility.FromJson<BackupManifest>(
                File.ReadAllText(manifestPath));
            RestoreBackup(report, manifest);
        }

        private static void RestoreBackup(SceneSetupReport report, BackupManifest manifest)
        {
            if (manifest == null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "BACKUP_INVALID", "Rollback",
                    BackupAbsolutePath, string.Empty, "Backup manifest is invalid.");
                return;
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            for (int i = 0; i < manifest.createdAssetPaths.Count; i++)
            {
                if (AssetDatabase.LoadMainAssetAtPath(manifest.createdAssetPaths[i]) != null)
                {
                    AssetDatabase.DeleteAsset(manifest.createdAssetPaths[i]);
                }
            }
            for (int i = 0; i < manifest.backups.Count; i++)
            {
                BackupFileRecord backup = manifest.backups[i];
                if (!File.Exists(backup.backupPath))
                {
                    report.Add(SceneSetupIssueSeverity.Error, "BACKUP_FILE_MISSING", "Rollback",
                        backup.backupPath, string.Empty, "A backup file is missing.");
                    continue;
                }
                string destination = AbsoluteProjectPath(backup.assetPath);
                if (File.Exists(destination))
                {
                    FileUtil.DeleteFileOrDirectory(destination);
                }
                FileUtil.CopyFileOrDirectory(backup.backupPath, destination);
            }
            EditorBuildSettings.scenes = RestoreBuildScenes(manifest.buildScenes);
            AssetDatabase.Refresh();
            report.Add(SceneSetupIssueSeverity.Info, "BACKUP_RESTORED", "Rollback",
                BackupAbsolutePath, string.Empty, "The last scene-setup backup was restored.");
        }

        private static BuildSceneRecord[] BuildSceneRecords(EditorBuildSettingsScene[] scenes)
        {
            BuildSceneRecord[] records = new BuildSceneRecord[scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                records[i] = new BuildSceneRecord { path = scenes[i].path, enabled = scenes[i].enabled };
            }
            return records;
        }

        private static EditorBuildSettingsScene[] RestoreBuildScenes(BuildSceneRecord[] records)
        {
            if (records == null)
            {
                return Array.Empty<EditorBuildSettingsScene>();
            }
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[records.Length];
            for (int i = 0; i < records.Length; i++)
            {
                scenes[i] = new EditorBuildSettingsScene(records[i].path, records[i].enabled);
            }
            return scenes;
        }

        private static void WriteAndLog(SceneSetupReport report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AbsoluteProjectPath(ReportPath)));
            File.WriteAllText(AbsoluteProjectPath(ReportPath), JsonUtility.ToJson(report, true));
            for (int i = 0; i < report.Issues.Count; i++)
            {
                SceneSetupIssue issue = report.Issues[i];
                string line = "[SceneSetup][" + issue.Code + "] " + issue.Message
                    + (string.IsNullOrEmpty(issue.HierarchyPath)
                        ? string.Empty
                        : " (" + issue.HierarchyPath + ")");
                if (issue.Severity == SceneSetupIssueSeverity.Error)
                {
                    Debug.LogError(line);
                }
                else if (issue.Severity == SceneSetupIssueSeverity.Warning)
                {
                    Debug.LogWarning(line);
                }
                else
                {
                    Debug.Log(line);
                }
            }
            Debug.Log("[SceneSetup] " + report.Operation + ": " + report.ErrorCount
                + " errors, " + report.WarningCount + " warnings, " + report.InfoCount + " info.");
        }

        // Lookup and validation helpers ---------------------------------------------

        private static bool CheckRootDuplicates(
            Scene scene,
            string name,
            SceneSetupReport report)
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name)
                {
                    count++;
                }
            }
            if (count <= 1)
            {
                return true;
            }
            report.Add(SceneSetupIssueSeverity.Error, "DUPLICATE_ROOT", "Hierarchy",
                scene.path, "/" + name,
                "Multiple root objects occupy this managed path; none were deleted.");
            return false;
        }

        private static GameObject FindUniqueRoot(
            Scene scene,
            string name,
            SceneSetupReport report)
        {
            GameObject found = null;
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name != name)
                {
                    continue;
                }
                found = roots[i];
                count++;
            }
            if (count > 1 && report != null)
            {
                report.Add(SceneSetupIssueSeverity.Error, "DUPLICATE_ROOT", "Hierarchy",
                    scene.path, "/" + name, "Multiple root objects occupy this managed path.");
            }
            return count == 1 ? found : null;
        }

        private static T[] FindComponentsInScene<T>(Scene scene) where T : Component
        {
            List<T> found = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                found.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }
            return found.ToArray();
        }

        private static GameObject RequirePath(
            Scene scene,
            string path,
            SceneSetupReport report)
        {
            string[] parts = path.Trim('/').Split('/');
            if (parts.Length == 0)
            {
                return null;
            }
            List<GameObject> roots = new List<GameObject>();
            GameObject[] sceneRoots = scene.GetRootGameObjects();
            for (int i = 0; i < sceneRoots.Length; i++)
            {
                if (sceneRoots[i].name == parts[0])
                {
                    roots.Add(sceneRoots[i]);
                }
            }
            if (roots.Count != 1)
            {
                report.Add(SceneSetupIssueSeverity.Error, "PATH_COUNT", "Hierarchy",
                    scene.path, "/" + parts[0],
                    "Expected one object at this managed path; found " + roots.Count + ".");
                return null;
            }
            Transform current = roots[0].transform;
            for (int p = 1; p < parts.Length; p++)
            {
                Transform next = null;
                int count = 0;
                for (int i = 0; i < current.childCount; i++)
                {
                    Transform child = current.GetChild(i);
                    if (child.name == parts[p])
                    {
                        next = child;
                        count++;
                    }
                }
                if (count != 1)
                {
                    report.Add(SceneSetupIssueSeverity.Error, "PATH_COUNT", "Hierarchy",
                        scene.path, string.Join("/", parts, 0, p + 1).Insert(0, "/"),
                        "Expected one object at this managed path; found " + count + ".");
                    return null;
                }
                current = next;
            }
            return current.gameObject;
        }

        /// <summary>Validate-side counterpart to <see cref="ConfigurePanelFadeAnimator"/>.</summary>
        private static PanelFadeAnimator ValidatePanelFadeAnimator(
            GameObject target, string scenePath, string hierarchyPath, SceneSetupReport report)
        {
            if (target == null)
            {
                return null;
            }
            CanvasGroup group = RequireSingleComponent<CanvasGroup>(target, scenePath, report);
            PanelFadeAnimator animator = RequireSingleComponent<PanelFadeAnimator>(
                target, scenePath, report);
            ValidateReference(animator, "panelRoot", target, scenePath, hierarchyPath, report);
            ValidateReference(animator, "canvasGroup", group, scenePath, hierarchyPath, report);
            return animator;
        }

        private static void ValidateReference(
            Object target,
            string propertyName,
            Object expected,
            string scenePath,
            string hierarchyPath,
            SceneSetupReport report)
        {
            if (target == null || GetObjectProperty(target, propertyName) != expected)
            {
                AddInvalid(report, scenePath, hierarchyPath,
                    (target != null ? target.GetType().Name : "Missing component")
                    + "." + propertyName + " is incorrect.");
            }
        }

        private static void AddInvalid(
            SceneSetupReport report,
            string scenePath,
            string hierarchyPath,
            string message)
        {
            report.Add(SceneSetupIssueSeverity.Error, "INVALID_SETUP", "Validation",
                scenePath, hierarchyPath, message);
        }

        private static string HierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }
            string path = "/" + transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = "/" + transform.name + path;
            }
            return path;
        }

        private static string AbsoluteProjectPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", relativePath));
        }

        private static string BackupAbsolutePath => AbsoluteProjectPath(BackupRelativePath);

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        private readonly struct CardParts
        {
            public CardParts(RectTransform area, CardView view, CardSwipeController swipe)
            {
                Area = area;
                View = view;
                Swipe = swipe;
            }
            public RectTransform Area { get; }
            public CardView View { get; }
            public CardSwipeController Swipe { get; }
        }

        private readonly struct TapChoiceButtonsParts
        {
            public TapChoiceButtonsParts(RectTransform root, TapChoiceButtonsView view)
            {
                Root = root;
                View = view;
            }
            public RectTransform Root { get; }
            public TapChoiceButtonsView View { get; }
        }

        private readonly struct GameOverParts
        {
            public GameOverParts(RectTransform root, GameOverView view)
            {
                Root = root;
                View = view;
            }
            public RectTransform Root { get; }
            public GameOverView View { get; }
        }

        private readonly struct SituationAreaParts
        {
            public SituationAreaParts(
                RectTransform root,
                TextMeshProUGUI text,
                Image artwork,
                ProceduralRoundedRectGraphic fallback)
            {
                Root = root;
                Text = text;
                Artwork = artwork;
                Fallback = fallback;
            }

            public RectTransform Root { get; }
            public TextMeshProUGUI Text { get; }
            public Image Artwork { get; }
            public ProceduralRoundedRectGraphic Fallback { get; }
        }

        private readonly struct FooterParts
        {
            public FooterParts(RectTransform root, RunStatusView runStatus, FooterView footer)
            {
                Root = root;
                RunStatus = runStatus;
                Footer = footer;
            }

            public RectTransform Root { get; }
            public RunStatusView RunStatus { get; }
            public FooterView Footer { get; }
        }

        private readonly struct TutorialParts
        {
            public TutorialParts(
                RectTransform root,
                TutorialOverlayView view,
                TutorialCoordinator coordinator)
            {
                Root = root;
                View = view;
                Coordinator = coordinator;
            }

            public RectTransform Root { get; }
            public TutorialOverlayView View { get; }
            public TutorialCoordinator Coordinator { get; }
        }

        private readonly struct SettingsParts
        {
            public SettingsParts(
                RectTransform root,
                SettingsPanelView view,
                SettingsController controller,
                PanelFadeAnimator[] panelAnimators)
            {
                Root = root;
                View = view;
                Controller = controller;
                PanelAnimators = panelAnimators;
            }

            public RectTransform Root { get; }
            public SettingsPanelView View { get; }
            public SettingsController Controller { get; }

            /// <summary>The panel-level open/close animator and the tab-crossfade animator built
            /// inside <see cref="ConfigureSettingsPanel"/>, for the caller to fold into the
            /// MainMenu-wide <see cref="AccessibilityPresentationController"/>.</summary>
            public PanelFadeAnimator[] PanelAnimators { get; }
        }

        [Serializable]
        private sealed class BackupManifest
        {
            public List<BackupFileRecord> backups = new List<BackupFileRecord>();
            public List<string> createdAssetPaths = new List<string>();
            public BuildSceneRecord[] buildScenes = Array.Empty<BuildSceneRecord>();
        }

        [Serializable]
        private sealed class BackupFileRecord
        {
            public string assetPath;
            public string backupPath;
        }

        [Serializable]
        private sealed class BuildSceneRecord
        {
            public string path;
            public bool enabled;
        }
    }
}
