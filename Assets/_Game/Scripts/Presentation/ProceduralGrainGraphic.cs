using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Texture-free weathering overlay: a field of low-alpha speckles plus a few vertical brushed
    /// streaks, giving the flat menu panels the worn metal surface CLAUDE.md's visual language asks
    /// for without shipping a single texture asset.
    /// </summary>
    /// <remarks>
    /// The pattern comes from a deterministic integer hash of each cell's grid coordinates, not
    /// <see cref="Random"/>: the same rect always produces the same speckles, so the grain never
    /// shimmers between rebuilds and an authoring pass writing this component produces identical
    /// scene YAML every run. The mesh is built only in <see cref="OnPopulateMesh"/> — which Unity
    /// calls on rect/colour changes, not per frame — so there is no per-frame allocation.
    /// </remarks>
    public sealed class ProceduralGrainGraphic : MaskableGraphic
    {
        private const int MinimumCells = 4;
        private const int MaximumCells = 64;

        [Range(MinimumCells, MaximumCells)]
        [SerializeField] private int columns = 18;

        [Range(MinimumCells, MaximumCells)]
        [SerializeField] private int rows = 32;

        /// <summary>Fraction of grid cells that receive a speckle.</summary>
        [Range(0f, 1f)]
        [SerializeField] private float density = 0.32f;

        /// <summary>Alpha of the strongest speckle; every other speckle is a fraction of this.</summary>
        [Range(0f, 0.5f)]
        [SerializeField] private float maximumAlpha = 0.05f;

        /// <summary>Fraction of columns that carry a full-height brushed streak.</summary>
        [Range(0f, 1f)]
        [SerializeField] private float streakDensity = 0.22f;

        [SerializeField] private int seed = 1481;

        public void SetStyle(Color colour, float density, float maximumAlpha)
        {
            color = colour;
            this.density = Mathf.Clamp01(density);
            this.maximumAlpha = Mathf.Clamp(maximumAlpha, 0f, 0.5f);
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f || columns <= 0 || rows <= 0)
            {
                return;
            }

            float cellWidth = rect.width / columns;
            float cellHeight = rect.height / rows;

            AddStreaks(vertexHelper, rect, cellWidth);
            AddSpeckles(vertexHelper, rect, cellWidth, cellHeight);
        }

        private void AddSpeckles(
            VertexHelper vertexHelper, Rect rect, float cellWidth, float cellHeight)
        {
            for (int column = 0; column < columns; column++)
            {
                for (int row = 0; row < rows; row++)
                {
                    float pick = Hash(column, row, seed);
                    if (pick > density)
                    {
                        continue;
                    }

                    // A second, independent hash decides both how strong this speckle is and where
                    // inside its cell it sits, so the field never reads as a regular grid.
                    float strength = Hash(column, row, seed + 1);
                    float offsetX = Hash(column, row, seed + 2) * 0.5f;
                    float offsetY = Hash(column, row, seed + 3) * 0.5f;

                    float xMin = rect.xMin + ((column + offsetX) * cellWidth);
                    float yMin = rect.yMin + ((row + offsetY) * cellHeight);
                    float width = cellWidth * Mathf.Lerp(0.18f, 0.5f, strength);
                    float height = cellHeight * Mathf.Lerp(0.18f, 0.5f, strength);

                    Color speckle = color;
                    speckle.a = maximumAlpha * Mathf.Lerp(0.35f, 1f, strength);
                    AddQuad(vertexHelper, xMin, yMin, xMin + width, yMin + height, speckle);
                }
            }
        }

        private void AddStreaks(VertexHelper vertexHelper, Rect rect, float cellWidth)
        {
            for (int column = 0; column < columns; column++)
            {
                if (Hash(column, 0, seed + 4) > streakDensity)
                {
                    continue;
                }

                float strength = Hash(column, 0, seed + 5);
                float xMin = rect.xMin + (column * cellWidth);
                float width = cellWidth * Mathf.Lerp(0.08f, 0.28f, strength);

                Color streak = color;
                // Streaks sit well under the speckles: they are meant to be felt, not seen.
                streak.a = maximumAlpha * Mathf.Lerp(0.15f, 0.45f, strength);
                AddQuad(vertexHelper, xMin, rect.yMin, xMin + width, rect.yMax, streak);
            }
        }

        /// <summary>
        /// Deterministic 2D integer hash returning 0..1. Chosen over <see cref="Random"/> so the
        /// same grid always yields the same pattern — see the class remarks.
        /// </summary>
        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint hash = (uint)x * 2654435761u;
                hash ^= (uint)y * 2246822519u;
                hash ^= (uint)seed * 3266489917u;
                hash ^= hash >> 15;
                hash *= 2246822519u;
                hash ^= hash >> 13;
                hash *= 3266489917u;
                hash ^= hash >> 16;
                return (hash & 0xFFFFFFu) / (float)0x1000000;
            }
        }

        private static void AddQuad(
            VertexHelper vertexHelper, float xMin, float yMin, float xMax, float yMax, Color colour)
        {
            int start = vertexHelper.currentVertCount;
            Color32 packed = colour;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = packed;

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
            columns = Mathf.Clamp(columns, MinimumCells, MaximumCells);
            rows = Mathf.Clamp(rows, MinimumCells, MaximumCells);
            density = Mathf.Clamp01(density);
            maximumAlpha = Mathf.Clamp(maximumAlpha, 0f, 0.5f);
            streakDensity = Mathf.Clamp01(streakDensity);
            SetVerticesDirty();
        }
#endif
    }
}
