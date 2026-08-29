using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>Draws a single top-left L-shaped corner-bracket accent. The other three corners of
    /// a card are achieved by flipping the owning GameObject's localScale (the standard Unity UI
    /// trick), so this mesh only ever needs to know about one orientation.</summary>
    public sealed class ProceduralCornerBracketGraphic : MaskableGraphic
    {
        [Range(0.1f, 0.9f)]
        [SerializeField] private float armLengthRatio = 0.35f;

        [Range(0.02f, 0.3f)]
        [SerializeField] private float thicknessRatio = 0.08f;

        public float ArmLengthRatio => armLengthRatio;

        public float ThicknessRatio => thicknessRatio;

        public void SetStyle(Color colour, float armLengthRatio, float thicknessRatio)
        {
            color = colour;
            this.armLengthRatio = Mathf.Clamp(armLengthRatio, 0.1f, 0.9f);
            this.thicknessRatio = Mathf.Clamp(thicknessRatio, 0.02f, 0.3f);
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

            float armLengthX = rect.width * armLengthRatio;
            float armLengthY = rect.height * armLengthRatio;
            float thickness = Mathf.Min(rect.width, rect.height) * thicknessRatio;
            Color32 strokeColour = color;

            AddQuad(vertexHelper,
                rect.xMin, rect.yMax - thickness, rect.xMin + armLengthX, rect.yMax, strokeColour);
            AddQuad(vertexHelper,
                rect.xMin, rect.yMax - armLengthY, rect.xMin + thickness, rect.yMax, strokeColour);
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
            armLengthRatio = Mathf.Clamp(armLengthRatio, 0.1f, 0.9f);
            thicknessRatio = Mathf.Clamp(thicknessRatio, 0.02f, 0.3f);
            raycastTarget = false;
            SetVerticesDirty();
        }
#endif
    }
}
