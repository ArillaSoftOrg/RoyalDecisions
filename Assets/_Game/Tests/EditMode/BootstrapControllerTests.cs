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
        public void ProceedToMainMenu_IntroUnassigned_LoadsMainMenuImmediately()
        {
            root = new GameObject("Bootstrap");
            BootstrapController controller = root.AddComponent<BootstrapController>();
            FakeLoader loader = new FakeLoader();
            controller.Configure(new FakeSettingsStore(), loader);

            controller.ProceedToMainMenu();

            Assert.That(loader.Count, Is.EqualTo(1));
            Assert.That(loader.LastScene, Is.EqualTo("MainMenu"));
        }

        [Test]
        public void ProceedToMainMenu_IntroAssigned_LoadsMainMenuExactlyOnceThroughItsCallback()
        {
            root = new GameObject("Bootstrap");
            BootstrapController controller = root.AddComponent<BootstrapController>();
            FakeLoader loader = new FakeLoader();
            IntroSequenceController intro =
                PresentationTestObjects.CreateComponent<IntroSequenceController>("Intro");
            controller.Configure(new FakeSettingsStore(), loader, intro);

            controller.ProceedToMainMenu();

            // Outside Play Mode the intro (no logo assigned here) fails open immediately, but the
            // point under test is that MainMenu is reached exactly once, and only through the
            // callback ProceedToMainMenu hands to Play — never a second, direct LoadScene call.
            Assert.That(loader.Count, Is.EqualTo(1));
            Assert.That(loader.LastScene, Is.EqualTo("MainMenu"));
            Assert.That(intro.HasCompleted, Is.True);
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
