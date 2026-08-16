using NUnit.Framework;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class PanelFadeAnimatorTests
    {
        private static PanelFadeAnimator Build(out GameObject root, out CanvasGroup group)
        {
            root = PresentationTestObjects.CreateObject("Panel");
            group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            root.SetActive(false);

            PanelFadeAnimator animator = PresentationTestObjects.CreateComponent<PanelFadeAnimator>(
                "Animator");
            animator.SetAuthoringReferences(root, group);
            return animator;
        }

        [Test]
        public void Show_ActivatesPanelAndRaisesAlphaImmediatelyOutsidePlayMode()
        {
            PanelFadeAnimator animator = Build(out GameObject root, out CanvasGroup group);

            animator.Show();

            Assert.That(root.activeSelf, Is.True);
            Assert.That(animator.IsVisible, Is.True);
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(group.interactable, Is.True);
            Assert.That(group.blocksRaycasts, Is.True);
        }

        [Test]
        public void Hide_DeactivatesPanelAndLowersAlphaImmediatelyOutsidePlayMode()
        {
            PanelFadeAnimator animator = Build(out GameObject root, out CanvasGroup group);
            animator.Show();

            animator.Hide();

            Assert.That(root.activeSelf, Is.False);
            Assert.That(animator.IsVisible, Is.False);
            Assert.That(group.alpha, Is.EqualTo(0f));
            Assert.That(group.blocksRaycasts, Is.False);
        }

        [Test]
        public void Hide_OnAlreadyClosedPanel_InvokesCompletionWithoutError()
        {
            PanelFadeAnimator animator = Build(out _, out _);
            bool completed = false;

            animator.Hide(() => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void SetReducedMotion_DoesNotPreventShowingOrHiding()
        {
            PanelFadeAnimator animator = Build(out GameObject root, out CanvasGroup group);

            animator.SetReducedMotion(true);
            animator.Show();
            Assert.That(root.activeSelf, Is.True);
            Assert.That(group.alpha, Is.EqualTo(1f));

            animator.Hide();
            Assert.That(root.activeSelf, Is.False);
            Assert.That(group.alpha, Is.EqualTo(0f));
        }
    }
}
