using NUnit.Framework;
using RoyalDecisions.Composition;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using RoyalDecisions.Editor;
using RoyalDecisions.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class MainMenuControllerTests
    {
        private GameObject root;
        private MainMenuController controller;
        private Button continueButton;
        private InterfaceTextDefinition interfaceText;
        private TextMeshProUGUI saveErrorText;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("MainMenuTest");
            controller = root.AddComponent<MainMenuController>();
            GameObject buttonObject = new GameObject(
                "ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(root.transform, false);
            continueButton = buttonObject.GetComponent<Button>();

            interfaceText = TurkishInterfaceTextLibrary.Create();
            GameObject errorObject = new GameObject(
                "SaveError", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            errorObject.transform.SetParent(root.transform, false);
            saveErrorText = errorObject.GetComponent<TextMeshProUGUI>();
            MainMenuTextView textView = root.AddComponent<MainMenuTextView>();
            textView.SetAuthoringReferences(interfaceText, null, null, null, saveErrorText);

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("continueButton").objectReferenceValue = continueButton;
            serialized.FindProperty("interfaceText").objectReferenceValue = interfaceText;
            serialized.FindProperty("mainMenuTextView").objectReferenceValue = textView;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(interfaceText);
        }

        [Test]
        public void NoSave_DisablesContinueButton()
        {
            controller.Configure(new FakeRunSaveStore(), null, null);

            Assert.That(controller.IsContinueAvailable, Is.False);
            Assert.That(continueButton.interactable, Is.False);
            Assert.That(saveErrorText.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void LoadableSave_EnablesContinueButton()
        {
            FakeRunSaveStore store = new FakeRunSaveStore();
            store.Seed(RunState.CreateNew(123));

            controller.Configure(store, null, null);

            Assert.That(controller.IsContinueAvailable, Is.True);
            Assert.That(continueButton.interactable, Is.True);
        }

        [Test]
        public void EndedSave_DisablesContinueWithoutDeletingSave()
        {
            FakeRunSaveStore store = new FakeRunSaveStore();
            RunState ended = RunState.CreateNew(123);
            ended.EndRun();
            store.Seed(ended);

            controller.Configure(store, null, null);

            Assert.That(controller.IsContinueAvailable, Is.False);
            Assert.That(continueButton.interactable, Is.False);
            Assert.That(store.DeleteCount, Is.Zero);
        }

        [Test]
        public void CorruptSave_DisablesContinueButton()
        {
            FakeRunSaveStore store = new FakeRunSaveStore
            {
                ForcedLoadStatus = RoyalDecisions.Application.RunLoadStatus.Corrupt
            };

            controller.Configure(store, null, null);

            Assert.That(controller.IsContinueAvailable, Is.False);
            Assert.That(continueButton.interactable, Is.False);
            Assert.That(saveErrorText.text, Is.EqualTo(interfaceText.CorruptSave));
            Assert.That(saveErrorText.gameObject.activeSelf, Is.True);
        }

        // --- New Game / Continue routing ------------------------------------------------

        [Test]
        public void NewGameDestination_IsPrologueByDefault()
        {
            Assert.That(controller.NewGameDestinationSceneName, Is.EqualTo("Prologue"));
        }

        [Test]
        public void ContinueDestination_IsGameByDefault()
        {
            Assert.That(controller.ContinueDestinationSceneName, Is.EqualTo("Game"));
        }

        [Test]
        public void OnNewGamePressed_OutsidePlayMode_RequestsNewGameAndLoadsPrologueDestination()
        {
            FakeSceneLoader loader = new FakeSceneLoader();
            SessionIntent intent = ScriptableObject.CreateInstance<SessionIntent>();
            controller.Configure(new FakeRunSaveStore(), loader, intent);

            controller.OnNewGamePressed();

            Assert.That(intent.Mode, Is.EqualTo(SessionStartMode.NewGame),
                "The existing New Game intent must still be recorded before any scene loads.");
            Assert.That(loader.Count, Is.EqualTo(1));
            Assert.That(loader.LastScene, Is.EqualTo(controller.NewGameDestinationSceneName));
            Assert.That(loader.LastScene, Is.EqualTo("Prologue"));

            Object.DestroyImmediate(intent);
        }

        [Test]
        public void OnContinuePressed_WithAvailableSave_RequestsContinueAndLoadsGameDestination()
        {
            FakeRunSaveStore store = new FakeRunSaveStore();
            store.Seed(RunState.CreateNew(123));
            FakeSceneLoader loader = new FakeSceneLoader();
            SessionIntent intent = ScriptableObject.CreateInstance<SessionIntent>();
            controller.Configure(store, loader, intent);

            controller.OnContinuePressed();

            Assert.That(intent.Mode, Is.EqualTo(SessionStartMode.Continue));
            Assert.That(loader.Count, Is.EqualTo(1));
            Assert.That(loader.LastScene, Is.EqualTo(controller.ContinueDestinationSceneName));
            Assert.That(loader.LastScene, Is.EqualTo("Game"),
                "Continue must never route through the prologue.");

            Object.DestroyImmediate(intent);
        }

        [Test]
        public void OnContinuePressed_WithoutAvailableSave_DoesNothing()
        {
            FakeSceneLoader loader = new FakeSceneLoader();
            controller.Configure(new FakeRunSaveStore(), loader, null);

            controller.OnContinuePressed();

            Assert.That(loader.Count, Is.Zero);
        }

        [Test]
        public void OnNewGamePressed_CalledTwice_LoadsSceneOnlyOnce()
        {
            FakeSceneLoader loader = new FakeSceneLoader();
            controller.Configure(new FakeRunSaveStore(), loader, null);

            controller.OnNewGamePressed();
            controller.OnNewGamePressed();

            Assert.That(loader.Count, Is.EqualTo(1),
                "Double-click protection must still prevent a second transition.");
        }

        private sealed class FakeSceneLoader : ISceneLoader
        {
            public int Count { get; private set; }
            public string LastScene { get; private set; }

            public void LoadScene(string sceneName)
            {
                Count++;
                LastScene = sceneName;
            }
        }
    }
}
