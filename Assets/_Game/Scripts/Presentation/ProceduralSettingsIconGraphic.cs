using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Draws one of a small set of texture-free Settings-menu badge icons (~32-48px), selected via
    /// <see cref="SettingsIconShape"/>.
    /// </summary>
    /// <remarks>
    /// One flexible class rather than one class per shape (unlike
    /// <see cref="ProceduralGearIconGraphic"/>/<see cref="ProceduralArrowIconGraphic"/>/
    /// <see cref="ProceduralTriangleIconGraphic"/>, each its own sealed class) — those three were
    /// each added ad hoc for a single specific button; these six are authored together, in one
    /// pass, for one identical use (an icon centred in a diamond badge), so one enum-driven class
    /// keeps the new-file/meta overhead down. <see cref="ProceduralGearIconGraphic"/> itself is
    /// still reused as-is wherever a gear icon is needed — it is not folded into this enum.
    /// </remarks>
    public sealed class ProceduralSettingsIconGraphic : MaskableGraphic
    {
        public enum SettingsIconShape
        {
            Speaker,
            Monitor,
            Gamepad,
            MusicNote,
            Sparkle,
            Vibration
        }

        [SerializeField] private SettingsIconShape shape = SettingsIconShape.Speaker;

        [Tooltip("Speaker only: swaps the sound-wave arcs for a muted X stroke.")]
        [SerializeField] private bool muted;

        public SettingsIconShape Shape => shape;

        public bool Muted => muted;

        public void SetStyle(SettingsIconShape shape, Color colour, bool muted = false)
        {
            this.shape = shape;
            this.muted = muted;
            color = colour;
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

            Color32 iconColour = color;

            switch (shape)
            {
                case SettingsIconShape.Speaker:
                    AddSpeaker(vertexHelper, rect, iconColour, muted);
                    break;
                case SettingsIconShape.Monitor:
                    AddMonitor(vertexHelper, rect, iconColour);
                    break;
                case SettingsIconShape.Gamepad:
                    AddGamepad(vertexHelper, rect, iconColour);
                    break;
                case SettingsIconShape.MusicNote:
                    AddMusicNote(vertexHelper, rect, iconColour);
                    break;
                case SettingsIconShape.Sparkle:
                    AddSparkle(vertexHelper, rect, iconColour);
                    break;
                case SettingsIconShape.Vibration:
                    AddVibration(vertexHelper, rect, iconColour);
                    break;
            }
        }

        // ------------------------------------------------------------------------------------
        // Per-shape builders. Each works in a normalised [-0.5, 0.5] square (mapped to the rect
        // via the local P() helper, scaled by the rect's shorter side so icons never stretch on
        // a non-square badge) and composes the shared primitives below — the same simple-geometry
        // approach ProceduralGearIconGraphic/ProceduralArrowIconGraphic already use.
        // ------------------------------------------------------------------------------------

        private static void AddSpeaker(VertexHelper vh, Rect rect, Color32 colour, bool muted)
        {
            Vector2 c = rect.center;
            float s = Mathf.Min(rect.width, rect.height);
            Vector2 P(float nx, float ny) => c + (new Vector2(nx, ny) * s);

            AddQuadPoints(vh, P(-0.38f, -0.14f), P(-0.38f, 0.14f), P(-0.16f, 0.14f), P(-0.16f, -0.14f), colour);
            AddQuadPoints(vh, P(-0.16f, 0.14f), P(0.05f, 0.36f), P(0.05f, -0.36f), P(-0.16f, -0.14f), colour);

            if (muted)
            {
                float thickness = s * 0.08f;
                AddStroke(vh, P(0.06f, 0.22f), P(0.34f, -0.22f), thickness, colour);
                AddStroke(vh, P(0.06f, -0.22f), P(0.34f, 0.22f), thickness, colour);
            }
            else
            {
                float arcThickness = s * 0.05f;
                AddArcStroke(vh, P(0.05f, 0f), s * 0.16f, arcThickness, -50f, 50f, 6, colour);
                AddArcStroke(vh, P(0.05f, 0f), s * 0.28f, arcThickness, -50f, 50f, 6, colour);
            }
        }

        private static void AddMonitor(VertexHelper vh, Rect rect, Color32 colour)
        {
            Vector2 c = rect.center;
            float s = Mathf.Min(rect.width, rect.height);
            Vector2 P(float nx, float ny) => c + (new Vector2(nx, ny) * s);

            Vector2 screenMin = P(-0.4f, -0.05f);
            Vector2 screenMax = P(0.4f, 0.35f);
            AddRectRing(vh, screenMin.x, screenMin.y, screenMax.x, screenMax.y, s * 0.07f, colour);

            AddAxisAlignedQuad(vh, P(-0.06f, -0.2f), P(0.06f, -0.05f), colour);
            AddAxisAlignedQuad(vh, P(-0.18f, -0.28f), P(0.18f, -0.2f), colour);
        }

        private static void AddGamepad(VertexHelper vh, Rect rect, Color32 colour)
        {
            Vector2 c = rect.center;
            float s = Mathf.Min(rect.width, rect.height);
            Vector2 P(float nx, float ny) => c + (new Vector2(nx, ny) * s);

            AddAxisAlignedQuad(vh, P(-0.4f, -0.18f), P(0.4f, 0.18f), colour);

            AddAxisAlignedQuad(vh, P(-0.27f, -0.05f), P(-0.13f, 0.05f), colour);
            AddAxisAlignedQuad(vh, P(-0.24f, -0.11f), P(-0.16f, 0.11f), colour);

            AddRegularPolygon(vh, P(0.18f, 0.07f), s * 0.06f, 8, 0f, colour);
            AddRegularPolygon(vh, P(0.30f, -0.07f), s * 0.06f, 8, 0f, colour);
        }

        private static void AddMusicNote(VertexHelper vh, Rect rect, Color32 colour)
        {
            Vector2 c = rect.center;
            float s = Mathf.Min(rect.width, rect.height);
            Vector2 P(float nx, float ny) => c + (new Vector2(nx, ny) * s);

            AddRegularPolygon(vh, P(-0.16f, -0.28f), s * 0.13f, 10, 0f, colour);
            AddAxisAlignedQuad(vh, P(-0.05f, -0.28f), P(0.01f, 0.32f), colour);
            AddQuadPoints(vh, P(0.01f, 0.32f), P(0.24f, 0.22f), P(0.24f, 0.08f), P(0.01f, 0.14f), colour);
        }

        private static void AddSparkle(VertexHelper vh, Rect rect, Color32 colour)
        {
            Vector2 c = rect.center;
            float s = Mathf.Min(rect.width, rect.height);
            Vector2 P(float nx, float ny) => c + (new Vector2(nx, ny) * s);

            AddStarPolygon(vh, P(-0.02f, -0.02f), s * 0.34f, s * 0.11f, 4, 0f, colour);
            AddStarPolygon(vh, P(0.28f, 0.28f), s * 0.14f, s * 0.045f, 4, 0f, colour);
        }

        private static void AddVibration(VertexHelper vh, Rect rect, Color32 colour)
        {
            Vector2 c = rect.center;
            float s = Mathf.Min(rect.width, rect.height);
            Vector2 P(float nx, float ny) => c + (new Vector2(nx, ny) * s);

            AddAxisAlignedQuad(vh, P(-0.14f, -0.36f), P(0.14f, 0.36f), colour);

            float thickness = s * 0.06f;
            AddStroke(vh, P(-0.24f, -0.2f), P(-0.34f, -0.1f), thickness, colour);
            AddStroke(vh, P(-0.24f, 0f), P(-0.36f, 0f), thickness, colour);
            AddStroke(vh, P(-0.24f, 0.2f), P(-0.34f, 0.1f), thickness, colour);
            AddStroke(vh, P(0.24f, -0.2f), P(0.34f, -0.1f), thickness, colour);
            AddStroke(vh, P(0.24f, 0f), P(0.36f, 0f), thickness, colour);
            AddStroke(vh, P(0.24f, 0.2f), P(0.34f, 0.1f), thickness, colour);
        }

        // ------------------------------------------------------------------------------------
        // Shared mesh primitives.
        // ------------------------------------------------------------------------------------

        private static void AddQuadPoints(
            VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 colour)
        {
            int start = vh.currentVertCount;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = colour;

            vertex.position = a;
            vh.AddVert(vertex);
            vertex.position = b;
            vh.AddVert(vertex);
            vertex.position = c;
            vh.AddVert(vertex);
            vertex.position = d;
            vh.AddVert(vertex);

            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddAxisAlignedQuad(VertexHelper vh, Vector2 min, Vector2 max, Color32 colour)
        {
            AddQuadPoints(
                vh, new Vector2(min.x, min.y), new Vector2(min.x, max.y),
                new Vector2(max.x, max.y), new Vector2(max.x, min.y), colour);
        }

        /// <summary>A thick line segment between two points, built as a quad offset perpendicular
        /// to the segment's own direction — used for X marks, D-pad arms and motion lines.</summary>
        private static void AddStroke(VertexHelper vh, Vector2 from, Vector2 to, float thickness, Color32 colour)
        {
            Vector2 direction = (to - from).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);
            AddQuadPoints(vh, from - perpendicular, from + perpendicular, to + perpendicular, to - perpendicular, colour);
        }

        /// <summary>A regular N-sided polygon (approximates a circle at higher side counts) —
        /// used for round note heads and buttons.</summary>
        private static void AddRegularPolygon(
            VertexHelper vh, Vector2 centre, float radius, int sides, float rotationDeg, Color32 colour)
        {
            int start = vh.currentVertCount;

            UIVertex centreVertex = UIVertex.simpleVert;
            centreVertex.color = colour;
            centreVertex.position = centre;
            vh.AddVert(centreVertex);

            for (int i = 0; i < sides; i++)
            {
                float degrees = rotationDeg + (360f * i / sides);
                float radians = degrees * Mathf.Deg2Rad;
                UIVertex vertex = UIVertex.simpleVert;
                vertex.color = colour;
                vertex.position = centre + (new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius);
                vh.AddVert(vertex);
            }

            for (int i = 0; i < sides; i++)
            {
                vh.AddTriangle(start, start + 1 + i, start + 1 + ((i + 1) % sides));
            }
        }

        /// <summary>An N-pointed star, alternating outer and inner radius vertices — used for the
        /// sparkle icon.</summary>
        private static void AddStarPolygon(
            VertexHelper vh, Vector2 centre, float outerRadius, float innerRadius, int points,
            float rotationDeg, Color32 colour)
        {
            int start = vh.currentVertCount;

            UIVertex centreVertex = UIVertex.simpleVert;
            centreVertex.color = colour;
            centreVertex.position = centre;
            vh.AddVert(centreVertex);

            int vertexCount = points * 2;
            for (int i = 0; i < vertexCount; i++)
            {
                float degrees = rotationDeg + (360f * i / vertexCount);
                float radians = degrees * Mathf.Deg2Rad;
                float radius = (i % 2 == 0) ? outerRadius : innerRadius;
                UIVertex vertex = UIVertex.simpleVert;
                vertex.color = colour;
                vertex.position = centre + (new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius);
                vh.AddVert(vertex);
            }

            for (int i = 0; i < vertexCount; i++)
            {
                vh.AddTriangle(start, start + 1 + i, start + 1 + ((i + 1) % vertexCount));
            }
        }

        /// <summary>A rectangular picture-frame border (4 quads) — used for the monitor screen
        /// bezel.</summary>
        private static void AddRectRing(
            VertexHelper vh, float xMin, float yMin, float xMax, float yMax, float thickness, Color32 colour)
        {
            AddQuadPoints(vh, new Vector2(xMin, yMax - thickness), new Vector2(xMin, yMax),
                new Vector2(xMax, yMax), new Vector2(xMax, yMax - thickness), colour);
            AddQuadPoints(vh, new Vector2(xMin, yMin), new Vector2(xMin, yMin + thickness),
                new Vector2(xMax, yMin + thickness), new Vector2(xMax, yMin), colour);
            AddQuadPoints(vh, new Vector2(xMin, yMin), new Vector2(xMin, yMax),
                new Vector2(xMin + thickness, yMax), new Vector2(xMin + thickness, yMin), colour);
            AddQuadPoints(vh, new Vector2(xMax - thickness, yMin), new Vector2(xMax - thickness, yMax),
                new Vector2(xMax, yMax), new Vector2(xMax, yMin), colour);
        }

        /// <summary>A thick partial-ring stroke between <paramref name="startDeg"/> and
        /// <paramref name="endDeg"/> — used for the speaker's sound-wave arcs.</summary>
        private static void AddArcStroke(
            VertexHelper vh, Vector2 centre, float radius, float thickness, float startDeg, float endDeg,
            int segments, Color32 colour)
        {
            float innerRadius = radius - (thickness * 0.5f);
            float outerRadius = radius + (thickness * 0.5f);
            int start = vh.currentVertCount;

            for (int i = 0; i <= segments; i++)
            {
                float degrees = Mathf.Lerp(startDeg, endDeg, (float)i / segments);
                float radians = degrees * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

                UIVertex inner = UIVertex.simpleVert;
                inner.color = colour;
                inner.position = centre + (direction * innerRadius);
                vh.AddVert(inner);

                UIVertex outer = UIVertex.simpleVert;
                outer.color = colour;
                outer.position = centre + (direction * outerRadius);
                vh.AddVert(outer);
            }

            for (int i = 0; i < segments; i++)
            {
                int innerCurrent = start + (i * 2);
                int outerCurrent = start + (i * 2) + 1;
                int innerNext = start + ((i + 1) * 2);
                int outerNext = start + ((i + 1) * 2) + 1;

                vh.AddTriangle(innerCurrent, outerCurrent, outerNext);
                vh.AddTriangle(innerCurrent, outerNext, innerNext);
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
