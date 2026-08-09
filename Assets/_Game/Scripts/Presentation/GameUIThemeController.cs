using UnityEngine;

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
            GameOverView gameOver)
        {
            theme = gameTheme;
            backgroundView = background;
            hudView = hud;
            cardView = card;
            footerView = footer;
            gameOverView = gameOver;
        }
#endif
    }
}
