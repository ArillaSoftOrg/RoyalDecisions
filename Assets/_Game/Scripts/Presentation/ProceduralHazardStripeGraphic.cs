using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>Draws a texture-free hazard-tape pattern: vertical bands alternating between
    /// <see cref="Graphic.color"/> and <see cref="secondaryColour"/>. Diagonal "hazard tape" is
    /// achieved by rotating the owning RectTransform — this graphic only tiles straight bands.</summary>
    public sealed class ProceduralHazardStripeGraphic : MaskableGraphic
    {
        [SerializeField] private Color secondaryColour = new Color(0.1098f, 0.098f, 0.0784f, 1f);

        [Range(2f, 100f)]
        [SerializeField] private float stripeWidth = 20f;

        public Color SecondaryColour => secondaryColour;

        public float StripeWidth => stripeWidth;

        public void SetStyle(Color primaryColour, Color secondary, float width)
        {
            color = primaryColour;
            secondaryColour = secondary;
            stripeWidth = Mathf.Clamp(width, 2f, 100f);
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f || stripeWidth <= 0f)
            {
                return;
            }

            int bandIndex = 0;
            for (float x = rect.xMin; x < rect.xMax; x += stripeWidth, bandIndex++)
            {
                float right = Mathf.Min(x + stripeWidth, rect.xMax);
                Color32 bandColour = (bandIndex % 2 == 0) ? color : secondaryColour;
                AddQuad(vertexHelper, x, rect.yMin, right, rect.yMax, bandColour);
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
            stripeWidth = Mathf.Clamp(stripeWidth, 2f, 100f);
            raycastTarget = false;
            SetVerticesDirty();
        }
#endif
    }
}
