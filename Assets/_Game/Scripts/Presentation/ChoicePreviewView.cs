using RoyalDecisions.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// One side's choice preview: a label whose visibility is driven by a normalised strength.
    /// </summary>
    /// <remarks>
    /// Phase 5 defines what a strength <em>looks like</em>. Phase 6 decides where the number comes
    /// from — drag distance over a threshold — so no swipe concept lives here.
    /// </remarks>
    public sealed class ChoicePreviewView : MonoBehaviour
    {
        [SerializeField] private ChoiceSide side = ChoiceSide.Left;
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image edgeHighlight;
        [SerializeField] private CanvasGroup commitMarker;

        [Range(0f, 1f)]
        [SerializeField] private float maxAlpha = 1f;

        [Tooltip("Optional: grow the preview slightly as the drag commits.")]
        [SerializeField] private bool scaleWithStrength;

        [SerializeField] private float minScale = 0.92f;
        [SerializeField] private float maxScale = 1f;

        public ChoiceSide Side => side;

        /// <summary>The last strength applied, always within <c>0..1</c>.</summary>
        public float Strength { get; private set; }

        public string Text => label != null ? label.text : string.Empty;

        public void SetText(string text)
        {
            if (label != null)
            {
                label.text = text ?? string.Empty;
            }
        }

        public void SetStrength(float strength)
        {
            Strength = Mathf.Clamp01(strength);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Strength * maxAlpha;
            }

            if (commitMarker != null)
            {
                commitMarker.alpha = Strength >= 0.999f ? 1f : 0f;
            }

            if (scaleWithStrength)
            {
                transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, Strength);
            }
        }

        public void Clear()
        {
            SetText(string.Empty);
            SetStrength(0f);
        }

        public void ApplyTheme(GameUITheme theme)
        {
            if (theme == null)
            {
                return;
            }

            if (label != null)
            {
                label.color = theme.PrimaryText;
                label.raycastTarget = false;
                if (theme.BodyFont != null)
                {
                    label.font = theme.BodyFont;
                }
            }

            if (edgeHighlight != null)
            {
                Sprite banner = side == ChoiceSide.Left ? theme.LeftEdgeSprite : theme.RightEdgeSprite;
                edgeHighlight.sprite = banner;
                edgeHighlight.raycastTarget = false;
                // Real banner art must render untinted (white) to show its own painted colours;
                // the flat fallback fill still needs the theme's directional tint to read as a
                // choice-side colour cue.
                edgeHighlight.color = banner != null
                    ? Color.white
                    : (side == ChoiceSide.Left ? theme.LeftChoice : theme.RightChoice);
                edgeHighlight.preserveAspect = banner != null;
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor-only wiring hook so tests and prefab setup share one path.</summary>
        public void SetAuthoringReferences(
            ChoiceSide previewSide,
            TMP_Text previewLabel,
            CanvasGroup group,
            Image edge = null,
            CanvasGroup thresholdMarker = null)
        {
            side = previewSide;
            label = previewLabel;
            canvasGroup = group;
            edgeHighlight = edge;
            commitMarker = thresholdMarker;
        }
#endif
    }
}
