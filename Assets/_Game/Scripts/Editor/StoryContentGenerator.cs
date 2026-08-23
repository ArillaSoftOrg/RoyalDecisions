using System;
using System.Collections.Generic;
using System.IO;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using UnityEditor;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Writes the "Sığınak: Saltanat Günlükleri" story content set into the project as
    /// ScriptableObject assets.
    /// </summary>
    /// <remarks>
    /// Structurally mirrors <see cref="PlaceholderContentGenerator"/> — build in memory, validate,
    /// verify every target path is either free or generator-owned, and only then touch the
    /// AssetDatabase — but is kept as its own type rather than sharing an implementation with it, so
    /// neither generator's tested behaviour is put at risk by changes made for the other.
    /// </remarks>
    public static class StoryContentGenerator
    {
        /// <summary>
        /// Marks an asset as generated. The overwrite guard reads this label, so anything the team
        /// hand-authors into the story folder is protected simply by not carrying it.
        /// </summary>
        public const string StoryLabel = "RoyalDecisions.Story";

        public const string DefaultRoot = "Assets/_Game/Content/Story";

        public const string CatalogueAssetName = "StoryContentCatalogue.asset";

        public const string CardsFolderName = "Cards";

        public const string EndingsFolderName = "Endings";

        private const string MenuPath = "Tools/Royal Decisions/Generate Story Content";

        [MenuItem(MenuPath)]
        public static void GenerateFromMenu()
        {
            ContentGenerationReport report = Generate(DefaultRoot);
            LogReport(report);
        }

        /// <summary>CLI entry point for <c>-executeMethod</c>: exits non-zero on failure.</summary>
        public static void GenerateBatch()
        {
            ContentGenerationReport report = Generate(DefaultRoot);
            LogReport(report);
            EditorApplication.Exit(report.Succeeded ? 0 : 1);
        }

        /// <summary>Generates into <paramref name="root"/>, which must sit inside <see cref="DefaultRoot"/>.</summary>
        public static ContentGenerationReport Generate(string root)
        {
            AssertRootIsAllowed(root);

            ContentGenerationReport report = new ContentGenerationReport();

            List<CardDefinition> cards = StoryContentLibrary.CreateCards();
            List<EndingDefinition> endings = StoryContentLibrary.CreateEndings();
            List<UnityEngine.Object> temporaries = new List<UnityEngine.Object>();

            try
            {
                if (!PreValidate(cards, endings, report))
                {
                    temporaries.AddRange(cards);
                    temporaries.AddRange(endings);
                    return report;
                }

                if (!IdsAreFileSafe(cards, endings, report))
                {
                    temporaries.AddRange(cards);
                    temporaries.AddRange(endings);
                    return report;
                }

                string cardsFolder = root + "/" + CardsFolderName;
                string endingsFolder = root + "/" + EndingsFolderName;
                string cataloguePath = root + "/" + CatalogueAssetName;

                Dictionary<string, UnityEngine.Object> existing =
                    new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);

                if (!ScanExistingAssets(cards, endings, cardsFolder, endingsFolder, cataloguePath,
                        existing, report))
                {
                    temporaries.AddRange(cards);
                    temporaries.AddRange(endings);
                    return report;
                }

                // Folders are created before batching: CreateFolder inside a Start/StopAssetEditing
                // block is not reliable.
                EnsureFolder(cardsFolder);
                EnsureFolder(endingsFolder);

                Write(cards, endings, cardsFolder, endingsFolder, cataloguePath, existing,
                    temporaries, report);
            }
            finally
            {
                DestroyTemporaries(temporaries);
            }

            return report;
        }

        // --- Guards ----------------------------------------------------------------

        private static void AssertRootIsAllowed(string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                throw new ArgumentException("Generation root must be supplied.", nameof(root));
            }

            string normalized = root.Replace('\\', '/').TrimEnd('/');

            bool allowed = string.Equals(normalized, DefaultRoot, StringComparison.Ordinal)
                || normalized.StartsWith(DefaultRoot + "/", StringComparison.Ordinal);

            if (!allowed)
            {
                throw new ArgumentException(
                    string.Format(
                        "Refusing to generate into '{0}'. The generator may only write inside '{1}'.",
                        root,
                        DefaultRoot),
                    nameof(root));
            }
        }

        private static bool PreValidate(
            IReadOnlyList<CardDefinition> cards,
            IReadOnlyList<EndingDefinition> endings,
            ContentGenerationReport report)
        {
            ContentValidationReport validation = new ContentValidator()
                .Validate(cards, endings, StoryContentLibrary.OpeningCardId);

            for (int i = 0; i < validation.Warnings.Count; i++)
            {
                report.RecordWarning(validation.Warnings[i].ToString());
            }

            if (!validation.HasErrors)
            {
                return true;
            }

            for (int i = 0; i < validation.Errors.Count; i++)
            {
                report.RecordError(validation.Errors[i].ToString());
            }

            report.MarkAborted("content validation failed; no assets were written");
            return false;
        }

        private static bool IdsAreFileSafe(
            IReadOnlyList<CardDefinition> cards,
            IReadOnlyList<EndingDefinition> endings,
            ContentGenerationReport report)
        {
            bool safe = true;

            for (int i = 0; i < cards.Count; i++)
            {
                if (!IsFileSafe(cards[i].Id))
                {
                    report.RecordError("Card ID is not usable as a file name: " + cards[i].Id);
                    safe = false;
                }
            }

            for (int i = 0; i < endings.Count; i++)
            {
                if (!IsFileSafe(endings[i].Id))
                {
                    report.RecordError("Ending ID is not usable as a file name: " + endings[i].Id);
                    safe = false;
                }
            }

            if (!safe)
            {
                report.MarkAborted("one or more IDs cannot be written to disk");
            }

            return safe;
        }

        private static bool IsFileSafe(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            return id.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        private static bool ScanExistingAssets(
            IReadOnlyList<CardDefinition> cards,
            IReadOnlyList<EndingDefinition> endings,
            string cardsFolder,
            string endingsFolder,
            string cataloguePath,
            Dictionary<string, UnityEngine.Object> existing,
            ContentGenerationReport report)
        {
            bool clean = true;

            for (int i = 0; i < cards.Count; i++)
            {
                clean &= Inspect(AssetPath(cardsFolder, cards[i].Id), existing, report);
            }

            for (int i = 0; i < endings.Count; i++)
            {
                clean &= Inspect(AssetPath(endingsFolder, endings[i].Id), existing, report);
            }

            clean &= Inspect(cataloguePath, existing, report);

            if (!clean)
            {
                report.MarkAborted(
                    "an asset at a target path is not generated story content; " +
                    "nothing was written");
            }

            return clean;
        }

        private static bool Inspect(
            string assetPath,
            Dictionary<string, UnityEngine.Object> existing,
            ContentGenerationReport report)
        {
            UnityEngine.Object asset =
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

            if (asset == null)
            {
                return true;
            }

            if (!HasStoryLabel(asset))
            {
                report.RecordSkipped(assetPath, "not labelled " + StoryLabel);
                report.RecordError(
                    "Refusing to overwrite '" + assetPath + "': it is not generated story " +
                    "content. Move or delete it, or add the " + StoryLabel + " label.");
                return false;
            }

            existing[assetPath] = asset;
            return true;
        }

        private static bool HasStoryLabel(UnityEngine.Object asset)
        {
            string[] labels = AssetDatabase.GetLabels(asset);
            for (int i = 0; i < labels.Length; i++)
            {
                if (string.Equals(labels[i], StoryLabel, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        // --- Writing -----------------------------------------------------------------

        private static void Write(
            List<CardDefinition> cards,
            List<EndingDefinition> endings,
            string cardsFolder,
            string endingsFolder,
            string cataloguePath,
            Dictionary<string, UnityEngine.Object> existing,
            List<UnityEngine.Object> temporaries,
            ContentGenerationReport report)
        {
            CardDefinition[] persistedCards = new CardDefinition[cards.Count];
            EndingDefinition[] persistedEndings = new EndingDefinition[endings.Count];
            List<UnityEngine.Object> persisted = new List<UnityEngine.Object>(cards.Count + endings.Count + 1);

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    persistedCards[i] = Persist(
                        cards[i], AssetPath(cardsFolder, cards[i].Id), existing, temporaries,
                        persisted, report);
                }

                for (int i = 0; i < endings.Count; i++)
                {
                    persistedEndings[i] = Persist(
                        endings[i], AssetPath(endingsFolder, endings[i].Id), existing, temporaries,
                        persisted, report);
                }

                PersistCatalogue(
                    cataloguePath, persistedCards, persistedEndings, existing, temporaries,
                    persisted, report);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            ApplyLabels(persisted);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ApplyLabels(List<UnityEngine.Object> assets)
        {
            for (int i = 0; i < assets.Count; i++)
            {
                UnityEngine.Object asset = assets[i];

                if (asset != null && !HasStoryLabel(asset))
                {
                    AssetDatabase.SetLabels(asset, new[] { StoryLabel });
                }
            }
        }

        private static T Persist<T>(
            T source,
            string assetPath,
            Dictionary<string, UnityEngine.Object> existing,
            List<UnityEngine.Object> temporaries,
            List<UnityEngine.Object> persisted,
            ContentGenerationReport report)
            where T : ScriptableObject
        {
            if (!existing.TryGetValue(assetPath, out UnityEngine.Object found) || found == null)
            {
                AssetDatabase.CreateAsset(source, assetPath);
                persisted.Add(source);
                report.RecordCreated(assetPath);
                return source;
            }

            T target = (T)found;
            temporaries.Add(source);
            persisted.Add(target);

            if (SerializedContentMatches(target, source))
            {
                report.RecordUnchanged(assetPath);
                return target;
            }

            EditorUtility.CopySerialized(source, target);
            EditorUtility.SetDirty(target);
            report.RecordUpdated(assetPath);
            return target;
        }

        private static void PersistCatalogue(
            string cataloguePath,
            CardDefinition[] cards,
            EndingDefinition[] endings,
            Dictionary<string, UnityEngine.Object> existing,
            List<UnityEngine.Object> temporaries,
            List<UnityEngine.Object> persisted,
            ContentGenerationReport report)
        {
            string openingCardId = StoryContentLibrary.OpeningCardId;

            if (existing.TryGetValue(cataloguePath, out UnityEngine.Object found)
                && found is ContentCatalogue target)
            {
                persisted.Add(target);

                if (CatalogueMatches(target, cards, endings, openingCardId))
                {
                    report.RecordUnchanged(cataloguePath);
                    return;
                }

                target.SetAuthoringData(cards, endings, openingCardId);
                EditorUtility.SetDirty(target);
                report.RecordUpdated(cataloguePath);
                return;
            }

            ContentCatalogue catalogue = ScriptableObject.CreateInstance<ContentCatalogue>();
            catalogue.name = Path.GetFileNameWithoutExtension(cataloguePath);
            catalogue.SetAuthoringData(cards, endings, openingCardId);

            AssetDatabase.CreateAsset(catalogue, cataloguePath);
            persisted.Add(catalogue);
            report.RecordCreated(cataloguePath);
        }

        private static bool SerializedContentMatches(
            ScriptableObject target,
            ScriptableObject source)
        {
            return string.Equals(
                EditorJsonUtility.ToJson(target),
                EditorJsonUtility.ToJson(source),
                StringComparison.Ordinal);
        }

        private static bool CatalogueMatches(
            ContentCatalogue catalogue,
            CardDefinition[] cards,
            EndingDefinition[] endings,
            string openingCardId)
        {
            if (!string.Equals(catalogue.OpeningCardId, openingCardId, StringComparison.Ordinal))
            {
                return false;
            }

            IReadOnlyList<CardDefinition> storedCards = catalogue.Cards;
            if (storedCards.Count != cards.Length)
            {
                return false;
            }

            for (int i = 0; i < cards.Length; i++)
            {
                if (storedCards[i] != cards[i])
                {
                    return false;
                }
            }

            IReadOnlyList<EndingDefinition> storedEndings = catalogue.Endings;
            if (storedEndings.Count != endings.Length)
            {
                return false;
            }

            for (int i = 0; i < endings.Length; i++)
            {
                if (storedEndings[i] != endings[i])
                {
                    return false;
                }
            }

            return true;
        }

        // --- Helpers -------------------------------------------------------------------

        private static string AssetPath(string folder, string id)
        {
            return folder + "/" + id + ".asset";
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string leaf = Path.GetFileName(folderPath);

            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
            {
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void DestroyTemporaries(List<UnityEngine.Object> temporaries)
        {
            for (int i = 0; i < temporaries.Count; i++)
            {
                if (temporaries[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(temporaries[i]);
                }
            }

            temporaries.Clear();
        }

        private static void LogReport(ContentGenerationReport report)
        {
            for (int i = 0; i < report.Messages.Count; i++)
            {
                Debug.Log("[Story Content] " + report.Messages[i]);
            }

            string summary = "[Story Content] " + report;

            if (report.Aborted || report.Errors > 0)
            {
                Debug.LogError(summary);
            }
            else if (report.Warnings > 0)
            {
                Debug.LogWarning(summary);
            }
            else
            {
                Debug.Log(summary);
            }
        }
    }
}
