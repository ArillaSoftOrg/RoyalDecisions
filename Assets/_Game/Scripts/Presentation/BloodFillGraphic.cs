using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Texture-free vertical three-stop colour gradient for the blood-tube loading indicator's
    /// liquid body: deep dark burgundy at the bottom, a brighter red band above it, and a slightly
    /// darker tone at the very top (the liquid's own surface). No shader, no texture, no sprite —
    /// same per-vertex-colour technique as <see cref="ProceduralVerticalGradientGraphic"/>, just with
    /// full RGB stops instead of an alpha-only ramp, since the blood needs real hue/brightness
    /// variation rather than a transparency fade.
    /// </summary>
    /// <remarks>
    /// Deliberately stays at its full final size at all times — <see cref="StartupLoadingController"/>
    /// reveals it by resizing a sibling <see cref="RectMask2D"/>, never by scaling or resizing this
    /// graphic itself, so the gradient is never stretched or distorted by progress.
    ///
    /// <see cref="SetBrightness"/> is the 100% completion pulse's only hook into this class: it lerps
    /// each stop a restrained fraction of the way towards white and back, entirely inside this
    /// component, so the controller only ever drives a single 0..1 envelope value and never needs to
    /// know the actual stop colours.
    /// </remarks>
    public sealed class BloodFillGraphic : MaskableGraphic
    {
        private const float MaxBrightenFraction = 0.4f;

        [SerializeField] private Color32 bottomColor = new Color32(0x3D, 0x06, 0x09, 0xFF);
        [SerializeField] private Color32 midColor = new Color32(0x9A, 0x18, 0x1C, 0xFF);
        [SerializeField] private Color32 topColor = new Color32(0x5C, 0x0A, 0x0E, 0xFF);
        [Range(0f, 1f)] [SerializeField] private float midStopHeight = 0.62f;

        private float brightness;

        /// <summary>Sets the three vertical stops. <paramref name="midHeight01"/> is where the bright
        /// band sits, 0 (bottom) to 1 (top).</summary>
        public void SetColors(Color32 bottom, Color32 mid, Color32 top, float midHeight01)
        {
            bottomColor = bottom;
            midColor = mid;
            topColor = top;
            midStopHeight = Mathf.Clamp01(midHeight01);
            SetVerticesDirty();
        }

        /// <summary>0 = normal colours, 1 = fully brightened (the 100% completion pulse's peak).
        /// Intermediate values blend linearly. Never allocates — only regenerates this graphic's own
        /// small mesh.</summary>
        public void SetBrightness(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(clamped, brightness))
            {
                return;
            }

            brightness = clamped;
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

            float midY = Mathf.Lerp(rect.yMin, rect.yMax, midStopHeight);
            float brightenAmount = brightness * MaxBrightenFraction;

            AddRow(vertexHelper, rect, rect.yMin, Tint(bottomColor, brightenAmount));
            AddRow(vertexHelper, rect, midY, Tint(midColor, brightenAmount));
            AddRow(vertexHelper, rect, rect.yMax, Tint(topColor, brightenAmount));

            vertexHelper.AddTriangle(0, 1, 3);
            vertexHelper.AddTriangle(0, 3, 2);
            vertexHelper.AddTriangle(2, 3, 5);
            vertexHelper.AddTriangle(2, 5, 4);
        }

        private static void AddRow(VertexHelper vertexHelper, Rect rect, float y, Color32 rowColor)
        {
            UIVertex left = UIVertex.simpleVert;
            left.position = new Vector3(rect.xMin, y);
            left.color = rowColor;
            vertexHelper.AddVert(left);

            UIVertex right = UIVertex.simpleVert;
            right.position = new Vector3(rect.xMax, y);
            right.color = rowColor;
            vertexHelper.AddVert(right);
        }

        private static Color32 Tint(Color32 stop, float towardsWhite)
        {
            return towardsWhite <= 0f ? stop : Color32.Lerp(stop, Color.white, towardsWhite);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            midStopHeight = Mathf.Clamp01(midStopHeight);
            raycastTarget = false;
            SetVerticesDirty();
        }
#endif
    }
}
