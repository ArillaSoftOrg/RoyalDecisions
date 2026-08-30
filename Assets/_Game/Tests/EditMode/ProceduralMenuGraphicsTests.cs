using System.Reflection;
using NUnit.Framework;
using RoyalDecisions.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Covers the texture-free menu graphics introduced with the post-apocalyptic re-theme: they
    /// must actually emit geometry at a normal size, must survive the zero-sized rect a layout
    /// group hands them on the frame before it measures, and — for the grain — must be
    /// deterministic, since a shifting pattern would make every authoring pass dirty the scene.
    /// </summary>
    public sealed class ProceduralMenuGraphicsTests
    {
        [TearDown]
        public void TearDown()
        {
            PresentationTestObjects.DestroyAll();
        }

        [Test]
        public void GrainEmitsGeometryAtNormalSize()
        {
            ProceduralGrainGraphic grain = CreateGraphic<ProceduralGrainGraphic>(1080f, 1920f);
            Assert.That(CountVertices(grain), Is.GreaterThan(0));
        }

        [Test]
        public void GrainIsDeterministic()
        {
            // Two separate instances of the same size must produce byte-identical geometry: the
            // pattern comes from a positional hash, not Random. If this drifts, every Apply pass
            // would rewrite the scene YAML and the idempotency guarantee goes with it.
            ProceduralGrainGraphic first = CreateGraphic<ProceduralGrainGraphic>(400f, 600f);
            ProceduralGrainGraphic second = CreateGraphic<ProceduralGrainGraphic>(400f, 600f);
            Assert.That(ReadVertexPositions(second), Is.EqualTo(ReadVertexPositions(first)));
        }

        [Test]
        public void GrainEmitsNothingForAZeroSizedRect()
        {
            ProceduralGrainGraphic grain = CreateGraphic<ProceduralGrainGraphic>(0f, 0f);
            Assert.That(CountVertices(grain), Is.Zero);
        }

        [TestCase(AudioIconKind.Speaker)]
        [TestCase(AudioIconKind.Note)]
        [TestCase(AudioIconKind.Effect)]
        public void EveryAudioIconEmitsGeometry(AudioIconKind kind)
        {
            ProceduralAudioIconGraphic icon =
                CreateGraphic<ProceduralAudioIconGraphic>(48f, 48f);
            icon.SetKind(kind);
            Assert.That(CountVertices(icon), Is.GreaterThan(0));
        }

        [TestCase(AudioIconKind.Speaker)]
        [TestCase(AudioIconKind.Note)]
        [TestCase(AudioIconKind.Effect)]
        public void EveryAudioIconSurvivesAZeroSizedRect(AudioIconKind kind)
        {
            ProceduralAudioIconGraphic icon = CreateGraphic<ProceduralAudioIconGraphic>(0f, 0f);
            icon.SetKind(kind);
            Assert.That(CountVertices(icon), Is.Zero);
        }

        [Test]
        public void AudioIconIsNotARaycastTargetOnceConfigured()
        {
            // The icon sits on top of its slider row; if it raycast, it would swallow drags that
            // belong to the Slider underneath it.
            ProceduralAudioIconGraphic icon = CreateGraphic<ProceduralAudioIconGraphic>(48f, 48f);
            icon.SetKind(AudioIconKind.Speaker);
            Assert.That(icon.raycastTarget, Is.False);
        }

        private static T CreateGraphic<T>(float width, float height) where T : Graphic
        {
            T graphic = PresentationTestObjects.CreateComponent<T>();
            RectTransform rect = graphic.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            return graphic;
        }

        private static int CountVertices(Graphic graphic)
        {
            return ReadVertexPositions(graphic).Length;
        }

        /// <summary>
        /// Drives the real <c>OnPopulateMesh</c> override and returns the geometry it produced. The
        /// method is protected — as Unity defines it — so the test reaches it by reflection rather
        /// than widening the production API purely to be observable.
        /// </summary>
        private static Vector3[] ReadVertexPositions(Graphic graphic)
        {
            MethodInfo populate = graphic.GetType().GetMethod(
                "OnPopulateMesh",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(VertexHelper) },
                null);
            Assert.That(populate, Is.Not.Null, "OnPopulateMesh(VertexHelper) was not found.");

            using (VertexHelper helper = new VertexHelper())
            {
                populate.Invoke(graphic, new object[] { helper });

                Vector3[] positions = new Vector3[helper.currentVertCount];
                UIVertex vertex = default;
                for (int i = 0; i < positions.Length; i++)
                {
                    helper.PopulateUIVertex(ref vertex, i);
                    positions[i] = vertex.position;
                }
                return positions;
            }
        }
    }
}
