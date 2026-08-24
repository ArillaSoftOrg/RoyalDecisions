using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Draws a texture-free gear/cog silhouette: a hollow ring plus evenly spaced radial teeth.
    /// Used as the settings icon so the button reads as "settings" without a designer sprite.
    /// </summary>
    public sealed class ProceduralGearIconGraphic : MaskableGraphic
    {
        private const int RingSegments = 32;
        private const int MinimumTeeth = 3;
        private const int MaximumTeeth = 16;

        [Range(MinimumTeeth, MaximumTeeth)]
        [SerializeField] private int toothCount = 8;

        [Range(0.1f, 0.85f)]
        [SerializeField] private float holeRadiusRatio = 0.34f;

        [Range(0.2f, 0.95f)]
        [SerializeField] private float ringOuterRadiusRatio = 0.62f;

        [Range(0.55f, 1f)]
        [SerializeField] private float toothTipRadiusRatio = 0.92f;

        [Range(0.15f, 0.9f)]
        [SerializeField] private float toothWidthRatio = 0.55f;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f || toothCount <= 0)
            {
                return;
            }

            Vector2 centre = rect.center;
            float baseRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            float holeRadius = baseRadius * Mathf.Min(holeRadiusRatio, ringOuterRadiusRatio * 0.95f);
            float ringOuterRadius = baseRadius * ringOuterRadiusRatio;
            float toothTipRadius = Mathf.Max(ringOuterRadius, baseRadius * toothTipRadiusRatio);
            Color32 fillColour = color;

            AddRing(vertexHelper, centre, holeRadius, ringOuterRadius, fillColour);
            AddTeeth(vertexHelper, centre, ringOuterRadius, toothTipRadius, fillColour);
        }

        private void AddTeeth(
            VertexHelper vertexHelper,
            Vector2 centre,
            float rootRadius,
            float tipRadius,
            Color32 fillColour)
        {
            float slotDegrees = 360f / toothCount;
            float halfToothDegrees = slotDegrees * toothWidthRatio * 0.5f;

            for (int tooth = 0; tooth < toothCount; tooth++)
            {
                float centreDegrees = slotDegrees * tooth;
                float startDegrees = centreDegrees - halfToothDegrees;
                float endDegrees = centreDegrees + halfToothDegrees;

                Vector2 rootStart = centre + Direction(startDegrees) * rootRadius;
                Vector2 rootEnd = centre + Direction(endDegrees) * rootRadius;
                Vector2 tipStart = centre + Direction(startDegrees) * tipRadius;
                Vector2 tipEnd = centre + Direction(endDegrees) * tipRadius;

                int rootStartIndex = AddVertex(vertexHelper, rootStart, fillColour);
                int tipStartIndex = AddVertex(vertexHelper, tipStart, fillColour);
                int tipEndIndex = AddVertex(vertexHelper, tipEnd, fillColour);
                int rootEndIndex = AddVertex(vertexHelper, rootEnd, fillColour);

                vertexHelper.AddTriangle(rootStartIndex, tipStartIndex, tipEndIndex);
                vertexHelper.AddTriangle(rootStartIndex, tipEndIndex, rootEndIndex);
            }
        }

        private static void AddRing(
            VertexHelper vertexHelper,
            Vector2 centre,
            float innerRadius,
            float outerRadius,
            Color32 fillColour)
        {
            int firstIndex = vertexHelper.currentVertCount;
            for (int i = 0; i < RingSegments; i++)
            {
                float degrees = 360f * i / RingSegments;
                Vector2 direction = Direction(degrees);
                AddVertex(vertexHelper, centre + direction * outerRadius, fillColour);
                AddVertex(vertexHelper, centre + direction * innerRadius, fillColour);
            }

            for (int i = 0; i < RingSegments; i++)
            {
                int next = (i + 1) % RingSegments;
                int outerCurrent = firstIndex + (i * 2);
                int innerCurrent = outerCurrent + 1;
                int outerNext = firstIndex + (next * 2);
                int innerNext = outerNext + 1;

                vertexHelper.AddTriangle(outerCurrent, outerNext, innerNext);
                vertexHelper.AddTriangle(outerCurrent, innerNext, innerCurrent);
            }
        }

        private static int AddVertex(VertexHelper vertexHelper, Vector2 position, Color32 colour)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = colour;
            vertex.position = position;
            vertexHelper.AddVert(vertex);
            return vertexHelper.currentVertCount - 1;
        }

        private static Vector2 Direction(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            toothCount = Mathf.Clamp(toothCount, MinimumTeeth, MaximumTeeth);
            holeRadiusRatio = Mathf.Clamp(holeRadiusRatio, 0.1f, 0.85f);
            ringOuterRadiusRatio = Mathf.Clamp(ringOuterRadiusRatio, 0.2f, 0.95f);
            toothTipRadiusRatio = Mathf.Clamp(toothTipRadiusRatio, 0.55f, 1f);
            toothWidthRatio = Mathf.Clamp(toothWidthRatio, 0.15f, 0.9f);
            SetVerticesDirty();
        }
#endif
    }
}
