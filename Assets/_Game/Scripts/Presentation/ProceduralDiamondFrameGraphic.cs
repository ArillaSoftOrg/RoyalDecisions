using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>Draws a texture-free diamond inscribed in the rect's own edge midpoints — the mesh
    /// itself is the diamond, so the owning RectTransform never needs a 45° rotation (which would
    /// also rotate any icon child sitting on top of it).</summary>
    public sealed class ProceduralDiamondFrameGraphic : MaskableGraphic
    {
        /// <summary>0 = solid filled diamond. Above 0 = a hollow diamond ring, the border's
        /// thickness scaling with the diamond's own half-width.</summary>
        [Range(0f, 1f)]
        [SerializeField] private float borderThicknessRatio;

        public float BorderThicknessRatio => borderThicknessRatio;

        public void SetStyle(Color colour, float borderThicknessRatio)
        {
            color = colour;
            this.borderThicknessRatio = Mathf.Clamp01(borderThicknessRatio);
            raycastTarget = false;
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

            Vector2 centre = rect.center;
            Vector2[] outerPoints =
            {
                new Vector2(centre.x, rect.yMax),
                new Vector2(rect.xMax, centre.y),
                new Vector2(centre.x, rect.yMin),
                new Vector2(rect.xMin, centre.y)
            };
            Color32 fillColour = color;

            if (borderThicknessRatio <= 0f)
            {
                AddSolidDiamond(vertexHelper, centre, outerPoints, fillColour);
                return;
            }

            AddHollowDiamond(vertexHelper, centre, outerPoints, fillColour);
        }

        private static void AddSolidDiamond(
            VertexHelper vertexHelper, Vector2 centre, Vector2[] outerPoints, Color32 fillColour)
        {
            UIVertex centreVertex = UIVertex.simpleVert;
            centreVertex.color = fillColour;
            centreVertex.position = centre;
            vertexHelper.AddVert(centreVertex);

            for (int i = 0; i < 4; i++)
            {
                UIVertex vertex = UIVertex.simpleVert;
                vertex.color = fillColour;
                vertex.position = outerPoints[i];
                vertexHelper.AddVert(vertex);
            }

            for (int i = 0; i < 4; i++)
            {
                vertexHelper.AddTriangle(0, 1 + i, 1 + ((i + 1) % 4));
            }
        }

        private void AddHollowDiamond(
            VertexHelper vertexHelper, Vector2 centre, Vector2[] outerPoints, Color32 fillColour)
        {
            float innerScale = 1f - borderThicknessRatio;

            for (int i = 0; i < 4; i++)
            {
                UIVertex outer = UIVertex.simpleVert;
                outer.color = fillColour;
                outer.position = outerPoints[i];
                vertexHelper.AddVert(outer);

                UIVertex inner = UIVertex.simpleVert;
                inner.color = fillColour;
                inner.position = centre + (outerPoints[i] - centre) * innerScale;
                vertexHelper.AddVert(inner);
            }

            for (int i = 0; i < 4; i++)
            {
                int outerCurrent = i * 2;
                int innerCurrent = (i * 2) + 1;
                int outerNext = ((i + 1) % 4) * 2;
                int innerNext = (((i + 1) % 4) * 2) + 1;

                vertexHelper.AddTriangle(outerCurrent, outerNext, innerCurrent);
                vertexHelper.AddTriangle(outerNext, innerNext, innerCurrent);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            borderThicknessRatio = Mathf.Clamp01(borderThicknessRatio);
            SetVerticesDirty();
        }
#endif
    }
}
