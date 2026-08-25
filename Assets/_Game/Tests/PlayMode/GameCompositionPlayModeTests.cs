using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RoyalDecisions.Application;
using RoyalDecisions.Composition;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.PlayMode
{
    /// <summary>
    /// Wires the real controller to real views and drives it with a real drag.
    /// </summary>
    /// <remarks>
    /// The session's own logic is covered exhaustively in EditMode. What is left here is genuinely
    /// Unity-shaped: MonoBehaviour lifecycle, event subscription symmetry, and a swipe that has to
    /// travel through the controller to reach a save.
    ///
    /// Persistence is injected, so nothing here touches the player's persistent data.
    /// </remarks>
    [TestFixture]
    public class GameCompositionPlayModeTests
    {
        private const string OpeningCardId = "card_01_opening";
        private const float ParentWidth = 1000f;
        private const float Threshold = 250f;
        private const float PressX = 500f;
        private const float PressY = 800f;

        private GameObject root;
        private readonly List<Object> tracked = new List<Object>();

        private GameSceneController controller;
        private CardSwipeController swipe;
        private CardView cardView;
        private GameOverView gameOverView;
        private TapChoiceButtonsView tapChoices;
        private Button tapChoiceLeftButton;
        private Button tapChoiceRightButton;
        private StubRunSaveStore store;
        private StubSettingsStore settingsStore;
        private AccessibilityPresentationController accessibility;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.Destroy(root);
                root = null;
            }

            for (int i = 0; i < tracked.Count; i++)
            {
                if (tracked[i] != null)
                {
                    Object.Destroy(tracked[i]);
                }
            }

            tracked.Clear();
        }

        private T Track<T>(T asset) where T : Object
        {
            tracked.Add(asset);
            return asset;
        }

        private RectTransform Child(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            RectTransform rect = child.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private CardDefinition Card(string id)
        {
            CardDefinition card = Track(ScriptableObject.CreateInstance<CardDefinition>());
            card.name = id;
            card.SetAuthoringData(
                id, "Speaker", "Body",
                new ChoiceDefinition("Left", new StatDeltas(1, 0, 0, 0)),
                new ChoiceDefinition("Right", new StatDeltas(0, 1, 0, 0)));
            return card;
        }

        private ContentCatalogue BuildCatalogue()
        {
            List<EndingDefinition> endings = new List<EndingDefinition>();

            foreach (StatType stat in (StatType[])System.Enum.GetValues(typeof(StatType)))
            {
                foreach (StatBoundary boundary in
                         (StatBoundary[])System.Enum.GetValues(typeof(StatBoundary)))
                {
                    EndingDefinition ending = Track(
                        ScriptableObject.CreateInstance<EndingDefinition>());
                    ending.SetAuthoringData(
                        "ending_" + stat + "_" + boundary, "Title", "Body", stat, boundary);
                    endings.Add(ending);
                }
            }

            ContentCatalogue catalogue = Track(ScriptableObject.CreateInstance<ContentCatalogue>());
            catalogue.SetAuthoringData(
                new[] { Card(OpeningCardId), Card("card_02_other") },
                endings.ToArray(),
                OpeningCardId);

            return catalogue;
        }

        /// <summary>Builds the scene inactive, configures it, then activates it.</summary>
        private void BuildScene(GameSettings settingsSeed = null)
        {
            root = new GameObject("GameRoot");
            root.SetActive(false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            RectTransform parent = Child(root.transform, "CardArea");
            parent.sizeDelta = new Vector2(ParentWidth, 1600f);

            RectTransform card = Child(parent, "Card");
            card.sizeDelta = new Vector2(600f, 900f);
            card.anchoredPosition = Vector2.zero;

            ChoicePreviewView left = card.gameObject.AddComponent<ChoicePreviewView>();
            left.SetAuthoringReferences(ChoiceSide.Left, null, null);

            ChoicePreviewView right = Child(card, "Right").gameObject
                .AddComponent<ChoicePreviewView>();
            right.SetAuthoringReferences(ChoiceSide.Right, null, null);

            cardView = card.gameObject.AddComponent<CardView>();
            cardView.SetAuthoringReferences(null, null, null, left, right);

            swipe = card.gameObject.AddComponent<CardSwipeController>();
            swipe.SetAuthoringReferences(cardView, parent, snapDuration: 0.02f, exitSeconds: 0.02f);

            gameOverView = Child(root.transform, "GameOver").gameObject
                .AddComponent<GameOverView>();
            gameOverView.SetAuthoringReferences(null, null, null);

            tapChoiceLeftButton = Child(root.transform, "TapLeft").gameObject.AddComponent<Button>();
            tapChoiceRightButton = Child(root.transform, "TapRight").gameObject.AddComponent<Button>();
            tapChoices = Child(root.transform, "TapChoices").gameObject
                .AddComponent<TapChoiceButtonsView>();
            tapChoices.SetAuthoringReferences(swipe, tapChoiceLeftButton, tapChoiceRightButton);

            accessibility = root.AddComponent<AccessibilityPresentationController>();
            accessibility.SetAuthoringReferences(null, null, swipe, null);

            controller = root.AddComponent<GameSceneController>();
            controller.SetAuthoringReferences(
                BuildCatalogue(), cardView, null, gameOverView, swipe, tapChoices: tapChoices,
                accessibilityController: accessibility);

            store = new StubRunSaveStore();
            settingsStore = new StubSettingsStore();
            if (settingsSeed != null)
            {
                settingsStore.Save(settingsSeed);
            }
            controller.Configure(store, settingsStore, new StubSeedProvider(99));

            root.SetActive(true);
        }

        private static PointerEventData Pointer(float x)
        {
            return new PointerEventData(EventSystem.current)
            {
                pointerId = 0,
                position = new Vector2(x, PressY)
            };
        }

        private void SwipeRight()
        {
            PointerEventData data = Pointer(PressX);
            swipe.OnBeginDrag(data);
            data.position = new Vector2(PressX + Threshold + 50f, PressY);
            swipe.OnDrag(data);
            swipe.OnEndDrag(data);
        }

        private IEnumerator WaitForSwipeToSettle()
        {
            int frames = 0;

            while (swipe.IsAnimating && frames < 300)
            {
                frames++;
                yield return null;
            }

            Assert.That(swipe.IsAnimating, Is.False, "the swipe animation did not settle");
        }

        // --- Wiring -----------------------------------------------------------

        [UnityTest]
        public IEnumerator TheSceneStartsARunAndPresentsTheOpeningCard()
        {
            BuildScene();
            yield return null;

            Assert.That(controller.Session, Is.Not.Null);
            Assert.That(controller.State, Is.EqualTo(GameSessionState.AwaitingDecision));
            Assert.That(controller.Session.CurrentCard.Id, Is.EqualTo(OpeningCardId));
            Assert.That(store.SaveCount, Is.EqualTo(1), "the new run is saved immediately");
        }

        [UnityTest]
        public IEnumerator ARealDragResolvesADecisionAndSavesIt()
        {
            BuildScene();
            yield return null;

            int savesBefore = store.SaveCount;
            SwipeRight();

            Assert.That(controller.State, Is.EqualTo(GameSessionState.WaitingForCardExit),
                "the decision resolves while the card is still flying");
            Assert.That(store.SaveCount, Is.EqualTo(savesBefore + 1));

            yield return WaitForSwipeToSettle();
            yield return null;

            Assert.That(controller.State, Is.EqualTo(GameSessionState.AwaitingDecision),
                "the next card arrives once the exit animation completes");
        }

        [UnityTest]
        public IEnumerator SeveralSwipesInARowEachProduceExactlyOneSave()
        {
            BuildScene();
            yield return null;

            int savesBefore = store.SaveCount;

            for (int turn = 0; turn < 3; turn++)
            {
                SwipeRight();
                yield return WaitForSwipeToSettle();
                yield return null;
            }

            Assert.That(store.SaveCount, Is.EqualTo(savesBefore + 3));
            Assert.That(controller.Session.CurrentRun.Turn, Is.EqualTo(3));
        }

        // --- Subscription symmetry ----------------------------------------------

        [UnityTest]
        public IEnumerator ADisabledControllerIgnoresSwipeEvents()
        {
            BuildScene();
            yield return null;

            controller.enabled = false;
            yield return null;

            int savesBefore = store.SaveCount;
            int turnBefore = controller.Session.CurrentRun.Turn;

            SwipeRight();
            yield return null;

            Assert.That(store.SaveCount, Is.EqualTo(savesBefore),
                "a disabled controller must not resolve a decision");
            Assert.That(controller.Session.CurrentRun.Turn, Is.EqualTo(turnBefore));
        }

        [UnityTest]
        public IEnumerator ReEnablingResubscribesExactlyOnce()
        {
            BuildScene();
            yield return null;

            controller.enabled = false;
            yield return null;
            controller.enabled = true;
            yield return null;

            // The swipe controller was left locked by the disable, so ready it as the flow would.
            swipe.ResetForNextCard();

            int savesBefore = store.SaveCount;
            SwipeRight();

            Assert.That(store.SaveCount, Is.EqualTo(savesBefore + 1),
                "one subscription, so one save — not two");
        }

        // --- Interrupted exit ---------------------------------------------------------

        [UnityTest]
        public IEnumerator AnExitInterruptedByDisablingResumesOnReEnable()
        {
            BuildScene();
            yield return null;

            SwipeRight();
            Assert.That(controller.State, Is.EqualTo(GameSessionState.WaitingForCardExit));

            // The card never finishes leaving, so ExitAnimationCompleted never fires.
            controller.enabled = false;
            yield return null;

            Assert.That(controller.State, Is.EqualTo(GameSessionState.WaitingForCardExit),
                "the decision stays resolved and saved while the flow waits");

            controller.enabled = true;
            yield return null;

            Assert.That(controller.State, Is.EqualTo(GameSessionState.AwaitingDecision),
                "re-enabling continues the flow rather than deadlocking");
        }

        [UnityTest]
        public IEnumerator AnInterruptedExitDoesNotResolveTheDecisionTwice()
        {
            BuildScene();
            yield return null;

            int savesBefore = store.SaveCount;
            SwipeRight();
            controller.enabled = false;
            yield return null;
            controller.enabled = true;
            yield return null;

            Assert.That(store.SaveCount, Is.EqualTo(savesBefore + 1),
                "the decision was already resolved and persisted; resuming must not repeat it");
        }

        // --- Shutdown -------------------------------------------------------------------

        [UnityTest]
        public IEnumerator DestroyingTheSceneShutsTheSessionDown()
        {
            BuildScene();
            yield return null;

            GameSession session = controller.Session;
            Object.Destroy(root);
            root = null;
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.Uninitialized));
        }

        private static float PrivateFloat(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "field " + fieldName + " must exist");
            return (float)field.GetValue(target);
        }

        // --- Reduced Motion actually reaches the real swipe controller, not just GameSettings ---

        [UnityTest]
        public IEnumerator ReducedMotionInSettingsShortensTheRealSwipeControllersAnimation()
        {
            GameSettings settings = GameSettings.CreateDefault();
            settings.SetReducedMotion(true);
            BuildScene(settings);
            yield return null;

            Assert.That(PrivateFloat(swipe, "maxRotationDegrees"), Is.EqualTo(4f).Within(0.001f),
                "GameSceneController.ApplySettings must forward Reduced Motion to the real "
                + "CardSwipeController via AccessibilityPresentationController, not leave it a "
                + "GameSettings-only flag");
            Assert.That(PrivateFloat(swipe, "snapBackDuration"), Is.LessThanOrEqualTo(0.05f));
            Assert.That(PrivateFloat(swipe, "exitDuration"), Is.LessThanOrEqualTo(0.05f));
        }

        // --- ReapplySettings: Settings opened mid-run (from the new in-Game Settings panel) must
        //     take effect immediately, not only on the next scene load ---------------------------

        [UnityTest]
        public IEnumerator ReapplySettingsPicksUpAChangeMadeAfterTheSceneAlreadyStarted()
        {
            BuildScene(GameSettings.CreateDefault());
            yield return null;

            Assert.That(tapChoices.gameObject.activeSelf, Is.False,
                "precondition: tap buttons start hidden with default settings");

            // Simulate what SettingsController.ApplyRuntime now does when a real in-Game Settings
            // panel's Uygula is pressed: save a changed GameSettings, then tell this already-
            // running scene to re-read and re-apply it.
            GameSettings changed = GameSettings.CreateDefault();
            changed.SetDisableSwipe(true);
            settingsStore.Save(changed);
            controller.ReapplySettings();
            yield return null;

            Assert.That(tapChoices.gameObject.activeSelf, Is.True,
                "ReapplySettings must forward a settings change to the real views immediately, "
                + "exactly like the initial ApplySettings() call at Start already does");

            int savesBefore = store.SaveCount;
            SwipeRight();
            yield return null;
            Assert.That(store.SaveCount, Is.EqualTo(savesBefore),
                "the newly-disabled swipe must actually block the drag, not just show the buttons");
        }

        // --- Settings: Kaydırmayı Devre Dışı Bırak -------------------------------------------

        [UnityTest]
        public IEnumerator DisablingSwipeInSettingsBlocksTheDragButTapButtonsStillResolveTheCard()
        {
            GameSettings settings = GameSettings.CreateDefault();
            settings.SetDisableSwipe(true);
            settings.SetTapButtonsEnabled(false); // even off, DisableSwipe must force buttons visible
            BuildScene(settings);
            yield return null;

            Assert.That(tapChoices.gameObject.activeSelf, Is.True,
                "with swipe disabled, decision buttons must be forced visible regardless of their own toggle");

            int savesBefore = store.SaveCount;
            SwipeRight();
            yield return null;

            Assert.That(store.SaveCount, Is.EqualTo(savesBefore),
                "a real drag must not resolve a card while swipe is disabled");
            Assert.That(controller.Session.CurrentRun.Turn, Is.EqualTo(0));

            tapChoiceRightButton.onClick.Invoke();

            Assert.That(store.SaveCount, Is.EqualTo(savesBefore + 1),
                "the tap-button path must still resolve the card even though swipe is disabled");
        }

        [UnityTest]
        public IEnumerator LeavingSwipeEnabledKeepsTapButtonsVisibilityDrivenByItsOwnToggle()
        {
            GameSettings settings = GameSettings.CreateDefault();
            settings.SetTapButtonsEnabled(false);
            BuildScene(settings);
            yield return null;

            Assert.That(tapChoices.gameObject.activeSelf, Is.False,
                "swipe is enabled, so tap buttons should follow their own (off) toggle");

            int savesBefore = store.SaveCount;
            SwipeRight();

            Assert.That(store.SaveCount, Is.EqualTo(savesBefore + 1),
                "swipe must still resolve the card when DisableSwipe is off");
        }
    }
}
