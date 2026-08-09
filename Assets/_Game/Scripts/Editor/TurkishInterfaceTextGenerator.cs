using System;
using System.IO;
using RoyalDecisions.Data;
using UnityEditor;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    /// <summary>Creates or updates the project-owned Turkish interface text asset in place.</summary>
    public static class TurkishInterfaceTextGenerator
    {
        public const string Root = "Assets/_Game/Content/Interface";
        public const string AssetPath = Root + "/TurkishInterfaceText.asset";
        public const string OwnershipLabel = "RoyalDecisions.InterfaceText";

        [MenuItem("Tools/Royal Decisions/Generate Turkish Interface Text")]
        public static void GenerateMenu()
        {
            InterfaceTextDefinition result = Generate(AssetPath);
            Debug.Log("[TurkishInterfaceText] Ready: " + AssetDatabase.GetAssetPath(result), result);
        }

        public static InterfaceTextDefinition Generate(string path)
        {
            string normalized = (path ?? string.Empty).Replace('\\', '/');
            if (!normalized.StartsWith(Root + "/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Interface text may only be generated under " + Root, nameof(path));
            }

            EnsureFolder(Path.GetDirectoryName(normalized)?.Replace('\\', '/'));
            InterfaceTextDefinition source = TurkishInterfaceTextLibrary.Create();
            InterfaceTextDefinition existing = AssetDatabase.LoadAssetAtPath<InterfaceTextDefinition>(normalized);

            if (AssetDatabase.LoadMainAssetAtPath(normalized) != null && existing == null)
            {
                UnityEngine.Object.DestroyImmediate(source);
                throw new InvalidOperationException("The interface-text path is occupied by another asset type.");
            }

            if (existing != null && !HasOwnershipLabel(existing))
            {
                UnityEngine.Object.DestroyImmediate(source);
                throw new InvalidOperationException("Refusing to overwrite unlabelled interface text at " + normalized);
            }

            if (existing == null)
            {
                AssetDatabase.CreateAsset(source, normalized);
                existing = source;
            }
            else
            {
                string before = EditorJsonUtility.ToJson(existing);
                string after = EditorJsonUtility.ToJson(source);
                if (!string.Equals(before, after, StringComparison.Ordinal))
                {
                    EditorUtility.CopySerialized(source, existing);
                    EditorUtility.SetDirty(existing);
                }
                UnityEngine.Object.DestroyImmediate(source);
            }

            if (!HasOwnershipLabel(existing))
            {
                AssetDatabase.SetLabels(existing, new[] { OwnershipLabel });
            }

            AssetDatabase.SaveAssets();
            return existing;
        }

        private static bool HasOwnershipLabel(UnityEngine.Object asset)
        {
            string[] labels = AssetDatabase.GetLabels(asset);
            for (int i = 0; i < labels.Length; i++)
            {
                if (string.Equals(labels[i], OwnershipLabel, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
