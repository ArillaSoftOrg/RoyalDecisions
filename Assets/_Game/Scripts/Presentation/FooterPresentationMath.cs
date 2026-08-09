using RoyalDecisions.Domain;

namespace RoyalDecisions.Presentation
{
    /// <summary>Formats existing run data for the decorative footer.</summary>
    public static class FooterPresentationMath
    {
        public static int ToReignYear(int turn)
        {
            return System.Math.Max(1, turn - GameConstants.FirstTurn + 1);
        }

        public static string FormatReign(int turn, string format)
        {
            string safeFormat = string.IsNullOrEmpty(format) ? "Reign Year {0}" : format;
            return string.Format(safeFormat, ToReignYear(turn));
        }
    }
}
