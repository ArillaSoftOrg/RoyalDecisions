using NUnit.Framework;
using RoyalDecisions.Composition;
using RoyalDecisions.Data;
using RoyalDecisions.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Guards the one thing a domain-level "new game starts at the opening card" test cannot see:
    /// which <see cref="ContentCatalogue"/> asset the committed Game scene actually points at.
    /// </summary>
    /// <remarks>
    /// <see cref="RoyalDecisions.Application.GameSession.StartNewGame"/> always opens on whatever
    /// <c>catalogue.OpeningCardId</c> says — that logic is covered exhaustively elsewhere with
    /// synthetic catalogues. What synthetic-catalogue tests cannot catch is the scene's serialized
    /// reference itself drifting to the wrong asset (as happened here: the working copy of
    /// Game.unity pointed at the placeholder catalogue instead of the story one). This test opens
    /// the real committed scene and checks that one reference.
    /// </remarks>
    [TestFixture]
    public class GameSceneStoryWiringTests
    {
        [Test]
        public void GameScene_PointsAtTheStoryCatalogueWhoseOpeningCardIsK1()
        {
            EditorSceneManager.OpenScene(SceneSetupAutomation.GameScenePath, OpenSceneMode.Single);

            GameSceneController controller = UnityEngine.Object
                .FindFirstObjectByType<GameSceneController>();
            Assert.That(controller, Is.Not.Null,
                "GameSceneController was not found in the Game scene.");

            SerializedObject serialized = new SerializedObject(controller);
            ContentCatalogue catalogue = serialized.FindProperty("catalogue")
                .objectReferenceValue as ContentCatalogue;

            Assert.That(catalogue, Is.Not.Null,
                "GameSceneController.catalogue is unassigned in the committed Game scene.");
            Assert.That(catalogue.OpeningCardId, Is.EqualTo(StoryContentLibrary.OpeningCardId),
                "A fresh game must open on K1 (" + StoryContentLibrary.OpeningCardId + "), but the " +
                "Game scene's wired catalogue names '" + catalogue.OpeningCardId + "' as its " +
                "opening card. Run Tools > Royal Decisions > Scene Setup > " +
                "Use Story Catalogue In Game Scene to fix the wiring.");
        }
    }
}
