using System.Collections;
using NUnit.Framework;
using RoyalDecisions.Composition;
using RoyalDecisions.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.PlayMode
{
    /// <summary>
    /// Drives the real Settings screen — <see cref="SettingsController"/>, <see cref="SettingsPanelView"/>
    /// and all four tab views — through actual <see cref="Slider"/>/<see cref="Toggle"/>/
    /// <see cref="Button"/>/<see cref="ToggleGroup"/> components, exactly as SceneSetupAutomation wires
    /// the real MainMenu scene.
    /// </summary>
    /// <remarks>
    /// EditMode proves each view's isolated logic against fakes. This proves the whole stack wired
    /// together the way a player actually touches it — a slider drag firing through a real Unity
    /// UI component, a grouped toggle actually excluding its siblings, Apply/Cancel/Reset actually
    /// round-tripping through a persisted store — which no EditMode assertion can, since it never
    /// exercises a real Selectable's value-change pipeline or a real ToggleGroup.
    /// </remarks>
    [TestFixture]
    public class SettingsFlowPlayModeTests
    {
        private GameObject root;

        private RoyalDecisions.Presentation.SettingsPanelView view;
        private SettingsController controller;
        private RoyalDecisions.Presentation.AudioService audioService;
        private RoyalDecisions.Presentation.GraphicsSettingsPanelView graphicsPanel;
        private RoyalDecisions.Presentation.GeneralSettingsPanelView generalPanel;
        private StubSettingsStore store;

        private Slider masterSlider;
        private Slider musicSlider;
        private Slider sfxSlider;
        private Slider sensitivitySlider;
        private Slider frameRateSlider;
        private TMP_Text masterLabel;
        private TMP_Text musicLabel;
        private TMP_Text sfxLabel;
        private TMP_Text sensitivityLabel;
        private TMP_Text frameRateLabel;
        private Toggle muteToggle;
        private Toggle batterySaver;
        private Toggle tapButtons;
        private Toggle invertRotation;
        private Toggle disableSwipe;
        private Toggle haptics;
        private Toggle reducedMotion;
        private Toggle textSmall;
        private Toggle textNormal;
        private Toggle textLarge;
        private Toggle highContrast;
        private Button applyButton;
        private Button cancelButton;
        private Button resetButton;
        private Button resetProgressButton;
        private Button resetTutorialButton;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.Destroy(root);
                root = null;
            }
        }

        private static T Child<T>(Transform parent, string name) where T : Component
        {
            GameObject go = new GameObject(name);
            go.AddComponent<RectTransform>().SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private static GameSettings CustomSettings()
        {
            GameSettings settings = GameSettings.CreateDefault();
            settings.SetMasterVolume(0.65f);
            settings.SetMusicVolume(0.4f);
            settings.SetSfxVolume(0.8f);
            settings.SetFrameRateMode(FrameRateMode.Thirty);
            settings.SetSwipeSensitivity(0.7f);
            settings.SetDisableSwipe(true);
            settings.SetTextSizeMode(TextSizeMode.Large);
            return settings;
        }

        /// <summary>Builds the whole panel inactive, wires every cross-reference, then activates
        /// it once — the same sequencing <see cref="GameCompositionPlayModeTests"/> already relies
        /// on for real MonoBehaviour wiring in this codebase.</summary>
        private void BuildScene(GameSettings seed, StubSettingsStore existingStore = null)
        {
            root = new GameObject("SettingsRoot");
            root.SetActive(false);
            root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            GameObject panelRoot = new GameObject("SettingsPanel");
            panelRoot.AddComponent<RectTransform>().SetParent(root.transform, false);

            // --- Audio tab ---
            Transform audioTab = new GameObject("AudioTab").transform;
            audioTab.SetParent(panelRoot.transform, false);
            masterSlider = Child<Slider>(audioTab, "Master");
            musicSlider = Child<Slider>(audioTab, "Music");
            sfxSlider = Child<Slider>(audioTab, "Sfx");
            muteToggle = Child<Toggle>(audioTab, "Mute");
            masterLabel = Child<TextMeshProUGUI>(audioTab, "MasterLabel");
            musicLabel = Child<TextMeshProUGUI>(audioTab, "MusicLabel");
            sfxLabel = Child<TextMeshProUGUI>(audioTab, "SfxLabel");
            RoyalDecisions.Presentation.AudioSettingsPanelView audioPanel =
                audioTab.gameObject.AddComponent<RoyalDecisions.Presentation.AudioSettingsPanelView>();
            audioPanel.SetAuthoringReferences(
                masterSlider, musicSlider, sfxSlider, muteToggle, masterLabel, musicLabel, sfxLabel);

            // --- Graphics tab ---
            Transform graphicsTab = new GameObject("GraphicsTab").transform;
            graphicsTab.SetParent(panelRoot.transform, false);
            frameRateSlider = Child<Slider>(graphicsTab, "FrameRate");
            frameRateSlider.minValue = 0f;
            frameRateSlider.maxValue = 3f;
            frameRateSlider.wholeNumbers = true;
            frameRateLabel = Child<TextMeshProUGUI>(graphicsTab, "FrameRateLabel");
            batterySaver = Child<Toggle>(graphicsTab, "Battery");
            graphicsPanel = graphicsTab.gameObject
                .AddComponent<RoyalDecisions.Presentation.GraphicsSettingsPanelView>();
            graphicsPanel.SetAuthoringReferences(frameRateSlider, frameRateLabel, batterySaver);

            // --- Controls tab ---
            Transform controlsTab = new GameObject("ControlsTab").transform;
            controlsTab.SetParent(panelRoot.transform, false);
            sensitivitySlider = Child<Slider>(controlsTab, "Sensitivity");
            sensitivityLabel = Child<TextMeshProUGUI>(controlsTab, "SensitivityLabel");
            tapButtons = Child<Toggle>(controlsTab, "TapButtons");
            invertRotation = Child<Toggle>(controlsTab, "Invert");
            disableSwipe = Child<Toggle>(controlsTab, "DisableSwipe");
            haptics = Child<Toggle>(controlsTab, "Haptics");
            RoyalDecisions.Presentation.ControlsSettingsPanelView controlsPanel = controlsTab.gameObject
                .AddComponent<RoyalDecisions.Presentation.ControlsSettingsPanelView>();
            controlsPanel.SetAuthoringReferences(
                sensitivitySlider, tapButtons, invertRotation, disableSwipe, haptics, sensitivityLabel);

            // --- General tab ---
            Transform generalTab = new GameObject("GeneralTab").transform;
            generalTab.SetParent(panelRoot.transform, false);
            reducedMotion = Child<Toggle>(generalTab, "ReducedMotion");
            textSmall = Child<Toggle>(generalTab, "TextSmall");
            textNormal = Child<Toggle>(generalTab, "TextNormal");
            textLarge = Child<Toggle>(generalTab, "TextLarge");
            ToggleGroup textGroup = generalTab.gameObject.AddComponent<ToggleGroup>();
            textGroup.allowSwitchOff = false;
            textSmall.group = textGroup;
            textNormal.group = textGroup;
            textLarge.group = textGroup;
            highContrast = Child<Toggle>(generalTab, "HighContrast");
            TMP_Text languageLabel = Child<TextMeshProUGUI>(generalTab, "LanguageLabel");
            resetProgressButton = Child<Button>(generalTab, "ResetProgress");
            GameObject idleLabel = new GameObject("Idle");
            idleLabel.transform.SetParent(resetProgressButton.transform, false);
            GameObject armedLabel = new GameObject("Armed");
            armedLabel.transform.SetParent(resetProgressButton.transform, false);
            resetTutorialButton = Child<Button>(generalTab, "ResetTutorial");
            Button about = Child<Button>(generalTab, "About");
            generalPanel = generalTab.gameObject
                .AddComponent<RoyalDecisions.Presentation.GeneralSettingsPanelView>();
            generalPanel.SetAuthoringReferences(
                reducedMotion, textSmall, textNormal, textLarge, highContrast,
                resetProgressButton, idleLabel, armedLabel, resetTutorialButton, about, languageLabel);

            // --- Bottom actions ---
            applyButton = Child<Button>(panelRoot.transform, "Apply");
            cancelButton = Child<Button>(panelRoot.transform, "Cancel");
            resetButton = Child<Button>(panelRoot.transform, "Reset");

            view = panelRoot.AddComponent<RoyalDecisions.Presentation.SettingsPanelView>();
            view.SetAuthoringReferences(
                panelRoot, audioPanel, graphicsPanel, controlsPanel, generalPanel,
                null, null, null, null, applyButton, cancelButton, resetButton);
            panelRoot.SetActive(false); // starts closed, exactly like the authored scene

            // --- Audio service ---
            GameObject audioServiceObject = new GameObject("AudioService");
            audioServiceObject.transform.SetParent(root.transform, false);
            AudioSource sfxSource = audioServiceObject.AddComponent<AudioSource>();
            AudioSource musicSource = audioServiceObject.AddComponent<AudioSource>();
            audioService = audioServiceObject.AddComponent<RoyalDecisions.Presentation.AudioService>();
            audioService.SetAuthoringReferences(sfxSource, null, musicSource);

            // --- Controller ---
            GameObject controllerObject = new GameObject("SettingsController");
            controllerObject.transform.SetParent(root.transform, false);
            controller = controllerObject.AddComponent<SettingsController>();
            controller.SetAuthoringReferences(view, audioService, null);

            store = existingStore ?? new StubSettingsStore();
            if (seed != null)
            {
                store.Save(seed);
            }
            controller.Configure(store);

            root.SetActive(true);
        }

        // --- 1. The screen genuinely opens and shows the persisted values -------------------

        [UnityTest]
        public IEnumerator OpeningSettingsShowsThePersistedValues()
        {
            BuildScene(CustomSettings());
            yield return null;

            Assert.That(view.IsOpen, Is.False, "closed until Open() is called");
            controller.Open();
            yield return null;

            Assert.That(view.IsOpen, Is.True);
            Assert.That(masterSlider.value, Is.EqualTo(0.65f).Within(0.001f));
            Assert.That(musicSlider.value, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(sfxSlider.value, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(masterLabel.text, Is.EqualTo("65%"));
            Assert.That(musicLabel.text, Is.EqualTo("40%"));
            Assert.That(sfxLabel.text, Is.EqualTo("80%"));
            Assert.That(frameRateSlider.value, Is.EqualTo(0f).Within(0.001f), "Thirty is step 0");
            Assert.That(frameRateLabel.text, Is.EqualTo("30 FPS"));
            Assert.That(sensitivitySlider.value, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(disableSwipe.isOn, Is.True);
            Assert.That(textLarge.isOn, Is.True);
            Assert.That(textSmall.isOn, Is.False);
            Assert.That(textNormal.isOn, Is.False);
        }

        // --- 2. Sliders move, percentages track live, 0% and 100% render correctly ----------

        [UnityTest]
        public IEnumerator DraggingSlidersUpdatesPercentagesLiveAndZeroAndMaxRenderCorrectly()
        {
            BuildScene(GameSettings.CreateDefault());
            yield return null;
            controller.Open();
            yield return null;

            masterSlider.value = 0.5f;
            Assert.That(masterLabel.text, Is.EqualTo("50%"));

            musicSlider.value = 0f;
            Assert.That(musicLabel.text, Is.EqualTo("0%"), "zero must render as 0%, not blank/NaN");

            sfxSlider.value = 1f;
            Assert.That(sfxLabel.text, Is.EqualTo("100%"));

            applyButton.onClick.Invoke();
            yield return null;

            GameSettings saved = store.Load();
            Assert.That(saved.MasterVolume, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(saved.MusicVolume, Is.EqualTo(0f).Within(0.001f));
            Assert.That(saved.SfxVolume, Is.EqualTo(1f).Within(0.001f));
        }

        // --- 3. Mute never zeroes the underlying volumes -----------------------------------

        [UnityTest]
        public IEnumerator MutingAndUnmutingLeavesEveryVolumeExactlyAsItWas()
        {
            BuildScene(GameSettings.CreateDefault());
            yield return null;
            controller.Open();
            yield return null;

            masterSlider.value = 0.3f;
            float musicBefore = audioService.MusicVolume;
            float sfxBefore = audioService.Volume;

            muteToggle.isOn = true;
            yield return null;
            Assert.That(audioService.IsMuted, Is.True);
            Assert.That(audioService.MasterVolume, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(audioService.MusicVolume, Is.EqualTo(musicBefore).Within(0.0001f));
            Assert.That(audioService.Volume, Is.EqualTo(sfxBefore).Within(0.0001f));

            muteToggle.isOn = false;
            yield return null;
            Assert.That(audioService.IsMuted, Is.False);
            Assert.That(audioService.MasterVolume, Is.EqualTo(0.3f).Within(0.001f),
                "unmuting must not have reset the master slider's value");
            Assert.That(audioService.MusicVolume, Is.EqualTo(musicBefore).Within(0.0001f));
            Assert.That(audioService.Volume, Is.EqualTo(sfxBefore).Within(0.0001f));
        }

        // --- 4. Frame rate is a real mutually-exclusive choice, and it persists -------------

        [UnityTest]
        public IEnumerator FrameRateSliderStepsThroughAllFourModesAndPersists()
        {
            BuildScene(GameSettings.CreateDefault());
            yield return null;
            controller.Open();
            yield return null;
            view.ShowGraphicsTab();
            yield return null;

            Assert.That(frameRateSlider.value, Is.EqualTo(1f).Within(0.001f), "60 FPS is the default");
            Assert.That(frameRateLabel.text, Is.EqualTo("60 FPS"));

            frameRateSlider.value = 0f;
            yield return null;
            Assert.That(graphicsPanel.FrameRateMode, Is.EqualTo(FrameRateMode.Thirty));
            Assert.That(frameRateLabel.text, Is.EqualTo("30 FPS"));

            frameRateSlider.value = 2f;
            yield return null;
            Assert.That(graphicsPanel.FrameRateMode, Is.EqualTo(FrameRateMode.Ninety));
            Assert.That(frameRateLabel.text, Is.EqualTo("90 FPS"));

            frameRateSlider.value = 3f;
            yield return null;
            Assert.That(graphicsPanel.FrameRateMode, Is.EqualTo(FrameRateMode.OneTwenty));
            Assert.That(frameRateLabel.text, Is.EqualTo("120 FPS"));

            applyButton.onClick.Invoke();
            yield return null;
            Assert.That(store.Load().FrameRateMode, Is.EqualTo(FrameRateMode.OneTwenty));
            Assert.That(UnityEngine.Application.targetFrameRate, Is.EqualTo(120),
                "Application.targetFrameRate must reflect the applied selection");
            Assert.That(QualitySettings.vSyncCount, Is.EqualTo(0),
                "vSyncCount must be 0 or targetFrameRate is ignored by the engine");

            controller.Open();
            yield return null;
            Assert.That(frameRateSlider.value, Is.EqualTo(3f).Within(0.001f),
                "reopening must reflect the persisted selection");
            Assert.That(frameRateLabel.text, Is.EqualTo("120 FPS"));
        }

        // --- 4b. Battery Saver temporarily overrides the applied rate without losing the pick --

        [UnityTest]
        public IEnumerator BatterySaverCapsFrameRateAndRestoresTheChosenValueWhenTurnedOff()
        {
            BuildScene(GameSettings.CreateDefault());
            yield return null;
            controller.Open();
            yield return null;
            view.ShowGraphicsTab();
            yield return null;

            frameRateSlider.value = 3f; // 120 FPS
            batterySaver.isOn = true;
            applyButton.onClick.Invoke();
            yield return null;

            Assert.That(UnityEngine.Application.targetFrameRate, Is.EqualTo(30),
                "Battery Saver caps the applied rate to 30 while enabled");
            Assert.That(store.Load().FrameRateMode, Is.EqualTo(FrameRateMode.OneTwenty),
                "the player's own FPS pick must not be overwritten by Battery Saver");

            controller.Open();
            yield return null;
            view.ShowGraphicsTab();
            yield return null;
            Assert.That(frameRateSlider.value, Is.EqualTo(3f).Within(0.001f),
                "the FPS slider selection must survive while Battery Saver is on");
            Assert.That(batterySaver.isOn, Is.True);

            batterySaver.isOn = false;
            applyButton.onClick.Invoke();
            yield return null;

            Assert.That(UnityEngine.Application.targetFrameRate, Is.EqualTo(120),
                "turning Battery Saver off must restore the previously chosen FPS");
            Assert.That(store.Load().BatterySaverEnabled, Is.False);

            controller.Open();
            yield return null;
            Assert.That(UnityEngine.Application.targetFrameRate, Is.EqualTo(120),
                "a fresh Open() (simulating relaunch) must re-apply the persisted, restored FPS");
        }

        // --- 5. Swipe sensitivity slider persists --------------------------------------------

        [UnityTest]
        public IEnumerator SwipeSensitivityPersistsAcrossReopen()
        {
            BuildScene(GameSettings.CreateDefault());
            yield return null;
            controller.Open();
            yield return null;
            view.ShowControlsTab();
            yield return null;

            sensitivitySlider.value = 0.15f;
            Assert.That(sensitivityLabel.text, Is.EqualTo("15%"));

            applyButton.onClick.Invoke();
            yield return null;
            Assert.That(store.Load().SwipeSensitivity, Is.EqualTo(0.15f).Within(0.001f));

            controller.Open();
            yield return null;
            Assert.That(sensitivitySlider.value, Is.EqualTo(0.15f).Within(0.001f));
        }

        // --- 6. Disable Swipe: state persists (gameplay blocking is covered in
        //        GameCompositionPlayModeTests, which owns the real CardSwipeController) --------

        [UnityTest]
        public IEnumerator DisableSwipeTogglePersists()
        {
            BuildScene(GameSettings.CreateDefault());
            yield return null;
            controller.Open();
            yield return null;
            view.ShowControlsTab();
            yield return null;

            Assert.That(disableSwipe.isOn, Is.False);
            disableSwipe.isOn = true;
            applyButton.onClick.Invoke();
            yield return null;

            Assert.That(store.Load().DisableSwipe, Is.True);

            controller.Open();
            yield return null;
            Assert.That(disableSwipe.isOn, Is.True);
        }

        // --- 7. Text size is a real mutually-exclusive choice --------------------------------

        [UnityTest]
        public IEnumerator TextSizeOptionsAreMutuallyExclusiveAndDefaultIsNormal()
        {
            BuildScene(GameSettings.CreateDefault());
            yield return null;
            controller.Open();
            yield return null;
            view.ShowGeneralTab();
            yield return null;

            Assert.That(textNormal.isOn, Is.True, "Normal must match the old default appearance");

            textLarge.isOn = true;
            yield return null;
            Assert.That(textNormal.isOn, Is.False);
            Assert.That(textSmall.isOn, Is.False);
            Assert.That(generalPanel.TextSizeMode, Is.EqualTo(TextSizeMode.Large));

            textSmall.isOn = true;
            yield return null;
            Assert.That(textLarge.isOn, Is.False);
            Assert.That(generalPanel.TextSizeMode, Is.EqualTo(TextSizeMode.Small));
        }

        // --- 8. Reset to defaults restores every field, in UI and in the store --------------

        [UnityTest]
        public IEnumerator ResetToDefaultsRestoresEveryNewFieldInUiAndPersistence()
        {
            GameSettings nonDefault = CustomSettings();
            nonDefault.SetMasterMuted(true);
            BuildScene(nonDefault);
            yield return null;
            controller.Open();
            yield return null;

            // Sanity: the non-default seed actually loaded before we reset it.
            Assert.That(frameRateSlider.value, Is.EqualTo(0f).Within(0.001f), "precondition");

            resetButton.onClick.Invoke();
            yield return null;

            Assert.That(masterSlider.value, Is.EqualTo(GameSettings.MaxVolume).Within(0.001f));
            Assert.That(musicSlider.value, Is.EqualTo(GameSettings.DefaultVolume).Within(0.001f));
            Assert.That(sfxSlider.value, Is.EqualTo(GameSettings.DefaultVolume).Within(0.001f));
            Assert.That(muteToggle.isOn, Is.False);
            Assert.That(frameRateSlider.value, Is.EqualTo(1f).Within(0.001f));
            Assert.That(frameRateLabel.text, Is.EqualTo("60 FPS"));
            Assert.That(sensitivitySlider.value, Is.EqualTo(GameSettings.DefaultSwipeSensitivity).Within(0.001f));
            Assert.That(disableSwipe.isOn, Is.False);
            Assert.That(textNormal.isOn, Is.True);

            GameSettings persisted = store.Load();
            Assert.That(persisted.MasterVolume, Is.EqualTo(GameSettings.MaxVolume));
            Assert.That(persisted.FrameRateMode, Is.EqualTo(FrameRateMode.Sixty));
            Assert.That(persisted.SwipeSensitivity, Is.EqualTo(GameSettings.DefaultSwipeSensitivity));
            Assert.That(persisted.DisableSwipe, Is.False);
            Assert.That(persisted.TextSizeMode, Is.EqualTo(TextSizeMode.Normal));
        }

        // --- 9. Tutorial reset touches only the tutorial flag --------------------------------

        [UnityTest]
        public IEnumerator ResetTutorialOnlyTouchesTheTutorialFlag()
        {
            GameSettings seed = GameSettings.CreateDefault();
            seed.SetTutorialCompleted(true);
            seed.SetMasterVolume(0.42f);
            BuildScene(seed);
            yield return null;
            controller.Open();
            yield return null;
            view.ShowGeneralTab();
            yield return null;

            resetTutorialButton.onClick.Invoke();
            yield return null;

            GameSettings persisted = store.Load();
            Assert.That(persisted.TutorialCompleted, Is.False);
            Assert.That(persisted.MasterVolume, Is.EqualTo(0.42f).Within(0.001f),
                "resetting the tutorial must not touch unrelated settings");
        }

        // --- 10. Reset Progress needs two taps and never touches the settings store ---------

        [UnityTest]
        public IEnumerator ResetProgressArmsOnFirstTapAndOnlyConfirmsOnTheSecond()
        {
            BuildScene(GameSettings.CreateDefault());
            yield return null;
            controller.Open();
            yield return null;
            view.ShowGeneralTab();
            yield return null;

            int confirmedCount = 0;
            view.ResetProgressConfirmed += () => confirmedCount++;

            resetProgressButton.onClick.Invoke();
            Assert.That(generalPanel.IsResetProgressArmed, Is.True);
            Assert.That(confirmedCount, Is.EqualTo(0), "the arming tap must not itself confirm");

            // Simulate backing out (what a tab switch does) instead of confirming.
            generalPanel.DisarmResetProgress();
            Assert.That(generalPanel.IsResetProgressArmed, Is.False);
            Assert.That(confirmedCount, Is.EqualTo(0), "backing out must not delete progress");

            resetProgressButton.onClick.Invoke();
            resetProgressButton.onClick.Invoke();
            Assert.That(confirmedCount, Is.EqualTo(1), "the second tap confirms exactly once");

            // SettingsController has no handler for this event at all — resetting progress is
            // ResetProgressController's job (a different component, not wired into this scene) —
            // so the settings store must be completely untouched by it.
            Assert.That(store.Load().MasterVolume, Is.EqualTo(GameSettings.MaxVolume),
                "confirming Reset Progress must never touch the Settings store");
        }

        // --- 11. Cancel reverts the live audio preview and never saves ------------------------

        [UnityTest]
        public IEnumerator CancelRevertsLivePreviewAndDoesNotSave()
        {
            BuildScene(GameSettings.CreateDefault());
            yield return null;
            controller.Open();
            yield return null;

            masterSlider.value = 0.2f;
            Assert.That(audioService.MasterVolume, Is.EqualTo(0.2f).Within(0.001f),
                "dragging previews live against the real AudioService before Apply");

            cancelButton.onClick.Invoke();
            yield return null;

            Assert.That(audioService.MasterVolume, Is.EqualTo(GameSettings.MaxVolume).Within(0.001f),
                "cancelling must put the live preview back");
            Assert.That(masterSlider.value, Is.EqualTo(GameSettings.MaxVolume).Within(0.001f));
            Assert.That(view.IsOpen, Is.False);
        }

        // --- 13. Persistence survives a simulated app restart --------------------------------

        [UnityTest]
        public IEnumerator SettingsSurviveASimulatedRestart()
        {
            BuildScene(GameSettings.CreateDefault());
            yield return null;
            controller.Open();
            yield return null;

            masterSlider.value = 0.65f;
            musicSlider.value = 0.4f;
            sfxSlider.value = 0.8f;
            frameRateSlider.value = 0f;
            sensitivitySlider.value = 0.7f;
            disableSwipe.isOn = true;
            textLarge.isOn = true;
            applyButton.onClick.Invoke();
            yield return null;

            // Tear down every object this session built — the same store instance (standing in
            // for the persisted file, which genuinely outlives the process) is carried over as
            // the only thing that survives.
            StubSettingsStore survivingStore = store;
            Object.Destroy(root);
            root = null;
            yield return null;

            // No in-memory seed: the fresh controller must load whatever the old one last saved.
            BuildScene(null, survivingStore);
            yield return null;
            controller.Open();
            yield return null;

            Assert.That(masterSlider.value, Is.EqualTo(0.65f).Within(0.001f));
            Assert.That(musicSlider.value, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(sfxSlider.value, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(frameRateSlider.value, Is.EqualTo(0f).Within(0.001f));
            Assert.That(sensitivitySlider.value, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(disableSwipe.isOn, Is.True);
            Assert.That(textLarge.isOn, Is.True);
        }

        // --- 14. Small-viewport-adjacent safety net: nothing here throws with a bare panel ---

        [UnityTest]
        public IEnumerator EveryControlSurvivesInteractionWithoutErrors()
        {
            BuildScene(GameSettings.CreateDefault());
            yield return null;
            controller.Open();
            yield return null;

            Assert.That(() =>
            {
                masterSlider.value = 0f;
                musicSlider.value = 1f;
                sfxSlider.value = 0.33f;
                muteToggle.isOn = true;
                muteToggle.isOn = false;
                frameRateSlider.value = 2f;
                batterySaver.isOn = true;
                sensitivitySlider.value = 0f;
                sensitivitySlider.value = 1f;
                tapButtons.isOn = false;
                invertRotation.isOn = true;
                disableSwipe.isOn = true;
                haptics.isOn = false;
                reducedMotion.isOn = true;
                highContrast.isOn = true;
                textSmall.isOn = true;
                applyButton.onClick.Invoke();
            }, Throws.Nothing);

            yield return null;
        }
    }
}
