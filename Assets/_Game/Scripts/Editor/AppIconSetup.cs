using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Wires the pre-rendered Android launcher icon layers (generated from
    /// <c>AppIconSource.png</c>) into <see cref="PlayerSettings"/> for Legacy, Round and
    /// Adaptive icon kinds. The PNGs themselves are baked ahead of time (see
    /// <c>Assets/_Game/Art/Branding/AppIcon/</c>) so this tool only has to assign textures
    /// to the icon slots that Unity's Android module already expects; it never touches
    /// package name, signing, version, or scripting-backend settings.
    /// </summary>
    /// <remarks>
    /// Idempotent: re-running just reassigns the same textures to the same slots.
    /// Adaptive foreground layers were pre-scaled to sit inside Android's 66dp/108dp
    /// safe-zone circle so the hand and ring survive circular/squircle launcher masks;
    /// see the generation notes in <c>Assets/_Game/Art/Branding/AppIcon/</c>.
    ///
    /// <c>UnityEditor.Android.AndroidPlatformIconKind</c> lives in the Android module's Editor
    /// extension assembly (<c>UnityEditor.Android.Extensions.dll</c> under
    /// <c>PlaybackEngines/AndroidPlayer/</c>), which only exists on disk when Android Build
    /// Support is installed for that Editor. A direct <c>using UnityEditor.Android;</c> reference
    /// fails to compile in any Editor install missing that module — this project is pinned to
    /// 6000.3.18f1, which currently has no Android module installed on this machine, even though
    /// PlayerSettings icon assignment itself (<see cref="PlatformIconKind"/>, <see cref="PlatformIcon"/>,
    /// <c>Get/SetPlatformIcons</c>) is core, module-agnostic API. So the three Android-specific
    /// icon-kind values are looked up via reflection instead, which compiles regardless of whether
    /// the module is installed and still resolves correctly whenever it is.
    /// </remarks>
    public static class AppIconSetup
    {
        private const string IconRoot = "Assets/_Game/Art/Branding/AppIcon";
        private const string AndroidPlatformIconKindTypeName = "UnityEditor.Android.AndroidPlatformIconKind";

        private static readonly int[] LegacyRoundSizes = { 192, 144, 96, 72, 48, 36 };
        private static readonly int[] AdaptiveSizes = { 432, 324, 216, 162, 108, 81 };

        [MenuItem("Tools/Royal Decisions/Configure Android App Icon")]
        public static void Configure()
        {
            if (!TryGetAndroidPlatformIconKinds(out var legacy, out var round, out var adaptive))
            {
                Debug.LogError("[AppIconSetup] The Android module (Build Support) is not installed in this " +
                    "Unity Editor, so Android launcher icon kinds are unavailable. Install Android Build " +
                    "Support via Unity Hub, then re-run Tools > Royal Decisions > Configure Android App Icon.");
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            ConfigureImporters();

            var androidTarget = NamedBuildTarget.Android;

            // Legacy/Round are still shipped alongside Adaptive because AndroidMinSdkVersion is 25
            // (below the API 26 floor where adaptive icons exist), so real fallback icons are needed
            // for those older devices instead of relying on Unity's synthesized ones.
            ApplyLegacyOrRound(androidTarget, legacy, "Legacy");
            ApplyLegacyOrRound(androidTarget, round, "Round");
            ApplyAdaptive(androidTarget, adaptive);

            AssetDatabase.SaveAssets();
            Debug.Log("[AppIconSetup] Android launcher icon configured (Legacy, Round, Adaptive).");
        }

        private static bool TryGetAndroidPlatformIconKinds(out PlatformIconKind legacy, out PlatformIconKind round, out PlatformIconKind adaptive)
        {
            legacy = round = adaptive = null;

            Type kindType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                kindType = assembly.GetType(AndroidPlatformIconKindTypeName);
                if (kindType != null)
                {
                    break;
                }
            }

            if (kindType == null)
            {
                return false;
            }

            legacy = GetStaticPlatformIconKind(kindType, "Legacy");
            round = GetStaticPlatformIconKind(kindType, "Round");
            adaptive = GetStaticPlatformIconKind(kindType, "Adaptive");
            return legacy != null && round != null && adaptive != null;
        }

        private static PlatformIconKind GetStaticPlatformIconKind(Type kindType, string fieldName)
        {
            var field = kindType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            return field?.GetValue(null) as PlatformIconKind;
        }

        private static void ApplyLegacyOrRound(NamedBuildTarget target, PlatformIconKind kind, string prefix)
        {
            var icons = PlayerSettings.GetPlatformIcons(target, kind);
            if (icons == null || icons.Length == 0)
            {
                Debug.LogWarning($"[AppIconSetup] No {prefix} icon slots reported by PlayerSettings; skipping.");
                return;
            }

            foreach (var icon in icons)
            {
                var size = icon.width;
                if (!LegacyRoundSizes.Contains(size))
                {
                    Debug.LogWarning($"[AppIconSetup] Unexpected {prefix} icon size {size}px; no generated asset for it.");
                    continue;
                }

                var texture = LoadTexture($"{IconRoot}/{prefix}_{size}.png");
                if (texture == null)
                {
                    continue;
                }

                icon.SetTextures(new[] { texture });
            }

            PlayerSettings.SetPlatformIcons(target, kind, icons);
        }

        private static void ApplyAdaptive(NamedBuildTarget target, PlatformIconKind adaptive)
        {
            var icons = PlayerSettings.GetPlatformIcons(target, adaptive);
            if (icons == null || icons.Length == 0)
            {
                Debug.LogWarning("[AppIconSetup] No Adaptive icon slots reported by PlayerSettings; skipping.");
                return;
            }

            foreach (var icon in icons)
            {
                var size = icon.width;
                if (!AdaptiveSizes.Contains(size))
                {
                    Debug.LogWarning($"[AppIconSetup] Unexpected Adaptive icon size {size}px; no generated asset for it.");
                    continue;
                }

                var foreground = LoadTexture($"{IconRoot}/AdaptiveForeground_{size}.png");
                var background = LoadTexture($"{IconRoot}/AdaptiveBackground_{size}.png");
                if (foreground == null || background == null)
                {
                    continue;
                }

                // Index 0 = foreground layer, index 1 = background layer.
                icon.SetTextures(new[] { foreground, background });
            }

            PlayerSettings.SetPlatformIcons(target, adaptive, icons);
        }

        private static Texture2D LoadTexture(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                Debug.LogError($"[AppIconSetup] Missing generated icon asset: {path}");
            }

            return texture;
        }

        private static void ConfigureImporters()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconRoot });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                var isForeground = path.Contains("AdaptiveForeground_");

                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.alphaIsTransparency = isForeground;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.crunchedCompression = false;

                var platformSettings = importer.GetDefaultPlatformTextureSettings();
                platformSettings.format = TextureImporterFormat.RGBA32;
                platformSettings.compressionQuality = 100;
                platformSettings.overridden = false;
                importer.SetPlatformTextureSettings(platformSettings);

                importer.SaveAndReimport();
            }
        }
    }
}
