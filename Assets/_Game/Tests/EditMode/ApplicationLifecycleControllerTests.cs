using NUnit.Framework;
using RoyalDecisions.Composition;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public sealed class ApplicationLifecycleControllerTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void MainMenuBackRequestsQuitWithoutSceneLoad()
        {
            root = new GameObject("Lifecycle");
            ApplicationLifecycleController controller =
                root.AddComponent<ApplicationLifecycleController>();
            FakeLoader loader = new FakeLoader();
            FakeQuitter quitter = new FakeQuitter();
            controller.SetAuthoringReferences(null, true);
            controller.ConfigureForTests(loader, quitter);

            controller.HandleBackRequested();

            Assert.That(quitter.Count, Is.EqualTo(1));
            Assert.That(loader.Count, Is.Zero);
        }

        [Test]
        public void GameBackReturnsToMainMenu()
        {
            root = new GameObject("Lifecycle");
            ApplicationLifecycleController controller =
                root.AddComponent<ApplicationLifecycleController>();
            FakeLoader loader = new FakeLoader();
            controller.SetAuthoringReferences(null, false, "MainMenu");
            controller.ConfigureForTests(loader, new FakeQuitter());

            controller.HandleBackRequested();

            Assert.That(loader.Count, Is.EqualTo(1));
            Assert.That(loader.LastScene, Is.EqualTo("MainMenu"));
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

        private sealed class FakeQuitter : IApplicationQuitter
        {
            public int Count { get; private set; }
            public void Quit() => Count++;
        }
    }
}
