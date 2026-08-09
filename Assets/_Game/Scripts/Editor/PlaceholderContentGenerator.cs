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
    /// Writes the placeholder content set into the project as ScriptableObject assets.
    /// </summary>
    /// <remarks>
    /// The run is ordered so that nothing is written until everything has been checked: build in
    /// memory, validate, verify every target path is either free or generator-owned, and only then
    /// touch the AssetDatabase. A failure in any earlier step leaves the project untouched.
    /// </remarks>
    public static class PlaceholderContentGenerator
    {
        /// <summary>
        /// Marks an asset as generated. The overwrite guard reads this label, so anything the team
        /// hand-authors into the placeholder folder is protected simply by not carrying it.
        /// </summary>
        public const string PlaceholderLabel = "RoyalDecisions.Placeholder";

        public const string DefaultRoot = "Assets/_Game/Content/Placeholder";

        public const string CatalogueAssetName = "PlaceholderContentCatalogue.asset";

        public const string CardsFolderName = "Cards";

        public const string EndingsFolderName = "Endings";

        private const string MenuPath = "Tools/Royal Decisions/Generate Placeholder Content";

        [MenuItem(MenuPath)]
        public static void GenerateFromMenu()
        {
            ContentGenerationReport report = Generate(DefaultRoot);
            LogReport(report);
        }

        /// <summary>
        /// Generates into <paramref name="root"/>, which must sit inside <see cref="DefaultRoot"/>.
        /// </summary>
        public static ContentGenerationReport Generate(string root)
        {
            AssertRootIsAllowed(root);

            ContentGenerationReport report = new ContentGenerationReport();

            List<CardDefinition> cards = PlaceholderContentLibrary.CreateCards();
            List<EndingDefinition> endings = PlaceholderContentLibrary.CreateEndings();
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

        /// <summary>
        /// Hard boundary on where the generator may write. Tests pass a subfolder; nothing can
        /// direct writes outside the placeholder tree.
        /// </summary>
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
                .Validate(cards, endings, PlaceholderContentLibrary.OpeningCardId);

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

        /// <summary>
        /// Loads whatever already sits at each target path and refuses to continue if any of it is
        /// not generator-owned. Runs before batching so the loads are reliable.
        /// </summary>
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
                    "an asset at a target path is not generated placeholder content; " +
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

            if (!HasPlaceholderLabel(asset))
            {
                report.RecordSkipped(assetPath, "not labelled " + PlaceholderLabel);
                report.RecordError(
                    "Refusing to overwrite '" + assetPath + "': it is not generated placeholder " +
                    "content. Move or delete it, or add the " + PlaceholderLabel + " label.");
                return false;
            }

            existing[assetPath] = asset;
            return true;
        }

        private static bool HasPlaceholderLabel(UnityEngine.Object asset)
        {
            string[] labels = AssetDatabase.GetLabels(asset);
            for (int i = 0; i < labels.Length; i++)
            {
                if (string.Equals(labels[i], PlaceholderLabel, StringComparison.Ordinal))
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
                // Always paired, so a throw mid-write cannot leave the database batching forever.
                AssetDatabase.StopAssetEditing();
            }

            ApplyLabels(persisted);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Stamps the ownership label on every asset the run touched.
        /// </summary>
        /// <remarks>
        /// Deliberately outside the Start/StopAssetEditing block. Labels live in the .meta file and
        /// are written through the importer, which is suspended while asset editing is batched — a
        /// SetLabels call inside the batch is silently dropped, which would leave the next run
        /// treating its own output as hand-authored content and refusing to proceed.
        ///
        /// Only missing labels are written, so a repeat run does not touch a single .meta file.
        /// </remarks>
        private static void ApplyLabels(List<UnityEngine.Object> assets)
        {
            for (int i = 0; i < assets.Count; i++)
            {
                UnityEngine.Object asset = assets[i];

                if (asset != null && !HasPlaceholderLabel(asset))
                {
                    AssetDatabase.SetLabels(asset, new[] { PlaceholderLabel });
                }
            }
        }

        /// <summary>
        /// Writes one asset, updating in place when it already exists.
        /// </summary>
        /// <remarks>
        /// Deliberately not delete-and-recreate: that would mint a new GUID on every run and break
        /// the catalogue's references, plus any Inspector wiring the team has done.
        /// </remarks>
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
            string openingCardId = PlaceholderContentLibrary.OpeningCardId;

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

        /// <summary>
        /// Compares serialized state so an unchanged asset is never re-saved, which is what makes a
        /// second run leave the working tree clean.
        /// </summary>
        private static bool SerializedContentMatches(
            ScriptableObject target,
            ScriptableObject source)
        {
            return string.Equals(
                EditorJsonUtility.ToJson(target),
                EditorJsonUtility.ToJson(source),
                StringComparison.Ordinal);
        }

        /// <summary>
        /// The catalogue is compared by reference identity rather than serialized JSON, because its
        /// arrays hold asset references whose serialized form is not stable to compare directly.
        /// </summary>
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

        /// <summary>
        /// Releases the in-memory instances that did not become assets. Without this, every aborted
        /// run would leak twenty-nine ScriptableObjects into the Editor session.
        /// </summary>
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
                Debug.Log("[Placeholder Content] " + report.Messages[i]);
            }

            string summary = "[Placeholder Content] " + report;

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
