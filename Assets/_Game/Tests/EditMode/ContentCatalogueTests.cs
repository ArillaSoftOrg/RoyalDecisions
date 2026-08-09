using NUnit.Framework;
using RoyalDecisions.Data;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class ContentCatalogueTests
    {
        private ContentCatalogue catalogue;

        [SetUp]
        public void SetUp()
        {
            catalogue = ScriptableObject.CreateInstance<ContentCatalogue>();
        }

        [TearDown]
        public void TearDown()
        {
            if (catalogue != null)
            {
                Object.DestroyImmediate(catalogue);
            }

            CardTestFactory.DestroyAll();
        }

        [Test]
        public void FreshCatalogue_ReturnsEmptyCollectionsRatherThanNull()
        {
            Assert.That(catalogue.Cards, Is.Not.Null.And.Empty);
            Assert.That(catalogue.Endings, Is.Not.Null.And.Empty);
            Assert.That(catalogue.OpeningCardId, Is.Empty);
            Assert.That(catalogue.HasOpeningCard, Is.False);
        }

        [Test]
        public void SetAuthoringData_StoresCardsInTheGivenOrder()
        {
            CardDefinition first = CardTestFactory.Card(id: "card_a");
            CardDefinition second = CardTestFactory.Card(id: "card_b");

            catalogue.SetAuthoringData(new[] { first, second }, null, "card_a");

            Assert.That(catalogue.Cards.Count, Is.EqualTo(2));
            Assert.That(catalogue.Cards[0], Is.SameAs(first));
            Assert.That(catalogue.Cards[1], Is.SameAs(second));
        }

        [Test]
        public void SetAuthoringData_StoresEndingsAndOpeningCard()
        {
            EndingDefinition ending = CardTestFactory.Ending(id: "ending_x");

            catalogue.SetAuthoringData(null, new[] { ending }, "card_opening");

            Assert.That(catalogue.Endings.Count, Is.EqualTo(1));
            Assert.That(catalogue.Endings[0], Is.SameAs(ending));
            Assert.That(catalogue.OpeningCardId, Is.EqualTo("card_opening"));
            Assert.That(catalogue.HasOpeningCard, Is.True);
        }

        [Test]
        public void SetAuthoringData_TreatsNullArgumentsAsEmpty()
        {
            catalogue.SetAuthoringData(null, null, null);

            Assert.That(catalogue.Cards, Is.Empty);
            Assert.That(catalogue.Endings, Is.Empty);
            Assert.That(catalogue.OpeningCardId, Is.Empty);
            Assert.That(catalogue.HasOpeningCard, Is.False);
        }

        [Test]
        public void EmptyOpeningCardId_MeansNoOpeningCard()
        {
            catalogue.SetAuthoringData(null, null, string.Empty);

            Assert.That(catalogue.HasOpeningCard, Is.False);
        }
    }
}
