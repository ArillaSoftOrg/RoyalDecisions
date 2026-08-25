using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Draws a texture-free left-pointing chevron: two thick strokes meeting at a tip.
    /// Used as the back icon so the button reads as "back" without depending on a font glyph.
    /// </summary>
    public sealed class ProceduralArrowIconGraphic : MaskableGraphic
    {
        [Range(0.05f, 0.4f)]
        [SerializeField] private float strokeThicknessRatio = 0.16f;

        [Range(0.2f, 0.95f)]
        [SerializeField] private float armLengthRatio = 0.8f;

        [Range(0f, 0.9f)]
        [SerializeField] private float tipInsetRatio = 0.5f;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float halfSize = Mathf.Min(rect.width, rect.height) * 0.5f;
            float thickness = halfSize * strokeThicknessRatio;
            Vector2 centre = rect.center;

            Vector2 tip = centre + new Vector2(-halfSize * tipInsetRatio, 0f);
            Vector2 topEnd = centre
                + new Vector2(halfSize * armLengthRatio * 0.5f, halfSize * armLengthRatio);
            Vector2 bottomEnd = centre
                + new Vector2(halfSize * armLengthRatio * 0.5f, -halfSize * armLengthRatio);

            Color32 fillColour = color;
            AddStroke(vertexHelper, tip, topEnd, thickness, fillColour);
            AddStroke(vertexHelper, tip, bottomEnd, thickness, fillColour);
        }

        private static void AddStroke(
            VertexHelper vertexHelper, Vector2 from, Vector2 to, float thickness, Color32 colour)
        {
            Vector2 direction = (to - from).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);

            int a = AddVertex(vertexHelper, from - normal, colour);
            int b = AddVertex(vertexHelper, from + normal, colour);
            int c = AddVertex(vertexHelper, to + normal, colour);
            int d = AddVertex(vertexHelper, to - normal, colour);

            vertexHelper.AddTriangle(a, b, c);
            vertexHelper.AddTriangle(a, c, d);
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
            strokeThicknessRatio = Mathf.Clamp(strokeThicknessRatio, 0.05f, 0.4f);
            armLengthRatio = Mathf.Clamp(armLengthRatio, 0.2f, 0.95f);
            tipInsetRatio = Mathf.Clamp(tipInsetRatio, 0f, 0.9f);
            SetVerticesDirty();
        }
#endif
    }
}
