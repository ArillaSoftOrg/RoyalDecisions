using RoyalDecisions.Data;
using TMPro;
using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>Applies reusable interface wording to the main menu.</summary>
    public sealed class MainMenuTextView : MonoBehaviour
    {
        [SerializeField] private InterfaceTextDefinition interfaceText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text newGameText;
        [SerializeField] private TMP_Text continueText;
        [SerializeField] private TMP_Text saveErrorText;

        private void Awake()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (interfaceText == null)
            {
                return;
            }

            SetText(titleText, interfaceText.MainMenuTitle);
            SetText(newGameText, interfaceText.NewGame);
            SetText(continueText, interfaceText.ContinueGame);
        }

        public void SetSaveError(string message)
        {
            SetText(saveErrorText, message);
            if (saveErrorText != null)
            {
                saveErrorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            InterfaceTextDefinition text,
            TMP_Text title,
            TMP_Text newGame,
            TMP_Text continueGame,
            TMP_Text error = null)
        {
            interfaceText = text;
            titleText = title;
            newGameText = newGame;
            continueText = continueGame;
            saveErrorText = error;
        }
#endif
    }
}
