using NUnit.Framework;
using RoyalDecisions.Composition;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public sealed class TutorialCoordinatorTests
    {
        private GameObject root;
        private GameObject panel;
        private TutorialCoordinator coordinator;
        private FakeSettingsStore store;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("TutorialRoot");
            panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            TutorialOverlayView view = root.AddComponent<TutorialOverlayView>();
            view.SetAuthoringReferences(
                panel,
                NewText("Title"),
                NewText("Body"),
                NewButton("Next"),
                NewButton("Skip"));
            coordinator = root.AddComponent<TutorialCoordinator>();
            coordinator.SetAuthoringReference(view);
            store = new FakeSettingsStore();
            panel.SetActive(false);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void NewUserIsGatedUntilSynchronousFinalStep()
        {
            int starts = 0;

            bool gated = coordinator.TryGateNewGame(store, () => starts++);
            coordinator.Advance();
            coordinator.Advance();
            Assert.That(starts, Is.Zero);
            coordinator.Advance();

            Assert.That(gated, Is.True);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(store.SaveCount, Is.EqualTo(1));
            Assert.That(store.Load().TutorialCompleted, Is.True);
            Assert.That(panel.activeSelf, Is.False);
        }

        [Test]
        public void CompletedUserBypassesWithoutSavingOrInvokingCallback()
        {
            GameSettings completed = GameSettings.CreateDefault();
            completed.SetTutorialCompleted(true);
            store.Save(completed);
            int savesBefore = store.SaveCount;
            int starts = 0;

            bool gated = coordinator.TryGateNewGame(store, () => starts++);

            Assert.That(gated, Is.False);
            Assert.That(starts, Is.Zero);
            Assert.That(store.SaveCount, Is.EqualTo(savesBefore));
        }

        private TextMeshProUGUI NewText(string name)
        {
            GameObject child = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            child.transform.SetParent(root.transform, false);
            return child.GetComponent<TextMeshProUGUI>();
        }

        private Button NewButton(string name)
        {
            GameObject child = new GameObject(
                name, typeof(RectTransform), typeof(Image), typeof(Button));
            child.transform.SetParent(root.transform, false);
            return child.GetComponent<Button>();
        }
    }
}
