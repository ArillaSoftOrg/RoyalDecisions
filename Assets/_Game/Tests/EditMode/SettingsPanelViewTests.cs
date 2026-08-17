using NUnit.Framework;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Covers the tab-switching spam guard added alongside the tab crossfade: reselecting the
    /// active tab (a repeated button press, or <see cref="SettingsPanelView.Show"/> re-selecting
    /// the tab it was already on) must never leave two tab bodies active at once inside the
    /// ScrollRect/layout-group stack, which would otherwise double its measured height mid-swap.
    /// </summary>
    [TestFixture]
    public class SettingsPanelViewTests
    {
        private SettingsPanelView view;
        private AudioSettingsPanelView audioPanel;
        private GraphicsSettingsPanelView graphicsPanel;
        private ControlsSettingsPanelView controlsPanel;
        private GeneralSettingsPanelView generalPanel;

        [SetUp]
        public void SetUp()
        {
            GameObject root = PresentationTestObjects.CreateObject("SettingsPanel");
            audioPanel = PresentationTestObjects.CreateComponent<AudioSettingsPanelView>("AudioTab");
            graphicsPanel =
                PresentationTestObjects.CreateComponent<GraphicsSettingsPanelView>("GraphicsTab");
            controlsPanel =
                PresentationTestObjects.CreateComponent<ControlsSettingsPanelView>("ControlsTab");
            generalPanel = PresentationTestObjects.CreateComponent<GeneralSettingsPanelView>("GeneralTab");

            view = PresentationTestObjects.CreateComponent<SettingsPanelView>("SettingsPanelView");
            view.SetAuthoringReferences(
                root, audioPanel, graphicsPanel, controlsPanel, generalPanel,
                null, null, null, null, null, null, null);
        }

        [TearDown]
        public void TearDown()
        {
            PresentationTestObjects.DestroyAll();
        }

        [Test]
        public void Show_ActivatesOnlyTheAudioTab()
        {
            view.Show(GameSettings.CreateDefault());

            Assert.That(audioPanel.gameObject.activeSelf, Is.True);
            Assert.That(graphicsPanel.gameObject.activeSelf, Is.False);
            Assert.That(controlsPanel.gameObject.activeSelf, Is.False);
            Assert.That(generalPanel.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ShowControlsTab_ActivatesOnlyControls()
        {
            view.Show(GameSettings.CreateDefault());

            view.ShowControlsTab();

            Assert.That(controlsPanel.gameObject.activeSelf, Is.True);
            Assert.That(audioPanel.gameObject.activeSelf, Is.False);
            Assert.That(graphicsPanel.gameObject.activeSelf, Is.False);
            Assert.That(generalPanel.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ReselectingTheActiveTab_LeavesExactlyOneTabActive()
        {
            view.Show(GameSettings.CreateDefault());
            view.ShowControlsTab();

            Assert.DoesNotThrow(() => view.ShowControlsTab());

            Assert.That(controlsPanel.gameObject.activeSelf, Is.True);
            Assert.That(audioPanel.gameObject.activeSelf, Is.False);
            Assert.That(graphicsPanel.gameObject.activeSelf, Is.False);
            Assert.That(generalPanel.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void SwitchingBackToAudioTab_ReturnsToOnlyAudioActive()
        {
            view.Show(GameSettings.CreateDefault());
            view.ShowGeneralTab();

            view.ShowAudioTab();

            Assert.That(audioPanel.gameObject.activeSelf, Is.True);
            Assert.That(generalPanel.gameObject.activeSelf, Is.False);
        }
    }
}
