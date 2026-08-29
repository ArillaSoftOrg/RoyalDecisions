using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>Draws a texture-free CRT scanline overlay: repeating horizontal bars at a fixed
    /// spacing, tinted by the inherited <see cref="Graphic.color"/> (typically low-alpha black).</summary>
    public sealed class ProceduralScanlineGraphic : MaskableGraphic
    {
        [Range(1f, 40f)]
        [SerializeField] private float lineSpacing = 4f;

        [Range(0.25f, 10f)]
        [SerializeField] private float lineThickness = 1f;

        public float LineSpacing => lineSpacing;

        public float LineThickness => lineThickness;

        public void SetStyle(Color lineColour, float spacing, float thickness)
        {
            color = lineColour;
            lineSpacing = Mathf.Clamp(spacing, 1f, 40f);
            lineThickness = Mathf.Clamp(thickness, 0.25f, 10f);
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f || lineSpacing <= 0f)
            {
                return;
            }

            Color32 lineColour = color;
            float thickness = Mathf.Min(lineThickness, lineSpacing);

            for (float y = rect.yMin; y < rect.yMax; y += lineSpacing)
            {
                float top = Mathf.Min(y + thickness, rect.yMax);
                AddQuad(vertexHelper, rect.xMin, y, rect.xMax, top, lineColour);
            }
        }

        private static void AddQuad(
            VertexHelper vertexHelper, float xMin, float yMin, float xMax, float yMax, Color32 colour)
        {
            int start = vertexHelper.currentVertCount;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = colour;

            vertex.position = new Vector3(xMin, yMin);
            vertexHelper.AddVert(vertex);
            vertex.position = new Vector3(xMin, yMax);
            vertexHelper.AddVert(vertex);
            vertex.position = new Vector3(xMax, yMax);
            vertexHelper.AddVert(vertex);
            vertex.position = new Vector3(xMax, yMin);
            vertexHelper.AddVert(vertex);

            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start, start + 2, start + 3);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            lineSpacing = Mathf.Clamp(lineSpacing, 1f, 40f);
            lineThickness = Mathf.Clamp(lineThickness, 0.25f, 10f);
            raycastTarget = false;
            SetVerticesDirty();
        }
#endif
    }
}
