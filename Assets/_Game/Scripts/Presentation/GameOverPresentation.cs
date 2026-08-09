using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Everything the ending screen needs, already resolved.
    /// </summary>
    /// <remarks>
    /// The string getters coalesce rather than relying on the constructor: <see cref="None"/> is
    /// <c>default</c>, which never runs one. A view must never receive a null string.
    /// </remarks>
    public readonly struct GameOverPresentation
    {
        private readonly string title;
        private readonly string bodyText;

        public GameOverPresentation(
            string title,
            string bodyText,
            Sprite illustration,
            bool isGenericFallback)
        {
            this.title = title;
            this.bodyText = bodyText;
            Illustration = illustration;
            IsGenericFallback = isGenericFallback;
            HasEnding = true;
        }

        /// <summary>The run has not ended; nothing to show.</summary>
        public static GameOverPresentation None => default;

        public string Title => title ?? string.Empty;

        public string BodyText => bodyText ?? string.Empty;

        public Sprite Illustration { get; }

        /// <summary>
        /// True when the run ended but no ending asset covered the boundary, so the text shown is
        /// the configured generic wording rather than authored content.
        /// </summary>
        public bool IsGenericFallback { get; }

        public bool HasEnding { get; }
    }
}
