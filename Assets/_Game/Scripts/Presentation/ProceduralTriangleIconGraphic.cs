using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Draws a texture-free solid triangle pointing left or right. Used for the card's tap
    /// left/right choice buttons so they read as directional arrows without depending on a font
    /// glyph — the Unicode "◀"/"▶" triangles previously used here fall outside the project's
    /// Turkish SDF atlas and rendered as the missing-glyph fallback box.
    /// </summary>
    public sealed class ProceduralTriangleIconGraphic : MaskableGraphic
    {
        [SerializeField] private bool pointsRight;

        [Range(0.2f, 1f)]
        [SerializeField] private float sizeRatio = 0.7f;

        public bool PointsRight
        {
            get => pointsRight;
            set
            {
                pointsRight = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float halfSize = Mathf.Min(rect.width, rect.height) * 0.5f * sizeRatio;
            Vector2 centre = rect.center;
            float direction = pointsRight ? 1f : -1f;

            Vector2 tip = centre + new Vector2(halfSize * direction, 0f);
            Vector2 backTop = centre + new Vector2(-halfSize * direction, halfSize);
            Vector2 backBottom = centre + new Vector2(-halfSize * direction, -halfSize);

            Color32 fillColour = color;
            int a = AddVertex(vertexHelper, tip, fillColour);
            int b = AddVertex(vertexHelper, backTop, fillColour);
            int c = AddVertex(vertexHelper, backBottom, fillColour);
            vertexHelper.AddTriangle(a, b, c);
        }

        private static int AddVertex(VertexHelper vertexHelper, Vector2 position, Color32 colour)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = colour;
            vertex.position = position;
            vertexHelper.AddVert(vertex);
            return vertexHelper.currentVertCount - 1;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            sizeRatio = Mathf.Clamp(sizeRatio, 0.2f, 1f);
            SetVerticesDirty();
        }
#endif
    }
}
