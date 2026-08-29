using NUnit.Framework;
using RoyalDecisions.Editor;
using RoyalDecisions.Presentation;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Focused coverage for the CanvasRenderer guarantee inside
    /// <see cref="StartupLoadingSetup.EnsureComponent{T}"/> — the fix for
    /// "MissingComponentException: There is no 'CanvasRenderer' attached to ... BloodFill", where
    /// <see cref="Graphic"/>'s own <c>[RequireComponent(typeof(CanvasRenderer))]</c> auto-add lost the
    /// race with a single-shot batch Editor run before <c>SaveScene</c> serialized the object — the
    /// same class of bug already fixed once for MainMenu buttons (see <c>MANUAL_UNITY_STEPS.md</c>).
    /// Empirically, every procedural graphic the old blood tube built (TubeShadow, TubeFrame,
    /// TubeInterior, BloodFill, BloodLeadingEdge, GlassHighlight) was found missing its
    /// CanvasRenderer in the saved scene. Exercised here against
    /// <see cref="ProceduralRoundedRectGraphic"/> — still used elsewhere in this file (and by other
    /// scenes) even after the blood tube itself moved to plain sprite-based Images — since the fix
    /// lives in the one shared helper every Graphic-derived component in this file goes through,
    /// regardless of which concrete type is passed.
    /// </summary>
    [TestFixture]
    public class StartupLoadingSetupCanvasRendererTests
    {
        private GameObject host;

        [TearDown]
        public void TearDown()
        {
            if (host != null)
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void EnsureComponent_FreshGraphic_AddsCanvasRendererToo()
        {
            host = new GameObject("Fresh", typeof(RectTransform));

            StartupLoadingSetup.EnsureComponent<ProceduralRoundedRectGraphic>(host);

            Assert.That(host.GetComponent<RectTransform>(), Is.Not.Null);
            Assert.That(host.GetComponent<CanvasRenderer>(), Is.Not.Null);
            Assert.That(host.GetComponent<ProceduralRoundedRectGraphic>(), Is.Not.Null);
            // Unity's Test Runner already fails a test on any unexpected error/exception-level log
            // (this is how the wrong add-order — Graphic added before CanvasRenderer — would have
            // been caught: Graphic.OnEnable() touching its own canvasRenderer property throws
            // MissingComponentException the instant CanvasRenderer isn't attached yet, which Unity
            // logs as an error during AddComponent<ProceduralRoundedRectGraphic>() above). Asserting it here too
            // makes that expectation explicit rather than relying only on the Test Runner's default.
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void EnsureComponent_ExistingGraphicWithoutCanvasRenderer_RepairsIt()
        {
            // Reproduces the exact malformed state found in Bootstrap.unity: a GameObject that
            // already has the Graphic component but no CanvasRenderer, because RequireComponent's
            // auto-add never landed the first time it was authored (the batch-mode race).
            // DestroyImmediate bypasses the RequireComponent guard the Editor's own "Remove
            // Component" menu enforces, which is exactly how this malformed state actually arises.
            host = new GameObject("Malformed", typeof(RectTransform));
            ProceduralRoundedRectGraphic existing = host.AddComponent<ProceduralRoundedRectGraphic>();
            Object.DestroyImmediate(host.GetComponent<CanvasRenderer>());
            Assert.That(host.GetComponent<CanvasRenderer>(), Is.Null,
                "Test setup sanity check: the reproduced malformed state must not already have one.");

            ProceduralRoundedRectGraphic result = StartupLoadingSetup.EnsureComponent<ProceduralRoundedRectGraphic>(host);

            Assert.That(result, Is.SameAs(existing),
                "Repairing CanvasRenderer must reuse the existing Graphic component, never replace it.");
            Assert.That(host.GetComponent<CanvasRenderer>(), Is.Not.Null);
        }

        [Test]
        public void EnsureComponent_CalledTwiceOnSameObject_NeverDuplicatesComponents()
        {
            host = new GameObject("Idempotent", typeof(RectTransform));

            StartupLoadingSetup.EnsureComponent<ProceduralRoundedRectGraphic>(host);
            StartupLoadingSetup.EnsureComponent<ProceduralRoundedRectGraphic>(host);

            Assert.That(host.GetComponents<CanvasRenderer>().Length, Is.EqualTo(1));
            Assert.That(host.GetComponents<ProceduralRoundedRectGraphic>().Length, Is.EqualTo(1));
        }

        [Test]
        public void EnsureComponent_RepairedTwice_StaysAtExactlyOneCanvasRenderer()
        {
            host = new GameObject("RepairedTwice", typeof(RectTransform));
            host.AddComponent<ProceduralRoundedRectGraphic>();
            Object.DestroyImmediate(host.GetComponent<CanvasRenderer>());

            StartupLoadingSetup.EnsureComponent<ProceduralRoundedRectGraphic>(host);
            StartupLoadingSetup.EnsureComponent<ProceduralRoundedRectGraphic>(host);

            Assert.That(host.GetComponents<CanvasRenderer>().Length, Is.EqualTo(1));
        }

        [Test]
        public void EnsureComponent_NonGraphicComponent_DoesNotAddCanvasRenderer()
        {
            host = new GameObject("NonGraphic", typeof(RectTransform));

            StartupLoadingSetup.EnsureComponent<RectMask2D>(host);

            Assert.That(host.GetComponent<CanvasRenderer>(), Is.Null);
        }
    }
}
