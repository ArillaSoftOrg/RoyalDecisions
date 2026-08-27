using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Editor;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Exercises the placeholder prologue slide set in memory — no AssetDatabase involvement.
    /// </summary>
    [TestFixture]
    public class PrologueDefaultContentTests
    {
        private static readonly string[] ExpectedSubtitles =
        {
            "Dünya, birkaç yıl içinde sessizliğe gömüldü.",
            "Hayatta kalanlar, güvenli olduğunu düşündükleri son sığınaklara çekildi.",
            "Fakat duvarlar açlığı, korkuyu ve insanların birbirine olan güvensizliğini durduramadı.",
            "Şimdi onların geleceğini belirleyecek kararlar senin elinde.",
            "Her seçim bir hayat kurtarabilir... ya da her şeyi sona erdirebilir.",
        };

        [Test]
        public void CreateSlides_ReturnsExactlyFiveSlides()
        {
            PrologueSlideData[] slides = PrologueDefaultContent.CreateSlides();

            Assert.That(slides.Length, Is.EqualTo(PrologueDefaultContent.SlideCount));
            Assert.That(slides.Length, Is.EqualTo(5));
        }

        [Test]
        public void CreateSlides_SubtitlesMatchThePlaceholderStoryInOrder()
        {
            PrologueSlideData[] slides = PrologueDefaultContent.CreateSlides();

            for (int i = 0; i < ExpectedSubtitles.Length; i++)
            {
                Assert.That(slides[i].Subtitle, Is.EqualTo(ExpectedSubtitles[i]),
                    "Slide " + i + " subtitle must match the authored placeholder story exactly.");
            }
        }

        [Test]
        public void CreateSlides_WithoutIllustrations_LeavesEverySlideWithNoSprite()
        {
            PrologueSlideData[] slides = PrologueDefaultContent.CreateSlides();

            foreach (PrologueSlideData slide in slides)
            {
                Assert.That(slide.Illustration, Is.Null);
            }
        }

        [Test]
        public void CreateSlides_EveryMotionValueIsExplicitlyAuthored()
        {
            PrologueSlideData[] slides = PrologueDefaultContent.CreateSlides();

            foreach (PrologueSlideData slide in slides)
            {
                Assert.That(slide.Motion, Is.Not.EqualTo(PrologueSlideMotion.None),
                    "Every sample slide should authors a real motion style, not the inert default.");
            }
        }

        [Test]
        public void CreateSlides_NoneOfTheSampleSlidesAutoAdvanceByDefault()
        {
            PrologueSlideData[] slides = PrologueDefaultContent.CreateSlides();

            foreach (PrologueSlideData slide in slides)
            {
                Assert.That(slide.HasAutoAdvance, Is.False,
                    "Tap-to-continue must be the default interaction; auto-advance is opt-in only.");
            }
        }
    }
}
