using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Everything a card view needs, already resolved from content.
    /// </summary>
    /// <remarks>
    /// The string getters coalesce rather than relying on the constructor, because
    /// <c>default(CardPresentation)</c> — which <see cref="Empty"/> is, and which any uninitialised
    /// field would be — never runs a constructor at all. A view must never receive a null string.
    /// </remarks>
    public readonly struct CardPresentation
    {
        private readonly string speaker;
        private readonly string bodyText;
        private readonly string leftPreviewText;
        private readonly string rightPreviewText;

        public CardPresentation(
            string speaker,
            string bodyText,
            string leftPreviewText,
            string rightPreviewText,
            Sprite portrait)
        {
            this.speaker = speaker;
            this.bodyText = bodyText;
            this.leftPreviewText = leftPreviewText;
            this.rightPreviewText = rightPreviewText;
            Portrait = portrait;
            HasCard = true;
        }

        /// <summary>What a null or unusable card resolves to: blank, and flagged as empty.</summary>
        public static CardPresentation Empty => default;

        public string Speaker => speaker ?? string.Empty;

        public string BodyText => bodyText ?? string.Empty;

        public string LeftPreviewText => leftPreviewText ?? string.Empty;

        public string RightPreviewText => rightPreviewText ?? string.Empty;

        public Sprite Portrait { get; }

        public bool HasCard { get; }
    }
}
