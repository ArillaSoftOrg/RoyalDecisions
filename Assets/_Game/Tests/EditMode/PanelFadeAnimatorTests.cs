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
        public void Show_OutsidePlayMode_InvokesCompletionAfterRaisingAlpha()
        {
            PanelFadeAnimator animator = Build(out GameObject root, out CanvasGroup group);
            bool completed = false;

            animator.Show(() => completed = true);

            Assert.That(root.activeSelf, Is.True);
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(completed, Is.True);
        }

        [Test]
        public void Show_OnAlreadyFullyVisiblePanel_InvokesCompletionWithoutError()
        {
            PanelFadeAnimator animator = Build(out GameObject root, out CanvasGroup group);
            root.SetActive(true);
            group.alpha = 1f;
            bool completed = false;

            animator.Show(() => completed = true);

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

        [Test]
        public void Swap_OutsidePlayMode_InvokesActionAndRestoresFullAlpha()
        {
            PanelFadeAnimator animator = Build(out GameObject root, out CanvasGroup group);
            root.SetActive(true);
            group.alpha = 1f;
            bool swapped = false;

            animator.Swap(() => swapped = true);

            Assert.That(swapped, Is.True);
            Assert.That(group.alpha, Is.EqualTo(1f));
        }

        [Test]
        public void Swap_NeverDeactivatesPanelRoot()
        {
            PanelFadeAnimator animator = Build(out GameObject root, out CanvasGroup group);
            root.SetActive(true);
            group.alpha = 1f;

            animator.Swap(() => { });

            Assert.That(root.activeSelf, Is.True,
                "Swap is for in-place content changes (e.g. a settings tab) — unlike Hide(), the " +
                "panel itself must never close.");
        }
    }
}
