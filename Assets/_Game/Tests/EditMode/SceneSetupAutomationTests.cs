using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Editor;
using RoyalDecisions.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class SceneSetupAutomationTests
    {
        private Scene scene;
        private ContentCatalogue catalogue;
        private RoyalDecisions.Composition.SessionIntent intent;

        [SetUp]
        public void SetUp()
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            catalogue = ScriptableObject.CreateInstance<ContentCatalogue>();
            intent = ScriptableObject.CreateInstance<RoyalDecisions.Composition.SessionIntent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(catalogue);
            Object.DestroyImmediate(intent);
        }

        [Test]
        public void ApplyingTwice_ReusesPathsComponentsAndButtonListeners()
        {
            SceneSetupReport first = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);
            Assert.That(first.ErrorCount, Is.Zero);

            int objectCount = CountObjects(scene);
            int componentCount = CountComponents(scene);
            Button restart = Find("/UICanvas/SafeArea/GameOverPanel/Content/RestartButton")
                .GetComponent<Button>();
            Assert.That(restart.onClick.GetPersistentEventCount(), Is.EqualTo(1));

            SceneSetupReport second = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);

            Assert.That(second.ErrorCount, Is.Zero);
            Assert.That(CountObjects(scene), Is.EqualTo(objectCount));
            Assert.That(CountComponents(scene), Is.EqualTo(componentCount));
            Assert.That(restart.onClick.GetPersistentEventCount(), Is.EqualTo(1));

            SceneSetupReport validation = SceneSetupAutomation.ValidateGameSceneForTests(
                scene, catalogue, intent);
            Assert.That(validation.ErrorCount, Is.Zero,
                JoinIssues(validation));
        }

        [Test]
        public void DuplicateManagedRoot_BlocksWithoutDeletingEitherObject()
        {
            GameObject first = new GameObject("UICanvas", typeof(RectTransform));
            GameObject second = new GameObject("UICanvas", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(first, scene);
            SceneManager.MoveGameObjectToScene(second, scene);

            SceneSetupReport report = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);

            Assert.That(report.ErrorCount, Is.GreaterThan(0));
            Assert.That(CountRootsNamed(scene, "UICanvas"), Is.EqualTo(2));
        }

        [Test]
        public void GeneratedStatItems_HaveDistinctStatsAndFilledImages()
        {
            SceneSetupReport report = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);
            Assert.That(report.ErrorCount, Is.Zero);

            string[] names =
            {
                "StatItem_People", "StatItem_Security", "StatItem_Authority", "StatItem_Wealth"
            };
            string[] slots =
            {
                "StatSlot_People", "StatSlot_Security", "StatSlot_Authority", "StatSlot_Wealth"
            };
            StatType[] stats =
            {
                StatType.People, StatType.Security, StatType.Authority, StatType.Wealth
            };
            Color[] colours =
            {
                new Color32(0x8A, 0x41, 0x4B, 0xFF),
                new Color32(0x68, 0x70, 0x3D, 0xFF),
                new Color32(0x3E, 0x56, 0x7D, 0xFF),
                new Color32(0xB3, 0x8A, 0x3D, 0xFF)
            };
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            for (int i = 0; i < names.Length; i++)
            {
                string itemPath = "/UICanvas/SafeArea/HUD/" + slots[i] + "/" + names[i];
                GameObject itemObject = Find(itemPath);
                StatItemView item = itemObject.GetComponent<StatItemView>();
                Image background = itemObject.GetComponent<Image>();
                Image fill = Find(itemPath + "/Fill")
                    .GetComponent<Image>();
                Assert.That(item.Stat, Is.EqualTo(stats[i]));
                Assert.That(background.sprite, Is.SameAs(uiSprite));
                Assert.That(background.type, Is.EqualTo(Image.Type.Simple));
                Assert.That(background.raycastTarget, Is.False);
                Assert.That(background.color, Is.EqualTo((Color)new Color32(0x2A, 0x2F, 0x3A, 0xFF)));
                Assert.That(fill.sprite, Is.SameAs(uiSprite));
                Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
                Assert.That(fill.fillMethod, Is.EqualTo(Image.FillMethod.Horizontal));
                Assert.That(fill.fillOrigin, Is.EqualTo((int)Image.OriginHorizontal.Left));
                Assert.That(fill.preserveAspect, Is.False);
                Assert.That(fill.raycastTarget, Is.False);
                Assert.That(fill.color, Is.EqualTo(colours[i]));

                SerializedProperty fillReference = new SerializedObject(item)
                    .FindProperty("fillImage");
                Assert.That(fillReference.objectReferenceValue, Is.SameAs(fill));
                string slotPath = "/UICanvas/SafeArea/HUD/" + slots[i];
                Assert.That(Find(slotPath + "/Icon").GetComponent<Image>(), Is.Not.Null);
                Assert.That(Find(slotPath + "/IconFallback").GetComponent<TextMeshProUGUI>(),
                    Is.Not.Null);
                Assert.That(Find(slotPath + "/Name").GetComponent<TextMeshProUGUI>(), Is.Not.Null);
                Assert.That(Find(slotPath + "/Value").GetComponent<TextMeshProUGUI>(), Is.Not.Null);
                Assert.That(Find(slotPath + "/Impact").GetComponent<TextMeshProUGUI>(), Is.Not.Null);
                Assert.That(Find(slotPath + "/Critical").GetComponent<TextMeshProUGUI>(),
                    Is.Not.Null);
            }
        }

        [Test]
        public void GeneratedTurkishLayout_UsesOwnedFontReadableCardAndTurnFooter()
        {
            SceneSetupReport report = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);
            Assert.That(report.ErrorCount, Is.Zero, JoinIssues(report));

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                TurkishGlyphValidator.FontAssetPath);
            Assert.That(font, Is.Not.Null);

            TextMeshProUGUI body = Find("/UICanvas/SafeArea/CardArea/Card/Body")
                .GetComponent<TextMeshProUGUI>();
            Assert.That(body.rectTransform.anchorMin, Is.EqualTo(new Vector2(0.09f, 0.19f)));
            Assert.That(body.rectTransform.anchorMax, Is.EqualTo(new Vector2(0.91f, 0.42f)));
            Assert.That(body.font, Is.SameAs(font));
            Assert.That(body.enableAutoSizing, Is.True);
            Assert.That(body.fontSizeMin, Is.EqualTo(34f));
            Assert.That(body.fontSizeMax, Is.EqualTo(46f));
            Assert.That(body.textWrappingMode, Is.EqualTo(TextWrappingModes.Normal));
            Assert.That(body.overflowMode, Is.EqualTo(TextOverflowModes.Ellipsis));

            TextMeshProUGUI speaker = Find("/UICanvas/SafeArea/CardArea/Card/Speaker")
                .GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI leftChoice = Find(
                    "/UICanvas/SafeArea/CardArea/Card/PreviewLeft/Label")
                .GetComponent<TextMeshProUGUI>();
            Assert.That(speaker.fontSizeMin, Is.EqualTo(28f));
            Assert.That(speaker.fontSizeMax, Is.EqualTo(38f));
            Assert.That(leftChoice.fontSizeMin, Is.EqualTo(26f));
            Assert.That(leftChoice.fontSizeMax, Is.EqualTo(34f));
            Assert.That(speaker.font, Is.SameAs(font));
            Assert.That(leftChoice.font, Is.SameAs(font));

            GameObject footer = Find("/UICanvas/SafeArea/Footer");
            Assert.That(footer.GetComponent<RunStatusView>(), Is.Not.Null);
            TextMeshProUGUI turn = Find("/UICanvas/SafeArea/Footer/Reign")
                .GetComponent<TextMeshProUGUI>();
            Assert.That(turn.text, Is.EqualTo("Tur 1"));
            Assert.That(turn.font, Is.SameAs(font));
        }

        [Test]
        public void ExistingStatImagesWithNullParentSprite_AreRejectedAndRepaired()
        {
            SceneSetupReport initial = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);
            Assert.That(initial.ErrorCount, Is.Zero);

            const string itemPath =
                "/UICanvas/SafeArea/HUD/StatSlot_People/StatItem_People";
            GameObject itemObject = Find(itemPath);
            Image background = itemObject.GetComponent<Image>();
            Image fill = Find(itemPath + "/Fill")
                .GetComponent<Image>();
            background.sprite = null;
            background.color = new Color32(0x2E, 0xCC, 0x71, 0xFF);
            fill.sprite = null;
            fill.fillAmount = 0.37f;

            SceneSetupReport rejected = SceneSetupAutomation.ValidateGameSceneForTests(
                scene, catalogue, intent);
            Assert.That(rejected.ErrorCount, Is.GreaterThan(0),
                "Validation must reject the old null-sprite stat-bar state.");

            SceneSetupReport repaired = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);
            Assert.That(repaired.ErrorCount, Is.Zero, JoinIssues(repaired));

            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Assert.That(background.sprite, Is.SameAs(uiSprite));
            Assert.That(fill.sprite, Is.SameAs(uiSprite));
            Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
            Assert.That(fill.fillMethod, Is.EqualTo(Image.FillMethod.Horizontal));
            Assert.That(fill.fillOrigin, Is.EqualTo((int)Image.OriginHorizontal.Left));
            Assert.That(fill.raycastTarget, Is.False);
            Assert.That(fill.fillAmount, Is.EqualTo(0.37f),
                "Authoring must not overwrite the runtime-driven fill amount.");

            SceneSetupReport validation = SceneSetupAutomation.ValidateGameSceneForTests(
                scene, catalogue, intent);
            Assert.That(validation.ErrorCount, Is.Zero, JoinIssues(validation));
        }

        [Test]
        public void InactiveUnreferencedLegacyGameOverChild_IsRemoved()
        {
            SceneSetupReport initial = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);
            Assert.That(initial.ErrorCount, Is.Zero, JoinIssues(initial));

            Transform panel = Find("/UICanvas/SafeArea/GameOverPanel").transform;
            GameObject legacy = new GameObject(
                "Illustration", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            legacy.transform.SetParent(panel, false);
            legacy.SetActive(false);

            SceneSetupReport repaired = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);

            Assert.That(repaired.ErrorCount, Is.Zero, JoinIssues(repaired));
            Assert.That(panel.Find("Illustration"), Is.Null);
            Assert.That(panel.Find("Content/Illustration"), Is.Not.Null);
        }

        [Test]
        public void InactiveLegacyRestartButton_WithExactManagedDuplicateListener_IsRemoved()
        {
            SceneSetupReport initial = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);
            Assert.That(initial.ErrorCount, Is.Zero, JoinIssues(initial));

            Transform panel = Find("/UICanvas/SafeArea/GameOverPanel").transform;
            GameOverView view = panel.GetComponent<GameOverView>();
            GameObject legacy = new GameObject(
                "RestartButton", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            legacy.transform.SetParent(panel, false);
            legacy.SetActive(false);
            UnityEventTools.AddPersistentListener(
                legacy.GetComponent<Button>().onClick, view.HandleRestartButton);

            SceneSetupReport repaired = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);

            Assert.That(repaired.ErrorCount, Is.Zero, JoinIssues(repaired));
            Assert.That(panel.Find("RestartButton"), Is.Null);
            Assert.That(panel.Find("Content/RestartButton"), Is.Not.Null);
        }

        [Test]
        public void InactiveLegacyRestartButton_WithUnexpectedListener_BlocksAndIsPreserved()
        {
            SceneSetupReport initial = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);
            Assert.That(initial.ErrorCount, Is.Zero, JoinIssues(initial));

            Transform panel = Find("/UICanvas/SafeArea/GameOverPanel").transform;
            GameOverView view = panel.GetComponent<GameOverView>();
            GameObject legacy = new GameObject(
                "RestartButton", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            legacy.transform.SetParent(panel, false);
            legacy.SetActive(false);
            UnityEventTools.AddPersistentListener(legacy.GetComponent<Button>().onClick, view.Hide);

            SceneSetupReport blocked = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);

            Assert.That(blocked.ErrorCount, Is.GreaterThan(0));
            Assert.That(panel.Find("RestartButton"), Is.SameAs(legacy.transform));
        }

        [Test]
        public void ActiveLegacyGameOverChild_BlocksAndIsPreserved()
        {
            SceneSetupReport initial = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);
            Assert.That(initial.ErrorCount, Is.Zero, JoinIssues(initial));

            Transform panel = Find("/UICanvas/SafeArea/GameOverPanel").transform;
            GameObject legacy = new GameObject(
                "Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            legacy.transform.SetParent(panel, false);
            legacy.SetActive(true);

            SceneSetupReport blocked = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);

            Assert.That(blocked.ErrorCount, Is.GreaterThan(0));
            Assert.That(panel.Find("Title"), Is.SameAs(legacy.transform));
        }

        [Test]
        public void GeneratedPolish_HasResponsiveCapVignetteBordersAndPortraitFallback()
        {
            SceneSetupReport report = SceneSetupAutomation.ApplyGameSceneForTests(
                scene, catalogue, intent);
            Assert.That(report.ErrorCount, Is.Zero, JoinIssues(report));

            GameObject cardArea = Find("/UICanvas/SafeArea/CardArea");
            ResponsiveCardSizer sizer = cardArea.GetComponent<ResponsiveCardSizer>();
            SerializedObject serializedSizer = new SerializedObject(sizer);
            Assert.That(serializedSizer.FindProperty("preferredWidthRatio").floatValue,
                Is.EqualTo(0.78f));
            Assert.That(serializedSizer.FindProperty("maximumWidth").floatValue,
                Is.EqualTo(920f));
            Assert.That(serializedSizer.FindProperty("widthReference").objectReferenceValue,
                Is.SameAs(Find("/UICanvas/SafeArea").transform));

            ProceduralVignetteGraphic vignette = Find(
                    "/UICanvas/Background/ProceduralVignette")
                .GetComponent<ProceduralVignetteGraphic>();
            Assert.That(vignette.raycastTarget, Is.False);
            Assert.That(vignette.EdgeWidth, Is.EqualTo(0.22f));
            Assert.That(vignette.EdgeAlpha, Is.EqualTo(0.42f));

            Assert.That(Find(
                    "/UICanvas/SafeArea/CardArea/Card/PortraitRegion/PortraitMask/FallbackSilhouette")
                .GetComponent<PortraitFallbackView>(), Is.Not.Null);
            string[] edges = { "Top", "Right", "Bottom", "Left" };
            for (int i = 0; i < edges.Length; i++)
            {
                Assert.That(Find(
                        "/UICanvas/SafeArea/CardArea/Card/TemporaryBorder/" + edges[i])
                    .GetComponent<Image>().raycastTarget, Is.False);
            }
        }

        private GameObject Find(string path)
        {
            string[] parts = path.Trim('/').Split('/');
            GameObject current = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == parts[0])
                {
                    current = roots[i];
                    break;
                }
            }
            for (int p = 1; p < parts.Length; p++)
            {
                current = current.transform.Find(parts[p]).gameObject;
            }
            return current;
        }

        private static int CountObjects(Scene target)
        {
            int count = 0;
            GameObject[] roots = target.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                count += roots[i].GetComponentsInChildren<Transform>(true).Length;
            }
            return count;
        }

        private static int CountComponents(Scene target)
        {
            int count = 0;
            GameObject[] roots = target.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < transforms.Length; t++)
                {
                    count += transforms[t].GetComponents<Component>().Length;
                }
            }
            return count;
        }

        private static int CountRootsNamed(Scene target, string name)
        {
            int count = 0;
            GameObject[] roots = target.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name)
                {
                    count++;
                }
            }
            return count;
        }

        private static string JoinIssues(SceneSetupReport report)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < report.Issues.Count; i++)
            {
                builder.Append(report.Issues[i].Code).Append(": ")
                    .AppendLine(report.Issues[i].Message);
            }
            return builder.ToString();
        }
    }
}
