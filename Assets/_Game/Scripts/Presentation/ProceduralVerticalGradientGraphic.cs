using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Texture-free vertical alpha gradient: an evenly spaced sequence of stops, each multiplying
    /// the shared <see cref="Graphic.color"/> alpha at that point from the bottom of the rect to the
    /// top, index 0 at the bottom.
    /// </summary>
    /// <remarks>
    /// Vertical counterpart of <see cref="ProceduralHorizontalGradientGraphic"/>, same reasoning: no
    /// shader, no texture, just per-vertex colour on the built-in UI material. Used by the startup
    /// loading screen's readability scrim so it darkens only towards the bottom band that holds the
    /// status/progress text, keeping the background artwork undimmed near the top instead of a flat
    /// full-screen tint.
    /// </remarks>
    public sealed class ProceduralVerticalGradientGraphic : MaskableGraphic
    {
        private static readonly float[] DefaultStops = { 0f, 1f };

        private float[] stopAlphas = DefaultStops;

        /// <summary>At least two values in <c>[0, 1]</c>, evenly spaced bottom to top across the
        /// rect. Fewer than two falls back to a plain transparent-to-opaque edge.</summary>
        public void SetStops(params float[] alphas)
        {
            stopAlphas = (alphas != null && alphas.Length >= 2) ? alphas : DefaultStops;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f || stopAlphas.Length < 2)
            {
                return;
            }

            int rowCount = stopAlphas.Length;
            for (int i = 0; i < rowCount; i++)
            {
                float y = Mathf.Lerp(rect.yMin, rect.yMax, i / (float)(rowCount - 1));
                Color32 stopColor = new Color(color.r, color.g, color.b, color.a * stopAlphas[i]);

                UIVertex left = UIVertex.simpleVert;
                left.position = new Vector3(rect.xMin, y);
                left.color = stopColor;
                vertexHelper.AddVert(left);

                UIVertex right = UIVertex.simpleVert;
                right.position = new Vector3(rect.xMax, y);
                right.color = stopColor;
                vertexHelper.AddVert(right);
            }

            for (int i = 0; i < rowCount - 1; i++)
            {
                int left0 = i * 2;
                int right0 = left0 + 1;
                int left1 = left0 + 2;
                int right1 = left0 + 3;
                vertexHelper.AddTriangle(left0, left1, right1);
                vertexHelper.AddTriangle(left0, right1, right0);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            raycastTarget = false;
            SetVerticesDirty();
        }
#endif
    }
}
