namespace RoyalDecisions.Application
{
    /// <summary>
    /// Why a session command failed, as a value a test can assert on rather than a log line.
    /// </summary>
    public enum SessionErrorCode
    {
        None = 0,

        /// <summary>No content catalogue was supplied.</summary>
        MissingCatalogue = 1,

        /// <summary>The catalogue holds no cards.</summary>
        EmptyCatalogue = 2,

        /// <summary>The catalogue names no opening card, or names one that does not exist.</summary>
        InvalidOpeningCard = 3,

        /// <summary>No card is currently drawable. Terminal: nothing can change without a decision.</summary>
        NoEligibleCard = 4,

        /// <summary>A boundary was reached that no ending covers. Diagnostic, not fatal.</summary>
        MissingEnding = 5,

        /// <summary>Writing the save failed. Recoverable.</summary>
        SaveFailed = 6,

        /// <summary>The save could not be read.</summary>
        LoadFailed = 7,

        /// <summary>The save was written by a newer build.</summary>
        UnsupportedSave = 8,

        /// <summary>The save is unreadable. The file is left untouched.</summary>
        CorruptSave = 9,

        /// <summary>A required view or controller reference is missing.</summary>
        MissingPresenter = 10,

        /// <summary>The command is not valid in the current state, and was ignored.</summary>
        InvalidStateForCommand = 11,

        /// <summary>The domain refused the decision — usually its own duplicate guard.</summary>
        DecisionRejected = 12,

        /// <summary>
        /// The chosen side is not currently available (see
        /// <see cref="RoyalDecisions.Data.ChoiceDefinition.Availability"/>).
        /// </summary>
        ChoiceUnavailable = 13
    }
}
