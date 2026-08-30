using NUnit.Framework;
using RoyalDecisions.Domain;
using RoyalDecisions.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Guards the settings panel against the "rows land on top of each other for the first few
    /// frames, then snap apart" opening glitch.
    /// </summary>
    /// <remarks>
    /// The panel is authored inactive and each tab is a chain of nested ContentSizeFitters, which
    /// Unity resolves about one level per frame once the hierarchy is enabled. The fixture below
    /// reproduces that chain in miniature — ScrollContent → tab → group card → two rows — and
    /// asserts the rows are already separated the instant <see cref="SettingsPanelView.Show"/>
    /// returns, rather than several frames later.
    /// </remarks>
    [TestFixture]
    public sealed class SettingsPanelLayoutTests
    {
        private const float RowHeight = 108f;

        private GameObject panelRoot;
        private SettingsPanelView view;
        private RectTransform firstRow;
        private RectTransform secondRow;

        [SetUp]
        public void SetUp()
        {
            panelRoot = PresentationTestObjects.CreateObject("SettingsPanel");
            RectTransform panelRect = (RectTransform)panelRoot.transform;
            panelRect.sizeDelta = new Vector2(1080f, 1920f);

            RectTransform scrollContent = CreateStack("ScrollContent", panelRect);
            RectTransform tab = CreateStack("AudioTab", scrollContent);
            RectTransform group = CreateStack("VolumeGroup", tab);

            firstRow = CreateRow("MasterVolume", group);
            secondRow = CreateRow("MusicVolume", group);

            AudioSettingsPanelView audioPanel = tab.gameObject.AddComponent<AudioSettingsPanelView>();

            view = PresentationTestObjects.CreateComponent<SettingsPanelView>("SettingsPanelView");
            view.SetAuthoringReferences(
                panelRoot, audioPanel, null, null, null,
                null, null, null, null, null, null, null);

            // The real panel is authored closed; that is precisely why nothing has been measured
            // by the time it is opened.
            panelRoot.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            PresentationTestObjects.DestroyAll();
        }

        [Test]
        public void Opening_SeparatesRowsImmediately()
        {
            view.Show(GameSettings.CreateDefault());

            Assert.That(panelRoot.activeSelf, Is.True, "the panel should be open");
            Assert.That(
                Overlaps(firstRow, secondRow), Is.False,
                "rows must be laid out on the frame the panel opens, not several frames later");
        }

        [Test]
        public void Opening_StacksRowsByTheirRowHeight()
        {
            view.Show(GameSettings.CreateDefault());

            float gap = Mathf.Abs(WorldCentreY(firstRow) - WorldCentreY(secondRow));
            Assert.That(gap, Is.EqualTo(RowHeight).Within(1f));
        }

        [Test]
        public void Opening_AnAlreadyMeasuredPanelKeepsTheRowsSeparated()
        {
            // Close and reopen: the second pass must not undo the first one's layout.
            view.Show(GameSettings.CreateDefault());
            view.Hide();
            view.Show(GameSettings.CreateDefault());

            Assert.That(Overlaps(firstRow, secondRow), Is.False);
        }

        /// <summary>A container that sizes itself to its children — the nesting that causes the bug.</summary>
        private static RectTransform CreateStack(string name, RectTransform parent)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)host.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = host.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = host.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        private static RectTransform CreateRow(string name, RectTransform parent)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)host.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, RowHeight);

            LayoutElement element = host.AddComponent<LayoutElement>();
            element.preferredHeight = RowHeight;
            element.minHeight = RowHeight;
            return rect;
        }

        private static bool Overlaps(RectTransform first, RectTransform second)
        {
            Vector3[] a = new Vector3[4];
            Vector3[] b = new Vector3[4];
            first.GetWorldCorners(a);
            second.GetWorldCorners(b);

            float aBottom = a[0].y;
            float aTop = a[1].y;
            float bBottom = b[0].y;
            float bTop = b[1].y;
            return aBottom < bTop && bBottom < aTop;
        }

        private static float WorldCentreY(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[0].y + corners[1].y) * 0.5f;
        }
    }
}
