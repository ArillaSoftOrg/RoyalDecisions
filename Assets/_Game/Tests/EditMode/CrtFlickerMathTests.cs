using NUnit.Framework;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class CrtFlickerMathTests
    {
        [Test]
        public void BurstAlphaMultiplier_AtStartAndEnd_ReturnsOne()
        {
            Assert.That(CrtFlickerMath.BurstAlphaMultiplier(0f, 0.35f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(CrtFlickerMath.BurstAlphaMultiplier(1f, 0.35f), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void BurstAlphaMultiplier_AtMidpoint_ReturnsDipMultiplier()
        {
            Assert.That(CrtFlickerMath.BurstAlphaMultiplier(0.5f, 0.35f), Is.EqualTo(0.35f).Within(1e-5f));
        }

        [Test]
        public void BurstAlphaMultiplier_ClampsOutOfRangeProgress()
        {
            Assert.That(CrtFlickerMath.BurstAlphaMultiplier(-1f, 0.35f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(CrtFlickerMath.BurstAlphaMultiplier(2f, 0.35f), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void ScaleForReducedMotion_WhenDisabled_ReturnsBaseValueUnchanged()
        {
            Assert.That(CrtFlickerMath.ScaleForReducedMotion(4f, false, 2.5f), Is.EqualTo(4f));
        }

        [Test]
        public void ScaleForReducedMotion_WhenEnabled_MultipliesByScale()
        {
            Assert.That(CrtFlickerMath.ScaleForReducedMotion(4f, true, 2.5f), Is.EqualTo(10f));
        }
    }
}
