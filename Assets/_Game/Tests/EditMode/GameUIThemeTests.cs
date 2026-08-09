using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    public sealed class GameUIThemeTests
    {
        private GameUITheme theme;

        [SetUp]
        public void SetUp()
        {
            theme = ScriptableObject.CreateInstance<GameUITheme>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void DefaultPaletteMatchesNeutralFoundation()
        {
            Assert.That((Color32)theme.OverallBackground, Is.EqualTo(new Color32(0x07, 0x11, 0x1B, 0xFF)));
            Assert.That((Color32)theme.UISurface, Is.EqualTo(new Color32(0x12, 0x16, 0x20, 0xFF)));
            Assert.That((Color32)theme.CardSurface, Is.EqualTo(new Color32(0x21, 0x17, 0x1A, 0xFF)));
            Assert.That((Color32)theme.EmptyBar, Is.EqualTo(new Color32(0x2A, 0x2F, 0x3A, 0xFF)));
        }

        [TestCase(StatType.People, 0x8A, 0x41, 0x4B, "People", "P")]
        [TestCase(StatType.Security, 0x68, 0x70, 0x3D, "Security", "S")]
        [TestCase(StatType.Authority, 0x3E, 0x56, 0x7D, "Authority", "A")]
        [TestCase(StatType.Wealth, 0xB3, 0x8A, 0x3D, "Wealth", "W")]
        public void StatIdentityIsStable(
            StatType stat, byte red, byte green, byte blue, string name, string symbol)
        {
            Assert.That((Color32)theme.GetStatColor(stat), Is.EqualTo(new Color32(red, green, blue, 0xFF)));
            Assert.That(theme.GetStatName(stat), Is.EqualTo(name));
            Assert.That(theme.GetStatFallbackSymbol(stat), Is.EqualTo(symbol));
            Assert.That(theme.GetStatIcon(stat), Is.Null);
        }
    }
}
