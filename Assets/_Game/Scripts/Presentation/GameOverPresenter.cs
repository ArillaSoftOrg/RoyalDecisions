using RoyalDecisions.Domain;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Turns a game-over result into the values the ending screen renders.
    /// </summary>
    /// <remarks>
    /// Phase 2 deliberately allows <c>IsGameOver</c> with a null <c>Ending</c> — a statistic hit a
    /// boundary that no ending asset covers. Ending the run with a blank screen would be worse than
    /// either alternative, so that case resolves to configured generic wording and says so.
    /// </remarks>
    public static class GameOverPresenter
    {
        public static GameOverPresentation Create(
            GameOverResult result,
            string genericTitle,
            string genericBody)
        {
            if (!result.IsGameOver)
            {
                return GameOverPresentation.None;
            }

            if (!result.HasEnding)
            {
                return new GameOverPresentation(genericTitle, genericBody, null, true);
            }

            return new GameOverPresentation(
                result.Ending.Title,
                result.Ending.BodyText,
                result.Ending.Image,
                false);
        }
    }
}
