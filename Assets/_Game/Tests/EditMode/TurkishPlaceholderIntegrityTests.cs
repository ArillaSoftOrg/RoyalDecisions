using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Editor;
using UnityEditor;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class TurkishPlaceholderIntegrityTests
    {
        private const string Root = PlaceholderContentGenerator.DefaultRoot + "/__TurkishIntegrity";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Root);
        }

        [Test]
        public void TranslationUpdatePreservesEveryGuidAndGameplayField()
        {
            PlaceholderContentGenerator.Generate(Root);
            Dictionary<string, string> guids = CaptureGuids();
            Dictionary<string, string> fingerprints = CaptureFingerprints();

            ReplaceDisplayTextOnly();
            ContentGenerationReport report = PlaceholderContentGenerator.Generate(Root);

            Assert.That(report.Aborted, Is.False, report.ToString());
            Assert.That(report.Updated, Is.EqualTo(28), report.ToString());
            Assert.That(CaptureGuids(), Is.EquivalentTo(guids));
            Assert.That(CaptureFingerprints(), Is.EquivalentTo(fingerprints));

            CardDefinition opening = AssetDatabase.LoadAssetAtPath<CardDefinition>(
                Root + "/Cards/" + PlaceholderContentLibrary.OpeningCardId + ".asset");
            Assert.That(opening.Speaker, Does.StartWith(PlaceholderContentLibrary.PlaceholderTag));
            Assert.That(opening.BodyText, Does.Contain("Taç"));
        }

        private static Dictionary<string, string> CaptureGuids()
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { Root });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith(".asset", StringComparison.Ordinal))
                {
                    result[path] = guids[i];
                }
            }
            Assert.That(result.Count, Is.EqualTo(29));
            return result;
        }

        private static Dictionary<string, string> CaptureFingerprints()
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] cardGuids = AssetDatabase.FindAssets("t:CardDefinition", new[] { Root });
            for (int i = 0; i < cardGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(cardGuids[i]);
                result[path] = Fingerprint(AssetDatabase.LoadAssetAtPath<CardDefinition>(path));
            }
            string[] endingGuids = AssetDatabase.FindAssets("t:EndingDefinition", new[] { Root });
            for (int i = 0; i < endingGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(endingGuids[i]);
                EndingDefinition ending = AssetDatabase.LoadAssetAtPath<EndingDefinition>(path);
                result[path] = string.Join("|", ending.Id, (int)ending.TriggerStat,
                    (int)ending.Boundary, ending.Priority,
                    ending.Image != null ? AssetDatabase.GetAssetPath(ending.Image) : string.Empty);
            }
            return result;
        }

        private static string Fingerprint(CardDefinition card)
        {
            StringBuilder value = new StringBuilder();
            value.Append(card.Id).Append('|').Append(card.SelectionWeight).Append('|')
                .Append(card.OncePerRun).Append('|').Append(card.CooldownTurns).Append('|')
                .Append(card.ForcedNextCardId).Append('|');
            AppendChoice(value, card.LeftChoice);
            AppendChoice(value, card.RightChoice);
            AppendList(value, card.Conditions.RequiredFlags);
            AppendList(value, card.Conditions.ForbiddenFlags);
            for (int i = 0; i < card.Conditions.StatRanges.Count; i++)
            {
                StatRange range = card.Conditions.StatRanges[i];
                value.Append((int)range.Stat).Append(':').Append(range.Min).Append(':')
                    .Append(range.Max).Append(';');
            }
            return value.ToString();
        }

        private static void AppendChoice(StringBuilder value, ChoiceDefinition choice)
        {
            value.Append(choice.Deltas.Authority).Append(',').Append(choice.Deltas.People)
                .Append(',').Append(choice.Deltas.Security).Append(',').Append(choice.Deltas.Wealth)
                .Append('|').Append(choice.ForcedNextCardId).Append('|').Append(choice.AudioEventId)
                .Append('|');
            AppendList(value, choice.FlagsToAdd);
            AppendList(value, choice.FlagsToRemove);
        }

        private static void AppendList<T>(StringBuilder value, IReadOnlyList<T> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                value.Append(items[i]).Append(',');
            }
            value.Append('|');
        }

        private static void ReplaceDisplayTextOnly()
        {
            string[] cardGuids = AssetDatabase.FindAssets("t:CardDefinition", new[] { Root });
            for (int i = 0; i < cardGuids.Length; i++)
            {
                CardDefinition card = AssetDatabase.LoadAssetAtPath<CardDefinition>(
                    AssetDatabase.GUIDToAssetPath(cardGuids[i]));
                SerializedObject serialized = new SerializedObject(card);
                serialized.FindProperty("speaker").stringValue = "Legacy speaker";
                serialized.FindProperty("bodyText").stringValue = "Legacy body";
                serialized.FindProperty("leftChoice").FindPropertyRelative("previewText").stringValue =
                    "Legacy left";
                serialized.FindProperty("rightChoice").FindPropertyRelative("previewText").stringValue =
                    "Legacy right";
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            string[] endingGuids = AssetDatabase.FindAssets("t:EndingDefinition", new[] { Root });
            for (int i = 0; i < endingGuids.Length; i++)
            {
                EndingDefinition ending = AssetDatabase.LoadAssetAtPath<EndingDefinition>(
                    AssetDatabase.GUIDToAssetPath(endingGuids[i]));
                SerializedObject serialized = new SerializedObject(ending);
                serialized.FindProperty("title").stringValue = "Legacy title";
                serialized.FindProperty("bodyText").stringValue = "Legacy ending";
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
        }
    }
}
