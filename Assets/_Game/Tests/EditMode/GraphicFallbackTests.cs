using NUnit.Framework;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class GraphicFallbackTests
    {
        private Sprite sourceSprite;
        private Sprite fallbackSprite;

        [SetUp]
        public void SetUp()
        {
            sourceSprite = PresentationTestObjects.CreateSprite();
            fallbackSprite = PresentationTestObjects.CreateSprite();
        }

        [TearDown]
        public void TearDown()
        {
            PresentationTestObjects.DestroyAll();
        }

        [Test]
        public void ASourceSpriteAlwaysWins()
        {
            Assert.That(
                GraphicFallback.Resolve(sourceSprite, fallbackSprite, useFallbackColour: true),
                Is.EqualTo(GraphicFallbackMode.UseSource));
        }

        [Test]
        public void AFallbackSpriteIsUsedWhenContentHasNone()
        {
            Assert.That(
                GraphicFallback.Resolve(null, fallbackSprite, useFallbackColour: true),
                Is.EqualTo(GraphicFallbackMode.UseFallbackSprite));
        }

        [Test]
        public void TheFallbackColourIsUsedWhenThereIsNoSpriteAtAll()
        {
            Assert.That(
                GraphicFallback.Resolve(null, null, useFallbackColour: true),
                Is.EqualTo(GraphicFallbackMode.UseFallbackColour));
        }

        [Test]
        public void TheGraphicIsHiddenWhenNothingIsConfigured()
        {
            // The state every placeholder card is in today: no portrait, no fallback art.
            Assert.That(
                GraphicFallback.Resolve(null, null, useFallbackColour: false),
                Is.EqualTo(GraphicFallbackMode.HideGraphic));
        }

        [Test]
        public void ADestroyedSpriteBehavesAsMissing()
        {
            Sprite doomed = PresentationTestObjects.CreateSprite();
            Object.DestroyImmediate(doomed);

            Assert.That(
                GraphicFallback.Resolve(doomed, null, useFallbackColour: false),
                Is.EqualTo(GraphicFallbackMode.HideGraphic),
                "Unity's fake-null must be treated as absent, not as a live reference");
        }

        [Test]
        public void NullSettingsDoNotThrow()
        {
            Assert.That(GraphicFallback.Resolve(sourceSprite, null), Is.EqualTo(GraphicFallbackMode.UseSource));
            Assert.That(GraphicFallback.Resolve(null, null), Is.EqualTo(GraphicFallbackMode.HideGraphic));
        }

        // --- Applying to a real Image ---------------------------------------

        [Test]
        public void ApplyingUseSourceShowsTheContentSprite()
        {
            UnityEngine.UI.Image image = PresentationTestObjects.CreateImage();

            GraphicFallback.Apply(image, sourceSprite, Settings(fallbackSprite, useColour: true));

            Assert.That(image.enabled, Is.True);
            Assert.That(image.sprite, Is.SameAs(sourceSprite));
        }

        [Test]
        public void ApplyingTheColourFallbackClearsTheSpriteAndStaysVisible()
        {
            UnityEngine.UI.Image image = PresentationTestObjects.CreateImage();
            Color colour = new Color(0.1f, 0.2f, 0.3f, 1f);

            GraphicFallback.Apply(image, null, new GraphicFallbackSettings(null, colour, true));

            Assert.That(image.enabled, Is.True);
            Assert.That(image.sprite, Is.Null, "a leftover sprite would tint instead of fill");
            Assert.That(image.color, Is.EqualTo(colour));
        }

        [Test]
        public void ApplyingHideDisablesTheImage()
        {
            UnityEngine.UI.Image image = PresentationTestObjects.CreateImage();

            GraphicFallback.Apply(image, null, Settings(null, useColour: false));

            Assert.That(image.enabled, Is.False);
            Assert.That(image.sprite, Is.Null);
        }

        [Test]
        public void ApplyingToANullImageDoesNotThrow()
        {
            Assert.That(
                () => GraphicFallback.Apply(null, null, Settings(null, useColour: false)),
                Throws.Nothing,
                "an unwired optional slot must never take the game down");
        }

        private static GraphicFallbackSettings Settings(Sprite fallback, bool useColour)
        {
            return new GraphicFallbackSettings(fallback, Color.magenta, useColour);
        }
    }
}
