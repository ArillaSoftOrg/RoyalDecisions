namespace RoyalDecisions.Domain
{
    /// <summary>
    /// The player's text-size preference. Replaces the old boolean "larger text" toggle with a
    /// three-way choice; see <see cref="AccessibilityPresentationController"/> for the scale each
    /// value maps to.
    /// </summary>
    public enum TextSizeMode
    {
        /// <summary>Matches the pre-existing default (the old toggle's "off" state).</summary>
        Normal = 0,
        Small = 1,

        /// <summary>Matches the old "larger text" toggle's "on" state exactly (1.15x scale).</summary>
        Large = 2
    }
}
