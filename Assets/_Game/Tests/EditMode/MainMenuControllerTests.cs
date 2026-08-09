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
    }
}
