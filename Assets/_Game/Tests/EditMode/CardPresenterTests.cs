using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class CardPresenterTests
    {
        [TearDown]
        public void TearDown()
        {
            CardTestFactory.DestroyAll();
            PresentationTestObjects.DestroyAll();
        }

        [Test]
        public void ContentIsCarriedThrough()
        {
            CardDefinition card = CardTestFactory.Card(
                id: "card_a",
                speaker: "The Chancellor",
                bodyText: "The treasury is empty.",
                left: CardTestFactory.Choice("Refuse"),
                right: CardTestFactory.Choice("Agree"));

            CardPresentation presentation = CardPresenter.Create(card);

            Assert.That(presentation.HasCard, Is.True);
            Assert.That(presentation.Speaker, Is.EqualTo("The Chancellor"));
            Assert.That(presentation.BodyText, Is.EqualTo("The treasury is empty."));
            Assert.That(presentation.LeftPreviewText, Is.EqualTo("Refuse"));
            Assert.That(presentation.RightPreviewText, Is.EqualTo("Agree"));
        }

        [Test]
        public void APortraitIsCarriedThroughUnchanged()
        {
            CardDefinition card = CardTestFactory.Card(id: "card_a");
            Assert.That(card.Portrait, Is.Null, "placeholder content has no portraits");

            Assert.That(CardPresenter.Create(card).Portrait, Is.Null);
        }

        [Test]
        public void ANullCardProducesAnEmptyPresentation()
        {
            CardPresentation presentation = CardPresenter.Create(null);

            Assert.That(presentation.HasCard, Is.False);
            Assert.That(presentation.Speaker, Is.Empty);
            Assert.That(presentation.BodyText, Is.Empty);
            Assert.That(presentation.LeftPreviewText, Is.Empty);
            Assert.That(presentation.RightPreviewText, Is.Empty);
            Assert.That(presentation.Portrait, Is.Null);
        }

        [Test]
        public void ACardWithNullChoicesDoesNotThrow()
        {
            // Phase 3 validation reports this as a content error; the view must survive it anyway.
            CardDefinition broken = CardTestFactory.CardWithNullChoices("card_broken");

            CardPresentation presentation = default;
            Assert.That(() => presentation = CardPresenter.Create(broken), Throws.Nothing);
            Assert.That(presentation.HasCard, Is.True);
            Assert.That(presentation.LeftPreviewText, Is.Empty);
            Assert.That(presentation.RightPreviewText, Is.Empty);
        }

        [Test]
        public void EmptyIsDistinctFromACardWithEmptyText()
        {
            CardDefinition blank = CardTestFactory.Card(
                id: "card_blank", speaker: string.Empty, bodyText: string.Empty);

            Assert.That(CardPresenter.Create(blank).HasCard, Is.True);
            Assert.That(CardPresentation.Empty.HasCard, Is.False);
        }
    }
}
