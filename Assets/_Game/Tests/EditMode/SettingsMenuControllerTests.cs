using NUnit.Framework;
using RoyalDecisions.Composition;
using RoyalDecisions.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Covers <see cref="SettingsMenuController"/>'s staged-edit contract: what Render shows, what
    /// Apply commits, and — the part that is easy to get wrong — that Cancel writes nothing.
    /// </summary>
    /// <remarks>
    /// Everything here drives the controller through its public API. Unity does not run
    /// <c>Awake</c>/<c>OnEnable</c> for a plain MonoBehaviour outside play mode, so the
    /// <c>onValueChanged</c> wiring itself is covered by the PlayMode suite instead; the logic those
    /// handlers delegate to is exercised directly below.
    /// </remarks>
    [TestFixture]
    public sealed class SettingsMenuControllerTests
    {
        private SettingsMenuController controller;
        private FakeSettingsStore store;

        private Slider master;
        private Slider music;
        private Slider sfx;
        private Slider frameRate;
        private Slider sensitivity;
        private Slider textSize;
        private Toggle mute;
        private Toggle haptics;
        private Toggle batterySaver;
        private Toggle reducedMotion;
        private TMP_Text masterLabel;
        private TMP_Text frameRateLabel;
        private TMP_Text textSizeLabel;

        [SetUp]
        public void SetUp()
        {
            controller = PresentationTestObjects.CreateComponent<SettingsMenuController>("Settings");
            store = new FakeSettingsStore();
            controller.Configure(store);

            master = CreateSlider("Master");
            music = CreateSlider("Music");
            sfx = CreateSlider("Sfx");
            frameRate = CreateSlider("FrameRate", 0f, 3f, wholeNumbers: true);
            sensitivity = CreateSlider("Sensitivity");
            textSize = CreateSlider("TextSize", 0f, 2f, wholeNumbers: true);

            mute = PresentationTestObjects.CreateComponent<Toggle>("Mute");
            haptics = PresentationTestObjects.CreateComponent<Toggle>("Haptics");
            batterySaver = PresentationTestObjects.CreateComponent<Toggle>("BatterySaver");
            reducedMotion = PresentationTestObjects.CreateComponent<Toggle>("ReducedMotion");

            masterLabel = PresentationTestObjects.CreateText("MasterLabel");
            frameRateLabel = PresentationTestObjects.CreateText("FrameRateLabel");
            textSizeLabel = PresentationTestObjects.CreateText("TextSizeLabel");

            controller.SetAudioAuthoringReferences(master, music, sfx, mute, masterLabel);
            controller.SetAuthoringReferences(
                hapticsSwitch: haptics,
                frameRate: frameRate, frameRateName: frameRateLabel, batterySaver: batterySaver,
                sensitivity: sensitivity,
                textSize: textSize, textSizeName: textSizeLabel,
                reducedMotion: reducedMotion);
        }

        [TearDown]
        public void TearDown()
        {
            PresentationTestObjects.DestroyAll();
        }

        [Test]
        public void Render_ShowsTheSettingsInTheWidgets()
        {
            GameSettings settings = GameSettings.CreateDefault();
            settings.SetMasterVolume(0.4f);
            settings.SetMasterMuted(true);
            settings.SetFrameRateMode(FrameRateMode.Ninety);
            settings.SetTextSizeMode(TextSizeMode.Large);

            controller.Render(settings);

            Assert.That(master.value, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(mute.isOn, Is.True);
            Assert.That(frameRate.value, Is.EqualTo(2f).Within(0.0001f), "90 FPS is step 2");
            Assert.That(textSize.value, Is.EqualTo(2f).Within(0.0001f), "Large is step 2");
        }

        [Test]
        public void Render_WritesThePercentageLabel()
        {
            GameSettings settings = GameSettings.CreateDefault();
            settings.SetMasterVolume(0.42f);

            controller.Render(settings);

            Assert.That(masterLabel.text, Is.EqualTo("42%"));
        }

        [Test]
        public void Render_WritesTheStepNameLabels()
        {
            GameSettings settings = GameSettings.CreateDefault();
            settings.SetFrameRateMode(FrameRateMode.Thirty);
            settings.SetTextSizeMode(TextSizeMode.Small);

            controller.Render(settings);

            Assert.That(frameRateLabel.text, Is.EqualTo("30 FPS"));
            Assert.That(textSizeLabel.text, Is.EqualTo("Küçük"));
        }

        [Test]
        public void Render_DoesNotCountAsAnEdit()
        {
            // SetValueWithoutNotify everywhere: a programmatic render must never look like the
            // player touched a control, or it would save state nobody chose.
            controller.Render(GameSettings.CreateDefault());

            Assert.That(store.SaveCount, Is.Zero);
        }

        [Test]
        public void Apply_SavesTheWidgetStateExactlyOnce()
        {
            controller.Render(GameSettings.CreateDefault());
            master.value = 0.3f;
            mute.isOn = true;
            frameRate.value = 3f;
            textSize.value = 0f;

            controller.Apply();

            Assert.That(store.SaveCount, Is.EqualTo(1));
            GameSettings saved = store.Load();
            Assert.That(saved.MasterVolume, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(saved.MasterMuted, Is.True);
            Assert.That(saved.FrameRateMode, Is.EqualTo(FrameRateMode.OneTwenty));
            Assert.That(saved.TextSizeMode, Is.EqualTo(TextSizeMode.Small));
        }

        [Test]
        public void Apply_RaisesAppliedWithWhatWasSaved()
        {
            GameSettings received = null;
            controller.Applied += settings => received = settings;

            controller.Render(GameSettings.CreateDefault());
            sensitivity.value = 0.6f;
            controller.Apply();

            Assert.That(received, Is.Not.Null);
            Assert.That(received.SwipeSensitivity, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void Cancel_SavesNothingAndPutsTheWidgetsBack()
        {
            GameSettings stored = GameSettings.CreateDefault();
            stored.SetMasterVolume(0.9f);
            store.Save(stored);
            controller.LoadAndApply();
            int savesBefore = store.SaveCount;

            master.value = 0.1f;
            mute.isOn = true;

            controller.Cancel();

            Assert.That(store.SaveCount, Is.EqualTo(savesBefore), "Cancel must not write");
            Assert.That(master.value, Is.EqualTo(0.9f).Within(0.0001f));
            Assert.That(mute.isOn, Is.False);
        }

        [Test]
        public void Cancel_RaisesCancelled()
        {
            bool raised = false;
            controller.Cancelled += () => raised = true;

            controller.Cancel();

            Assert.That(raised, Is.True);
        }

        [Test]
        public void ResetToDefaults_ShowsDefaultsButDoesNotSaveUntilApply()
        {
            controller.Render(GameSettings.CreateDefault());
            master.value = 0.1f;
            reducedMotion.isOn = true;

            controller.ResetToDefaults();

            GameSettings defaults = GameSettings.CreateDefault();
            Assert.That(master.value, Is.EqualTo(defaults.MasterVolume).Within(0.0001f));
            Assert.That(reducedMotion.isOn, Is.EqualTo(defaults.ReducedMotion));
            Assert.That(store.SaveCount, Is.Zero, "the player still has to confirm with Apply");
        }

        [Test]
        public void LoadAndApply_ShowsWhatTheStoreHeld()
        {
            GameSettings stored = GameSettings.CreateDefault();
            stored.SetSfxVolume(0.05f);
            stored.SetBatterySaverEnabled(true);
            store.Save(stored);

            controller.LoadAndApply();

            Assert.That(sfx.value, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(batterySaver.isOn, Is.True);
        }

        [Test]
        public void UnwiredControls_DoNotOverwriteTheirStoredValues()
        {
            // Only some widgets are wired in this fixture; Apply must leave the rest of the saved
            // settings alone rather than stamping them with a default.
            GameSettings stored = GameSettings.CreateDefault();
            stored.SetLanguage("en");
            stored.SetTutorialCompleted(true);
            store.Save(stored);
            controller.LoadAndApply();

            controller.Apply();

            GameSettings saved = store.Load();
            Assert.That(saved.Language, Is.EqualTo("en"));
            Assert.That(saved.TutorialCompleted, Is.True);
        }

        private static Slider CreateSlider(
            string name, float min = 0f, float max = 1f, bool wholeNumbers = false)
        {
            Slider slider = PresentationTestObjects.CreateComponent<Slider>(name);
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            return slider;
        }
    }
}
