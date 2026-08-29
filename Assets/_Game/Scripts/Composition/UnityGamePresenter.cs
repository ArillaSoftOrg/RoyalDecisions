using RoyalDecisions.Application;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;

namespace RoyalDecisions.Composition
{
    /// <summary>
    /// Drives the Phase 5 views and the Phase 6 swipe controller on the session's behalf.
    /// </summary>
    /// <remarks>
    /// Pure translation: every method forwards to a view and calculates nothing. Each reference is
    /// optional at runtime — a scene missing its HUD should render an incomplete screen, not throw
    /// mid-decision.
    /// </remarks>
    public sealed class UnityGamePresenter : IGamePresenter
    {
        private readonly CardView cardView;
        private readonly HUDView hudView;
        private readonly GameOverView gameOverView;
        private readonly CardSwipeController swipeController;
        private readonly RunStatusView runStatusView;
        private readonly FooterView footerView;
        private readonly CardFlipController cardFlip;

        public UnityGamePresenter(
            CardView cardView,
            HUDView hudView,
            GameOverView gameOverView,
            CardSwipeController swipeController,
            RunStatusView runStatusView = null,
            FooterView footerView = null,
            CardFlipController cardFlip = null)
        {
            this.cardView = cardView;
            this.hudView = hudView;
            this.gameOverView = gameOverView;
            this.swipeController = swipeController;
            this.runStatusView = runStatusView;
            this.footerView = footerView;
            this.cardFlip = cardFlip;
        }

        public void ShowCard(CardDefinition card, ResolvedCard resolved)
        {
            hudView?.ClearChoiceImpact();

            // Armed only for a genuine card-to-card transition (see CardFlipController.Arm) —
            // the first card of a new or restarted run always appears immediately, exactly as
            // before, since there is no outgoing card to flip away from.
            if (cardFlip != null && cardFlip.IsArmed)
            {
                cardFlip.BeginTransition(card, resolved);
                return;
            }

            if (cardView != null)
            {
                cardView.Show(card, resolved);
            }

            // Set before PrepareForInput's ResetForNextCard arms the controller for this card —
            // see SetSideAvailability's remarks on ordering.
            swipeController?.SetSideAvailability(resolved.LeftAvailable, resolved.RightAvailable);
        }

        public void ClearCard()
        {
            hudView?.ClearChoiceImpact();

            if (cardFlip != null && cardFlip.IsArmed)
            {
                // Deferred: the armed transition (ShowCard below) keeps the outgoing card's
                // content on screen and fading through the flip instead of blanking it now: an
                // immediate Clear() here would wipe CardBack/Speaker/SituationText a beat before
                // the flip that is meant to carry them out. If a game-over intervenes instead of
                // another card, ShowGameOver below picks up this deferred clear.
                return;
            }

            if (cardView != null)
            {
                cardView.Clear();
            }
        }

        public void PrepareForInput()
        {
            hudView?.ClearChoiceImpact();

            if (cardFlip != null && cardFlip.IsTransitioning)
            {
                // The transition itself calls CardSwipeController.ResetForNextCard when it
                // finishes; arming input now would unlock dragging mid-flip.
                return;
            }

            if (swipeController != null)
            {
                swipeController.ResetForNextCard();
            }
        }

        public void CancelInput()
        {
            hudView?.ClearChoiceImpact();
            if (swipeController != null)
            {
                swipeController.CancelInteraction();
            }
        }

        public void BindStats(StatSystem statSystem)
        {
            if (hudView != null)
            {
                hudView.Bind(statSystem);
            }
        }

        public void UnbindStats()
        {
            hudView?.ClearChoiceImpact();
            if (hudView != null)
            {
                hudView.Unbind();
            }
        }

        public void RefreshStats(StatValues values)
        {
            if (hudView != null)
            {
                hudView.Render(values, true);
            }
        }

        public void ShowTurn(int oneBasedTurn)
        {
            if (runStatusView != null)
            {
                runStatusView.ShowTurn(oneBasedTurn);
            }

            footerView?.ShowTurn(oneBasedTurn);
        }

        public void ShowGameOver(GameOverResult result)
        {
            hudView?.ClearChoiceImpact();

            if (cardFlip != null && cardFlip.IsArmed)
            {
                // The run ended instead of continuing to another card: the transition ClearCard()
                // deferred to never begins, so disarm it and finish that deferred clear here.
                cardFlip.Disarm();
                cardView?.Clear();
            }

            if (gameOverView != null)
            {
                gameOverView.Show(result);
            }
        }

        public void HideGameOver()
        {
            hudView?.ClearChoiceImpact();
            if (gameOverView != null)
            {
                gameOverView.Hide();
            }
        }
    }
}
