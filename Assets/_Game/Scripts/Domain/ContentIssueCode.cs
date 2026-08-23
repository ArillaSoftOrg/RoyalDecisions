namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Identifies which validation rule produced an issue.
    /// </summary>
    /// <remarks>
    /// Tests assert on these rather than on message text, so a rule cannot appear to pass because
    /// some unrelated rule happened to fire.
    /// </remarks>
    public enum ContentIssueCode
    {
        None = 0,

        // --- Errors ---
        NullCardEntry = 1,
        NullEndingEntry = 2,
        EmptyCardId = 3,
        EmptyEndingId = 4,
        DuplicateCardId = 5,
        DuplicateEndingId = 6,
        MissingChoice = 7,
        ForcedCardTargetMissing = 8,
        ForcedCardCycle = 9,
        InvalidStatRange = 10,
        MissingEndingBoundary = 11,
        OpeningCardMissing = 12,
        CardsNotOrdinallySorted = 13,
        ConflictingFlags = 14,
        EmptyStatRangeIntersection = 15,

        // --- Warnings ---
        DuplicateEndingBoundary = 100,
        UnreachableRequiredFlag = 101,
        RedundantCooldown = 102,
        EmptyText = 103,
        DuplicateConditionEntry = 104,
        RemovedFlagNeverProduced = 105,
        FlagReadNeverProduced = 106,
        ExcessiveStatDelta = 107,
        ExcessiveTextLength = 108,
        ShadowedEnding = 109,
        UnreachableForcedChainOnlyCard = 110,
        MultipleForcedChainsConvergeOnOncePerRunCard = 111,

        // --- Information ---
        OptionalPortraitMissing = 200,
        OptionalEndingImageMissing = 201,
        FlagWrittenNeverRead = 202,
        TerminalForcedChainOnlyCard = 203
    }
}
