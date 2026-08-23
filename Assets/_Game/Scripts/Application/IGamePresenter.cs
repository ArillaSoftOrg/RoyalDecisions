using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Application
{
    /// <summary>
    /// The game screen, as the application sees it.
    /// </summary>
    /// <remarks>
    /// One interface rather than three, because the Unity side is one controller and the test side
    /// is one fake — splitting it would be ceremony (CLAUDE.md §7).
    ///
    /// Note what is absent: there is no <c>DisableInput</c>. The swipe controller locks itself the
    /// moment a decision is confirmed and stays locked until <see cref="PrepareForInput"/> arms the
    /// next card, so the session never has to ask for something it already gets.
    /// </remarks>
    public interface IGamePresenter
    {
        /// <summary>
        /// Shows <paramref name="card"/> as resolved for the current run: <paramref name="resolved"/>
        /// carries the effective speaker/body text and effective choices (a matched
        /// <see cref="RoyalDecisions.Data.CardVariant"/>'s, or the base card's if none matched),
        /// plus each side's availability. <paramref name="card"/> itself is kept only for identity
        /// and its portrait, which variants do not override.
        /// </summary>
        void ShowCard(CardDefinition card, ResolvedCard resolved);

        void ClearCard();

        /// <summary>Readies the swipe interaction for a freshly presented card.</summary>
        void PrepareForInput();

        /// <summary>Abandons any interaction in flight, without producing a decision.</summary>
        void CancelInput();

        void BindStats(StatSystem statSystem);

        void UnbindStats();

        void RefreshStats(StatValues values);

        void ShowTurn(int oneBasedTurn);

        void ShowGameOver(GameOverResult result);

        void HideGameOver();
    }
}
