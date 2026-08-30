using NUnit.Framework;
using RoyalDecisions.Application;
using RoyalDecisions.Composition;
using RoyalDecisions.Domain;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Covers the PlayerPrefs settings store: a full round trip, the first-run and
    /// unsupported-version fallbacks, and the promise that <c>Load</c> never hands back a value the
    /// rest of the game has to re-validate.
    /// </summary>
    /// <remarks>
    /// Every fixture uses its own key prefix and deletes it again in teardown, so running the suite
    /// never disturbs the editor's real preferences.
    /// </remarks>
    [TestFixture]
    public sealed class PlayerPrefsSettingsStoreTests
    {
        private string prefix;
        private PlayerPrefsSettingsStore store;

        [SetUp]
        public void SetUp()
        {
            prefix = "royaldecisions.tests." + System.Guid.NewGuid().ToString("N") + ".";
            store = new PlayerPrefsSettingsStore(prefix);
        }

        [TearDown]
        public void TearDown()
        {
            string[] keys = store.AllKeys();
            for (int i = 0; i < keys.Length; i++)
            {
                PlayerPrefs.DeleteKey(keys[i]);
            }

            PlayerPrefs.Save();
        }

        [Test]
        public void LoadWithNothingSaved_ReturnsDefaults()
        {
            GameSettings loaded = store.Load();
            GameSettings defaults = GameSettings.CreateDefault();

            Assert.That(loaded.MasterVolume, Is.EqualTo(defaults.MasterVolume).Within(0.0001f));
            Assert.That(loaded.HapticsEnabled, Is.EqualTo(defaults.HapticsEnabled));
            Assert.That(loaded.FrameRateMode, Is.EqualTo(defaults.FrameRateMode));
        }

        [Test]
        public void SaveThenLoad_RoundTripsEveryField()
        {
            GameSettings saved = GameSettings.CreateDefault();
            saved.SetMasterVolume(0.42f);
            saved.SetMusicVolume(0.13f);
            saved.SetSfxVolume(0.77f);
            saved.SetMasterMuted(true);
            saved.SetHapticsEnabled(false);
            saved.SetReducedMotion(true);
            saved.SetTextSizeMode(TextSizeMode.Large);
            saved.SetHighContrast(true);
            saved.SetTutorialCompleted(true);
            saved.SetLanguage("en");
            saved.SetFrameRateMode(FrameRateMode.OneTwenty);
            saved.SetBatterySaverEnabled(true);
            saved.SetTapButtonsEnabled(false);
            saved.SetInvertSwipeRotation(true);
            saved.SetSwipeSensitivity(0.25f);
            saved.SetDisableSwipe(true);

            Assert.That(store.Save(saved).Succeeded, Is.True);
            GameSettings loaded = store.Load();

            Assert.That(loaded.MasterVolume, Is.EqualTo(0.42f).Within(0.0001f));
            Assert.That(loaded.MusicVolume, Is.EqualTo(0.13f).Within(0.0001f));
            Assert.That(loaded.SfxVolume, Is.EqualTo(0.77f).Within(0.0001f));
            Assert.That(loaded.MasterMuted, Is.True);
            Assert.That(loaded.HapticsEnabled, Is.False);
            Assert.That(loaded.ReducedMotion, Is.True);
            Assert.That(loaded.TextSizeMode, Is.EqualTo(TextSizeMode.Large));
            Assert.That(loaded.HighContrast, Is.True);
            Assert.That(loaded.TutorialCompleted, Is.True);
            Assert.That(loaded.Language, Is.EqualTo("en"));
            Assert.That(loaded.FrameRateMode, Is.EqualTo(FrameRateMode.OneTwenty));
            Assert.That(loaded.BatterySaverEnabled, Is.True);
            Assert.That(loaded.TapButtonsEnabled, Is.False);
            Assert.That(loaded.InvertSwipeRotation, Is.True);
            Assert.That(loaded.SwipeSensitivity, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(loaded.DisableSwipe, Is.True);
        }

        [Test]
        public void UnsupportedVersion_FallsBackToDefaultsInsteadOfGuessing()
        {
            GameSettings saved = GameSettings.CreateDefault();
            saved.SetMasterVolume(0.1f);
            store.Save(saved);

            // A newer build wrote this file; this one must not try to interpret it.
            PlayerPrefs.SetInt(prefix + "version", PlayerPrefsSettingsStore.CurrentVersion + 1);
            PlayerPrefs.Save();

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                "unsupported version"));
            GameSettings loaded = store.Load();

            Assert.That(
                loaded.MasterVolume,
                Is.EqualTo(GameSettings.CreateDefault().MasterVolume).Within(0.0001f));
        }

        [Test]
        public void OutOfRangeStoredValues_AreSanitizedOnLoad()
        {
            store.Save(GameSettings.CreateDefault());

            // Values a hand-edited or corrupted preferences file could plausibly contain.
            PlayerPrefs.SetFloat(prefix + "masterVolume", 7.5f);
            PlayerPrefs.SetInt(prefix + "textSizeMode", 99);
            PlayerPrefs.SetString(prefix + "language", string.Empty);
            PlayerPrefs.Save();

            GameSettings loaded = store.Load();

            Assert.That(loaded.MasterVolume, Is.LessThanOrEqualTo(GameSettings.MaxVolume));
            Assert.That(loaded.MasterVolume, Is.GreaterThanOrEqualTo(GameSettings.MinVolume));
            Assert.That(System.Enum.IsDefined(typeof(TextSizeMode), loaded.TextSizeMode), Is.True);
            Assert.That(loaded.Language, Is.EqualTo(GameSettings.DefaultLanguage));
        }

        [Test]
        public void SavingNull_ReportsFailureRatherThanThrowing()
        {
            SaveOutcome outcome = store.Save(null);

            Assert.That(outcome.Succeeded, Is.False);
            Assert.That(outcome.Message, Is.Not.Empty);
        }

        [Test]
        public void TwoStoresWithDifferentPrefixes_DoNotSeeEachOther()
        {
            GameSettings saved = GameSettings.CreateDefault();
            saved.SetMasterVolume(0.2f);
            store.Save(saved);

            var other = new PlayerPrefsSettingsStore(prefix + "other.");
            try
            {
                Assert.That(
                    other.Load().MasterVolume,
                    Is.EqualTo(GameSettings.CreateDefault().MasterVolume).Within(0.0001f));
            }
            finally
            {
                string[] keys = other.AllKeys();
                for (int i = 0; i < keys.Length; i++)
                {
                    PlayerPrefs.DeleteKey(keys[i]);
                }
            }
        }
    }
}
