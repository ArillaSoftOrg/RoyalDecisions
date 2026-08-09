using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    public sealed class UIFoundationViewTests
    {
        private GameObject root;
        private GameUITheme theme;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Stat", typeof(RectTransform));
            theme = ScriptableObject.CreateInstance<GameUITheme>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void NullIconUsesSemanticFallbackAndNumericValue()
        {
            Image fill = Child<Image>("Fill");
            Image icon = Child<Image>("Icon");
            TMP_Text name = Child<TextMeshProUGUI>("Name");
            TMP_Text fallback = Child<TextMeshProUGUI>("Fallback");
            TMP_Text value = Child<TextMeshProUGUI>("Value");
            TMP_Text impact = Child<TextMeshProUGUI>("Impact");
            CanvasGroup impactGroup = impact.gameObject.AddComponent<CanvasGroup>();
            TMP_Text critical = Child<TextMeshProUGUI>("Critical");
            StatItemView view = root.AddComponent<StatItemView>();
            view.SetAuthoringReferences(StatType.People, fill, icon, name, null, null, 0f,
                value, fallback, impact, impactGroup, critical);

            view.ApplyTheme(theme);
            view.SetValue(42);

            Assert.That(icon.enabled, Is.False);
            Assert.That(fallback.gameObject.activeSelf, Is.True);
            Assert.That(fallback.text, Is.EqualTo("P"));
            Assert.That(name.text, Is.EqualTo("People"));
            Assert.That(view.DisplayedValue, Is.EqualTo("42"));
            Assert.That(fill.fillAmount, Is.EqualTo(0.42f).Within(0.001f));
        }

        [Test]
        public void ImpactAndCriticalIndicatorsArePresentationOnly()
        {
            Image fill = Child<Image>("Fill");
            TMP_Text impact = Child<TextMeshProUGUI>("Impact");
            CanvasGroup group = impact.gameObject.AddComponent<CanvasGroup>();
            TMP_Text critical = Child<TextMeshProUGUI>("Critical");
            StatItemView view = root.AddComponent<StatItemView>();
            view.SetAuthoringReferences(StatType.Authority, fill, null, null, null, null, 0f,
                null, null, impact, group, critical);
            view.ApplyTheme(theme);

            view.ShowImpact(-10, 0.5f);
            Assert.That(view.ImpactText, Is.EqualTo("▼▼"));
            Assert.That(group.alpha, Is.EqualTo(0.5f));

            view.SetValue(10);
            Assert.That(view.IsCritical, Is.True);
            view.SetValue(50);
            Assert.That(view.IsCritical, Is.False);

            view.ClearImpact();
            Assert.That(view.ImpactText, Is.Empty);
            Assert.That(group.alpha, Is.Zero);
        }

        private T Child<T>(string name) where T : Component
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(T));
            child.transform.SetParent(root.transform, false);
            return child.GetComponent<T>();
        }
    }
}
