using NUnit.Framework;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    public sealed class UIContrastMathTests
    {
        [Test]
        public void BlackAndWhiteHaveMaximumContrast()
        {
            Assert.That(UIContrastMath.ContrastRatio(Color.black, Color.white), Is.EqualTo(21f).Within(0.01f));
        }

        [Test]
        public void DefaultNormalTextPairsMeetTarget()
        {
            GameUITheme theme = ScriptableObject.CreateInstance<GameUITheme>();
            try
            {
                Assert.That(UIContrastMath.MeetsNormalText(theme.PrimaryText, theme.CardSurface), Is.True);
                Assert.That(UIContrastMath.MeetsNormalText(theme.SecondaryText, theme.UISurface), Is.True);
                Assert.That(UIContrastMath.MeetsNormalText(theme.HighlightGold, theme.CardSurface), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(theme);
            }
        }
    }
}
