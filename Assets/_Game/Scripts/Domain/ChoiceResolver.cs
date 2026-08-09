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
        public ChoiceResolution Resolve(RunState runState, CardDefinition card, ChoiceSide side)
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

            ChoiceDefinition choice = side == ChoiceSide.Left ? card.LeftChoice : card.RightChoice;
            if (choice == null)
            {
                return ChoiceResolution.Rejected(ChoiceResolutionStatus.InvalidCard);
            }

            // Every rejection is decided above. Nothing below this line can fail — the writes are
            // list appends and struct assignments — so the run either sees the whole decision or
            // none of it.
            StatValues statsBefore = statSystem.Current;
            statSystem.Apply(choice.Deltas);
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
    }
}
