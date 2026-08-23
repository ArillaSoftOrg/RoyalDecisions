using System;
using NUnit.Framework;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class SeededRandomSourceTests
    {
        private const int SampleCount = 64;
        private const int Range = 100;

        private static int[] Take(IRandomSource random, int count, int exclusiveMax)
        {
            int[] values = new int[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = random.NextInt(exclusiveMax);
            }

            return values;
        }

        [Test]
        public void SameSeed_ProducesTheSameSequence()
        {
            int[] first = Take(new SeededRandomSource(12345), SampleCount, Range);
            int[] second = Take(new SeededRandomSource(12345), SampleCount, Range);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            int[] first = Take(new SeededRandomSource(12345), SampleCount, Range);
            int[] second = Take(new SeededRandomSource(54321), SampleCount, Range);

            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void ForTurn_IsStableForTheSameSeedAndTurn()
        {
            int[] first = Take(SeededRandomSource.ForTurn(777, 4), SampleCount, Range);
            int[] second = Take(SeededRandomSource.ForTurn(777, 4), SampleCount, Range);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void ForTurn_GivesEachTurnItsOwnStream()
        {
            int[] turn4 = Take(SeededRandomSource.ForTurn(777, 4), SampleCount, Range);
            int[] turn5 = Take(SeededRandomSource.ForTurn(777, 5), SampleCount, Range);

            Assert.That(turn5, Is.Not.EqualTo(turn4));
        }

        [Test]
        public void ForTurn_GivesEachSeedItsOwnStream()
        {
            int[] seedA = Take(SeededRandomSource.ForTurn(1, 0), SampleCount, Range);
            int[] seedB = Take(SeededRandomSource.ForTurn(2, 0), SampleCount, Range);

            Assert.That(seedB, Is.Not.EqualTo(seedA));
        }

        [Test]
        public void SeedZero_StillVaries()
        {
            // xorshift is absorbing at zero: without the fallback state this sequence would be all
            // zeroes and every draw would pick the first card forever.
            int[] values = Take(new SeededRandomSource(0), SampleCount, Range);

            Assert.That(values, Is.Not.All.EqualTo(0));
        }

        [Test]
        public void ForTurnZeroZero_StillVaries()
        {
            int[] values = Take(SeededRandomSource.ForTurn(0, 0), SampleCount, Range);

            Assert.That(values, Is.Not.All.EqualTo(0));
        }

        [Test]
        public void NegativeSeed_IsAccepted()
        {
            Assert.That(() => Take(new SeededRandomSource(-98765), SampleCount, Range),
                Throws.Nothing);
        }

        [Test]
        public void NextInt_StaysWithinRange()
        {
            IRandomSource random = new SeededRandomSource(4242);

            for (int i = 0; i < 2000; i++)
            {
                int value = random.NextInt(7);
                Assert.That(value, Is.InRange(0, 6));
            }
        }

        [Test]
        public void NextInt_WithRangeOfOne_AlwaysReturnsZero()
        {
            IRandomSource random = new SeededRandomSource(4242);

            for (int i = 0; i < 32; i++)
            {
                Assert.That(random.NextInt(1), Is.Zero);
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void NextInt_RejectsNonPositiveRange(int exclusiveMax)
        {
            IRandomSource random = new SeededRandomSource(1);

            Assert.That(() => random.NextInt(exclusiveMax),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void NextInt_EventuallyReachesEveryValueInASmallRange()
        {
            // Guards against a stream that is deterministic but degenerate.
            IRandomSource random = new SeededRandomSource(31337);
            bool[] seen = new bool[4];

            for (int i = 0; i < 500; i++)
            {
                seen[random.NextInt(seen.Length)] = true;
            }

            Assert.That(seen, Is.All.True);
        }

        // --- ForChoiceResolution ----------------------------------------------

        [Test]
        public void ForChoiceResolution_IsStableForTheSameSeedAndTurn()
        {
            int[] first = Take(SeededRandomSource.ForChoiceResolution(777, 4), SampleCount, Range);
            int[] second = Take(SeededRandomSource.ForChoiceResolution(777, 4), SampleCount, Range);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void ForChoiceResolution_DivergesFromCardSelectionOnTheSameTurn()
        {
            // Deliberately salted differently from ForTurn: a card-selection draw and a choice's
            // random outcome must not silently share one stream just because they land on the same
            // turn number.
            int[] cardSelection = Take(SeededRandomSource.ForTurn(777, 4), SampleCount, Range);
            int[] choiceResolution = Take(SeededRandomSource.ForChoiceResolution(777, 4), SampleCount, Range);

            Assert.That(choiceResolution, Is.Not.EqualTo(cardSelection));
        }

        [Test]
        public void ForChoiceResolution_GivesEachTurnItsOwnStream()
        {
            int[] turn4 = Take(SeededRandomSource.ForChoiceResolution(777, 4), SampleCount, Range);
            int[] turn5 = Take(SeededRandomSource.ForChoiceResolution(777, 5), SampleCount, Range);

            Assert.That(turn5, Is.Not.EqualTo(turn4));
        }
    }
}
