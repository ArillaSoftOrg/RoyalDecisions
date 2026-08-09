using System;
using System.Globalization;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace RoyalDecisions.Editor
{
    /// <summary>Creates and validates the project-owned static TMP font used by Turkish UI.</summary>
    public static class TurkishGlyphValidator
    {
        public const string Probe = "Çığ, öğüt, şüphe, İmparator, özgürlük ve güvenlik";
        public const string RequiredTurkishCharacters = "ÇĞİÖŞÜçğıiöşü";
        public const string FontRoot = "Assets/_Game/Art/Fonts/Resources";
        public const string SourceFontPath = FontRoot + "/LiberationSans-Turkish.ttf";
        public const string LicensePath = FontRoot + "/LiberationSans-Turkish-OFL.txt";
        public const string FontAssetPath = FontRoot + "/LiberationSans-Turkish SDF.asset";
        public const string OwnershipLabel = "RoyalDecisions.TurkishFont";

        private const string OriginalSourcePath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
        private const string OriginalLicensePath = "Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt";

        [MenuItem("Tools/Royal Decisions/Generate Turkish TMP Font")]
        public static void GenerateMenu()
        {
            TMP_FontAsset asset = EnsureFontAsset();
            if (!TryValidate(asset, out string message))
            {
                throw new InvalidOperationException(message);
            }
            Debug.Log("[TurkishFont] " + message, asset);
        }

        public static TMP_FontAsset EnsureFontAsset()
        {
            EnsureFolder(FontRoot);
            CopyIfMissing(OriginalSourcePath, SourceFontPath);
            CopyIfMissing(OriginalLicensePath, LicensePath);
            AssetDatabase.ImportAsset(SourceFontPath, ImportAssetOptions.ForceSynchronousImport);

            Font source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (source == null)
            {
                throw new InvalidOperationException("The project-owned Turkish font source could not be imported.");
            }

            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (AssetDatabase.LoadMainAssetAtPath(FontAssetPath) != null && asset == null)
            {
                throw new InvalidOperationException("The Turkish TMP font path is occupied by another asset type.");
            }

            if (asset != null && !HasOwnershipLabel(asset))
            {
                throw new InvalidOperationException("Refusing to alter an unlabelled TMP font asset.");
            }

            if (asset == null)
            {
                asset = TMP_FontAsset.CreateFontAsset(
                    source,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    2048,
                    2048,
                    AtlasPopulationMode.Dynamic,
                    false);
                if (asset == null)
                {
                    throw new InvalidOperationException("TMP could not create the Turkish font asset.");
                }

                asset.name = "LiberationSans-Turkish SDF";
                AssetDatabase.CreateAsset(asset, FontAssetPath);
                // Mark ownership before atlas population so a failed render remains safely
                // recoverable by this generator instead of becoming an unlabelled blocker.
                AssetDatabase.SetLabels(asset, new[] { OwnershipLabel });
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }

            asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            string characters = BuildCharacterSet();
            for (int i = 0; i < characters.Length; i++)
            {
                char character = characters[i];
                if (characters.IndexOf(character) != i || asset.HasCharacter(character, false, false))
                {
                    continue;
                }

                if (!asset.TryAddCharacters(character.ToString(), out string missing, false))
                {
                    throw new InvalidOperationException(
                        "The source font is missing required characters: " + FormatCharacters(missing));
                }
            }

            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            asset.isMultiAtlasTexturesEnabled = false;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SetLabels(asset, new[] { OwnershipLabel });
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static bool TryValidate(TMP_FontAsset asset, out string message)
        {
            if (asset == null)
            {
                message = "The Turkish TMP font asset is missing.";
                return false;
            }

            string required = Probe + RequiredTurkishCharacters;
            if (!asset.HasCharacters(required, out uint[] missing, false, false))
            {
                StringBuilder builder = new StringBuilder("Missing Turkish glyphs:");
                for (int i = 0; i < missing.Length; i++)
                {
                    builder.Append(' ').Append("U+")
                        .Append(missing[i].ToString("X4", CultureInfo.InvariantCulture));
                }
                message = builder.ToString();
                return false;
            }

            if (asset.atlasPopulationMode != AtlasPopulationMode.Static)
            {
                message = "The Turkish TMP font must ship with a static atlas.";
                return false;
            }

            message = "All required Turkish glyphs are present in the static project-owned atlas.";
            return true;
        }

        private static string BuildCharacterSet()
        {
            StringBuilder builder = new StringBuilder();
            for (char character = ' '; character <= '~'; character++)
            {
                builder.Append(character);
            }
            builder.Append(RequiredTurkishCharacters);
            builder.Append("âî’“”…–—");
            return builder.ToString();
        }

        private static string FormatCharacters(string characters)
        {
            if (string.IsNullOrEmpty(characters))
            {
                return "<unknown>";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < characters.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(characters[i]).Append(" (U+")
                    .Append(((int)characters[i]).ToString("X4", CultureInfo.InvariantCulture))
                    .Append(')');
            }
            return builder.ToString();
        }

        private static void CopyIfMissing(string source, string destination)
        {
            if (File.Exists(destination))
            {
                return;
            }
            if (!File.Exists(source))
            {
                throw new FileNotFoundException("Required font source is missing.", source);
            }
            FileUtil.CopyFileOrDirectory(source, destination);
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
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
