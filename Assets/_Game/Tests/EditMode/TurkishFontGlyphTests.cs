using NUnit.Framework;
using RoyalDecisions.Editor;
using TMPro;
using UnityEditor;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class TurkishFontGlyphTests
    {
        [Test]
        public void ProjectOwnedStaticFontContainsEveryRequiredTurkishGlyph()
        {
            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                TurkishGlyphValidator.FontAssetPath);

            Assert.That(asset, Is.Not.Null,
                "Run Tools > Royal Decisions > Generate Turkish TMP Font.");
            Assert.That(TurkishGlyphValidator.TryValidate(asset, out string message),
                Is.True, message);
            Assert.That(AssetDatabase.GetLabels(asset),
                Does.Contain(TurkishGlyphValidator.OwnershipLabel));
        }

        [Test]
        public void TurkishFontIsNotTheTextMeshProFallbackAsset()
        {
            string fallback =
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";
            Assert.That(TurkishGlyphValidator.FontAssetPath, Is.Not.EqualTo(fallback));
            Assert.That(AssetDatabase.AssetPathToGUID(TurkishGlyphValidator.FontAssetPath),
                Is.Not.EqualTo(AssetDatabase.AssetPathToGUID(fallback)));
        }
    }
}
