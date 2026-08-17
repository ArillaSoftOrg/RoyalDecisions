using NUnit.Framework;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class NoOpHapticServiceTests
    {
        [Test]
        public void Pulse_NeverThrowsRegardlessOfEnabledState()
        {
            NoOpHapticService service = new NoOpHapticService();

            Assert.DoesNotThrow(() => service.Pulse());

            service.SetEnabled(true);
            Assert.DoesNotThrow(() => service.Pulse(HapticFeedbackLevel.Critical));
        }

        [Test]
        public void SetEnabled_UpdatesIsEnabled()
        {
            NoOpHapticService service = new NoOpHapticService();

            Assert.That(service.IsEnabled, Is.False);

            service.SetEnabled(true);
            Assert.That(service.IsEnabled, Is.True);
        }
    }
}
