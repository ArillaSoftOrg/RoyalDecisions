using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>Draws a texture-free vignette that can be replaced by designer artwork.</summary>
    public sealed class ProceduralVignetteGraphic : MaskableGraphic
    {
        [Range(0.05f, 0.45f)]
        [SerializeField] private float edgeWidth = 0.22f;

        [Range(0f, 1f)]
        [SerializeField] private float edgeAlpha = 0.42f;

        public float EdgeWidth => edgeWidth;

        public float EdgeAlpha => edgeAlpha;

        public void SetStyle(Color edgeColour, float width, float alpha)
        {
            color = edgeColour;
            edgeWidth = Mathf.Clamp(width, 0.05f, 0.45f);
            edgeAlpha = Mathf.Clamp01(alpha);
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

            float insetX = rect.width * edgeWidth;
            float insetY = rect.height * edgeWidth;
            float[] xs = { rect.xMin, rect.xMin + insetX, rect.xMax - insetX, rect.xMax };
            float[] ys = { rect.yMin, rect.yMin + insetY, rect.yMax - insetY, rect.yMax };
            Color32 outer = new Color(color.r, color.g, color.b, color.a * edgeAlpha);
            Color32 inner = new Color(color.r, color.g, color.b, 0f);

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    bool isOuter = x == 0 || x == 3 || y == 0 || y == 3;
                    UIVertex vertex = UIVertex.simpleVert;
                    vertex.position = new Vector3(xs[x], ys[y]);
                    vertex.color = isOuter ? outer : inner;
                    vertexHelper.AddVert(vertex);
                }
            }

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    int lowerLeft = (y * 4) + x;
                    vertexHelper.AddTriangle(lowerLeft, lowerLeft + 4, lowerLeft + 5);
                    vertexHelper.AddTriangle(lowerLeft, lowerLeft + 5, lowerLeft + 1);
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            edgeWidth = Mathf.Clamp(edgeWidth, 0.05f, 0.45f);
            edgeAlpha = Mathf.Clamp01(edgeAlpha);
            raycastTarget = false;
            SetVerticesDirty();
        }
#endif
    }
}
