using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class CardVariantResolverTests
    {
        private const int TestSeed = 4242;

        private CardVariantResolver resolver;
        private RunState runState;

        [SetUp]
        public void SetUp()
        {
            resolver = new CardVariantResolver(new ConditionEvaluator());
            runState = RunState.CreateNew(TestSeed);
        }

        [TearDown]
        public void TearDown()
        {
            CardTestFactory.DestroyAll();
        }

        [Test]
        public void Resolve_WithNoMatchingVariant_ReturnsTheBaseCardsOwnFields()
        {
            ChoiceDefinition left = CardTestFactory.Choice("Base left");
            ChoiceDefinition right = CardTestFactory.Choice("Base right");
            CardDefinition card = CardTestFactory.Card(
                speaker: "Base speaker", bodyText: "Base body", left: left, right: right,
                variants: new[]
                {
                    new CardVariant(CardTestFactory.Conditions(requiredFlags: new[] { "never_set" }),
                        bodyText: "Should not appear")
                });

            ResolvedCard resolved = resolver.Resolve(card, runState);

            Assert.That(resolved.Speaker, Is.EqualTo("Base speaker"));
            Assert.That(resolved.BodyText, Is.EqualTo("Base body"));
            Assert.That(resolved.LeftChoice, Is.SameAs(left));
            Assert.That(resolved.RightChoice, Is.SameAs(right));
        }

        [Test]
        public void Resolve_WithAMatchingVariant_OverridesTheBaseCardsFields()
        {
            ChoiceDefinition variantLeft = CardTestFactory.Choice("Variant left");
            ChoiceDefinition variantRight = CardTestFactory.Choice("Variant right");
            CardDefinition card = CardTestFactory.Card(
                speaker: "Base speaker", bodyText: "Base body",
                variants: new[]
                {
                    new CardVariant(CardTestFactory.Conditions(requiredFlags: new[] { "unlocked" }),
                        speaker: "Variant speaker", bodyText: "Variant body",
                        leftChoice: variantLeft, rightChoice: variantRight)
                });

            runState.AddFlag("unlocked");
            ResolvedCard resolved = resolver.Resolve(card, runState);

            Assert.That(resolved.Speaker, Is.EqualTo("Variant speaker"));
            Assert.That(resolved.BodyText, Is.EqualTo("Variant body"));
            Assert.That(resolved.LeftChoice, Is.SameAs(variantLeft));
            Assert.That(resolved.RightChoice, Is.SameAs(variantRight));
        }

        [Test]
        public void Resolve_APartialVariant_OnlyOverridesTheFieldsItSets()
        {
            ChoiceDefinition baseLeft = CardTestFactory.Choice("Base left");
            ChoiceDefinition baseRight = CardTestFactory.Choice("Base right");
            ChoiceDefinition variantRight = CardTestFactory.Choice("Variant right only");
            CardDefinition card = CardTestFactory.Card(
                speaker: "Base speaker", bodyText: "Base body", left: baseLeft, right: baseRight,
                variants: new[]
                {
                    // No speaker, no bodyText, no leftChoice override — only the right choice.
                    new CardVariant(CardTestFactory.Conditions(requiredFlags: new[] { "flag" }),
                        rightChoice: variantRight)
                });

            runState.AddFlag("flag");
            ResolvedCard resolved = resolver.Resolve(card, runState);

            Assert.That(resolved.Speaker, Is.EqualTo("Base speaker"), "speaker not overridden");
            Assert.That(resolved.BodyText, Is.EqualTo("Base body"), "body text not overridden");
            Assert.That(resolved.LeftChoice, Is.SameAs(baseLeft), "left choice not overridden");
            Assert.That(resolved.RightChoice, Is.SameAs(variantRight), "right choice overridden");
        }

        [Test]
        public void Resolve_WithMultipleMatchingVariants_TheFirstInOrderWins()
        {
            CardDefinition card = CardTestFactory.Card(
                bodyText: "Base",
                variants: new[]
                {
                    new CardVariant(CardTestFactory.Conditions(requiredFlags: new[] { "flag" }),
                        bodyText: "First matching variant"),
                    new CardVariant(CardTestFactory.Conditions(requiredFlags: new[] { "flag" }),
                        bodyText: "Second matching variant, never chosen")
                });

            runState.AddFlag("flag");
            ResolvedCard resolved = resolver.Resolve(card, runState);

            Assert.That(resolved.BodyText, Is.EqualTo("First matching variant"));
        }

        [Test]
        public void Resolve_MostSpecificVariantFirst_LetsACompoundConditionTakePriority()
        {
            CardDefinition card = CardTestFactory.Card(
                bodyText: "Neither flag",
                variants: new[]
                {
                    new CardVariant(
                        CardTestFactory.Conditions(requiredFlags: new[] { "a", "b" }),
                        bodyText: "Both flags"),
                    new CardVariant(CardTestFactory.Conditions(requiredFlags: new[] { "a" }),
                        bodyText: "Only a")
                });

            runState.AddFlag("a");
            Assert.That(resolver.Resolve(card, runState).BodyText, Is.EqualTo("Only a"));

            runState.AddFlag("b");
            Assert.That(resolver.Resolve(card, runState).BodyText, Is.EqualTo("Both flags"));
        }

        [Test]
        public void Resolve_MarksAConditionallyUnavailableChoiceCorrectly()
        {
            ChoiceDefinition gated = CardTestFactory.Choice("Gated",
                availability: CardTestFactory.Conditions(requiredFlags: new[] { "has_key" }));
            CardDefinition card = CardTestFactory.Card(left: gated);

            Assert.That(resolver.Resolve(card, runState).LeftAvailable, Is.False);

            runState.AddFlag("has_key");
            Assert.That(resolver.Resolve(card, runState).LeftAvailable, Is.True);
        }

        [Test]
        public void Resolve_WithNoAvailabilityCondition_IsAlwaysAvailable()
        {
            CardDefinition card = CardTestFactory.Card();

            ResolvedCard resolved = resolver.Resolve(card, runState);

            Assert.That(resolved.LeftAvailable, Is.True);
            Assert.That(resolved.RightAvailable, Is.True);
        }

        [Test]
        public void Resolve_AvailabilityIsEvaluatedAgainstTheEffectiveVariantChoice()
        {
            // The variant's own right choice is gated; the base card's right choice is not.
            ChoiceDefinition variantRight = CardTestFactory.Choice("Gated in variant",
                availability: CardTestFactory.Conditions(requiredFlags: new[] { "has_key" }));
            CardDefinition card = CardTestFactory.Card(
                variants: new[]
                {
                    new CardVariant(CardTestFactory.Conditions(requiredFlags: new[] { "in_variant" }),
                        rightChoice: variantRight)
                });

            runState.AddFlag("in_variant");
            Assert.That(resolver.Resolve(card, runState).RightAvailable, Is.False);

            runState.AddFlag("has_key");
            Assert.That(resolver.Resolve(card, runState).RightAvailable, Is.True);
        }

        [Test]
        public void Resolve_WithANullCard_ReturnsEmpty()
        {
            ResolvedCard resolved = resolver.Resolve(null, runState);

            Assert.That(resolved.HasCard, Is.False);
        }

        [Test]
        public void Constructor_RejectsANullConditionEvaluator()
        {
            Assert.That(() => new CardVariantResolver(null), Throws.ArgumentNullException);
        }
    }
}
