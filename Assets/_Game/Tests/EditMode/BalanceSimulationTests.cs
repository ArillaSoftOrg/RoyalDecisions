using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Editor;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public sealed class BalanceSimulationTests
    {
        private ContentCatalogue catalogue;
        private EndingDefinition[] endings;

        [SetUp]
        public void SetUp()
        {
            CardDefinition card = CardTestFactory.Card(
                id: "card_opening",
                left: CardTestFactory.Choice("Left", authority: -50),
                right: CardTestFactory.Choice("Right", authority: 50));
            endings = new EndingDefinition[8];
            int index = 0;
            StatType[] stats =
            {
                StatType.Authority, StatType.People, StatType.Security, StatType.Wealth
            };
            for (int s = 0; s < stats.Length; s++)
            {
                endings[index++] = CardTestFactory.Ending(
                    "ending_" + stats[s] + "_min", triggerStat: stats[s],
                    boundary: StatBoundary.Min);
                endings[index++] = CardTestFactory.Ending(
                    "ending_" + stats[s] + "_max", triggerStat: stats[s],
                    boundary: StatBoundary.Max);
            }
            catalogue = ScriptableObject.CreateInstance<ContentCatalogue>();
            catalogue.SetAuthoringData(new[] { card }, endings, card.Id);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(catalogue);
            CardTestFactory.DestroyAll();
        }

        [Test]
        public void SameInputsProduceByteIdenticalReportJsonAndHash()
        {
            BalanceSimulationOptions options = new BalanceSimulationOptions
            {
                RunCount = 20,
                BaseSeed = 41,
                MaximumTurns = 10,
                Strategies = new[] { BalanceSimulationStrategy.Random }
            };
            BalanceSimulationRunner runner = new BalanceSimulationRunner();

            BalanceSimulationReport first = runner.Run(catalogue, options);
            BalanceSimulationReport second = runner.Run(catalogue, options);

            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
            Assert.That(second.reproducibilityHash, Is.EqualTo(first.reproducibilityHash));
            Assert.That(first.strategies[0].completedRuns, Is.EqualTo(20));
            Assert.That(first.strategies[0].censoredRuns, Is.Zero);
        }

        [Test]
        public void MaximumTurnReportsCensoredRunWithoutPretendingItEnded()
        {
            CardDefinition neutral = CardTestFactory.Card(
                id: "card_neutral",
                left: CardTestFactory.Choice("Left"),
                right: CardTestFactory.Choice("Right"));
            catalogue.SetAuthoringData(
                new[] { neutral },
                endings,
                neutral.Id);
            BalanceSimulationReport report = new BalanceSimulationRunner().Run(
                catalogue,
                new BalanceSimulationOptions
                {
                    RunCount = 1,
                    MaximumTurns = 3,
                    Strategies = new[] { BalanceSimulationStrategy.AlwaysLeft }
                });

            Assert.That(report.strategies[0].censoredRuns, Is.EqualTo(1));
            Assert.That(report.strategies[0].completedRuns, Is.Zero);
            Assert.That(report.strategies[0].longestTurns, Is.EqualTo(3));
        }
    }
}
