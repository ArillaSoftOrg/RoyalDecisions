using RoyalDecisions.Data;
using UnityEditor;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    [CustomEditor(typeof(CardDefinition))]
    public sealed class CardDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty id = serializedObject.FindProperty("id");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(id);
            }
            DrawPropertiesExcluding(serializedObject, "m_Script", "id");
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();
            if (GUILayout.Button("Open in Royal Decisions Content Authoring"))
            {
                ContentAuthoringWindow.Open((CardDefinition)target);
            }
        }
    }
}
