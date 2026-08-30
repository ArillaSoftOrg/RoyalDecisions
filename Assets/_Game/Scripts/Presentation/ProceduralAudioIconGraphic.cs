using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Draws a texture-free speaker / musical-note / equaliser silhouette, selected by
    /// <see cref="AudioIconKind"/>. One class rather than three because all three shapes are built
    /// from the same quad/disc/arc primitives — a designer sprite is never needed for the volume
    /// rows' leading icons.
    /// </summary>
    /// <remarks>
    /// All geometry is expressed in units of the rect's shorter side, measured from its centre, so
    /// the icon stays square and centred whatever rect the layout hands it.
    /// </remarks>
    public sealed class ProceduralAudioIconGraphic : MaskableGraphic
    {
        private const int DiscSegments = 20;
        private const int ArcSegments = 12;

        [SerializeField] private AudioIconKind kind = AudioIconKind.Speaker;

        public AudioIconKind Kind => kind;

        public void SetKind(AudioIconKind value)
        {
            kind = value;
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
            float scale = Mathf.Min(rect.width, rect.height);
            Color32 fill = color;

            switch (kind)
            {
                case AudioIconKind.Note:
                    AddNote(vertexHelper, centre, scale, fill);
                    break;
                case AudioIconKind.Effect:
                    AddEqualiser(vertexHelper, centre, scale, fill);
                    break;
                default:
                    AddSpeaker(vertexHelper, centre, scale, fill);
                    break;
            }
        }

        private static void AddSpeaker(
            VertexHelper vertexHelper, Vector2 centre, float scale, Color32 fill)
        {
            // Driver box, then the cone flaring out of it as a single trapezoid.
            AddRect(vertexHelper, centre, scale, -0.30f, -0.12f, -0.12f, 0.12f, fill);
            AddQuad(vertexHelper,
                Point(centre, scale, -0.12f, -0.12f),
                Point(centre, scale, -0.12f, 0.12f),
                Point(centre, scale, 0.06f, 0.32f),
                Point(centre, scale, 0.06f, -0.32f),
                fill);

            // Two radiating waves, struck from the mouth of the cone.
            Vector2 origin = Point(centre, scale, 0.06f, 0f);
            AddArc(vertexHelper, origin, scale * 0.16f, scale * 0.045f, -52f, 52f, fill);
            AddArc(vertexHelper, origin, scale * 0.30f, scale * 0.045f, -52f, 52f, fill);
        }

        private static void AddNote(
            VertexHelper vertexHelper, Vector2 centre, float scale, Color32 fill)
        {
            AddRect(vertexHelper, centre, scale, 0.02f, -0.12f, 0.08f, 0.32f, fill);
            // Flag, drawn as a slab off the top of the stem rather than a curl: at icon sizes a
            // curved tail collapses into a smudge, a solid beam stays readable.
            AddQuad(vertexHelper,
                Point(centre, scale, 0.08f, 0.32f),
                Point(centre, scale, 0.30f, 0.22f),
                Point(centre, scale, 0.30f, 0.08f),
                Point(centre, scale, 0.08f, 0.18f),
                fill);
            AddDisc(vertexHelper, Point(centre, scale, -0.06f, -0.18f), scale * 0.15f, fill);
        }

        private static void AddEqualiser(
            VertexHelper vertexHelper, Vector2 centre, float scale, Color32 fill)
        {
            const float baseline = -0.30f;
            AddRect(vertexHelper, centre, scale, -0.30f, baseline, -0.17f, baseline + 0.30f, fill);
            AddRect(vertexHelper, centre, scale, -0.07f, baseline, 0.06f, baseline + 0.60f, fill);
            AddRect(vertexHelper, centre, scale, 0.16f, baseline, 0.29f, baseline + 0.44f, fill);
        }

        private static void AddArc(
            VertexHelper vertexHelper,
            Vector2 origin,
            float radius,
            float thickness,
            float startDegrees,
            float endDegrees,
            Color32 fill)
        {
            float innerRadius = Mathf.Max(0f, radius - (thickness * 0.5f));
            float outerRadius = radius + (thickness * 0.5f);
            int firstIndex = vertexHelper.currentVertCount;

            for (int i = 0; i <= ArcSegments; i++)
            {
                float degrees = Mathf.Lerp(startDegrees, endDegrees, i / (float)ArcSegments);
                Vector2 direction = Direction(degrees);
                AddVertex(vertexHelper, origin + (direction * outerRadius), fill);
                AddVertex(vertexHelper, origin + (direction * innerRadius), fill);
            }

            for (int i = 0; i < ArcSegments; i++)
            {
                int outerCurrent = firstIndex + (i * 2);
                int innerCurrent = outerCurrent + 1;
                int outerNext = outerCurrent + 2;
                int innerNext = outerCurrent + 3;
                vertexHelper.AddTriangle(outerCurrent, outerNext, innerNext);
                vertexHelper.AddTriangle(outerCurrent, innerNext, innerCurrent);
            }
        }

        private static void AddDisc(
            VertexHelper vertexHelper, Vector2 origin, float radius, Color32 fill)
        {
            int centreIndex = AddVertex(vertexHelper, origin, fill);
            for (int i = 0; i < DiscSegments; i++)
            {
                AddVertex(vertexHelper, origin + (Direction(360f * i / DiscSegments) * radius), fill);
            }

            for (int i = 0; i < DiscSegments; i++)
            {
                int current = centreIndex + 1 + i;
                int next = centreIndex + 1 + ((i + 1) % DiscSegments);
                vertexHelper.AddTriangle(centreIndex, current, next);
            }
        }

        private static void AddRect(
            VertexHelper vertexHelper,
            Vector2 centre,
            float scale,
            float xMin,
            float yMin,
            float xMax,
            float yMax,
            Color32 fill)
        {
            AddQuad(vertexHelper,
                Point(centre, scale, xMin, yMin),
                Point(centre, scale, xMin, yMax),
                Point(centre, scale, xMax, yMax),
                Point(centre, scale, xMax, yMin),
                fill);
        }

        private static void AddQuad(
            VertexHelper vertexHelper,
            Vector2 first,
            Vector2 second,
            Vector2 third,
            Vector2 fourth,
            Color32 fill)
        {
            int start = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, first, fill);
            AddVertex(vertexHelper, second, fill);
            AddVertex(vertexHelper, third, fill);
            AddVertex(vertexHelper, fourth, fill);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start, start + 2, start + 3);
        }

        private static int AddVertex(VertexHelper vertexHelper, Vector2 position, Color32 colour)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = colour;
            vertex.position = position;
            vertexHelper.AddVert(vertex);
            return vertexHelper.currentVertCount - 1;
        }

        private static Vector2 Point(Vector2 centre, float scale, float x, float y)
        {
            return new Vector2(centre.x + (x * scale), centre.y + (y * scale));
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
            SetVerticesDirty();
        }
#endif
    }
}
