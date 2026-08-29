using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Draws a texture-free filled rounded rectangle. Used as the visual and raycast target for
    /// buttons so every interactive control shares one consistent corner radius without a 9-sliced
    /// sprite asset.
    /// </summary>
    public sealed class ProceduralRoundedRectGraphic : MaskableGraphic
    {
        /// <summary>Round arcs every corner (the original behaviour), or flat-cut each corner at
        /// 45° instead — the same arc math, just keeping only each arc's first and last point.
        /// On a square whose radius is half its side, Chamfer degenerates into a regular octagon
        /// "gem" shape for free; on a wider rect it reads as a chamfered/cut-corner button.</summary>
        public enum CornerStyle
        {
            Round,
            Chamfer
        }

        private const int MinimumCornerSegments = 1;
        private const int MaximumCornerSegments = 16;

        [Range(0f, 200f)]
        [SerializeField] private float cornerRadius = 20f;

        [Range(MinimumCornerSegments, MaximumCornerSegments)]
        [SerializeField] private int cornerSegments = 8;

        [SerializeField] private CornerStyle cornerStyle = CornerStyle.Round;

        public float CornerRadius => cornerRadius;

        public CornerStyle Style => cornerStyle;

        public void SetCornerRadius(float radius)
        {
            cornerRadius = Mathf.Max(0f, radius);
            SetVerticesDirty();
        }

        public void SetCornerSegments(int segments)
        {
            cornerSegments = Mathf.Clamp(segments, MinimumCornerSegments, MaximumCornerSegments);
            SetVerticesDirty();
        }

        public void SetCornerStyle(CornerStyle style)
        {
            cornerStyle = style;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float radius = Mathf.Min(cornerRadius, Mathf.Min(rect.width, rect.height) * 0.5f);
            int segments = Mathf.Max(MinimumCornerSegments, cornerSegments);
            Color32 fillColour = color;

            // One arc centre per corner, walked counter-clockwise starting bottom-right.
            Vector2[] arcCentres =
            {
                new Vector2(rect.xMax - radius, rect.yMin + radius),
                new Vector2(rect.xMax - radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMin + radius)
            };
            float[] arcStartDegrees = { -90f, 0f, 90f, 180f };

            UIVertex centreVertex = UIVertex.simpleVert;
            centreVertex.color = fillColour;
            centreVertex.position = rect.center;
            vertexHelper.AddVert(centreVertex);

            bool chamfer = cornerStyle == CornerStyle.Chamfer;

            int firstOutlineIndex = 1;
            int outlineCount = 0;
            for (int corner = 0; corner < 4; corner++)
            {
                for (int step = 0; step <= segments; step++)
                {
                    // Chamfer keeps only each arc's endpoints, skipping the interior round steps.
                    if (chamfer && step != 0 && step != segments)
                    {
                        continue;
                    }

                    float degrees = arcStartDegrees[corner] + (90f * step / segments);
                    float radians = degrees * Mathf.Deg2Rad;
                    Vector2 offset = radius > 0f
                        ? new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius
                        : Vector2.zero;

                    UIVertex vertex = UIVertex.simpleVert;
                    vertex.color = fillColour;
                    vertex.position = arcCentres[corner] + offset;
                    vertexHelper.AddVert(vertex);
                    outlineCount++;
                }
            }

            for (int i = 0; i < outlineCount; i++)
            {
                int current = firstOutlineIndex + i;
                int next = firstOutlineIndex + ((i + 1) % outlineCount);
                vertexHelper.AddTriangle(0, current, next);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSegments = Mathf.Clamp(cornerSegments, MinimumCornerSegments, MaximumCornerSegments);
            SetVerticesDirty();
        }
#endif
    }
}
