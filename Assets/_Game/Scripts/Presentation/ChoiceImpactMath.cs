using System;

namespace RoyalDecisions.Presentation
{
    /// <summary>Maps authored deltas to a presentation-only sign and magnitude.</summary>
    public static class ChoiceImpactMath
    {
        public static int MagnitudeLevel(int delta)
        {
            long magnitude = Math.Abs((long)delta);
            if (magnitude == 0)
            {
                return 0;
            }

            if (magnitude <= 5)
            {
                return 1;
            }

            return magnitude <= 12 ? 2 : 3;
        }

        public static string Format(int delta, string positiveGlyph, string negativeGlyph)
        {
            int level = MagnitudeLevel(delta);
            if (level == 0)
            {
                return string.Empty;
            }

            string glyph = delta > 0 ? positiveGlyph : negativeGlyph;
            if (string.IsNullOrEmpty(glyph))
            {
                glyph = delta > 0 ? "+" : "-";
            }

            return level switch
            {
                1 => glyph,
                2 => glyph + glyph,
                _ => glyph + glyph + glyph
            };
        }
    }
}
