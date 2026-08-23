using System;
using System.Collections.Generic;
using RoyalDecisions.Data;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Checks a content set for the mistakes that would otherwise surface as a broken run.
    /// </summary>
    /// <remarks>
    /// Takes plain lists rather than a <see cref="ContentCatalogue"/>, so it works equally on
    /// generated content, hand-authored content, and content built in a test.
    ///
    /// Every string comparison here is ordinal. Culture-aware collation would make the set of
    /// "duplicate" IDs depend on the machine's locale — under Turkish rules the dotted and dotless
    /// I compare differently, so the same content could validate on one developer's machine and
    /// fail on another's.
    /// </remarks>
    public sealed class ContentValidator
    {
        private static readonly StatType[] AllStats =
        {
            StatType.Authority,
            StatType.People,
            StatType.Security,
            StatType.Wealth
        };

        private static readonly StatBoundary[] AllBoundaries =
        {
            StatBoundary.Min,
            StatBoundary.Max
        };

        public ContentValidationReport Validate(
            IReadOnlyList<CardDefinition> cards,
            IReadOnlyList<EndingDefinition> endings,
            string openingCardId = null,
            ContentValidationOptions options = null)
        {
            options ??= new ContentValidationOptions();
            List<ContentValidationIssue> issues = new List<ContentValidationIssue>();

            Dictionary<string, CardDefinition> cardsById =
                new Dictionary<string, CardDefinition>(StringComparer.Ordinal);

            ValidateCards(cards, issues, cardsById, options);
            ValidateCardOrdering(cards, issues);
            ValidateForcedTargets(cards, cardsById, issues);
            ValidateForcedCycles(cards, cardsById, issues);
            ValidateFlagReachability(cards, issues);
            ValidateFlagUsage(cards, issues, options);
            ValidateEndings(endings, issues);
            ValidateOptionalEndingArt(endings, issues, options);
            ValidateOpeningCard(openingCardId, cardsById, issues);
            ValidateForcedChainOnlyReachability(cards, openingCardId, issues);
            ValidateOncePerRunConvergence(cards, issues);
            ValidateDeadEndTerminals(cards, options, issues);

            return new ContentValidationReport(issues);
        }

        // --- Cards -----------------------------------------------------------

        private static void ValidateCards(
            IReadOnlyList<CardDefinition> cards,
            List<ContentValidationIssue> issues,
            Dictionary<string, CardDefinition> cardsById,
            ContentValidationOptions options)
        {
            if (cards == null)
            {
                return;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];

                if (card == null)
                {
                    issues.Add(ContentValidationIssue.Error(
                        ContentIssueCode.NullCardEntry,
                        "index " + i,
                        "Card list contains a null entry."));
                    continue;
                }

                string id = card.Id;

                if (string.IsNullOrWhiteSpace(id))
                {
                    issues.Add(ContentValidationIssue.Error(
                        ContentIssueCode.EmptyCardId,
                        "index " + i,
                        "Card has no ID; it can never be referenced or saved."));
                    continue;
                }

                if (cardsById.ContainsKey(id))
                {
                    issues.Add(ContentValidationIssue.Error(
                        ContentIssueCode.DuplicateCardId,
                        id,
                        "Two cards share this ID. Weighted selection orders cards by ID, and that " +
                        "order is undefined when IDs repeat."));
                }
                else
                {
                    cardsById.Add(id, card);
                }

                ValidateChoices(card, issues);
                ValidateStatRanges(card, issues);
                ValidateConditionConsistency(card, issues);
                ValidateCardText(card, issues);
                ValidateAuthoringBounds(card, issues, options);

                if (options.IncludeInformation && card.Portrait == null)
                {
                    issues.Add(ContentValidationIssue.Information(
                        ContentIssueCode.OptionalPortraitMissing,
                        id,
                        "Optional portrait artwork is not assigned."));
                }

                if (card.OncePerRun && card.HasCooldown)
                {
                    issues.Add(ContentValidationIssue.Warning(
                        ContentIssueCode.RedundantCooldown,
                        id,
                        "A once-per-run card can never come back, so its cooldown has no effect."));
                }
            }
        }

        private static void ValidateChoices(CardDefinition card, List<ContentValidationIssue> issues)
        {
            if (card.LeftChoice == null)
            {
                issues.Add(ContentValidationIssue.Error(
                    ContentIssueCode.MissingChoice,
                    card.Id,
                    "Card has no left choice."));
            }

            if (card.RightChoice == null)
            {
                issues.Add(ContentValidationIssue.Error(
                    ContentIssueCode.MissingChoice,
                    card.Id,
                    "Card has no right choice."));
            }
        }

        private static void ValidateStatRanges(
            CardDefinition card,
            List<ContentValidationIssue> issues)
        {
            CardConditions conditions = card.Conditions;
            if (conditions == null)
            {
                return;
            }

            IReadOnlyList<StatRange> ranges = conditions.StatRanges;
            for (int i = 0; i < ranges.Count; i++)
            {
                StatRange range = ranges[i];
                if (range == null)
                {
                    continue;
                }

                if (range.Min > range.Max)
                {
                    issues.Add(ContentValidationIssue.Error(
                        ContentIssueCode.InvalidStatRange,
                        card.Id,
                        string.Format(
                            "{0} range {1}..{2} is inverted, so no run can ever satisfy it.",
                            range.Stat,
                            range.Min,
                            range.Max)));
                    continue;
                }

                if (range.Min < StatBounds.Min || range.Max > StatBounds.Max)
                {
                    issues.Add(ContentValidationIssue.Error(
                        ContentIssueCode.InvalidStatRange,
                        card.Id,
                        string.Format(
                            "{0} range {1}..{2} falls outside the legal {3}..{4} bounds.",
                            range.Stat,
                            range.Min,
                            range.Max,
                            StatBounds.Min,
                            StatBounds.Max)));
                }
            }
        }

        private static void ValidateConditionConsistency(
            CardDefinition card,
            List<ContentValidationIssue> issues)
        {
            CardConditions conditions = card.Conditions;
            if (conditions == null)
            {
                return;
            }

            HashSet<string> required = new HashSet<string>(StringComparer.Ordinal);
            AddDuplicateFindings(
                conditions.RequiredFlags, required, card.Id, "required flag", issues);
            HashSet<string> forbidden = new HashSet<string>(StringComparer.Ordinal);
            AddDuplicateFindings(
                conditions.ForbiddenFlags, forbidden, card.Id, "forbidden flag", issues);
            foreach (string flag in required)
            {
                if (forbidden.Contains(flag))
                {
                    issues.Add(ContentValidationIssue.Error(
                        ContentIssueCode.ConflictingFlags,
                        card.Id,
                        "Flag '" + flag + "' is both required and forbidden."));
                }
            }

            Dictionary<StatType, int[]> intersections =
                new Dictionary<StatType, int[]>();
            HashSet<string> rangeKeys = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<StatRange> ranges = conditions.StatRanges;
            for (int i = 0; i < ranges.Count; i++)
            {
                StatRange range = ranges[i];
                if (range == null)
                {
                    continue;
                }
                string key = range.Stat + ":" + range.Min + ":" + range.Max;
                if (!rangeKeys.Add(key))
                {
                    issues.Add(ContentValidationIssue.Warning(
                        ContentIssueCode.DuplicateConditionEntry,
                        card.Id,
                        "Duplicate stat range " + key + "."));
                }
                int[] current = intersections.TryGetValue(range.Stat, out int[] value)
                    ? value
                    : new[] { StatBounds.Min, StatBounds.Max };
                current[0] = Math.Max(current[0], range.Min);
                current[1] = Math.Min(current[1], range.Max);
                intersections[range.Stat] = current;
            }
            foreach (KeyValuePair<StatType, int[]> pair in intersections)
            {
                if (pair.Value[0] > pair.Value[1])
                {
                    issues.Add(ContentValidationIssue.Error(
                        ContentIssueCode.EmptyStatRangeIntersection,
                        card.Id,
                        pair.Key + " ranges have no shared value."));
                }
            }
        }

        private static void AddDuplicateFindings(
            IReadOnlyList<string> values,
            HashSet<string> unique,
            string cardId,
            string label,
            List<ContentValidationIssue> issues)
        {
            for (int i = 0; i < values.Count; i++)
            {
                string value = values[i];
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }
                if (!unique.Add(value))
                {
                    issues.Add(ContentValidationIssue.Warning(
                        ContentIssueCode.DuplicateConditionEntry,
                        cardId,
                        "Duplicate " + label + " '" + value + "'."));
                }
            }
        }

        private static void ValidateAuthoringBounds(
            CardDefinition card,
            List<ContentValidationIssue> issues,
            ContentValidationOptions options)
        {
            if (card.Speaker.Length > options.MaximumSpeakerLength
                || card.BodyText.Length > options.MaximumBodyLength)
            {
                issues.Add(ContentValidationIssue.Warning(
                    ContentIssueCode.ExcessiveTextLength,
                    card.Id,
                    "Speaker or body text exceeds the configured authoring length."));
            }
            ValidateChoiceBounds(card.Id, "left", card.LeftChoice, issues, options);
            ValidateChoiceBounds(card.Id, "right", card.RightChoice, issues, options);
        }

        private static void ValidateChoiceBounds(
            string cardId,
            string side,
            ChoiceDefinition choice,
            List<ContentValidationIssue> issues,
            ContentValidationOptions options)
        {
            if (choice == null)
            {
                return;
            }
            if (choice.PreviewText.Length > options.MaximumPreviewLength)
            {
                issues.Add(ContentValidationIssue.Warning(
                    ContentIssueCode.ExcessiveTextLength,
                    cardId,
                    side + " preview text exceeds the configured authoring length."));
            }
            int total = 0;
            bool singleTooLarge = false;
            for (int i = 0; i < AllStats.Length; i++)
            {
                int magnitude = Math.Abs(choice.Deltas[AllStats[i]]);
                total += magnitude;
                singleTooLarge |= magnitude > options.MaximumSingleStatDelta;
            }
            if (singleTooLarge || total > options.MaximumTotalAbsoluteDelta)
            {
                issues.Add(ContentValidationIssue.Warning(
                    ContentIssueCode.ExcessiveStatDelta,
                    cardId,
                    side + " choice exceeds the configured stat-delta budget."));
            }
        }

        private static void ValidateCardText(CardDefinition card, List<ContentValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(card.BodyText))
            {
                issues.Add(ContentValidationIssue.Warning(
                    ContentIssueCode.EmptyText,
                    card.Id,
                    "Card has no body text."));
            }

            if (card.LeftChoice != null && string.IsNullOrWhiteSpace(card.LeftChoice.PreviewText))
            {
                issues.Add(ContentValidationIssue.Warning(
                    ContentIssueCode.EmptyText,
                    card.Id,
                    "Left choice has no preview text, so the drag gives no feedback."));
            }

            if (card.RightChoice != null && string.IsNullOrWhiteSpace(card.RightChoice.PreviewText))
            {
                issues.Add(ContentValidationIssue.Warning(
                    ContentIssueCode.EmptyText,
                    card.Id,
                    "Right choice has no preview text, so the drag gives no feedback."));
            }
        }

        /// <summary>
        /// The catalogue stores cards pre-sorted so regeneration produces no spurious asset diff.
        /// </summary>
        private static void ValidateCardOrdering(
            IReadOnlyList<CardDefinition> cards,
            List<ContentValidationIssue> issues)
        {
            if (cards == null)
            {
                return;
            }

            string previousId = null;

            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (card == null || string.IsNullOrWhiteSpace(card.Id))
                {
                    continue;
                }

                string id = card.Id;

                if (previousId != null && StringComparer.Ordinal.Compare(previousId, id) > 0)
                {
                    issues.Add(ContentValidationIssue.Error(
                        ContentIssueCode.CardsNotOrdinallySorted,
                        id,
                        string.Format(
                            "Cards are not in ascending ordinal ID order: '{0}' precedes '{1}'.",
                            previousId,
                            id)));
                    return;
                }

                previousId = id;
            }
        }

        // --- Forced chains ------------------------------------------------------

        private static void ValidateForcedTargets(
            IReadOnlyList<CardDefinition> cards,
            Dictionary<string, CardDefinition> cardsById,
            List<ContentValidationIssue> issues)
        {
            if (cards == null)
            {
                return;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (card == null || string.IsNullOrWhiteSpace(card.Id))
                {
                    continue;
                }

                CheckTarget(card, card.ForcedNextCardId, "card-level", cardsById, issues);

                if (card.LeftChoice != null)
                {
                    CheckTarget(card, card.LeftChoice.ForcedNextCardId, "left choice", cardsById, issues);
                }

                if (card.RightChoice != null)
                {
                    CheckTarget(card, card.RightChoice.ForcedNextCardId, "right choice", cardsById, issues);
                }

                IReadOnlyList<CardVariant> variants = card.Variants;
                for (int v = 0; v < variants.Count; v++)
                {
                    CardVariant variant = variants[v];
                    if (variant == null)
                    {
                        continue;
                    }

                    if (variant.LeftChoice != null)
                    {
                        CheckTarget(card, variant.LeftChoice.ForcedNextCardId,
                            "variant " + v + " left choice", cardsById, issues);
                    }

                    if (variant.RightChoice != null)
                    {
                        CheckTarget(card, variant.RightChoice.ForcedNextCardId,
                            "variant " + v + " right choice", cardsById, issues);
                    }
                }
            }
        }

        private static void CheckTarget(
            CardDefinition card,
            string targetId,
            string origin,
            Dictionary<string, CardDefinition> cardsById,
            List<ContentValidationIssue> issues)
        {
            if (string.IsNullOrEmpty(targetId) || cardsById.ContainsKey(targetId))
            {
                return;
            }

            issues.Add(ContentValidationIssue.Error(
                ContentIssueCode.ForcedCardTargetMissing,
                card.Id,
                string.Format(
                    "The {0} forces '{1}', which no card provides.",
                    origin,
                    targetId)));
        }

        /// <summary>
        /// Depth-first search over forced-next edges.
        /// </summary>
        /// <remarks>
        /// Forced cards bypass their own conditions, so a cycle here is genuinely unescapable: the
        /// run would alternate between the same cards until a statistic hits a boundary. That is
        /// why this is an error rather than a warning.
        /// </remarks>
        private static void ValidateForcedCycles(
            IReadOnlyList<CardDefinition> cards,
            Dictionary<string, CardDefinition> cardsById,
            List<ContentValidationIssue> issues)
        {
            if (cards == null)
            {
                return;
            }

            HashSet<string> settled = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> onPath = new HashSet<string>(StringComparer.Ordinal);
            List<string> reported = new List<string>();

            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (card == null || string.IsNullOrWhiteSpace(card.Id))
                {
                    continue;
                }

                Walk(card.Id, cardsById, settled, onPath, reported, issues);
            }
        }

        private static void Walk(
            string cardId,
            Dictionary<string, CardDefinition> cardsById,
            HashSet<string> settled,
            HashSet<string> onPath,
            List<string> reported,
            List<ContentValidationIssue> issues)
        {
            if (settled.Contains(cardId))
            {
                return;
            }

            if (!onPath.Add(cardId))
            {
                // Back edge: this card is already on the current path.
                if (!reported.Contains(cardId))
                {
                    reported.Add(cardId);
                    issues.Add(ContentValidationIssue.Error(
                        ContentIssueCode.ForcedCardCycle,
                        cardId,
                        "Forced-next cards form a cycle through this card, which the run could " +
                        "never leave."));
                }

                return;
            }

            if (cardsById.TryGetValue(cardId, out CardDefinition card))
            {
                foreach (string target in CollectForcedTargets(card))
                {
                    Walk(target, cardsById, settled, onPath, reported, issues);
                }
            }

            onPath.Remove(cardId);
            settled.Add(cardId);
        }

        /// <summary>
        /// Every card this card's forced chain could possibly continue to: its own two sides, plus
        /// every <see cref="CardVariant"/>'s two sides (a variant's null side inherits the base
        /// card's, exactly as <see cref="CardVariantResolver"/> resolves it at runtime).
        /// </summary>
        private static HashSet<string> CollectForcedTargets(CardDefinition card)
        {
            HashSet<string> targets = new HashSet<string>(StringComparer.Ordinal);

            AddIfPresent(targets, ResolveForcedTarget(card, card.LeftChoice));
            AddIfPresent(targets, ResolveForcedTarget(card, card.RightChoice));

            IReadOnlyList<CardVariant> variants = card.Variants;
            for (int v = 0; v < variants.Count; v++)
            {
                CardVariant variant = variants[v];
                if (variant == null)
                {
                    continue;
                }

                AddIfPresent(targets, ResolveForcedTarget(card, variant.LeftChoice ?? card.LeftChoice));
                AddIfPresent(targets, ResolveForcedTarget(card, variant.RightChoice ?? card.RightChoice));
            }

            return targets;
        }

        private static void AddIfPresent(HashSet<string> targets, string target)
        {
            if (!string.IsNullOrEmpty(target))
            {
                targets.Add(target);
            }
        }

        /// <summary>
        /// Mirrors ChoiceResolver: a choice-level forced ID overrides the card-level one.
        /// </summary>
        private static string ResolveForcedTarget(CardDefinition card, ChoiceDefinition choice)
        {
            if (choice != null && choice.HasForcedNextCard)
            {
                return choice.ForcedNextCardId;
            }

            return card.ForcedNextCardId;
        }

        // --- Flags ----------------------------------------------------------------

        private static void ValidateFlagReachability(
            IReadOnlyList<CardDefinition> cards,
            List<ContentValidationIssue> issues)
        {
            if (cards == null)
            {
                return;
            }

            HashSet<string> addableFlags = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (card == null)
                {
                    continue;
                }

                CollectAddedFlags(card.LeftChoice, addableFlags);
                CollectAddedFlags(card.RightChoice, addableFlags);
            }

            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (card == null || card.Conditions == null || string.IsNullOrWhiteSpace(card.Id))
                {
                    continue;
                }

                IReadOnlyList<string> required = card.Conditions.RequiredFlags;
                for (int r = 0; r < required.Count; r++)
                {
                    string flag = required[r];
                    if (string.IsNullOrEmpty(flag) || addableFlags.Contains(flag))
                    {
                        continue;
                    }

                    issues.Add(ContentValidationIssue.Warning(
                        ContentIssueCode.UnreachableRequiredFlag,
                        card.Id,
                        string.Format(
                            "Requires flag '{0}', which no choice anywhere adds, so the card can " +
                            "never be drawn.",
                            flag)));
                }
            }
        }

        private static void CollectAddedFlags(ChoiceDefinition choice, HashSet<string> addableFlags)
        {
            if (choice == null)
            {
                return;
            }

            IReadOnlyList<string> flags = choice.FlagsToAdd;
            for (int i = 0; i < flags.Count; i++)
            {
                if (!string.IsNullOrEmpty(flags[i]))
                {
                    addableFlags.Add(flags[i]);
                }
            }
        }

        private static void ValidateFlagUsage(
            IReadOnlyList<CardDefinition> cards,
            List<ContentValidationIssue> issues,
            ContentValidationOptions options)
        {
            if (cards == null)
            {
                return;
            }
            HashSet<string> produced = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> removed = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> read = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (card == null)
                {
                    continue;
                }
                CollectFlags(card.LeftChoice, produced, removed);
                CollectFlags(card.RightChoice, produced, removed);
                CollectValues(card.Conditions?.RequiredFlags, read);
                CollectValues(card.Conditions?.ForbiddenFlags, read);
            }
            foreach (string flag in removed)
            {
                if (!produced.Contains(flag))
                {
                    issues.Add(ContentValidationIssue.Warning(
                        ContentIssueCode.RemovedFlagNeverProduced,
                        flag,
                        "Flag is removed by content but no choice produces it."));
                }
            }
            foreach (string flag in read)
            {
                if (!produced.Contains(flag))
                {
                    issues.Add(ContentValidationIssue.Warning(
                        ContentIssueCode.FlagReadNeverProduced,
                        flag,
                        "Flag is read by a condition but no choice produces it."));
                }
            }
            if (!options.IncludeInformation)
            {
                return;
            }
            foreach (string flag in produced)
            {
                if (!read.Contains(flag))
                {
                    issues.Add(ContentValidationIssue.Information(
                        ContentIssueCode.FlagWrittenNeverRead,
                        flag,
                        "Flag is produced but no condition reads it."));
                }
            }
        }

        private static void CollectFlags(
            ChoiceDefinition choice,
            HashSet<string> produced,
            HashSet<string> removed)
        {
            if (choice == null)
            {
                return;
            }
            CollectValues(choice.FlagsToAdd, produced);
            CollectValues(choice.FlagsToRemove, removed);
        }

        private static void CollectValues(IReadOnlyList<string> values, HashSet<string> output)
        {
            if (values == null)
            {
                return;
            }
            for (int i = 0; i < values.Count; i++)
            {
                if (!string.IsNullOrEmpty(values[i]))
                {
                    output.Add(values[i]);
                }
            }
        }

        // --- Endings ----------------------------------------------------------------

        private static void ValidateEndings(
            IReadOnlyList<EndingDefinition> endings,
            List<ContentValidationIssue> issues)
        {
            HashSet<string> endingIds = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<int, int> coverage = new Dictionary<int, int>();

            if (endings != null)
            {
                for (int i = 0; i < endings.Count; i++)
                {
                    EndingDefinition ending = endings[i];

                    if (ending == null)
                    {
                        issues.Add(ContentValidationIssue.Error(
                            ContentIssueCode.NullEndingEntry,
                            "index " + i,
                            "Ending list contains a null entry."));
                        continue;
                    }

                    string id = ending.Id;

                    if (string.IsNullOrWhiteSpace(id))
                    {
                        issues.Add(ContentValidationIssue.Error(
                            ContentIssueCode.EmptyEndingId,
                            "index " + i,
                            "Ending has no ID."));
                    }
                    else if (!endingIds.Add(id))
                    {
                        issues.Add(ContentValidationIssue.Error(
                            ContentIssueCode.DuplicateEndingId,
                            id,
                            "Two endings share this ID."));
                    }

                    int key = CoverageKey(ending.TriggerStat, ending.Boundary);
                    coverage.TryGetValue(key, out int count);
                    coverage[key] = count + 1;
                }
            }

            for (int s = 0; s < AllStats.Length; s++)
            {
                for (int b = 0; b < AllBoundaries.Length; b++)
                {
                    StatType stat = AllStats[s];
                    StatBoundary boundary = AllBoundaries[b];
                    coverage.TryGetValue(CoverageKey(stat, boundary), out int count);

                    if (count == 0)
                    {
                        issues.Add(ContentValidationIssue.Error(
                            ContentIssueCode.MissingEndingBoundary,
                            stat + "/" + boundary,
                            "No ending covers this boundary, so a run reaching it would end with " +
                            "nothing to show."));
                    }
                    else if (count > 1)
                    {
                        issues.Add(ContentValidationIssue.Warning(
                            ContentIssueCode.DuplicateEndingBoundary,
                            stat + "/" + boundary,
                            string.Format(
                                "{0} endings cover this boundary; the highest priority then the " +
                                "ordinal-lowest ID wins.",
                                count)));
                    }
                }
            }
        }

        private static int CoverageKey(StatType stat, StatBoundary boundary)
        {
            return ((int)stat * 2) + (int)boundary;
        }

        private static void ValidateOptionalEndingArt(
            IReadOnlyList<EndingDefinition> endings,
            List<ContentValidationIssue> issues,
            ContentValidationOptions options)
        {
            if (!options.IncludeInformation || endings == null)
            {
                return;
            }
            for (int i = 0; i < endings.Count; i++)
            {
                EndingDefinition ending = endings[i];
                if (ending != null && ending.Image == null)
                {
                    issues.Add(ContentValidationIssue.Information(
                        ContentIssueCode.OptionalEndingImageMissing,
                        ending.Id,
                        "Optional ending artwork is not assigned."));
                }
            }
        }

        private static void ValidateOpeningCard(
            string openingCardId,
            Dictionary<string, CardDefinition> cardsById,
            List<ContentValidationIssue> issues)
        {
            if (string.IsNullOrEmpty(openingCardId) || cardsById.ContainsKey(openingCardId))
            {
                return;
            }

            issues.Add(ContentValidationIssue.Error(
                ContentIssueCode.OpeningCardMissing,
                openingCardId,
                "The opening card ID matches no card, so a new run would open on a normal draw."));
        }

        // --- Forced-chain-only reachability, convergence and dead ends -------------------

        /// <summary>
        /// A <see cref="CardDefinition.ForcedChainOnly"/> card is, by design, never drawn by normal
        /// selection — the only way into it is a forced-next edge from some other card (or being the
        /// opening card). One with no such edge pointing at it can never be shown at all: a
        /// hand-authored card that is quietly unreachable.
        /// </summary>
        private static void ValidateForcedChainOnlyReachability(
            IReadOnlyList<CardDefinition> cards,
            string openingCardId,
            List<ContentValidationIssue> issues)
        {
            if (cards == null)
            {
                return;
            }

            HashSet<string> referenced = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (card != null)
                {
                    referenced.UnionWith(CollectForcedTargets(card));
                }
            }

            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (card == null || string.IsNullOrWhiteSpace(card.Id) || !card.ForcedChainOnly)
                {
                    continue;
                }

                if (referenced.Contains(card.Id)
                    || string.Equals(card.Id, openingCardId, StringComparison.Ordinal))
                {
                    continue;
                }

                issues.Add(ContentValidationIssue.Warning(
                    ContentIssueCode.UnreachableForcedChainOnlyCard,
                    card.Id,
                    "Marked forced-chain-only, but no card's forced-next (base or variant) points " +
                    "at it and it is not the opening card, so normal selection will never draw it " +
                    "and no forced chain reaches it either."));
            }
        }

        /// <summary>
        /// Not necessarily wrong — a branching story frequently has several paths rejoin at one
        /// card. That risk is real only for a card normal (non-forced) selection can also draw: a
        /// <see cref="CardDefinition.ForcedChainOnly"/> card is reached exclusively by being forced,
        /// which happens at most once per run along whichever single path that run actually takes —
        /// two of its incoming edges converging is therefore the ordinary, safe shape of a branching
        /// story rejoining itself, not a risk, and is not reported. For a card selection can also
        /// draw normally, though, more than one incoming forced edge genuinely is worth a human
        /// glance: if a path that forces it can be live in the same run as normal selection drawing
        /// it independently, the second arrival would silently find the card already shown.
        /// </summary>
        private static void ValidateOncePerRunConvergence(
            IReadOnlyList<CardDefinition> cards,
            List<ContentValidationIssue> issues)
        {
            if (cards == null)
            {
                return;
            }

            Dictionary<string, HashSet<string>> incomingSources =
                new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (card == null || string.IsNullOrWhiteSpace(card.Id))
                {
                    continue;
                }

                foreach (string target in CollectForcedTargets(card))
                {
                    if (!incomingSources.TryGetValue(target, out HashSet<string> sources))
                    {
                        sources = new HashSet<string>(StringComparer.Ordinal);
                        incomingSources[target] = sources;
                    }

                    sources.Add(card.Id);
                }
            }

            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (card == null || string.IsNullOrWhiteSpace(card.Id)
                    || !card.OncePerRun || card.ForcedChainOnly)
                {
                    continue;
                }

                if (incomingSources.TryGetValue(card.Id, out HashSet<string> sources)
                    && sources.Count > 1)
                {
                    issues.Add(ContentValidationIssue.Warning(
                        ContentIssueCode.MultipleForcedChainsConvergeOnOncePerRunCard,
                        card.Id,
                        string.Format(
                            "{0} different cards force a chain into this once-per-run card ({1}). " +
                            "Confirm at most one of those paths can be live in a single run.",
                            sources.Count,
                            string.Join(", ", sources))));
                }
            }
        }

        /// <summary>
        /// A <see cref="CardDefinition.ForcedChainOnly"/> card with no forced-next on either side
        /// (base or every variant) ends the story there: once a run resolves it, no further card is
        /// forced and — being forced-chain-only — none will be drawn normally either. Reported as
        /// information, not a defect: a story's true ending is exactly this shape. It is worth
        /// surfacing anyway, since the same shape also covers "an author forgot to set forced-next".
        /// </summary>
        private static void ValidateDeadEndTerminals(
            IReadOnlyList<CardDefinition> cards,
            ContentValidationOptions options,
            List<ContentValidationIssue> issues)
        {
            if (!options.IncludeInformation || cards == null)
            {
                return;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (card == null || string.IsNullOrWhiteSpace(card.Id) || !card.ForcedChainOnly)
                {
                    continue;
                }

                if (CollectForcedTargets(card).Count == 0)
                {
                    issues.Add(ContentValidationIssue.Information(
                        ContentIssueCode.TerminalForcedChainOnlyCard,
                        card.Id,
                        "No choice, base or variant, sets a forced-next card, and this card is " +
                        "forced-chain-only: resolving either side ends the story here."));
                }
            }
        }
    }
}
