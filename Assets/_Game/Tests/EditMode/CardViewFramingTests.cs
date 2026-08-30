using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Covers the card's framing and next-card-peek references. These are the decorations that are
    /// authored against optional art, so the behaviour that matters is what happens when the art is
    /// absent — the placeholder-friendly path the project actually ships in today.
    /// </summary>
    [TestFixture]
    public sealed class CardViewFramingTests
    {
        private CardView view;
        private GameUITheme theme;

        private Outline borderOutline;
        private Image frame;
        private Image portraitFrame;
        private Image bodyScrim;
        private Image[] corners;
        private Image[] temporaryBorders;
        private GameObject nextRoot;
        private Image nextSurface;
        private Image nextFrame;

        [SetUp]
        public void SetUp()
        {
            view = PresentationTestObjects.CreateComponent<CardView>("Card");

            // Outline is a mesh effect, so it needs a Graphic on the same object to attach to.
            GameObject outlineHost = PresentationTestObjects.CreateObject("CardSurface");
            outlineHost.AddComponent<Image>();
            borderOutline = outlineHost.AddComponent<Outline>();

            frame = PresentationTestObjects.CreateImage("Frame");
            portraitFrame = PresentationTestObjects.CreateImage("PortraitFrame");
            bodyScrim = PresentationTestObjects.CreateImage("BodyScrim");
            corners = new[]
            {
                PresentationTestObjects.CreateImage("CornerTopLeft"),
                PresentationTestObjects.CreateImage("CornerTopRight")
            };
            temporaryBorders = new[] { PresentationTestObjects.CreateImage("TemporaryBorder") };
            nextSurface = PresentationTestObjects.CreateImage("NextCard");
            nextFrame = PresentationTestObjects.CreateImage("NextCardFrame");
            nextRoot = nextSurface.gameObject;

            view.SetFramingAuthoringReferences(
                borderOutline, frame, portraitFrame, bodyScrim, corners, temporaryBorders,
                nextRoot, nextSurface, nextFrame);

            theme = ScriptableObject.CreateInstance<GameUITheme>();
        }

        [TearDown]
        public void TearDown()
        {
            if (theme != null)
            {
                Object.DestroyImmediate(theme);
            }

            CardTestFactory.DestroyAll();
            PresentationTestObjects.DestroyAll();
        }

        [Test]
        public void MissingDecorationArt_LeavesEveryOptionalFrameDisabled()
        {
            // A fresh theme has no frame/corner sprites, which is exactly the placeholder-art state
            // the project ships in. None of these may render as a flat gold rectangle.
            view.ApplyTheme(theme);

            Assert.That(frame.enabled, Is.False, "frameImage");
            Assert.That(portraitFrame.enabled, Is.False, "portraitFrameImage");
            Assert.That(nextFrame.enabled, Is.False, "nextCardFrame");
            for (int i = 0; i < corners.Length; i++)
            {
                Assert.That(corners[i].enabled, Is.False, "cornerImages[" + i + "]");
            }
        }

        [Test]
        public void BodyScrim_IsNeverDrawn()
        {
            bodyScrim.enabled = true;

            view.ApplyTheme(theme);

            Assert.That(bodyScrim.enabled, Is.False);
            Assert.That(bodyScrim.raycastTarget, Is.False);
        }

        [Test]
        public void TemporaryBorders_AreRetintedButKeepTheirAuthoredAlpha()
        {
            temporaryBorders[0].color = new Color(1f, 0f, 0f, 0.25f);

            view.ApplyTheme(theme);

            Color actual = temporaryBorders[0].color;
            Assert.That(actual.a, Is.EqualTo(0.25f).Within(0.001f), "alpha must survive theming");
            Assert.That(actual.r, Is.EqualTo(theme.BorderGold.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(theme.BorderGold.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(theme.BorderGold.b).Within(0.001f));
        }

        [Test]
        public void BorderOutline_IsRetintedButKeepsItsAuthoredAlpha()
        {
            borderOutline.effectColor = new Color(1f, 0f, 0f, 0.25f);

            view.ApplyTheme(theme);

            Assert.That(borderOutline.effectColor.a, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(borderOutline.effectColor.r, Is.EqualTo(theme.BorderGold.r).Within(0.001f));
        }

        [Test]
        public void NextCardSurface_FallsBackToTheCardSurfaceColourWithoutArt()
        {
            view.ApplyTheme(theme);

            Assert.That(nextSurface.enabled, Is.True);
            Assert.That(nextSurface.color, Is.EqualTo(theme.CardSurface));
        }

        [Test]
        public void NextCardPeek_IsHiddenWithTheCard()
        {
            // The peek is a sibling of the card root, so Clear() would otherwise leave an empty
            // card surface floating behind the game-over panel.
            CardDefinition card = CardTestFactory.Card();

            view.Show(card);
            Assert.That(nextRoot.activeSelf, Is.True, "peek should return with a card");

            view.Clear();
            Assert.That(nextRoot.activeSelf, Is.False, "peek should go with the card");
        }

        [Test]
        public void ApplyTheme_WithNoFramingReferences_DoesNotThrow()
        {
            CardView bare = PresentationTestObjects.CreateComponent<CardView>("BareCard");

            Assert.DoesNotThrow(() => bare.ApplyTheme(theme));
        }
    }
}
