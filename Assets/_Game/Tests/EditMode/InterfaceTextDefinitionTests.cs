using NUnit.Framework;
using RoyalDecisions.Data;
using RoyalDecisions.Editor;
using UnityEditor;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class InterfaceTextDefinitionTests
    {
        private const string TestRoot = TurkishInterfaceTextGenerator.Root + "/__Tests";
        private const string TestPath = TestRoot + "/TurkishInterfaceText.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestRoot);
        }

        [Test]
        public void TurkishLibraryContainsTheApprovedMvpTerms()
        {
            InterfaceTextDefinition text = TurkishInterfaceTextLibrary.Create();
            try
            {
                Assert.That(text.LanguageCode, Is.EqualTo("tr"));
                Assert.That(text.NewGame, Is.EqualTo("Yeni Oyun"));
                Assert.That(text.ContinueGame, Is.EqualTo("Devam Et"));
                Assert.That(text.Restart, Is.EqualTo("Yeniden Başlat"));
                Assert.That(text.GameOverTitle, Is.EqualTo("Hükümdarlık Sona Erdi"));
                Assert.That(text.GetStatLabel(StatType.Authority), Is.EqualTo("Otorite"));
                Assert.That(text.GetStatLabel(StatType.People), Is.EqualTo("Halk"));
                Assert.That(text.GetStatLabel(StatType.Security), Is.EqualTo("Güvenlik"));
                Assert.That(text.GetStatLabel(StatType.Wealth), Is.EqualTo("Servet"));
                Assert.That(text.Year, Is.EqualTo("Yıl"));
                Assert.That(text.Turn, Is.EqualTo("Tur"));
            }
            finally
            {
                Object.DestroyImmediate(text);
            }
        }

        [Test]
        public void GeneratorIsIdempotentAndPreservesGuid()
        {
            InterfaceTextDefinition first = TurkishInterfaceTextGenerator.Generate(TestPath);
            string guid = AssetDatabase.AssetPathToGUID(TestPath);

            InterfaceTextDefinition second = TurkishInterfaceTextGenerator.Generate(TestPath);

            Assert.That(second, Is.SameAs(first));
            Assert.That(AssetDatabase.AssetPathToGUID(TestPath), Is.EqualTo(guid));
            Assert.That(AssetDatabase.GetLabels(second),
                Does.Contain(TurkishInterfaceTextGenerator.OwnershipLabel));
        }

        [Test]
        public void GeneratorRefusesAnUnlabelledAsset()
        {
            EnsureFolder(TestRoot);
            InterfaceTextDefinition authored = ScriptableObject.CreateInstance<InterfaceTextDefinition>();
            AssetDatabase.CreateAsset(authored, TestPath);
            AssetDatabase.SaveAssets();

            Assert.That(
                () => TurkishInterfaceTextGenerator.Generate(TestPath),
                Throws.InvalidOperationException);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }
            AssetDatabase.CreateFolder(TurkishInterfaceTextGenerator.Root, "__Tests");
        }
    }
}
