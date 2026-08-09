using System;
using System.Collections.Generic;
using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Editor;
using UnityEditor;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Exercises the generator against the real AssetDatabase, sandboxed inside the placeholder
    /// tree and cleaned up whether or not a test passes.
    /// </summary>
    [TestFixture]
    public class PlaceholderContentGeneratorTests
    {
        private const string TestRoot =
            PlaceholderContentGenerator.DefaultRoot + "/__GeneratorTests__";

        private const int ExpectedAssetCount = 29; // 20 cards + 8 endings + 1 catalogue

        [TearDown]
        public void TearDown()
        {
            DeleteTestRoot();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Safety net: a test that throws before its TearDown must still not leave assets behind.
            DeleteTestRoot();
        }

        private static void DeleteTestRoot()
        {
            if (AssetDatabase.IsValidFolder(TestRoot))
            {
                AssetDatabase.DeleteAsset(TestRoot);
                AssetDatabase.Refresh();
            }
        }

        private static string CardPath(string id)
        {
            return TestRoot + "/" + PlaceholderContentGenerator.CardsFolderName + "/" + id + ".asset";
        }

        private static string CataloguePath()
        {
            return TestRoot + "/" + PlaceholderContentGenerator.CatalogueAssetName;
        }

        private static void CreateFolderChain(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            int lastSlash = folderPath.LastIndexOf('/');
            string parent = folderPath.Substring(0, lastSlash);
            string leaf = folderPath.Substring(lastSlash + 1);

            CreateFolderChain(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // --- First run ------------------------------------------------------------

        [Test]
        public void FirstRun_CreatesEveryAsset()
        {
            ContentGenerationReport report = PlaceholderContentGenerator.Generate(TestRoot);

            Assert.That(report.Aborted, Is.False, report.ToString());
            Assert.That(report.Errors, Is.Zero, report.ToString());
            Assert.That(report.Created, Is.EqualTo(ExpectedAssetCount), report.ToString());
            Assert.That(report.Updated, Is.Zero);
            Assert.That(report.Skipped, Is.Zero);
            Assert.That(report.Succeeded, Is.True);
        }

        [Test]
        public void FirstRun_WritesTwentyCardsAndEightEndings()
        {
            PlaceholderContentGenerator.Generate(TestRoot);

            string[] cardGuids = AssetDatabase.FindAssets(
                "t:" + nameof(CardDefinition),
                new[] { TestRoot });
            string[] endingGuids = AssetDatabase.FindAssets(
                "t:" + nameof(EndingDefinition),
                new[] { TestRoot });
            string[] catalogueGuids = AssetDatabase.FindAssets(
                "t:" + nameof(ContentCatalogue),
                new[] { TestRoot });

            Assert.That(cardGuids.Length, Is.EqualTo(20));
            Assert.That(endingGuids.Length, Is.EqualTo(8));
            Assert.That(catalogueGuids.Length, Is.EqualTo(1));
        }

        [Test]
        public void GeneratedAssetsCarryThePlaceholderLabel()
        {
            PlaceholderContentGenerator.Generate(TestRoot);

            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { TestRoot });
            int labelled = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

                if (asset == null || asset is DefaultAsset)
                {
                    continue; // folders
                }

                Assert.That(
                    AssetDatabase.GetLabels(asset),
                    Contains.Item(PlaceholderContentGenerator.PlaceholderLabel),
                    path);
                labelled++;
            }

            Assert.That(labelled, Is.EqualTo(ExpectedAssetCount));
        }

        [Test]
        public void GeneratedCatalogueHoldsOrdinallySortedCardsAndTheOpeningCard()
        {
            PlaceholderContentGenerator.Generate(TestRoot);

            ContentCatalogue catalogue =
                AssetDatabase.LoadAssetAtPath<ContentCatalogue>(CataloguePath());

            Assert.That(catalogue, Is.Not.Null);
            Assert.That(catalogue.Cards.Count, Is.EqualTo(20));
            Assert.That(catalogue.Endings.Count, Is.EqualTo(8));
            Assert.That(catalogue.OpeningCardId,
                Is.EqualTo(PlaceholderContentLibrary.OpeningCardId));

            for (int i = 1; i < catalogue.Cards.Count; i++)
            {
                Assert.That(
                    StringComparer.Ordinal.Compare(
                        catalogue.Cards[i - 1].Id, catalogue.Cards[i].Id),
                    Is.LessThan(0));
            }
        }

        [Test]
        public void GeneratedCatalogueReferencesTheWrittenAssetsNotStrayCopies()
        {
            PlaceholderContentGenerator.Generate(TestRoot);

            ContentCatalogue catalogue =
                AssetDatabase.LoadAssetAtPath<ContentCatalogue>(CataloguePath());

            for (int i = 0; i < catalogue.Cards.Count; i++)
            {
                CardDefinition card = catalogue.Cards[i];
                Assert.That(card, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(card), Is.Not.Empty,
                    "catalogue must reference persisted assets, not in-memory instances");
            }
        }

        // --- Repeated runs -----------------------------------------------------------

        [Test]
        public void SecondRun_ChangesNothing()
        {
            PlaceholderContentGenerator.Generate(TestRoot);

            ContentGenerationReport second = PlaceholderContentGenerator.Generate(TestRoot);

            Assert.That(second.Aborted, Is.False, second.ToString());
            Assert.That(second.Created, Is.Zero, second.ToString());
            Assert.That(second.Updated, Is.Zero, second.ToString());
            Assert.That(second.Unchanged, Is.EqualTo(ExpectedAssetCount), second.ToString());
        }

        [Test]
        public void RepeatedRuns_PreserveAssetGuids()
        {
            PlaceholderContentGenerator.Generate(TestRoot);

            string cardPath = CardPath(PlaceholderContentLibrary.OpeningCardId);
            string cardGuidBefore = AssetDatabase.AssetPathToGUID(cardPath);
            string catalogueGuidBefore = AssetDatabase.AssetPathToGUID(CataloguePath());

            Assert.That(cardGuidBefore, Is.Not.Empty, "precondition: the card asset exists");

            PlaceholderContentGenerator.Generate(TestRoot);

            // Delete-and-recreate would mint new GUIDs and break every reference to this content.
            Assert.That(AssetDatabase.AssetPathToGUID(cardPath), Is.EqualTo(cardGuidBefore));
            Assert.That(AssetDatabase.AssetPathToGUID(CataloguePath()),
                Is.EqualTo(catalogueGuidBefore));
        }

        [Test]
        public void RepeatedRuns_RestoreAnEditedGeneratedAsset()
        {
            PlaceholderContentGenerator.Generate(TestRoot);

            string cardPath = CardPath(PlaceholderContentLibrary.OpeningCardId);
            CardDefinition card = AssetDatabase.LoadAssetAtPath<CardDefinition>(cardPath);
            card.SetAuthoringData(
                PlaceholderContentLibrary.OpeningCardId,
                "Edited",
                "Edited body",
                null,
                null);
            EditorUtility.SetDirty(card);
            AssetDatabase.SaveAssets();

            ContentGenerationReport report = PlaceholderContentGenerator.Generate(TestRoot);

            Assert.That(report.Updated, Is.EqualTo(1), report.ToString());
            Assert.That(report.Unchanged, Is.EqualTo(ExpectedAssetCount - 1));

            CardDefinition restored = AssetDatabase.LoadAssetAtPath<CardDefinition>(cardPath);
            Assert.That(restored.Speaker,
                Does.StartWith(PlaceholderContentLibrary.PlaceholderTag));
        }

        // --- Overwrite protection ---------------------------------------------------------

        [Test]
        public void UnlabelledAssetAtATargetPath_AbortsTheRun()
        {
            string cardPath = CardPath(PlaceholderContentLibrary.OpeningCardId);
            CreateFolderChain(TestRoot + "/" + PlaceholderContentGenerator.CardsFolderName);

            CardDefinition handAuthored = ScriptableObject.CreateInstance<CardDefinition>();
            handAuthored.SetAuthoringData(
                "hand_authored", "Author", "Do not overwrite me", null, null);
            AssetDatabase.CreateAsset(handAuthored, cardPath);
            AssetDatabase.SaveAssets();

            ContentGenerationReport report = PlaceholderContentGenerator.Generate(TestRoot);

            Assert.That(report.Aborted, Is.True, report.ToString());
            Assert.That(report.Errors, Is.GreaterThan(0));
            Assert.That(report.Skipped, Is.GreaterThan(0));
            Assert.That(report.Created, Is.Zero, "nothing may be written once the run aborts");
            Assert.That(report.Updated, Is.Zero);

            CardDefinition survivor = AssetDatabase.LoadAssetAtPath<CardDefinition>(cardPath);
            Assert.That(survivor, Is.Not.Null);
            Assert.That(survivor.Id, Is.EqualTo("hand_authored"),
                "hand-authored content must survive untouched");
            Assert.That(survivor.BodyText, Is.EqualTo("Do not overwrite me"));
        }

        [Test]
        public void AbortLeavesNoPartiallyWrittenAssets()
        {
            string cardPath = CardPath(PlaceholderContentLibrary.OpeningCardId);
            CreateFolderChain(TestRoot + "/" + PlaceholderContentGenerator.CardsFolderName);

            CardDefinition handAuthored = ScriptableObject.CreateInstance<CardDefinition>();
            handAuthored.SetAuthoringData("hand_authored", "Author", "Body", null, null);
            AssetDatabase.CreateAsset(handAuthored, cardPath);
            AssetDatabase.SaveAssets();

            PlaceholderContentGenerator.Generate(TestRoot);

            string[] endingGuids = AssetDatabase.FindAssets(
                "t:" + nameof(EndingDefinition), new[] { TestRoot });
            string[] catalogueGuids = AssetDatabase.FindAssets(
                "t:" + nameof(ContentCatalogue), new[] { TestRoot });

            Assert.That(endingGuids, Is.Empty, "no ending should have been written");
            Assert.That(catalogueGuids, Is.Empty, "no catalogue should have been written");
        }

        // --- Path guard ---------------------------------------------------------------------

        [TestCase("Assets/SomewhereElse")]
        [TestCase("Assets/_Game/Content")]
        [TestCase("Assets/_Game/Content/PlaceholderSibling")]
        [TestCase("Assets")]
        [TestCase("")]
        public void RootOutsideThePlaceholderTree_IsRefused(string root)
        {
            Assert.That(() => PlaceholderContentGenerator.Generate(root),
                Throws.ArgumentException);
        }

        [Test]
        public void RootOutsideThePlaceholderTree_WritesNothing()
        {
            const string outsideRoot = "Assets/_Game/Content/ShouldNeverExist";

            Assert.That(() => PlaceholderContentGenerator.Generate(outsideRoot),
                Throws.ArgumentException);
            Assert.That(AssetDatabase.IsValidFolder(outsideRoot), Is.False);
        }

        [Test]
        public void DefaultRootIsAccepted()
        {
            // Guards the guard: the production path must not be rejected by its own rule.
            Assert.That(PlaceholderContentGenerator.DefaultRoot,
                Is.EqualTo("Assets/_Game/Content/Placeholder"));
            Assert.That(TestRoot.StartsWith(
                PlaceholderContentGenerator.DefaultRoot + "/", StringComparison.Ordinal),
                Is.True);
        }
    }
}
