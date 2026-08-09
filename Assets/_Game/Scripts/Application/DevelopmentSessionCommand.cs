namespace RoyalDecisions.Application
{
    /// <summary>Commands exposed only by development-build GameSession methods.</summary>
    public enum DevelopmentSessionCommand
    {
        NewGame = 0,
        DeleteSave = 1,
        ChooseLeft = 2,
        ChooseRight = 3
    }
}
