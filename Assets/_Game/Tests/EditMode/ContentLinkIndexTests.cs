using NUnit.Framework;
using RoyalDecisions.Editor;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public sealed class ContentLinkIndexTests
    {
        [TearDown]
        public void TearDown() => CardTestFactory.DestroyAll();

        [Test]
        public void IncomingAndOutgoingLinksAreOrdinalAndNameTheirOrigin()
        {
            var source = CardTestFactory.Card(
                id: "card_a",
                left: CardTestFactory.Choice(forcedNextCardId: "card_c"),
                right: CardTestFactory.Choice(forcedNextCardId: "card_b"));
            var b = CardTestFactory.Card(id: "card_b");
            var c = CardTestFactory.Card(id: "card_c");

            ContentLinkIndex index = new ContentLinkIndex(new[] { source, b, c });

            Assert.That(index.GetOutgoing("card_a"), Is.EqualTo(new[]
            {
                "card_b (right)", "card_c (left)"
            }));
            Assert.That(index.GetIncoming("card_b"), Is.EqualTo(new[] { "card_a (right)" }));
        }
    }
}
