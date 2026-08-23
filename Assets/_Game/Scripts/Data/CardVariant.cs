using System;
using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>
    /// An alternative presentation of a card, used when earlier decisions should change what a
    /// later card says or offers without it being a different card.
    /// </summary>
    /// <remarks>
    /// Every override is optional: a variant that only overrides <see cref="BodyText"/> leaves the
    /// speaker and both choices exactly as authored on the base card. Resolution (which variant, if
    /// any, applies) lives in <c>RoyalDecisions.Domain.CardVariantResolver</c> — this type only
    /// stores what a matching variant changes.
    /// </remarks>
    [Serializable]
    public sealed class CardVariant
    {
        [Tooltip("This variant applies when these conditions hold. Evaluated the same way card " +
            "eligibility is.")]
        [SerializeField] private CardConditions conditions = new CardConditions();

        [Tooltip("Leave blank to keep the base card's speaker.")]
        [SerializeField] private string speaker = string.Empty;

        [TextArea(3, 8)]
        [Tooltip("Leave blank to keep the base card's body text.")]
        [SerializeField] private string bodyText = string.Empty;

        [Tooltip("Leave unset to keep the base card's left choice.")]
        [SerializeField] private ChoiceDefinition leftChoice;

        [Tooltip("Leave unset to keep the base card's right choice.")]
        [SerializeField] private ChoiceDefinition rightChoice;

        public CardVariant()
        {
        }

        public CardVariant(
            CardConditions conditions,
            string speaker = null,
            string bodyText = null,
            ChoiceDefinition leftChoice = null,
            ChoiceDefinition rightChoice = null)
        {
            this.conditions = conditions ?? new CardConditions();
            this.speaker = speaker ?? string.Empty;
            this.bodyText = bodyText ?? string.Empty;
            this.leftChoice = leftChoice;
            this.rightChoice = rightChoice;
        }

        public CardConditions Conditions => conditions ?? new CardConditions();

        public string Speaker => speaker ?? string.Empty;

        public bool HasSpeakerOverride => !string.IsNullOrEmpty(speaker);

        public string BodyText => bodyText ?? string.Empty;

        public bool HasBodyTextOverride => !string.IsNullOrEmpty(bodyText);

        public ChoiceDefinition LeftChoice => leftChoice;

        public ChoiceDefinition RightChoice => rightChoice;
    }
}
