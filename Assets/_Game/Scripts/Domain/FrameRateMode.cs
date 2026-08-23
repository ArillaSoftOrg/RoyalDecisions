namespace RoyalDecisions.Domain
{
    /// <summary>
    /// The player's frame-rate preference.
    /// </summary>
    /// <remarks>
    /// Each member's underlying value is the actual FPS number, so
    /// <see cref="RoyalDecisions.Composition.SettingsController"/> can apply the choice straight to
    /// <c>Application.targetFrameRate</c> with a cast instead of a lookup table. It also means a
    /// settings file written under the old three-way (Sixty=0/Thirty=1/Auto=2) numbering has none
    /// of its values land on a defined member any more, so <see cref="GameSettings.SanitizeAfterLoad"/>
    /// safely resets it to <see cref="Sixty"/> instead of silently reinterpreting the old number.
    /// </remarks>
    public enum FrameRateMode
    {
        Thirty = 30,
        Sixty = 60,
        Ninety = 90,
        OneTwenty = 120
    }
}
