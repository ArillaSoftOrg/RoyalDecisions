using System.Collections;
using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.PlayMode
{
    /// <summary>
    /// Runs the whole presentation layer under a real Canvas for several frames.
    /// </summary>
    /// <remarks>
    /// The EditMode tests prove each view's logic. This proves they survive an actual UI rebuild —
    /// layout, CanvasRenderer, TMP mesh generation — which no EditMode assertion touches.
    /// </remarks>
    [TestFixture]
    public class PresentationSmokePlayModeTests
    {
        private GameObject canvasObject;
        private CardDefinition card;

        [TearDown]
        public void TearDown()
        {
            if (canvasObject != null)
            {
                Object.Destroy(canvasObject);
                canvasObject = null;
            }

            if (card != null)
            {
                Object.Destroy(card);
                card = null;
            }
        }

        private T CreateChild<T>(string name) where T : Component
        {
            GameObject child = new GameObject(name);
            child.AddComponent<RectTransform>();
            child.transform.SetParent(canvasObject.transform, false);
            return child.AddComponent<T>();
        }

        private void CreateCanvas()
        {
            canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
        }

        [UnityTest]
        public IEnumerator TheWholeLayerRendersWithNoContentAndNoArt()
        {
            CreateCanvas();

            // A card with nothing authored: no speaker, no body, no portrait. Exactly the shape of
            // the placeholder content, and the harshest case the fallbacks have to survive.
            card = ScriptableObject.CreateInstance<CardDefinition>();

            CardView cardView = CreateChild<CardView>("Card");
            HUDView hud = CreateChild<HUDView>("HUD");
            GameOverView gameOver = CreateChild<GameOverView>("GameOver");
            SafeAreaFitter safeArea = CreateChild<SafeAreaFitter>("SafeArea");

            yield return null;

            Assert.That(() => cardView.Show(card), Throws.Nothing);
            Assert.That(() => hud.Render(StatValues.CreateInitial()), Throws.Nothing);
            Assert.That(() => gameOver.Show(GameOverResult.NotOver()), Throws.Nothing);
            Assert.That(() => safeArea.ApplyIfChanged(), Throws.Nothing);

            yield return null;
            yield return null;

            Assert.That(cardView.HasCard, Is.True);
            Assert.That(gameOver.IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator AWiredCardRendersTextThroughARealCanvas()
        {
            CreateCanvas();

            TextMeshProUGUI speaker = CreateChild<TextMeshProUGUI>("Speaker");
            TextMeshProUGUI body = CreateChild<TextMeshProUGUI>("Body");
            Image portrait = CreateChild<Image>("Portrait");

            ChoicePreviewView left = CreateChild<ChoicePreviewView>("Left");
            ChoicePreviewView right = CreateChild<ChoicePreviewView>("Right");

            CardView cardView = CreateChild<CardView>("Card");
            cardView.SetAuthoringReferences(speaker, body, portrait, left, right);

            card = ScriptableObject.CreateInstance<CardDefinition>();

            yield return null;
            cardView.Show(card);
            yield return null;

            Assert.That(portrait.enabled, Is.True, "the colour fallback keeps the slot visible");
            Assert.That(portrait.sprite, Is.Null);
            Assert.That(cardView.PortraitMode, Is.EqualTo(GraphicFallbackMode.UseFallbackColour));
        }

        [UnityTest]
        public IEnumerator AnimatedStatFillReachesItsTargetOverFrames()
        {
            CreateCanvas();

            Image fill = CreateChild<Image>("Fill");
            fill.type = Image.Type.Filled;

            StatItemView item = CreateChild<StatItemView>("Stat");
            item.SetAuthoringReferences(StatType.Authority, fill, speed: 100f);

            yield return null;

            item.SetFill(0f);
            item.SetFillAnimated(1f);
            Assert.That(item.IsAnimating, Is.True);

            // Generous frame budget: this asserts that it arrives, not how fast.
            for (int frame = 0; frame < 120 && item.IsAnimating; frame++)
            {
                yield return null;
            }

            Assert.That(item.IsAnimating, Is.False, "the bar must settle rather than drift");
            Assert.That(item.DisplayedFill, Is.EqualTo(1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator AnIdleStatItemStaysIdle()
        {
            CreateCanvas();

            Image fill = CreateChild<Image>("Fill");
            fill.type = Image.Type.Filled;

            StatItemView item = CreateChild<StatItemView>("Stat");
            item.SetAuthoringReferences(StatType.People, fill);

            yield return null;
            item.SetFill(0.5f);

            yield return null;
            yield return null;

            Assert.That(item.IsAnimating, Is.False, "no work, and no allocation, while at rest");
            Assert.That(item.DisplayedFill, Is.EqualTo(0.5f).Within(0.0001f));
        }
    }
}
