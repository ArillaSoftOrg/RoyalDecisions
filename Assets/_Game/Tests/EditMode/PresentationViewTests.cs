using System;
using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Exercises the views as real components: build a GameObject, call Render, assert the state
    /// that actually reaches the screen.
    /// </summary>
    [TestFixture]
    public class PresentationViewTests
    {
        [TearDown]
        public void TearDown()
        {
            CardTestFactory.DestroyAll();
            PresentationTestObjects.DestroyAll();
        }

        // --- CardView -------------------------------------------------------

        private static CardView BuildCardView(
            out TextMeshProUGUI speaker,
            out TextMeshProUGUI body,
            out Image portrait,
            out ChoicePreviewView left,
            out ChoicePreviewView right,
            GraphicFallbackSettings fallback = null)
        {
            speaker = PresentationTestObjects.CreateText("Speaker");
            body = PresentationTestObjects.CreateText("Body");
            portrait = PresentationTestObjects.CreateImage("Portrait");

            left = PresentationTestObjects.CreateComponent<ChoicePreviewView>("PreviewLeft");
            left.SetAuthoringReferences(
                ChoiceSide.Left,
                PresentationTestObjects.CreateText("LeftLabel"),
                PresentationTestObjects.CreateCanvasGroup("LeftGroup"));

            right = PresentationTestObjects.CreateComponent<ChoicePreviewView>("PreviewRight");
            right.SetAuthoringReferences(
                ChoiceSide.Right,
                PresentationTestObjects.CreateText("RightLabel"),
                PresentationTestObjects.CreateCanvasGroup("RightGroup"));

            CardView view = PresentationTestObjects.CreateComponent<CardView>("CardView");
            view.SetAuthoringReferences(speaker, body, portrait, left, right, fallback);
            return view;
        }

        [Test]
        public void CardView_RendersEveryTextField()
        {
            CardView view = BuildCardView(
                out TextMeshProUGUI speaker, out TextMeshProUGUI body,
                out _, out ChoicePreviewView left, out ChoicePreviewView right);

            view.Show(CardTestFactory.Card(
                id: "card_a",
                speaker: "The Marshal",
                bodyText: "Riders report movement.",
                left: CardTestFactory.Choice("Reinforce"),
                right: CardTestFactory.Choice("Wait")));

            Assert.That(speaker.text, Is.EqualTo("The Marshal"));
            Assert.That(body.text, Is.EqualTo("Riders report movement."));
            Assert.That(left.Text, Is.EqualTo("Reinforce"));
            Assert.That(right.Text, Is.EqualTo("Wait"));
            Assert.That(view.HasCard, Is.True);
        }

        [Test]
        public void CardView_WithNoArtAtAllDisablesThePortrait()
        {
            // The state every generated placeholder card is in: no portrait, no fallback art.
            CardView view = BuildCardView(
                out _, out _, out Image portrait, out _, out _,
                new GraphicFallbackSettings(null, Color.magenta, useFallbackColour: false));

            Assert.That(() => view.Show(CardTestFactory.Card(id: "card_a")), Throws.Nothing);

            Assert.That(portrait.enabled, Is.False);
            Assert.That(view.PortraitMode, Is.EqualTo(GraphicFallbackMode.HideGraphic));
        }

        [Test]
        public void CardView_WithNoArtShowsTheProceduralPortraitFallback()
        {
            CardView view = BuildCardView(
                out TextMeshProUGUI speaker, out TextMeshProUGUI body,
                out Image portrait, out ChoicePreviewView left, out ChoicePreviewView right,
                new GraphicFallbackSettings(null, Color.magenta, useFallbackColour: false));
            PortraitFallbackView fallback =
                PresentationTestObjects.CreateComponent<PortraitFallbackView>("Fallback");
            fallback.SetAuthoringReferences(
                fallback.gameObject,
                PresentationTestObjects.CreateImage("Backdrop"),
                PresentationTestObjects.CreateImage("Head"),
                PresentationTestObjects.CreateImage("Shoulders"),
                PresentationTestObjects.CreateImage("Torso"));
            view.SetAuthoringReferences(
                speaker, body, portrait, left, right,
                new GraphicFallbackSettings(null, Color.magenta, useFallbackColour: false),
                generatedPortraitFallback: fallback);

            view.Show(CardTestFactory.Card(id: "card_a"));

            Assert.That(fallback.IsVisible, Is.True);
            Assert.That(portrait.enabled, Is.False);
        }

        [Test]
        public void CardView_UsesTheFallbackColourWhenConfigured()
        {
            Color colour = new Color(0.3f, 0.1f, 0.4f, 1f);
            CardView view = BuildCardView(
                out _, out _, out Image portrait, out _, out _,
                new GraphicFallbackSettings(null, colour, useFallbackColour: true));

            view.Show(CardTestFactory.Card(id: "card_a"));

            Assert.That(portrait.enabled, Is.True);
            Assert.That(portrait.sprite, Is.Null);
            Assert.That(portrait.color, Is.EqualTo(colour));
            Assert.That(view.PortraitMode, Is.EqualTo(GraphicFallbackMode.UseFallbackColour));
        }

        [Test]
        public void CardView_UsesAFallbackSpriteInPreferenceToColour()
        {
            Sprite fallback = PresentationTestObjects.CreateSprite();
            CardView view = BuildCardView(
                out _, out _, out Image portrait, out _, out _,
                new GraphicFallbackSettings(fallback, Color.magenta, useFallbackColour: true));

            view.Show(CardTestFactory.Card(id: "card_a"));

            Assert.That(portrait.sprite, Is.SameAs(fallback));
            Assert.That(view.PortraitMode, Is.EqualTo(GraphicFallbackMode.UseFallbackSprite));
        }

        [Test]
        public void CardView_ShowingNullClearsInsteadOfThrowing()
        {
            CardView view = BuildCardView(
                out TextMeshProUGUI speaker, out _, out _, out _, out _);

            view.Show(CardTestFactory.Card(id: "card_a", speaker: "Someone"));
            Assert.That(() => view.Show(null), Throws.Nothing);

            Assert.That(speaker.text, Is.Empty);
            Assert.That(view.HasCard, Is.False);
        }

        [Test]
        public void CardView_ClearBlanksEverythingAndDropsTheCard()
        {
            CardView view = BuildCardView(
                out TextMeshProUGUI speaker, out TextMeshProUGUI body,
                out _, out ChoicePreviewView left, out _);

            view.Show(CardTestFactory.Card(
                id: "card_a", speaker: "A", bodyText: "B",
                left: CardTestFactory.Choice("L")));
            view.SetChoicePreview(ChoiceSide.Left, 1f);

            view.Clear();

            Assert.That(speaker.text, Is.Empty);
            Assert.That(body.text, Is.Empty);
            Assert.That(left.Text, Is.Empty);
            Assert.That(left.Strength, Is.EqualTo(0f));
            Assert.That(view.HasCard, Is.False);
        }

        [Test]
        public void CardView_UpdateReplacesEveryFieldWithNoResidue()
        {
            CardView view = BuildCardView(
                out TextMeshProUGUI speaker, out TextMeshProUGUI body,
                out _, out ChoicePreviewView left, out ChoicePreviewView right);

            view.Show(CardTestFactory.Card(
                id: "card_a", speaker: "First", bodyText: "First body",
                left: CardTestFactory.Choice("FirstLeft"),
                right: CardTestFactory.Choice("FirstRight")));

            view.UpdateCard(CardTestFactory.Card(
                id: "card_b", speaker: "Second", bodyText: "Second body",
                left: CardTestFactory.Choice("SecondLeft"),
                right: CardTestFactory.Choice("SecondRight")));

            Assert.That(speaker.text, Is.EqualTo("Second"));
            Assert.That(body.text, Is.EqualTo("Second body"));
            Assert.That(left.Text, Is.EqualTo("SecondLeft"));
            Assert.That(right.Text, Is.EqualTo("SecondRight"));
        }

        [TestCase(-5f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(0.4f, 0.4f)]
        [TestCase(1f, 1f)]
        [TestCase(5f, 1f)]
        public void CardView_PreviewStrengthIsClamped(float input, float expected)
        {
            CardView view = BuildCardView(out _, out _, out _, out ChoicePreviewView left, out _);

            view.SetChoicePreview(ChoiceSide.Left, input);

            Assert.That(left.Strength, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void CardView_PreviewSidesAreIndependent()
        {
            CardView view = BuildCardView(
                out _, out _, out _, out ChoicePreviewView left, out ChoicePreviewView right);

            view.SetChoicePreview(ChoiceSide.Right, 0.8f);

            Assert.That(right.Strength, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(left.Strength, Is.EqualTo(0f), "the other side must not move");
        }

        [Test]
        public void CardView_SetChoicePreviewsDrivesBothSidesAtOnce()
        {
            CardView view = BuildCardView(
                out _, out _, out _, out ChoicePreviewView left, out ChoicePreviewView right);

            view.SetChoicePreviews(0.25f, 0.75f);

            Assert.That(view.GetChoicePreviewStrength(ChoiceSide.Left), Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(view.GetChoicePreviewStrength(ChoiceSide.Right), Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(left.Strength, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(right.Strength, Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void CardView_WithUnwiredReferencesDoesNotThrow()
        {
            CardView bare = PresentationTestObjects.CreateComponent<CardView>("BareCard");

            Assert.That(() => bare.Show(CardTestFactory.Card(id: "card_a")), Throws.Nothing);
            Assert.That(() => bare.SetChoicePreviews(1f, 1f), Throws.Nothing);
            Assert.That(() => bare.Clear(), Throws.Nothing);
        }

        // --- StatItemView -----------------------------------------------------

        private static StatItemView BuildStatItem(StatType stat, out Image fill)
        {
            fill = PresentationTestObjects.CreateImage("Fill_" + stat);
            fill.type = Image.Type.Filled;

            StatItemView item = PresentationTestObjects.CreateComponent<StatItemView>("Stat_" + stat);
            item.SetAuthoringReferences(stat, fill);
            return item;
        }

        [TestCase(-1f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(1f, 1f)]
        [TestCase(2f, 1f)]
        public void StatItem_FillIsClamped(float input, float expected)
        {
            StatItemView item = BuildStatItem(StatType.Authority, out Image fill);

            item.SetFill(input);

            Assert.That(fill.fillAmount, Is.EqualTo(expected).Within(0.0001f));
            Assert.That(item.DisplayedFill, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void StatItem_SetFillCancelsAnimation()
        {
            StatItemView item = BuildStatItem(StatType.People, out _);

            item.SetFillAnimated(1f);
            item.SetFill(0.2f);

            Assert.That(item.IsAnimating, Is.False);
            Assert.That(item.DisplayedFill, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void StatItem_WithNoFillImageDoesNotThrow()
        {
            StatItemView bare = PresentationTestObjects.CreateComponent<StatItemView>("BareStat");

            Assert.That(() => bare.SetFill(0.5f), Throws.Nothing);
            Assert.That(bare.DisplayedFill, Is.EqualTo(0f));
        }

        // --- HUDView -----------------------------------------------------------

        private static HUDView BuildHud(out StatItemView[] items)
        {
            items = new[]
            {
                BuildStatItem(StatType.Authority, out _),
                BuildStatItem(StatType.People, out _),
                BuildStatItem(StatType.Security, out _),
                BuildStatItem(StatType.Wealth, out _)
            };

            HUDView hud = PresentationTestObjects.CreateComponent<HUDView>("HUD");
            hud.SetAuthoringReferences(items);
            return hud;
        }

        [Test]
        public void Hud_RendersAllFourStats()
        {
            HUDView hud = BuildHud(out StatItemView[] items);

            hud.Render(new StatValues(0, 25, 75, 100));

            Assert.That(items[0].DisplayedFill, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(items[1].DisplayedFill, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(items[2].DisplayedFill, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(items[3].DisplayedFill, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Hud_RenderDoesNotMutateTheValuesItIsGiven()
        {
            HUDView hud = BuildHud(out _);
            StatValues values = new StatValues(10, 20, 30, 40);

            hud.Render(values);

            Assert.That(values.Authority, Is.EqualTo(10));
            Assert.That(values.Wealth, Is.EqualTo(40));
        }

        [Test]
        public void Hud_ApplyMovesOnlyTheChangedStat()
        {
            HUDView hud = BuildHud(out StatItemView[] items);
            hud.Render(StatValues.CreateInitial());

            hud.Apply(new StatChange(StatType.Security, 50, 90));

            Assert.That(items[2].TargetFill, Is.EqualTo(0.9f).Within(0.0001f));
            Assert.That(items[0].TargetFill, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(items[3].TargetFill, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void Hud_BindDrawsCurrentValuesImmediately()
        {
            HUDView hud = BuildHud(out StatItemView[] items);
            RunState run = RunState.CreateNew(1);
            run.SetStats(new StatValues(100, 0, 50, 50));
            StatSystem stats = new StatSystem(run);

            hud.Bind(stats);

            Assert.That(hud.IsBound, Is.True);
            Assert.That(items[0].DisplayedFill, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(items[1].DisplayedFill, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Hud_UnbindStopsListening()
        {
            HUDView hud = BuildHud(out StatItemView[] items);
            RunState run = RunState.CreateNew(1);
            StatSystem stats = new StatSystem(run);

            hud.Bind(stats);
            hud.Unbind();

            stats.Apply(new StatDeltas(50, 0, 0, 0));

            Assert.That(hud.IsBound, Is.False);
            Assert.That(items[0].TargetFill, Is.EqualTo(0.5f).Within(0.0001f),
                "an unbound HUD must not react to a later change");
        }

        [Test]
        public void Hud_BindingTwiceDoesNotDoubleSubscribe()
        {
            HUDView hud = BuildHud(out _);
            RunState run = RunState.CreateNew(1);
            StatSystem stats = new StatSystem(run);

            hud.Bind(stats);
            hud.Bind(stats);
            hud.Unbind();

            stats.Apply(new StatDeltas(10, 0, 0, 0));

            Assert.That(hud.IsBound, Is.False,
                "a single Unbind must release everything a second Bind added");
        }

        [Test]
        public void Hud_ReactsToStatChanges()
        {
            HUDView hud = BuildHud(out StatItemView[] items);
            RunState run = RunState.CreateNew(1);
            StatSystem stats = new StatSystem(run);
            hud.Bind(stats);

            stats.Apply(new StatDeltas(0, 0, 0, 25));

            Assert.That(items[3].TargetFill, Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void Hud_ValidatesItsStatItems()
        {
            HUDView hud = BuildHud(out _);
            Assert.That(hud.TryValidate(out string clean), Is.True, clean);

            HUDView missing = PresentationTestObjects.CreateComponent<HUDView>("MissingHud");
            missing.SetAuthoringReferences(new[] { BuildStatItem(StatType.Authority, out _) });

            Assert.That(missing.TryValidate(out string message), Is.False);
            Assert.That(message, Does.Contain("People"));
        }

        [Test]
        public void Hud_ReportsDuplicateAndNullStatItems()
        {
            HUDView hud = PresentationTestObjects.CreateComponent<HUDView>("DupeHud");
            hud.SetAuthoringReferences(new[]
            {
                BuildStatItem(StatType.Authority, out _),
                BuildStatItem(StatType.Authority, out _),
                null
            });

            Assert.That(hud.TryValidate(out string message), Is.False);
            Assert.That(message, Does.Contain("2 items render Authority"));
            Assert.That(message, Does.Contain("slot 2 is empty"));
        }

        // --- GameOverView ---------------------------------------------------------

        private static GameOverView BuildGameOverView(
            out TextMeshProUGUI title,
            out TextMeshProUGUI body,
            out Image illustration,
            GraphicFallbackSettings fallback = null)
        {
            title = PresentationTestObjects.CreateText("Title");
            body = PresentationTestObjects.CreateText("Body");
            illustration = PresentationTestObjects.CreateImage("Illustration");

            GameOverView view = PresentationTestObjects.CreateComponent<GameOverView>("GameOver");
            view.SetAuthoringReferences(
                title, body, illustration, null, fallback, "Generic Title", "Generic body.");
            return view;
        }

        [Test]
        public void GameOver_ShowsAnEnding()
        {
            GameOverView view = BuildGameOverView(
                out TextMeshProUGUI title, out TextMeshProUGUI body, out _);

            EndingDefinition ending = CardTestFactory.Ending(
                id: "ending_x", title: "The Gates Opened", bodyText: "And no one closed them.");

            view.Show(GameOverResult.Over(StatType.People, StatBoundary.Min, ending));

            Assert.That(view.IsVisible, Is.True);
            Assert.That(title.text, Is.EqualTo("The Gates Opened"));
            Assert.That(body.text, Is.EqualTo("And no one closed them."));
            Assert.That(view.IsShowingGenericFallback, Is.False);
        }

        [Test]
        public void GameOver_FallsBackToGenericWordingWhenTheEndingIsMissing()
        {
            GameOverView view = BuildGameOverView(
                out TextMeshProUGUI title, out TextMeshProUGUI body, out _);

            view.Show(GameOverResult.Over(StatType.Wealth, StatBoundary.Max, null));

            Assert.That(view.IsVisible, Is.True);
            Assert.That(title.text, Is.EqualTo("Generic Title"));
            Assert.That(body.text, Is.EqualTo("Generic body."));
            Assert.That(view.IsShowingGenericFallback, Is.True);
        }

        [Test]
        public void GameOver_StaysHiddenForARunThatHasNotEnded()
        {
            GameOverView view = BuildGameOverView(out _, out _, out _);

            view.Show(GameOverResult.NotOver());

            Assert.That(view.IsVisible, Is.False);
        }

        [Test]
        public void GameOver_HideTogglesVisibility()
        {
            GameOverView view = BuildGameOverView(out _, out _, out _);

            view.Show(GameOverResult.Over(
                StatType.Authority, StatBoundary.Min, CardTestFactory.Ending(id: "e")));
            Assert.That(view.IsVisible, Is.True);

            view.Hide();
            Assert.That(view.IsVisible, Is.False);
        }

        [Test]
        public void GameOver_WithNoIllustrationDisablesTheImage()
        {
            GameOverView view = BuildGameOverView(
                out _, out _, out Image illustration,
                new GraphicFallbackSettings(null, Color.magenta, useFallbackColour: false));

            view.Show(GameOverResult.Over(
                StatType.Authority, StatBoundary.Min, CardTestFactory.Ending(id: "e")));

            Assert.That(illustration.enabled, Is.False);
            Assert.That(view.IllustrationMode, Is.EqualTo(GraphicFallbackMode.HideGraphic));
        }

        [Test]
        public void GameOver_RestartButtonOnlyRaisesAnEvent()
        {
            GameOverView view = BuildGameOverView(out _, out _, out _);

            int raised = 0;
            Action handler = () => raised++;
            view.RestartRequested += handler;

            view.Show(GameOverResult.Over(
                StatType.Authority, StatBoundary.Min, CardTestFactory.Ending(id: "e")));
            view.HandleRestartButton();

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(view.IsVisible, Is.True,
                "the view must not act on the request; Phase 7 decides what restart means");

            view.RestartRequested -= handler;
            view.HandleRestartButton();
            Assert.That(raised, Is.EqualTo(1), "unsubscribing must stop delivery");
        }

        [Test]
        public void GameOver_RestartWithNoSubscriberDoesNotThrow()
        {
            GameOverView view = BuildGameOverView(out _, out _, out _);

            Assert.That(() => view.HandleRestartButton(), Throws.Nothing);
        }

        // --- GameUIThemeController: situation panel sprite/procedural fallback -----------------

        private static GameUITheme BuildTheme(Sprite situationPanelSprite)
        {
            GameUITheme theme = ScriptableObject.CreateInstance<GameUITheme>();
            SerializedObject serialized = new SerializedObject(theme);
            serialized.FindProperty("situationPanelSprite").objectReferenceValue = situationPanelSprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return theme;
        }

        private static GameUIThemeController BuildThemeController(
            GameUITheme theme, out Image artwork, out ProceduralRoundedRectGraphic fallback)
        {
            artwork = PresentationTestObjects.CreateImage("Artwork");
            fallback = PresentationTestObjects.CreateComponent<ProceduralRoundedRectGraphic>("Fallback");
            GameUIThemeController controller =
                PresentationTestObjects.CreateComponent<GameUIThemeController>("ThemeController");
            controller.SetAuthoringReferences(
                theme, null, null, null, null, null, artwork, fallback);
            return controller;
        }

        [Test]
        public void SituationPanel_ShowsArtworkAndHidesFallback_WhenThemeSpriteAssigned()
        {
            Sprite sprite = PresentationTestObjects.CreateSprite();
            GameUITheme theme = BuildTheme(sprite);
            try
            {
                GameUIThemeController controller = BuildThemeController(
                    theme, out Image artwork, out ProceduralRoundedRectGraphic fallback);

                controller.ApplyTheme();

                Assert.That(artwork.enabled, Is.True);
                Assert.That(artwork.sprite, Is.SameAs(sprite));
                Assert.That(fallback.enabled, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(theme);
            }
        }

        [Test]
        public void SituationPanel_ShowsFallbackAndHidesArtwork_WhenThemeHasNoSprite()
        {
            GameUITheme theme = BuildTheme(null);
            try
            {
                GameUIThemeController controller = BuildThemeController(
                    theme, out Image artwork, out ProceduralRoundedRectGraphic fallback);

                controller.ApplyTheme();

                Assert.That(artwork.enabled, Is.False);
                Assert.That(fallback.enabled, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(theme);
            }
        }
    }
}
