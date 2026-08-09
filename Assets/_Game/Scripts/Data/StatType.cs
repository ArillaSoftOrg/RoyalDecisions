namespace RoyalDecisions.Data
{
    /// <summary>
    /// The four statistics a run is scored against.
    /// </summary>
    /// <remarks>
    /// The explicit numeric values are part of the on-disk content format: Unity serialises enum
    /// fields by their integer value, so reordering or renumbering these members would silently
    /// repoint every authored condition and ending to a different stat. Append new members only.
    /// </remarks>
    public enum StatType
    {
        Authority = 0,
        People = 1,
        Security = 2,
        Wealth = 3
    }
}
