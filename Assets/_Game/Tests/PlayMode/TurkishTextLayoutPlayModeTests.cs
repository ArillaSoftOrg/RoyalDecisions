using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoyalDecisions.Tests.PlayMode
{
    [TestFixture]
    public class TurkishTextLayoutPlayModeTests
    {
        private GameObject root;
        private TMP_FontAsset font;

        [SetUp]
        public void SetUp()
        {
            font = Resources.Load<TMP_FontAsset>("LiberationSans-Turkish SDF");
            root = new GameObject("SafeArea", typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1080f, 1920f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(root);
            root = null;
            font = null;
        }

        [UnityTest]
        public IEnumerator DialogueChoiceSpeakerAndEndingStayAboveTheirMinimumSizes()
        {
            Assert.That(font, Is.Not.Null, "The project-owned Turkish TMP font is required.");

            TextMeshProUGUI dialogue = CreateText(
                "Dialogue", root.transform, new Vector2(760f, 360f), 46f, 34f, 46f);
            dialogue.text =
                "Halk uzun süren kışın ardından sarayın tahıl ambarlarını açmasını istiyor. " +
                "Hazinedar bunun bütün dengeleri değiştireceğini, muhafızlar ise gecikmenin " +
                "şehir meydanında yeni bir kargaşa çıkaracağını söylüyor.";

            TextMeshProUGUI speaker = CreateText(
                "Speaker", root.transform, new Vector2(760f, 96f), 36f, 32f, 38f);
            speaker.text = "[GEÇİCİ] Doğu Hudutlarının Başmuhafızı";

            TextMeshProUGUI choice = CreateText(
                "Choice", root.transform, new Vector2(400f, 180f), 38f, 34f, 40f);
            choice.text = "Ambarları halka hemen aç";

            TextMeshProUGUI ending = CreateText(
                "Ending", root.transform, new Vector2(850f, 300f), 38f, 34f, 42f);
            ending.text =
                "Hükümdarlığınız sona erdi. Çığ, öğüt, şüphe, İmparator, özgürlük ve " +
                "güvenlik üzerine verilen kararlar tarih defterine kaydedildi.";

            yield return null;

            AssertReadable(dialogue, 34f);
            AssertReadable(speaker, 32f);
            AssertReadable(choice, 34f);
            AssertReadable(ending, 34f);
        }

        [UnityTest]
        public IEnumerator DialogueRemainsInsideSafeAreaAtBothThresholdRotations()
        {
            Assert.That(font, Is.Not.Null, "The project-owned Turkish TMP font is required.");
            RectTransform card = CreateRect("Card", root.transform, new Vector2(880f, 1300f));
            TextMeshProUGUI dialogue = CreateText(
                "Dialogue", card, new Vector2(760f, 360f), 46f, 34f, 46f);
            dialogue.rectTransform.anchoredPosition = new Vector2(0f, -260f);
            dialogue.text =
                "Gözcüler doğu geçitlerinde hareketlilik bildirdi. Halk güvenlik isterken " +
                "hazine yeni birliklerin masrafını karşılayamayacağımızı söylüyor.";

            yield return null;
            dialogue.ForceMeshUpdate();

            card.anchoredPosition = new Vector2(-90f, 0f);
            card.localRotation = Quaternion.Euler(0f, 0f, 8f);
            AssertContained(dialogue.rectTransform, (RectTransform)root.transform);

            card.anchoredPosition = new Vector2(90f, 0f);
            card.localRotation = Quaternion.Euler(0f, 0f, -8f);
            AssertContained(dialogue.rectTransform, (RectTransform)root.transform);
        }

        private TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            Vector2 size,
            float target,
            float minimum,
            float maximum)
        {
            RectTransform rect = CreateRect(name, parent, size);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = target;
            text.enableAutoSizing = true;
            text.fontSizeMin = minimum;
            text.fontSizeMax = maximum;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 size)
        {
            GameObject instance = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)instance.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        private static void AssertReadable(TextMeshProUGUI text, float minimum)
        {
            text.ForceMeshUpdate();
            Assert.That(text.isTextOverflowing, Is.False, text.name + " overflowed.");
            Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(minimum),
                text.name + " dropped below its safe minimum.");
        }

        private static void AssertContained(RectTransform child, RectTransform parent)
        {
            Vector3[] childCorners = new Vector3[4];
            Vector3[] parentCorners = new Vector3[4];
            child.GetWorldCorners(childCorners);
            parent.GetWorldCorners(parentCorners);
            for (int i = 0; i < childCorners.Length; i++)
            {
                Assert.That(childCorners[i].x,
                    Is.InRange(parentCorners[0].x, parentCorners[2].x));
                Assert.That(childCorners[i].y,
                    Is.InRange(parentCorners[0].y, parentCorners[2].y));
            }
        }
    }
}
