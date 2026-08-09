using System;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Renders the ending screen and reports that the player asked to restart.
    /// </summary>
    /// <remarks>
    /// It never restarts anything. <see cref="RestartRequested"/> is a presentation event; Phase 7
    /// subscribes and decides what a restart means.
    ///
    /// The restart button is wired through the Inspector to <see cref="HandleRestartButton"/> rather
    /// than in <c>Awake</c>, so the view has no lifecycle requirements and a test can raise the same
    /// path the button does.
    /// </remarks>
    public sealed class GameOverView : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("Toggled by Show and Hide. Defaults to this object.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Content")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private GraphicFallbackSettings illustrationFallback = new GraphicFallbackSettings();
        [SerializeField] private InterfaceTextDefinition interfaceText;

        [Header("Missing-ending fallback")]
        [Tooltip("Shown when a boundary is reached that no ending asset covers.")]
        [SerializeField] private string genericTitle = "The Reign Ends";

        [TextArea(2, 5)]
        [SerializeField] private string genericBody =
            "Your rule is over. The chronicles do not record how.";

        [Header("Optional")]
        [SerializeField] private Button restartButton;
        [SerializeField] private TMP_Text restartButtonText;
        [SerializeField] private Image panelImage;

        /// <summary>Raised when the player presses restart. The view takes no other action.</summary>
        public event Action RestartRequested;

        public bool IsVisible { get; private set; }

        public bool IsShowingGenericFallback { get; private set; }

        public GraphicFallbackMode IllustrationMode { get; private set; } = GraphicFallbackMode.HideGraphic;

        public void Show(GameOverResult result)
        {
            string fallbackTitle = interfaceText != null ? interfaceText.GameOverTitle : genericTitle;
            string fallbackBody = interfaceText != null ? interfaceText.GameOverBody : genericBody;
            Show(GameOverPresenter.Create(result, fallbackTitle, fallbackBody));
        }

        public void Show(GameOverPresentation presentation)
        {
            if (!presentation.HasEnding)
            {
                Hide();
                return;
            }

            SetText(titleText, presentation.Title);
            SetText(bodyText, presentation.BodyText);

            IllustrationMode = GraphicFallback.Apply(
                illustrationImage, presentation.Illustration, illustrationFallback);

            IsShowingGenericFallback = presentation.IsGenericFallback;
            if (interfaceText != null)
            {
                SetText(restartButtonText, interfaceText.Restart);
            }
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        /// <summary>Wire the restart Button's OnClick to this method in the Inspector.</summary>
        public void HandleRestartButton()
        {
            RestartRequested?.Invoke();
        }

        public void SetRestartInteractable(bool interactable)
        {
            if (restartButton != null)
            {
                restartButton.interactable = interactable;
            }
        }

        public void ApplyTheme(GameUITheme theme)
        {
            if (theme == null)
            {
                return;
            }

            if (panelImage != null)
            {
                Color background = theme.OverallBackground;
                panelImage.color = new Color(background.r, background.g, background.b, 0.96f);
                panelImage.raycastTarget = true;
            }

            ConfigureText(titleText, theme.HighlightGold, theme.TitleFont);
            ConfigureText(bodyText, theme.PrimaryText, theme.BodyFont);
            ConfigureText(restartButtonText, theme.PrimaryText, theme.BodyFont);
        }

        private void SetVisible(bool visible)
        {
            IsVisible = visible;

            GameObject root = panelRoot != null ? panelRoot : gameObject;
            root.SetActive(visible);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private static void ConfigureText(TMP_Text target, Color color, TMP_FontAsset font)
        {
            if (target == null)
            {
                return;
            }

            target.color = color;
            target.raycastTarget = false;
            if (font != null)
            {
                target.font = font;
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor-only wiring hook shared by prefab setup and tests.</summary>
        public void SetAuthoringReferences(
            TMP_Text title,
            TMP_Text body,
            Image illustration,
            GameObject root = null,
            GraphicFallbackSettings fallback = null,
            string fallbackTitle = null,
            string fallbackBody = null,
            InterfaceTextDefinition text = null,
            TMP_Text restartLabel = null,
            Button restart = null,
            Image panel = null)
        {
            titleText = title;
            bodyText = body;
            illustrationImage = illustration;
            panelRoot = root;
            interfaceText = text;
            restartButtonText = restartLabel;
            restartButton = restart;
            panelImage = panel;

            if (fallback != null)
            {
                illustrationFallback = fallback;
            }

            if (fallbackTitle != null)
            {
                genericTitle = fallbackTitle;
            }

            if (fallbackBody != null)
            {
                genericBody = fallbackBody;
            }
        }
#endif
    }
}
