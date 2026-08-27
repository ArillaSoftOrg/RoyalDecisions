using System.Collections.Generic;
using NUnit.Framework;
using RoyalDecisions.Composition;
using RoyalDecisions.Data;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Covers <see cref="PrologueSceneController"/>'s scene-navigation-only responsibility: starting
    /// the prologue and loading Game exactly once when it finishes. Never touches real settings or
    /// save files — every test supplies a <see cref="FakeSettingsStore"/>.
    /// </summary>
    [TestFixture]
    public class PrologueSceneControllerTests
    {
        private readonly List<PrologueSequenceData> createdData = new List<PrologueSequenceData>();

        private PrologueSequenceController BuildPrologueController(int slideCount)
        {
            PrologueSequenceData data = ScriptableObject.CreateInstance<PrologueSequenceData>();
            createdData.Add(data);

            PrologueSlideData[] slides = new PrologueSlideData[slideCount];
            for (int i = 0; i < slideCount; i++)
            {
                slides[i] = new PrologueSlideData(null, "Subtitle " + i);
            }
            data.SetAuthoringData(slides);

            PrologueSequenceController controller =
                PresentationTestObjects.CreateComponent<PrologueSequenceController>("Prologue");
            controller.SetAuthoringReferences(
                data, null, null, null, null, null, null, null, null, null);
            return controller;
        }

        private static PrologueSceneController BuildSceneController(PrologueSequenceController prologue)
        {
            PrologueSceneController controller =
                PresentationTestObjects.CreateComponent<PrologueSceneController>("PrologueScene");
            controller.SetAuthoringReferences(prologue);
            return controller;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (PrologueSequenceData data in createdData)
            {
                if (data != null)
                {
                    Object.DestroyImmediate(data);
                }
            }
            createdData.Clear();

            PresentationTestObjects.DestroyAll();
        }

        [Test]
        public void BeginPrologueSequence_MissingController_LoadsGameFallback()
        {
            FakeSceneLoader loader = new FakeSceneLoader();
            PrologueSceneController controller = BuildSceneController(null);
            controller.Configure(loader, null, new FakeSettingsStore());

            controller.BeginPrologueSequence();

            Assert.That(loader.Count, Is.EqualTo(1));
            Assert.That(loader.LastScene, Is.EqualTo("Game"));
        }

        [Test]
        public void BeginPrologueSequence_EmptyData_LoadsGameFallback()
        {
            PrologueSequenceController prologue = BuildPrologueController(0);
            FakeSceneLoader loader = new FakeSceneLoader();
            PrologueSceneController controller = BuildSceneController(prologue);
            controller.Configure(loader, prologue, new FakeSettingsStore());

            controller.BeginPrologueSequence();

            Assert.That(loader.Count, Is.EqualTo(1));
            Assert.That(loader.LastScene, Is.EqualTo("Game"));
        }

        [Test]
        public void BeginPrologueSequence_CompletesNaturally_LoadsGameOnce()
        {
            PrologueSequenceController prologue = BuildPrologueController(2);
            FakeSceneLoader loader = new FakeSceneLoader();
            PrologueSceneController controller = BuildSceneController(prologue);
            controller.Configure(loader, prologue, new FakeSettingsStore());

            controller.BeginPrologueSequence();
            Assert.That(loader.Count, Is.Zero, "Game must not load before the prologue finishes.");

            prologue.OnTapAdvance(); // -> slide 1 (last)
            prologue.OnTapAdvance(); // -> completes

            Assert.That(loader.Count, Is.EqualTo(1));
            Assert.That(loader.LastScene, Is.EqualTo("Game"));
        }

        [Test]
        public void BeginPrologueSequence_Skip_LoadsGameOnce()
        {
            PrologueSequenceController prologue = BuildPrologueController(5);
            FakeSceneLoader loader = new FakeSceneLoader();
            PrologueSceneController controller = BuildSceneController(prologue);
            controller.Configure(loader, prologue, new FakeSettingsStore());

            controller.BeginPrologueSequence();
            prologue.Skip();

            Assert.That(loader.Count, Is.EqualTo(1));
            Assert.That(loader.LastScene, Is.EqualTo("Game"));
        }

        [Test]
        public void RepeatedCompletionRequests_CannotDoubleLoadGame()
        {
            PrologueSequenceController prologue = BuildPrologueController(1);
            FakeSceneLoader loader = new FakeSceneLoader();
            PrologueSceneController controller = BuildSceneController(prologue);
            controller.Configure(loader, prologue, new FakeSettingsStore());

            controller.BeginPrologueSequence();
            prologue.OnTapAdvance(); // single slide -> completes
            prologue.OnTapAdvance(); // repeated tap after completion, harmless
            prologue.Skip(); // repeated completion request, harmless

            Assert.That(loader.Count, Is.EqualTo(1));
        }

        [Test]
        public void BeginPrologueSequence_CalledTwice_StillLoadsGameOnlyOnce()
        {
            PrologueSequenceController prologue = BuildPrologueController(1);
            FakeSceneLoader loader = new FakeSceneLoader();
            PrologueSceneController controller = BuildSceneController(prologue);
            controller.Configure(loader, prologue, new FakeSettingsStore());

            controller.BeginPrologueSequence();
            controller.BeginPrologueSequence();
            prologue.OnTapAdvance();

            Assert.That(loader.Count, Is.EqualTo(1));
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
