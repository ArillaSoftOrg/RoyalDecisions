using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>
    /// A single decision presented to the player: who speaks, what they say, and the two choices.
    /// Static content only — nothing here changes at runtime.
    /// </summary>
    public class CardDefinition : ScriptableObject
    {
        /// <summary>Weight given to a card whose author left the field at zero.</summary>
        public const int DefaultSelectionWeight = 1;

        [Tooltip("Stable unique ID. Referenced by forced-next-card chains and by saves.")]
        [SerializeField] private string id = string.Empty;

        [SerializeField] private string speaker = string.Empty;

        [TextArea(3, 8)]
        [SerializeField] private string bodyText = string.Empty;

        [Tooltip("Optional. A missing portrait falls back to placeholder art rather than failing.")]
        [SerializeField] private Sprite portrait;

        [SerializeField] private ChoiceDefinition leftChoice = new ChoiceDefinition();

        [SerializeField] private ChoiceDefinition rightChoice = new ChoiceDefinition();

        [SerializeField] private CardConditions conditions = new CardConditions();

        [Tooltip("Relative likelihood of being drawn among eligible cards. Higher is more likely.")]
        [SerializeField] private int selectionWeight = DefaultSelectionWeight;

        [Tooltip("When set, the card can appear at most once per run.")]
        [SerializeField] private bool oncePerRun;

        [Tooltip("Turns that must pass before this card can be drawn again. Zero disables it.")]
        [SerializeField] private int cooldownTurns;

        [Tooltip("Optional card ID drawn next regardless of which side the player chose.")]
        [SerializeField] private string forcedNextCardId = string.Empty;

        public string Id => id ?? string.Empty;

        public string Speaker => speaker ?? string.Empty;

        public string BodyText => bodyText ?? string.Empty;

        public Sprite Portrait => portrait;

        public ChoiceDefinition LeftChoice => leftChoice;

        public ChoiceDefinition RightChoice => rightChoice;

        public CardConditions Conditions => conditions;

        /// <summary>
        /// Never returns less than 1, so a card left at the default zero weight stays drawable
        /// instead of silently disappearing from selection.
        /// </summary>
        public int SelectionWeight => selectionWeight > 0 ? selectionWeight : DefaultSelectionWeight;

        public bool OncePerRun => oncePerRun;

        public int CooldownTurns => cooldownTurns > 0 ? cooldownTurns : 0;

        public bool HasCooldown => CooldownTurns > 0;

        public string ForcedNextCardId => forcedNextCardId ?? string.Empty;

        public bool HasForcedNextCard => !string.IsNullOrEmpty(forcedNextCardId);

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only authoring seam used by the placeholder content generator and by tests.
        /// Compiled out of player builds so runtime code cannot mutate content.
        /// </summary>
        public void SetAuthoringData(
            string cardId,
            string cardSpeaker,
            string cardBodyText,
            ChoiceDefinition left,
            ChoiceDefinition right,
            CardConditions cardConditions = null,
            int weight = DefaultSelectionWeight,
            bool isOncePerRun = false,
            int cooldown = 0,
            string forcedNext = "",
            Sprite cardPortrait = null)
        {
            id = cardId ?? string.Empty;
            speaker = cardSpeaker ?? string.Empty;
            bodyText = cardBodyText ?? string.Empty;
            leftChoice = left ?? new ChoiceDefinition();
            rightChoice = right ?? new ChoiceDefinition();
            conditions = cardConditions ?? new CardConditions();
            selectionWeight = weight;
            oncePerRun = isOncePerRun;
            cooldownTurns = cooldown;
            forcedNextCardId = forcedNext ?? string.Empty;
            portrait = cardPortrait;
        }
#endif
    }
}
