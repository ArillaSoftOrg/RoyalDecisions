namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Where a card is in its swipe lifecycle.
    /// </summary>
    /// <remarks>
    /// Input is accepted in <see cref="Idle"/> and nowhere else, so the state is the primary
    /// exactly-once guard: a confirmed swipe leaves <see cref="Idle"/> before any external handler
    /// runs.
    /// </remarks>
    public enum CardSwipeState
    {
        /// <summary>Waiting for a pointer. The only state that accepts a new drag.</summary>
        Idle = 0,

        /// <summary>Following the pointer that began the interaction.</summary>
        Dragging = 1,

        /// <summary>Returning to neutral after a release below the threshold.</summary>
        SnappingBack = 2,

        /// <summary>Leaving the screen after a confirmed decision.</summary>
        Exiting = 3,

        /// <summary>Decided and finished. Stays locked until reset for the next card.</summary>
        Completed = 4
    }
}
