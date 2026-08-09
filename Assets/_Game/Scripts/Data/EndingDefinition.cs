using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>
    /// The screen shown when a statistic reaches one end of its range and the run ends.
    /// The MVP ships one ending per stat per boundary — eight in total.
    /// </summary>
    public class EndingDefinition : ScriptableObject
    {
        [Tooltip("Stable unique ID. Referenced by saves and validation.")]
        [SerializeField] private string id = string.Empty;

        [SerializeField] private string title = string.Empty;

        [TextArea(3, 8)]
        [SerializeField] private string bodyText = string.Empty;

        [Tooltip("Optional. A missing image falls back to placeholder art rather than failing.")]
        [SerializeField] private Sprite image;

        [SerializeField] private StatType triggerStat = StatType.Authority;

        [SerializeField] private StatBoundary boundary = StatBoundary.Min;

        [Tooltip("Tie-break when several stats hit a boundary on the same decision. Higher wins.")]
        [SerializeField] private int priority;

        public string Id => id ?? string.Empty;

        public string Title => title ?? string.Empty;

        public string BodyText => bodyText ?? string.Empty;

        public Sprite Image => image;

        public StatType TriggerStat => triggerStat;

        public StatBoundary Boundary => boundary;

        public int Priority => priority;

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only authoring seam used by the placeholder content generator and by tests.
        /// Compiled out of player builds so runtime code cannot mutate content.
        /// </summary>
        public void SetAuthoringData(
            string endingId,
            string endingTitle,
            string endingBodyText,
            StatType stat,
            StatBoundary statBoundary,
            int endingPriority = 0,
            Sprite endingImage = null)
        {
            id = endingId ?? string.Empty;
            title = endingTitle ?? string.Empty;
            bodyText = endingBodyText ?? string.Empty;
            triggerStat = stat;
            boundary = statBoundary;
            priority = endingPriority;
            image = endingImage;
        }
#endif
    }
}
