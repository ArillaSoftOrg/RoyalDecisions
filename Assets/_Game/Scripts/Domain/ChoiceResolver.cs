using System;
using System.Collections.Generic;
using RoyalDecisions.Data;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Applies exactly one choice to a run, exactly once.
    /// </summary>
    public sealed class ChoiceResolver
    {
        /// <summary>
        /// A card resolved on turn T with cooldown N becomes drawable again on turn T + N + 1.
        /// The card is resolved on the turn it was shown, so without this offset a cooldown of 1
        /// would expire immediately and have no effect at all.
        /// </summary>
        private const int CooldownOffset = 1;

        private readonly StatSystem statSystem;
        private readonly ConditionEvaluator conditionEvaluator = new ConditionEvaluator();

        public ChoiceResolver(StatSystem statSystem)
        {
            this.statSystem = statSystem ?? throw new ArgumentNullException(nameof(statSystem));
        }

        /// <summary>
        /// Validates that this card is genuinely awaiting a decision, then applies its stat and
        /// flag changes together.
        /// </summary>
        /// <remarks>
        /// Duplicate protection is state-based, not UI-based: <see cref="RunState.CurrentCardId"/>
        /// is a single-use token that a successful resolve consumes. A second call therefore
        /// returns <see cref="ChoiceResolutionStatus.NoActiveCard"/> and changes nothing, even if
        /// the swipe controller misbehaves or the app is backgrounded mid-animation.
        /// </remarks>
        public ChoiceResolution Resolve(
            RunState runState, CardDefinition card, ChoiceSide side, IRandomSource random = null)
        {
            ChoiceDefinition choice = card != null
                ? (side == ChoiceSide.Left ? card.LeftChoice : card.RightChoice)
                : null;

            return Resolve(runState, card, choice, side, random);
        }

        /// <summary>
        /// Resolves against <paramref name="effectiveChoice"/> rather than deriving it from
        /// <paramref name="card"/> — the seam a caller that has already resolved a
        /// <see cref="ResolvedCard"/> (a matched <see cref="Data.CardVariant"/>, possibly) uses, so a
        /// variant's overridden deltas/flags/forced-next apply exactly like the base card's would.
        /// </summary>
        public ChoiceResolution Resolve(
            RunState runState,
            CardDefinition card,
            ChoiceDefinition effectiveChoice,
            ChoiceSide side,
            IRandomSource random = null)
        {
            if (runState == null || card == null || string.IsNullOrEmpty(card.Id))
            {
                return ChoiceResolution.Rejected(ChoiceResolutionStatus.InvalidCard);
            }

            if (!runState.IsRunActive)
            {
                return ChoiceResolution.Rejected(ChoiceResolutionStatus.RunNotActive);
            }

            if (string.IsNullOrEmpty(runState.CurrentCardId))
            {
                return ChoiceResolution.Rejected(ChoiceResolutionStatus.NoActiveCard);
            }

            if (!string.Equals(runState.CurrentCardId, card.Id, StringComparison.Ordinal))
            {
                return ChoiceResolution.Rejected(ChoiceResolutionStatus.CardMismatch);
            }

            ChoiceDefinition choice = effectiveChoice;
            if (choice == null)
            {
                return ChoiceResolution.Rejected(ChoiceResolutionStatus.InvalidCard);
            }

            // Every rejection is decided above. Nothing below this line can fail — the writes are
            // list appends and struct assignments — so the run either sees the whole decision or
            // none of it.
            StatValues statsBefore = statSystem.Current;
            statSystem.Apply(choice.Deltas);

            if (choice.HasRandomOutcome)
            {
                IRandomSource resolutionRandom =
                    random ?? SeededRandomSource.ForChoiceResolution(runState.Seed, runState.Turn);
                ApplyRandomOutcome(choice.RandomOutcome, resolutionRandom);
            }

            if (choice.HasConditionalEffect)
            {
                ApplyConditionalEffect(runState, choice.ConditionalEffect);
            }

            ApplyCounters(runState, choice);

            StatValues statsAfter = statSystem.Current;

            ApplyFlags(runState, choice);

            runState.MarkCardShown(card.Id);

            if (card.HasCooldown)
            {
                // Read before AdvanceTurn: this is the turn the card was shown on.
                runState.SetCooldown(card.Id, runState.Turn + card.CooldownTurns + CooldownOffset);
            }

            // A choice-level chain overrides the card-level one, so one side of a card can branch
            // while the other follows the card's default.
            string forcedNextCardId = choice.HasForcedNextCard
                ? choice.ForcedNextCardId
                : card.ForcedNextCardId;
            runState.SetForcedNextCardId(forcedNextCardId);

            runState.AdvanceTurn();
            runState.SetCurrentCardId(string.Empty);

            return ChoiceResolution.Applied(side, statsBefore, statsAfter, forcedNextCardId);
        }

        /// <summary>
        /// Additions run before removals, so a choice naming the same flag in both lists ends
        /// without it.
        /// </summary>
        private static void ApplyFlags(RunState runState, ChoiceDefinition choice)
        {
            IReadOnlyList<string> toAdd = choice.FlagsToAdd;
            for (int i = 0; i < toAdd.Count; i++)
            {
                runState.AddFlag(toAdd[i]);
            }

            IReadOnlyList<string> toRemove = choice.FlagsToRemove;
            for (int i = 0; i < toRemove.Count; i++)
            {
                runState.RemoveFlag(toRemove[i]);
            }
        }

        private void ApplyRandomOutcome(RandomStatOutcome outcome, IRandomSource random)
        {
            IReadOnlyList<StatDeltas> options = outcome.Options;
            int index = random.NextInt(options.Count);
            statSystem.Apply(options[index]);
        }

        /// <summary>
        /// Branches on <paramref name="effect"/>'s condition, then — only on the branch that
        /// triggers a reign succession — resets the affected statistic and leader health together,
        /// atomically with everything else this decision does. This is what keeps
        /// <see cref="GameOverEvaluator"/> from ever seeing the statistic sitting at a boundary: by
        /// the time it runs, the succession has already happened.
        /// </summary>
        private void ApplyConditionalEffect(RunState runState, ConditionalChoiceEffect effect)
        {
            bool conditionMet = conditionEvaluator.EvaluateNumeric(effect.Condition, runState);

            if (conditionMet)
            {
                statSystem.Apply(effect.DeltasWhenTrue);
                runState.AdjustLeaderHealth(effect.LeaderHealthDeltaWhenTrue);

                if (effect.TriggersSuccessionWhenTrue)
                {
                    if (effect.HasSuccessionResetStat)
                    {
                        statSystem.Set(statSystem.Current.With(
                            effect.SuccessionResetStat, GameConstants.ReignSuccessionResetStatValue));
                    }

                    runState.SetLeaderHealth(LeaderHealthBounds.Initial);
                    runState.IncrementReignNumber();
                }
            }
            else
            {
                statSystem.Apply(effect.DeltasWhenFalse);
                runState.AdjustLeaderHealth(effect.LeaderHealthDeltaWhenFalse);
            }
        }

        private static void ApplyCounters(RunState runState, ChoiceDefinition choice)
        {
            IReadOnlyList<CounterDelta> deltas = choice.CounterDeltas;
            for (int i = 0; i < deltas.Count; i++)
            {
                CounterDelta delta = deltas[i];
                if (delta != null)
                {
                    runState.AddToCounter(delta.CounterId, delta.Delta);
                }
            }
        }
    }
}
