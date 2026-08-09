namespace RoyalDecisions.Data
{
    /// <summary>
    /// Which of a card's two choices a decision refers to.
    /// </summary>
    /// <remarks>
    /// Lives in the Data layer because it names a structural part of <see cref="CardDefinition"/>.
    /// Serialised by integer value wherever it is stored — append new members only.
    /// </remarks>
    public enum ChoiceSide
    {
        Left = 0,
        Right = 1
    }
}
