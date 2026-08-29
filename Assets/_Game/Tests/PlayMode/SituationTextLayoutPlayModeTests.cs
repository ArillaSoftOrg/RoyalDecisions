using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoyalDecisions.Tests.PlayMode
{
    /// <summary>
    /// Reproduces SituationText's exact box size and TMP configuration from
    /// <c>SceneSetupAutomation.ConfigureSituationArea</c> against real authored story text, so a
    /// margin/auto-size regression is caught here instead of only by eye in the Editor.
    /// </summary>
    [TestFixture]
    public class SituationTextLayoutPlayModeTests
    {
        // SituationPanel is full SafeArea width (1080 at reference resolution) — it now spans
        // edge-to-edge like ContentPanel, no side margins. SituationText's box is that minus its
        // configured margins. Kept in sync by hand with ConfigureSituationArea — if that method's
        // margins change, update these to match.
        private const float PanelWidth = 1080f;
        private const float PanelHeight = 160f;
        private const float HorizontalMarginEachSide = 90f;
        private const float VerticalMarginEachSide = 26f;
        private const float TextWidth = PanelWidth - (HorizontalMarginEachSide * 2f);
        private const float TextHeight = PanelHeight - (VerticalMarginEachSide * 2f);

        private const float FontSizeTarget = 36f;
        private const float FontSizeMin = 22f;
        private const float FontSizeMax = 40f;
        private const float LineSpacing = 2f;

        private const string OneLine = "Sinyal söner.";
        private const string ThreeLines =
            "Kemal ya da Mustafa devam eder — ya yeni bölme kararı ya da otorite sonrası " +
            "gerginlik. Sağlam mı çözelim, geçici mi geçelim?";
        private const string FourLines =
            "Sığınağın kaderi K1'den beri birikmiş tüm bayrakların toplamına bağlı: kaçıncı " +
            "liderdesiniz, hangi ittifaklar kuruldu, Vertak'la ilişki nasıl, zombilerle ateşkes " +
            "mi savaş mı — hepsi burada birleşiyor. Bu bir final değildir.";

        private GameObject root;
        private TMP_FontAsset font;

        [SetUp]
        public void SetUp()
        {
            font = Resources.Load<TMP_FontAsset>("LiberationSans-Turkish SDF");
            root = new GameObject("SituationText", typeof(RectTransform));
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(root);
            root = null;
            font = null;
        }

        [UnityTest]
        public IEnumerator AShortOneLineBodyFitsComfortably()
        {
            yield return AssertFits(OneLine);
        }

        [UnityTest]
        public IEnumerator ATypicalThreeLineBodyFitsWithoutOverflowing()
        {
            yield return AssertFits(ThreeLines);
        }

        [UnityTest]
        public IEnumerator ALongFourLineBodyFitsWithoutOverflowing()
        {
            yield return AssertFits(FourLines);
        }

        [UnityTest]
        public IEnumerator TextNeverExceedsItsConfiguredMaximumFontSize()
        {
            TextMeshProUGUI text = CreateText(OneLine);
            yield return null;
            text.ForceMeshUpdate();

            Assert.That(text.fontSize, Is.LessThanOrEqualTo(FontSizeMax));
        }

        private IEnumerator AssertFits(string body)
        {
            Assert.That(font, Is.Not.Null, "The project-owned Turkish TMP font is required.");
            TextMeshProUGUI text = CreateText(body);

            yield return null;
            text.ForceMeshUpdate();

            Assert.That(text.isTextOverflowing, Is.False,
                "SituationText overflowed its box for: " + body);
            Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(FontSizeMin),
                "auto-size dropped below its configured floor for: " + body);
        }

        private TextMeshProUGUI CreateText(string body)
        {
            RectTransform rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(TextWidth, TextHeight);

            TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = FontSizeTarget;
            text.enableAutoSizing = true;
            text.fontSizeMin = FontSizeMin;
            text.fontSizeMax = FontSizeMax;
            text.lineSpacing = LineSpacing;
            text.textWrappingMode = TextWrappingModes.Normal;
            // Overflow, not Ellipsis: the production panel uses Ellipsis so long text degrades
            // gracefully instead of spilling past the parchment, but that would make
            // isTextOverflowing unreliable here — Overflow lets a too-tall layout still report
            // itself as overflowing so this test can catch it.
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.Center;
            text.text = body;
            return text;
        }
    }
}
