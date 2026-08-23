using System;
using System.Collections.Generic;
using RoyalDecisions.Application;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Records what the session asked the screen to do, in order.
    /// </summary>
    /// <remarks>
    /// <see cref="Calls"/> is the point: ordering assertions like "saved before the next card"
    /// become index comparisons rather than timing races.
    /// </remarks>
    public sealed class FakeGamePresenter : IGamePresenter
    {
        public List<string> Calls { get; } = new List<string>();

        public List<CardDefinition> ShownCards { get; } = new List<CardDefinition>();

        public CardDefinition LastShownCard { get; private set; }

        public ResolvedCard LastResolvedCard { get; private set; }

        public StatValues LastStats { get; private set; }

        public GameOverResult LastGameOver { get; private set; }

        public int PrepareForInputCount { get; private set; }

        public int ShowGameOverCount { get; private set; }

        public int BindCount { get; private set; }

        public int UnbindCount { get; private set; }

        public bool IsBound { get; private set; }

        public int LastTurn { get; private set; }

        public void ShowCard(CardDefinition card, ResolvedCard resolved)
        {
            LastShownCard = card;
            LastResolvedCard = resolved;
            ShownCards.Add(card);
            Calls.Add("ShowCard:" + (card != null ? card.Id : "<null>"));
        }

        public void ClearCard()
        {
            Calls.Add("ClearCard");
        }

        public void PrepareForInput()
        {
            PrepareForInputCount++;
            Calls.Add("PrepareForInput");
        }

        public void CancelInput()
        {
            Calls.Add("CancelInput");
        }

        public void BindStats(StatSystem statSystem)
        {
            BindCount++;
            IsBound = true;
            Calls.Add("BindStats");
        }

        public void UnbindStats()
        {
            UnbindCount++;
            IsBound = false;
            Calls.Add("UnbindStats");
        }

        public void RefreshStats(StatValues values)
        {
            LastStats = values;
            Calls.Add("RefreshStats");
        }

        public void ShowTurn(int oneBasedTurn)
        {
            LastTurn = oneBasedTurn;
            Calls.Add("ShowTurn:" + oneBasedTurn);
        }

        public void ShowGameOver(GameOverResult result)
        {
            LastGameOver = result;
            ShowGameOverCount++;
            Calls.Add("ShowGameOver");
        }

        public void HideGameOver()
        {
            Calls.Add("HideGameOver");
        }

        /// <summary>Index of the first call starting with <paramref name="call"/>, or -1.</summary>
        public int IndexOf(string call)
        {
            return Calls.FindIndex(entry => entry.StartsWith(call, StringComparison.Ordinal));
        }

        public int CountOf(string call)
        {
            return Calls.FindAll(entry => entry.StartsWith(call, StringComparison.Ordinal)).Count;
        }
    }
}
