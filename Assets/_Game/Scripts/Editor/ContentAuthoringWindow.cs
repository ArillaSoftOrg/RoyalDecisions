using System;
using System.Collections.Generic;
using System.IO;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using UnityEditor;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    /// <summary>Small, Undo-safe authoring surface over existing ScriptableObjects.</summary>
    public sealed class ContentAuthoringWindow : EditorWindow
    {
        private const string CardFolder = "Assets/_Game/Content/Cards";
        private ContentCatalogue catalogue;
        private CardDefinition selectedCard;
        private SerializedObject selectedSerialized;
        private string search = string.Empty;
        private string speakerFilter = string.Empty;
        private string newCardId = "card_new";
        private Vector2 listScroll;
        private Vector2 editorScroll;

        [MenuItem("Tools/Royal Decisions/Content Authoring")]
        public static void OpenMenu() => GetWindow<ContentAuthoringWindow>("RD Content");

        public static void Open(CardDefinition card)
        {
            ContentAuthoringWindow window = GetWindow<ContentAuthoringWindow>("RD Content");
            window.Select(card);
            window.Show();
        }

        private void OnGUI()
        {
            catalogue = (ContentCatalogue)EditorGUILayout.ObjectField(
                "Catalogue", catalogue, typeof(ContentCatalogue), false);
            search = EditorGUILayout.TextField("ID search", search);
            speakerFilter = EditorGUILayout.TextField("Speaker filter", speakerFilter);
            DrawCreateControls();
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawCardList();
                DrawCardEditor();
            }
            DrawValidation();
        }

        private void DrawCreateControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                newCardId = EditorGUILayout.TextField("New card ID", newCardId);
                using (new EditorGUI.DisabledScope(
                    catalogue == null || string.IsNullOrWhiteSpace(newCardId)))
                {
                    if (GUILayout.Button("Create", GUILayout.Width(90f)))
                    {
                        CreateCard(newCardId.Trim());
                    }
                }
            }
        }

        private void DrawCardList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(250f)))
            {
                listScroll = EditorGUILayout.BeginScrollView(listScroll);
                if (catalogue != null)
                {
                    for (int i = 0; i < catalogue.Cards.Count; i++)
                    {
                        CardDefinition card = catalogue.Cards[i];
                        if (!Matches(card))
                        {
                            continue;
                        }
                        if (GUILayout.Toggle(card == selectedCard, card.Id, "Button"))
                        {
                            Select(card);
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawCardEditor()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (selectedCard == null || selectedSerialized == null)
                {
                    EditorGUILayout.HelpBox("Select a card to edit.", MessageType.Info);
                    return;
                }
                editorScroll = EditorGUILayout.BeginScrollView(editorScroll);
                selectedSerialized.Update();
                SerializedProperty iterator = selectedSerialized.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.name == "m_Script")
                    {
                        continue;
                    }
                    using (new EditorGUI.DisabledScope(iterator.name == "id"))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                }
                if (selectedSerialized.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(selectedCard);
                }
                DrawLinks();
                if (GUILayout.Button("Ping asset"))
                {
                    EditorGUIUtility.PingObject(selectedCard);
                    Selection.activeObject = selectedCard;
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawLinks()
        {
            if (catalogue == null || selectedCard == null)
            {
                return;
            }
            ContentLinkIndex links = new ContentLinkIndex(catalogue.Cards);
            EditorGUILayout.LabelField("Incoming", string.Join(", ", links.GetIncoming(selectedCard.Id)));
            EditorGUILayout.LabelField("Outgoing", string.Join(", ", links.GetOutgoing(selectedCard.Id)));
        }

        private void DrawValidation()
        {
            ContentValidationReport report = ProjectContentAudit.Validate(catalogue);
            MessageType type = report.HasErrors
                ? MessageType.Error
                : report.HasWarnings ? MessageType.Warning : MessageType.Info;
            EditorGUILayout.HelpBox(report.ToString(), type);
            for (int i = 0; i < report.Issues.Count; i++)
            {
                ContentValidationIssue issue = report.Issues[i];
                if (selectedCard == null || string.IsNullOrEmpty(issue.SubjectId)
                    || issue.SubjectId == selectedCard.Id)
                {
                    EditorGUILayout.LabelField(issue.ToString(), EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        private bool Matches(CardDefinition card)
        {
            return card != null
                && (string.IsNullOrEmpty(search)
                    || card.Id.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                && (string.IsNullOrEmpty(speakerFilter)
                    || card.Speaker.IndexOf(speakerFilter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void Select(CardDefinition card)
        {
            selectedCard = card;
            selectedSerialized = card != null ? new SerializedObject(card) : null;
            Repaint();
        }

        private void CreateCard(string id)
        {
            for (int i = 0; i < catalogue.Cards.Count; i++)
            {
                if (catalogue.Cards[i] != null
                    && string.Equals(catalogue.Cards[i].Id, id, StringComparison.Ordinal))
                {
                    EditorUtility.DisplayDialog("Duplicate card ID", id + " already exists.", "OK");
                    return;
                }
            }
            EnsureFolder(CardFolder);
            CardDefinition card = CreateInstance<CardDefinition>();
            card.SetAuthoringData(
                id, string.Empty, string.Empty,
                new ChoiceDefinition(), new ChoiceDefinition());
            string safeName = SanitizeFileName(id);
            string path = AssetDatabase.GenerateUniqueAssetPath(
                CardFolder + "/" + safeName + ".asset");
            AssetDatabase.CreateAsset(card, path);
            Undo.RegisterCreatedObjectUndo(card, "Create Royal Decisions card");
            AddCardToCatalogue(card);
            AssetDatabase.SaveAssets();
            Select(card);
        }

        private void AddCardToCatalogue(CardDefinition card)
        {
            List<CardDefinition> cards = new List<CardDefinition>(catalogue.Cards) { card };
            cards.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
            SerializedObject serializedCatalogue = new SerializedObject(catalogue);
            SerializedProperty cardsProperty = serializedCatalogue.FindProperty("cards");
            Undo.RecordObject(catalogue, "Add card to catalogue");
            cardsProperty.arraySize = cards.Count;
            for (int i = 0; i < cards.Count; i++)
            {
                cardsProperty.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            }
            serializedCatalogue.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalogue);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static string SanitizeFileName(string id)
        {
            string safe = id;
            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
            {
                safe = safe.Replace(invalid[i], '_');
            }
            return string.IsNullOrEmpty(safe) ? "Card" : safe;
        }
    }
}
