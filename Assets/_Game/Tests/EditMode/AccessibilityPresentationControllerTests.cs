using System.Reflection;
using NUnit.Framework;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;
using TMPro;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Proves Reduced Motion and Text Size actually reach the views they are wired to, not just
    /// the <see cref="GameSettings"/> model — the gap this controller exists to close.
    /// </summary>
    [TestFixture]
    public class AccessibilityPresentationControllerTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [TearDown]
        public void TearDown()
        {
            PresentationTestObjects.DestroyAll();
        }

        private static AccessibilityPresentationController Build(out TextMeshProUGUI text)
        {
            AccessibilityPresentationController controller =
                PresentationTestObjects.CreateComponent<AccessibilityPresentationController>(
                    "Accessibility");
            text = PresentationTestObjects.CreateText("Scalable");
            text.fontSizeMin = 20f;
            text.fontSizeMax = 30f;
            controller.SetAuthoringReferences(new TMP_Text[] { text }, null, null, null);
            return controller;
        }

        private static float PrivateFloat(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, "field " + fieldName + " must exist");
            return (float)field.GetValue(target);
        }

        [Test]
        public void Apply_Normal_LeavesFontSizeRangeUnchanged()
        {
            AccessibilityPresentationController controller = Build(out TextMeshProUGUI text);
            GameSettings settings = GameSettings.CreateDefault();

            controller.Apply(settings);

            Assert.That(text.fontSizeMin, Is.EqualTo(20f).Within(0.001f));
            Assert.That(text.fontSizeMax, Is.EqualTo(30f).Within(0.001f));
        }

        [Test]
        public void Apply_Small_ShrinksFontSizeRange()
        {
            AccessibilityPresentationController controller = Build(out TextMeshProUGUI text);
            GameSettings settings = GameSettings.CreateDefault();
            settings.SetTextSizeMode(TextSizeMode.Small);

            controller.Apply(settings);

            Assert.That(text.fontSizeMin, Is.EqualTo(20f * 0.9f).Within(0.001f));
            Assert.That(text.fontSizeMax, Is.EqualTo(30f * 0.9f).Within(0.001f));
        }

        [Test]
        public void Apply_Large_GrowsFontSizeRange()
        {
            AccessibilityPresentationController controller = Build(out TextMeshProUGUI text);
            GameSettings settings = GameSettings.CreateDefault();
            settings.SetTextSizeMode(TextSizeMode.Large);

            controller.Apply(settings);

            Assert.That(text.fontSizeMin, Is.EqualTo(20f * 1.15f).Within(0.001f));
            Assert.That(text.fontSizeMax, Is.EqualTo(30f * 1.15f).Within(0.001f));
        }

        [Test]
        public void Apply_TextSizeChangesLive_ReRendersFromTheSameOriginalBaseline()
        {
            // A live slider drag calls Apply repeatedly; scaling must always be computed from the
            // original authored size, never compounded from whatever the previous scale left.
            AccessibilityPresentationController controller = Build(out TextMeshProUGUI text);
            GameSettings settings = GameSettings.CreateDefault();

            settings.SetTextSizeMode(TextSizeMode.Large);
            controller.Apply(settings);
            settings.SetTextSizeMode(TextSizeMode.Small);
            controller.Apply(settings);
            settings.SetTextSizeMode(TextSizeMode.Normal);
            controller.Apply(settings);

            Assert.That(text.fontSizeMin, Is.EqualTo(20f).Within(0.001f));
            Assert.That(text.fontSizeMax, Is.EqualTo(30f).Within(0.001f));
        }

        [Test]
        public void Apply_ReducedMotionOn_ShortensEveryWiredPanelAnimator()
        {
            GameObject root = PresentationTestObjects.CreateObject("Panel");
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            PanelFadeAnimator panel = PresentationTestObjects.CreateComponent<PanelFadeAnimator>(
                "Animator");
            panel.SetAuthoringReferences(root, group);
            AccessibilityPresentationController controller =
                PresentationTestObjects.CreateComponent<AccessibilityPresentationController>(
                    "Accessibility");
            controller.SetAuthoringReferences(
                null, null, null, null, new[] { panel });
            GameSettings settings = GameSettings.CreateDefault();
            settings.SetReducedMotion(true);

            controller.Apply(settings);

            Assert.That(PrivateFloat(panel, "showDuration"), Is.LessThanOrEqualTo(0.05f));
            Assert.That(PrivateFloat(panel, "hideDuration"), Is.LessThanOrEqualTo(0.05f));
        }

        [Test]
        public void Apply_ReducedMotionOff_RestoresEveryWiredPanelAnimatorToItsAuthoredDuration()
        {
            GameObject root = PresentationTestObjects.CreateObject("Panel");
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            PanelFadeAnimator panel = PresentationTestObjects.CreateComponent<PanelFadeAnimator>(
                "Animator");
            panel.SetAuthoringReferences(root, group);
            float authoredShow = PrivateFloat(panel, "showDuration");
            AccessibilityPresentationController controller =
                PresentationTestObjects.CreateComponent<AccessibilityPresentationController>(
                    "Accessibility");
            controller.SetAuthoringReferences(
                null, null, null, null, new[] { panel });
            GameSettings settings = GameSettings.CreateDefault();
            settings.SetReducedMotion(true);
            controller.Apply(settings);

            settings.SetReducedMotion(false);
            controller.Apply(settings);

            Assert.That(PrivateFloat(panel, "showDuration"), Is.EqualTo(authoredShow).Within(0.0001f));
        }

        [Test]
        public void Apply_WithNullEntriesInWiredArrays_DoesNotThrow()
        {
            AccessibilityPresentationController controller =
                PresentationTestObjects.CreateComponent<AccessibilityPresentationController>(
                    "Accessibility");
            controller.SetAuthoringReferences(
                new TMP_Text[] { null }, new TMP_Text[] { null }, null, null, new PanelFadeAnimator[] { null });

            Assert.That(() => controller.Apply(GameSettings.CreateDefault()), Throws.Nothing);
        }
    }
}
