using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class GameOverPresenterTests
    {
        private const string GenericTitle = "The Reign Ends";
        private const string GenericBody = "The chronicles do not record how.";

        [TearDown]
        public void TearDown()
        {
            CardTestFactory.DestroyAll();
            PresentationTestObjects.DestroyAll();
        }

        [Test]
        public void AnEndingSuppliesItsOwnTitleAndBody()
        {
            EndingDefinition ending = CardTestFactory.Ending(
                id: "ending_people_min",
                title: "The People Rise",
                bodyText: "The gates opened inward.",
                triggerStat: StatType.People,
                boundary: StatBoundary.Min);

            GameOverPresentation presentation = GameOverPresenter.Create(
                GameOverResult.Over(StatType.People, StatBoundary.Min, ending),
                GenericTitle,
                GenericBody);

            Assert.That(presentation.HasEnding, Is.True);
            Assert.That(presentation.Title, Is.EqualTo("The People Rise"));
            Assert.That(presentation.BodyText, Is.EqualTo("The gates opened inward."));
            Assert.That(presentation.IsGenericFallback, Is.False);
        }

        [Test]
        public void ARunThatHasNotEndedShowsNothing()
        {
            GameOverPresentation presentation = GameOverPresenter.Create(
                GameOverResult.NotOver(), GenericTitle, GenericBody);

            Assert.That(presentation.HasEnding, Is.False);
        }

        [Test]
        public void AMissingEndingFallsBackToGenericWordingAndSaysSo()
        {
            // Phase 2 deliberately allows IsGameOver with a null Ending. A blank screen would be
            // worse than generic wording, and the caller needs to know which it got.
            GameOverPresentation presentation = GameOverPresenter.Create(
                GameOverResult.Over(StatType.Wealth, StatBoundary.Max, null),
                GenericTitle,
                GenericBody);

            Assert.That(presentation.HasEnding, Is.True);
            Assert.That(presentation.Title, Is.EqualTo(GenericTitle));
            Assert.That(presentation.BodyText, Is.EqualTo(GenericBody));
            Assert.That(presentation.IsGenericFallback, Is.True);
            Assert.That(presentation.Illustration, Is.Null);
        }

        [Test]
        public void AMissingIllustrationIsCarriedThroughAsNull()
        {
            EndingDefinition ending = CardTestFactory.Ending(id: "ending_x");
            Assert.That(ending.Image, Is.Null, "placeholder endings have no art");

            GameOverPresentation presentation = GameOverPresenter.Create(
                GameOverResult.Over(StatType.Authority, StatBoundary.Min, ending),
                GenericTitle,
                GenericBody);

            Assert.That(presentation.Illustration, Is.Null);
            Assert.That(presentation.IsGenericFallback, Is.False,
                "the ending exists; only its art is missing");
        }
    }
}
