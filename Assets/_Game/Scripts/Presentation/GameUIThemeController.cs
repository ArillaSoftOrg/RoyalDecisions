using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>Applies one serialized theme to all managed game-scene views.</summary>
    public sealed class GameUIThemeController : MonoBehaviour
    {
        [SerializeField] private GameUITheme theme;
        [SerializeField] private BackgroundView backgroundView;
        [SerializeField] private HUDView hudView;
        [SerializeField] private CardView cardView;
        [SerializeField] private FooterView footerView;
        [SerializeField] private GameOverView gameOverView;

        [Tooltip("The situation panel's sprite-driven surface. Shown when the theme supplies " +
            "SituationPanelSprite; hidden in favour of situationPanelFallback otherwise.")]
        [SerializeField] private Image situationPanelImage;
        [SerializeField] private ProceduralRoundedRectGraphic situationPanelFallback;

        public GameUITheme Theme => theme;

        private void Awake()
        {
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            backgroundView?.ApplyTheme(theme);
            hudView?.ApplyTheme(theme);
            cardView?.ApplyTheme(theme);
            footerView?.ApplyTheme(theme);
            gameOverView?.ApplyTheme(theme);
            ApplySituationPanel();
        }

        private void ApplySituationPanel()
        {
            Sprite sprite = theme != null ? theme.SituationPanelSprite : null;

            if (situationPanelImage != null)
            {
                situationPanelImage.sprite = sprite;
                // Simple, not Sliced: the supplied parchment has pronounced torn/curled edges on
                // every side, which 9-slicing would stretch and distort. Preserve Aspect is off —
                // the art's native ratio (~2.9:1) is far narrower than the approved panel Rect
                // (~5.9:1), so preserving it rendered as a small centred strip with the panel's
                // text overflowing past its edges; a flexible paper surface reads fine with the
                // moderate horizontal stretch needed to fill the panel exactly.
                situationPanelImage.type = Image.Type.Simple;
                situationPanelImage.preserveAspect = false;
                situationPanelImage.raycastTarget = false;
                situationPanelImage.enabled = sprite != null;
            }

            if (situationPanelFallback != null)
            {
                situationPanelFallback.enabled = sprite == null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyTheme();
        }

        public void SetAuthoringReferences(
            GameUITheme gameTheme,
            BackgroundView background,
            HUDView hud,
            CardView card,
            FooterView footer,
            GameOverView gameOver,
            Image situationPanel = null,
            ProceduralRoundedRectGraphic situationPanelFallbackGraphic = null)
        {
            theme = gameTheme;
            backgroundView = background;
            hudView = hud;
            cardView = card;
            footerView = footer;
            gameOverView = gameOver;
            situationPanelImage = situationPanel;
            situationPanelFallback = situationPanelFallbackGraphic;
        }
#endif
    }
}
