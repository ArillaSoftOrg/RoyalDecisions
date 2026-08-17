using NUnit.Framework;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Only the platform-independent surface is covered here: the Android native path is compiled
    /// out under UNITY_EDITOR, so in EditMode this exercises the Handheld.Vibrate() fallback, which
    /// this test only asserts is safe to call, not that a device actually buzzed.
    /// </summary>
    [TestFixture]
    public class UnityHapticServiceTests
    {
        [Test]
        public void IsEnabled_DefaultsTrue()
        {
            UnityHapticService service = new UnityHapticService();

            Assert.That(service.IsEnabled, Is.True);
        }

        [Test]
        public void SetEnabled_TogglesImmediately()
        {
            UnityHapticService service = new UnityHapticService();

            service.SetEnabled(false);
            Assert.That(service.IsEnabled, Is.False);

            service.SetEnabled(true);
            Assert.That(service.IsEnabled, Is.True);
        }

        [Test]
        public void Pulse_NeverThrowsForAnyLevelOrEnabledState()
        {
            UnityHapticService service = new UnityHapticService();

            Assert.DoesNotThrow(() => service.Pulse(HapticFeedbackLevel.Light));
            Assert.DoesNotThrow(() => service.Pulse(HapticFeedbackLevel.Standard));
            Assert.DoesNotThrow(() => service.Pulse(HapticFeedbackLevel.Critical));

            service.SetEnabled(false);
            Assert.DoesNotThrow(() => service.Pulse());
        }
    }
}
