using NUnit.Framework;
using RoyalDecisions.Composition;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public sealed class BootstrapControllerTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            PresentationTestObjects.DestroyAll();
        }

        [Test]
        public void BeginStartupSequence_IntroAndLoadingAssigned_LoadingStartsFromIntroCompletionThenMainMenuLoadsOnce()
        {
            root = new GameObject("Bootstrap");
            BootstrapController controller = root.AddComponent<BootstrapController>();
            FakeLoader loader = new FakeLoader();
            IntroSequenceController intro =
                PresentationTestObjects.CreateComponent<IntroSequenceController>("Intro");
            StartupLoadingController loading =
                PresentationTestObjects.CreateComponent<StartupLoadingController>("Loading");
            controller.Configure(new FakeSettingsStore(), loader, intro, loading);

            controller.BeginStartupSequence();

            // Outside Play Mode both the intro and the loading screen (no references assigned to
            // either here) fail open immediately, so this cannot observe the real hold/fade timing —
            // but the mechanism is still verified structurally: BootstrapController only starts
            // Loading from the intro's own completion callback (Play(HandleIntroCompleted)), not from
            // IntroSequenceController.FadeOutStarted — deliberately, so Loading can never paint over
            // the intro's still-visible hold or fade (see BootstrapController.PlayIntro's own remarks
            // for why the earlier FadeOutStarted-driven crossfade was replaced).
            Assert.That(intro.HasCompleted, Is.True);
            Assert.That(loading.HasCompleted, Is.True);
            Assert.That(loader.Count, Is.EqualTo(1),
                "Loading's own CompleteLoading(LoadMainMenuOnce) callback must be what reaches "
                + "MainMenu — never a second, independent path.");
            Assert.That(loader.LastScene, Is.EqualTo("MainMenu"));
        }

        [Test]
        public void BeginStartupSequence_IntroFadeOutStarted_FiresExactlyOnceButDoesNotStartLoading()
        {
            root = new GameObject("Bootstrap");
            BootstrapController controller = root.AddComponent<BootstrapController>();
            FakeLoader loader = new FakeLoader();
            IntroSequenceController intro =
                PresentationTestObjects.CreateComponent<IntroSequenceController>("Intro");
            StartupLoadingController loading =
                PresentationTestObjects.CreateComponent<StartupLoadingController>("Loading");
            controller.Configure(new FakeSettingsStore(), loader, intro, loading);

            int fadeOutStartedCount = 0;
            int loadingBegunBeforeFadeOutStarted = -1;
            intro.FadeOutStarted += () =>
            {
                fadeOutStartedCount++;
                loadingBegunBeforeFadeOutStarted = loader.Count;
            };

            controller.BeginStartupSequence();

            // IntroSequenceController still guarantees FadeOutStarted fires exactly once (outside
            // Play Mode, via Complete()'s own guarantee) — that guarantee is intentionally untouched.
            // What changed is that BootstrapController no longer listens for it: Loading is started
            // only from the intro's completion callback, so at the instant FadeOutStarted fires,
            // Loading must not have begun yet.
            Assert.That(fadeOutStartedCount, Is.EqualTo(1),
                "FadeOutStarted must still fire exactly once — this component's own guarantee is "
                + "unrelated to what BootstrapController chooses to do with it.");
            Assert.That(loadingBegunBeforeFadeOutStarted, Is.EqualTo(0),
                "Loading must not have started yet at the moment FadeOutStarted fires — only the "
                + "intro's own completion should ever start Loading.");
            Assert.That(loader.Count, Is.EqualTo(1));
        }

        [Test]
        public void BeginStartupSequence_IntroMissing_LoadingRunsThenLoadsMainMenuOnce()
        {
            root = new GameObject("Bootstrap");
            BootstrapController controller = root.AddComponent<BootstrapController>();
            FakeLoader loader = new FakeLoader();
            StartupLoadingController loading =
                PresentationTestObjects.CreateComponent<StartupLoadingController>("Loading");
            controller.Configure(new FakeSettingsStore(), loader, loading: loading);

            controller.BeginStartupSequence();

            // No intro assigned: the loading screen must still run on its own — proves loading is
            // reachable independently of the intro stage, not only as its callback.
            Assert.That(loading.HasCompleted, Is.True);
            Assert.That(loader.Count, Is.EqualTo(1));
            Assert.That(loader.LastScene, Is.EqualTo("MainMenu"));
        }

        [Test]
        public void BeginStartupSequence_LoadingMissing_IntroRunsThenLoadsMainMenuOnce()
        {
            root = new GameObject("Bootstrap");
            BootstrapController controller = root.AddComponent<BootstrapController>();
            FakeLoader loader = new FakeLoader();
            IntroSequenceController intro =
                PresentationTestObjects.CreateComponent<IntroSequenceController>("Intro");
            controller.Configure(new FakeSettingsStore(), loader, intro);

            controller.BeginStartupSequence();

            // No loading screen assigned: the intro must still play and skip straight to MainMenu
            // once it completes, without ever touching a loading stage that does not exist.
            Assert.That(intro.HasCompleted, Is.True);
            Assert.That(loader.Count, Is.EqualTo(1));
            Assert.That(loader.LastScene, Is.EqualTo("MainMenu"));
        }

        [Test]
        public void BeginStartupSequence_BothMissing_LoadsMainMenuOnce()
        {
            root = new GameObject("Bootstrap");
            BootstrapController controller = root.AddComponent<BootstrapController>();
            FakeLoader loader = new FakeLoader();
            controller.Configure(new FakeSettingsStore(), loader);

            controller.BeginStartupSequence();

            Assert.That(loader.Count, Is.EqualTo(1));
            Assert.That(loader.LastScene, Is.EqualTo("MainMenu"));
        }

        [Test]
        public void BeginStartupSequence_CalledTwice_LoadsMainMenuExactlyOnce()
        {
            root = new GameObject("Bootstrap");
            BootstrapController controller = root.AddComponent<BootstrapController>();
            FakeLoader loader = new FakeLoader();
            IntroSequenceController intro =
                PresentationTestObjects.CreateComponent<IntroSequenceController>("Intro");
            StartupLoadingController loading =
                PresentationTestObjects.CreateComponent<StartupLoadingController>("Loading");
            controller.Configure(new FakeSettingsStore(), loader, intro, loading);

            controller.BeginStartupSequence();
            controller.BeginStartupSequence();

            Assert.That(loader.Count, Is.EqualTo(1),
                "A second BeginStartupSequence() call must not replay the loading/intro sequence "
                + "or load MainMenu again.");
        }

        private sealed class FakeLoader : ISceneLoader
        {
            public int Count { get; private set; }
            public string LastScene { get; private set; }

            public void LoadScene(string sceneName)
            {
                Count++;
                LastScene = sceneName;
            }
        }
    }
}
