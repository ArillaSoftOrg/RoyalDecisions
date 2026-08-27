using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Texture-free horizontal alpha gradient: an evenly spaced sequence of stops, each multiplying
    /// the shared <see cref="Graphic.color"/> alpha at that point across the rect's width.
    /// </summary>
    /// <remarks>
    /// Used by the intro's wordmark reveal for a soft feather edge (two stops: transparent to full)
    /// and its travelling glint (three stops: transparent, peak, transparent), so neither reads as a
    /// hard-edged rectangle. No shader, no texture — the built-in UI material already interpolates
    /// per-vertex colour, exactly like <see cref="ProceduralVignetteGraphic"/>.
    /// </remarks>
    public sealed class ProceduralHorizontalGradientGraphic : MaskableGraphic
    {
        private static readonly float[] DefaultStops = { 0f, 1f };

        private float[] stopAlphas = DefaultStops;

        /// <summary>At least two values in <c>[0, 1]</c>, evenly spaced left to right across the
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

            int columnCount = stopAlphas.Length;
            for (int i = 0; i < columnCount; i++)
            {
                float x = Mathf.Lerp(rect.xMin, rect.xMax, i / (float)(columnCount - 1));
                Color32 stopColor = new Color(color.r, color.g, color.b, color.a * stopAlphas[i]);

                UIVertex bottom = UIVertex.simpleVert;
                bottom.position = new Vector3(x, rect.yMin);
                bottom.color = stopColor;
                vertexHelper.AddVert(bottom);

                UIVertex top = UIVertex.simpleVert;
                top.position = new Vector3(x, rect.yMax);
                top.color = stopColor;
                vertexHelper.AddVert(top);
            }

            for (int i = 0; i < columnCount - 1; i++)
            {
                int bottomLeft = i * 2;
                int topLeft = bottomLeft + 1;
                int bottomRight = bottomLeft + 2;
                int topRight = bottomLeft + 3;
                vertexHelper.AddTriangle(bottomLeft, topLeft, topRight);
                vertexHelper.AddTriangle(bottomLeft, topRight, bottomRight);
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
