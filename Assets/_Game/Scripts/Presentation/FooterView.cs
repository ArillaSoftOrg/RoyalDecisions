using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RoyalDecisions.Data;

namespace RoyalDecisions.Presentation
{
    /// <summary>Shows decorative run metadata without owning or persisting it.</summary>
    public sealed class FooterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text reignText;
        [SerializeField] private TMP_Text rulerText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image sealImage;
        [SerializeField] private InterfaceTextDefinition interfaceText;
        [SerializeField] private string defaultRulerName = "Royal Decisions";
        [SerializeField] private string reignFormat = "Tur {0}";
        [SerializeField] private bool showProgress;
        [SerializeField] private string progressFormat = "Tur {0}";

        public void RenderTurn(int turn)
        {
            if (reignText != null)
            {
                int oneBasedTurn = Mathf.Max(0, turn) + 1;
                reignText.text = interfaceText != null
                    ? string.Format("{0} {1}", interfaceText.Turn, oneBasedTurn)
                    : string.Format(string.IsNullOrEmpty(reignFormat) ? "Tur {0}" : reignFormat,
                        oneBasedTurn);
            }

            if (rulerText != null)
            {
                rulerText.text = defaultRulerName ?? string.Empty;
            }

            if (progressText != null)
            {
                progressText.gameObject.SetActive(showProgress);
                progressText.text = showProgress
                    ? string.Format(string.IsNullOrEmpty(progressFormat) ? "Tur {0}" : progressFormat, turn)
                    : string.Empty;
            }
        }

        public void ShowTurn(int oneBasedTurn)
        {
            RenderTurn(Mathf.Max(1, oneBasedTurn) - 1);
        }

        public void ApplyTheme(GameUITheme theme)
        {
            if (theme == null)
            {
                return;
            }

            ConfigureText(reignText, theme.SecondaryText, theme.BodyFont);
            ConfigureText(rulerText, theme.SecondaryText, theme.BodyFont);
            ConfigureText(progressText, theme.SecondaryText, theme.BodyFont);

            if (sealImage != null)
            {
                sealImage.sprite = theme.SealSprite;
                sealImage.color = theme.HighlightGold;
                sealImage.raycastTarget = false;
                sealImage.enabled = theme.SealSprite != null;
            }
        }

        private static void ConfigureText(TMP_Text text, Color color, TMP_FontAsset font)
        {
            if (text == null)
            {
                return;
            }

            text.color = color;
            text.raycastTarget = false;
            if (font != null)
            {
                text.font = font;
            }
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(TMP_Text reign, TMP_Text ruler, TMP_Text progress, Image seal)
        {
            reignText = reign;
            rulerText = ruler;
            progressText = progress;
            sealImage = seal;
        }
#endif
    }
}
