using System;
using NUnit.Framework;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class HapticProfileTests
    {
        [Test]
        public void Light_IsShorterAndWeakerThanStandard()
        {
            HapticProfile light = HapticProfile.For(HapticFeedbackLevel.Light);
            HapticProfile standard = HapticProfile.For(HapticFeedbackLevel.Standard);

            Assert.That(light.DurationMilliseconds, Is.LessThan(standard.DurationMilliseconds));
            Assert.That(light.Amplitude, Is.LessThan(standard.Amplitude));
        }

        [Test]
        public void Critical_IsStrongerAndLongerThanStandard()
        {
            HapticProfile critical = HapticProfile.For(HapticFeedbackLevel.Critical);
            HapticProfile standard = HapticProfile.For(HapticFeedbackLevel.Standard);

            Assert.That(critical.DurationMilliseconds, Is.GreaterThan(standard.DurationMilliseconds));
            Assert.That(critical.Amplitude, Is.GreaterThan(standard.Amplitude));
        }

        [Test]
        public void Amplitude_StaysWithinAndroidVibrationEffectRange()
        {
            foreach (HapticFeedbackLevel level in (HapticFeedbackLevel[])Enum.GetValues(
                typeof(HapticFeedbackLevel)))
            {
                HapticProfile profile = HapticProfile.For(level);

                Assert.That(profile.Amplitude, Is.InRange(1, 255));
                Assert.That(profile.DurationMilliseconds, Is.GreaterThan(0));
            }
        }
    }
}
