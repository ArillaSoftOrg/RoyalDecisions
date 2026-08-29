using NUnit.Framework;
using RoyalDecisions.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class CrtFlickerAnimatorTests
    {
        private const float BaseAlpha = 0.8f;

        [TearDown]
        public void TearDown()
        {
            PresentationTestObjects.DestroyAll();
        }

        private static CrtFlickerAnimator Build(out Image target)
        {
            target = PresentationTestObjects.CreateImage("Overlay");
            Color colour = target.color;
            colour.a = BaseAlpha;
            target.color = colour;

            CrtFlickerAnimator animator = PresentationTestObjects.CreateComponent<CrtFlickerAnimator>(
                "Flicker");
            animator.SetAuthoringReferences(new Graphic[] { target });
            return animator;
        }

        [Test]
        public void TriggerBurst_ThenTickToMidpoint_DipsAlphaBelowBase()
        {
            CrtFlickerAnimator animator = Build(out Image target);

            animator.TriggerBurst();
            animator.Tick(0.075f); // half of the default 0.15s burst

            Assert.That(target.color.a, Is.LessThan(BaseAlpha));
        }

        [Test]
        public void TriggerBurst_ThenTickPastDuration_RestoresBaseAlpha()
        {
            CrtFlickerAnimator animator = Build(out Image target);

            animator.TriggerBurst();
            animator.Tick(0.2f); // past the default 0.15s burst

            Assert.That(target.color.a, Is.EqualTo(BaseAlpha).Within(1e-5f));
        }

        [Test]
        public void Disabling_MidBurst_RestoresBaseAlpha()
        {
            CrtFlickerAnimator animator = Build(out Image target);

            animator.TriggerBurst();
            animator.Tick(0.075f);
            Assert.That(target.color.a, Is.LessThan(BaseAlpha), "Precondition: burst should be dipping alpha.");

            animator.enabled = false;

            Assert.That(target.color.a, Is.EqualTo(BaseAlpha).Within(1e-5f));
        }

        [Test]
        public void SetReducedMotion_Enabled_ShortensBurstDuration()
        {
            CrtFlickerAnimator animator = Build(out Image target);

            animator.SetReducedMotion(true);
            animator.TriggerBurst();
            animator.Tick(0.08f); // past the reduced (0.075s) burst, but short of the original 0.15s

            Assert.That(target.color.a, Is.EqualTo(BaseAlpha).Within(1e-5f),
                "Reduced motion should shorten the burst so it has already finished by 0.08s.");
        }

        [Test]
        public void SetReducedMotion_Disabled_RestoresOriginalBurstDuration()
        {
            CrtFlickerAnimator animator = Build(out Image target);

            animator.SetReducedMotion(true);
            animator.SetReducedMotion(false);
            animator.TriggerBurst();
            animator.Tick(0.08f); // within the restored, original 0.15s burst

            Assert.That(target.color.a, Is.LessThan(BaseAlpha));
        }
    }
}
