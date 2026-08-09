namespace RoyalDecisions.Application
{
    /// <summary>
    /// Supplies the seed for a new run.
    /// </summary>
    /// <remarks>
    /// Injected so gameplay code never reads the system clock. A test provides a fixed sequence and
    /// gets a reproducible run; the shipped implementation is the only clock read in the project.
    /// </remarks>
    public interface ISeedProvider
    {
        int NextSeed();
    }
}
