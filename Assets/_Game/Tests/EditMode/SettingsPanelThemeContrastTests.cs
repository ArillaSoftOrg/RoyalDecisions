using NUnit.Framework;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Guards the menu palette's text/fill pairings against the WCAG 4.5:1 normal-text floor.
    /// The post-apocalyptic re-theme moved the accent to a light rusty gold, which is exactly the
    /// case where the previous cream label colour stopped being legible — these assertions are what
    /// pin the label colours to dark ink rather than leaving it to eyeballing.
    /// </summary>
    public sealed class SettingsPanelThemeContrastTests
    {
        [Test]
        public void ActiveTabLabelIsLegibleOnAccentFill()
        {
            Assert.That(
                UIContrastMath.MeetsNormalText(
                    SettingsPanelTheme.ActiveTabTextColour, SettingsPanelTheme.ActiveTabColour),
                Is.True);
        }

        [Test]
        public void ApplyLabelIsLegibleOnAmberFill()
        {
            Assert.That(
                UIContrastMath.MeetsNormalText(
                    SettingsPanelTheme.ActiveTabTextColour, SettingsPanelTheme.ApplyColour),
                Is.True);
        }

        [Test]
        public void CancelLabelIsLegibleOnDangerFill()
        {
            Assert.That(
                UIContrastMath.MeetsNormalText(
                    SettingsPanelTheme.DangerTextColour, SettingsPanelTheme.DangerColour),
                Is.True);
        }

        [Test]
        public void InactiveTabLabelIsLegibleOnPanelSurface()
        {
            Assert.That(
                UIContrastMath.MeetsNormalText(
                    SettingsPanelTheme.InactiveTabTextColour, OpaquePanelGround()),
                Is.True);
        }

        [Test]
        public void CreamLabelWouldNotBeLegibleOnAccentFill()
        {
            // The reason ActiveTabTextColour is dark ink. If someone reverts it to the old cream,
            // the assertions above start failing rather than the regression shipping unnoticed.
            Color cream = new Color32(0xF2, 0xE7, 0xCF, 0xFF);
            Assert.That(
                UIContrastMath.MeetsNormalText(cream, SettingsPanelTheme.ActiveTabColour),
                Is.False);
        }

        /// <summary>
        /// The panel surface is deliberately semi-transparent, so contrast has to be measured
        /// against what actually sits behind it — the opaque screen ground.
        /// </summary>
        private static Color OpaquePanelGround()
        {
            Color surface = SettingsPanelTheme.PanelSurfaceColour;
            Color ground = SettingsPanelTheme.ScreenBackgroundColour;
            return Color.Lerp(ground, new Color(surface.r, surface.g, surface.b, 1f), surface.a);
        }
    }
}
