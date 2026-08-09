using RoyalDecisions.Data;
using TMPro;
using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>Renders the one-based turn number without adding calendar rules to the run.</summary>
    public sealed class RunStatusView : MonoBehaviour
    {
        [SerializeField] private InterfaceTextDefinition interfaceText;
        [SerializeField] private TMP_Text turnText;

        public int DisplayedTurn { get; private set; }

        public void ShowTurn(int oneBasedTurn)
        {
            DisplayedTurn = Mathf.Max(1, oneBasedTurn);
            if (turnText != null)
            {
                string label = interfaceText != null ? interfaceText.Turn : "Tur";
                turnText.text = string.Format("{0} {1}", label, DisplayedTurn);
            }
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(InterfaceTextDefinition text, TMP_Text target)
        {
            interfaceText = text;
            turnText = target;
        }
#endif
    }
}
