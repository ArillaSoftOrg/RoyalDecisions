using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>Sizes the card from CardArea dimensions without per-frame polling.</summary>
    public sealed class ResponsiveCardSizer : MonoBehaviour
    {
        [SerializeField] private RectTransform card;
        [SerializeField] private RectTransform nextCard;
        [Tooltip("Width is measured from this rect, normally SafeArea. Defaults to CardArea.")]
        [SerializeField] private RectTransform widthReference;
        [Range(0.7f, 0.85f)]
        [SerializeField] private float preferredWidthRatio = 0.78f;
        [Min(0.01f)]
        [SerializeField] private float widthToHeightRatio = 1024f / 1536f;
        [Range(0.1f, 1f)]
        [SerializeField] private float maximumHeightRatio = 0.94f;
        [Min(1f)]
        [SerializeField] private float maximumWidth = 920f;
        [SerializeField] private Vector2 nextCardOffset = new Vector2(0f, 12f);
        [Range(0.8f, 1f)]
        [SerializeField] private float nextCardScale = 0.96f;

        private void OnEnable()
        {
            RecalculateLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
            {
                RecalculateLayout();
            }
        }

        public void RecalculateLayout()
        {
            RectTransform area = transform as RectTransform;
            if (area == null)
            {
                return;
            }

            RectTransform reference = widthReference != null ? widthReference : area;
            Vector2 size = ResponsiveCardLayoutMath.Calculate(
                reference.rect.width,
                area.rect.size,
                preferredWidthRatio,
                widthToHeightRatio,
                maximumHeightRatio,
                maximumWidth);
            Apply(card, size, Vector2.zero, 1f);
            Apply(nextCard, size, nextCardOffset, nextCardScale);
        }

        private static void Apply(RectTransform target, Vector2 size, Vector2 position, float scale)
        {
            if (target == null)
            {
                return;
            }

            target.anchorMin = new Vector2(0.5f, 0.5f);
            target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.sizeDelta = size;
            target.anchoredPosition = position;
            target.localScale = Vector3.one * scale;
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            RectTransform activeCard,
            RectTransform queuedCard,
            RectTransform sizingReference = null)
        {
            card = activeCard;
            nextCard = queuedCard;
            widthReference = sizingReference;
        }
#endif
    }
}
