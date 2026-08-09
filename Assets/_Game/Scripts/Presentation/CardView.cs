using RoyalDecisions.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Renders one card. Passive: it shows what it is given and decides nothing.
    /// </summary>
    /// <remarks>
    /// It never resolves a choice, changes a statistic, writes a save, selects a card, or reads
    /// input. Phase 6 moves <see cref="CardRoot"/> and drives the preview strengths; Phase 7 decides
    /// which card to show.
    /// </remarks>
    public sealed class CardView : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("The transform Phase 6 will drag. Defaults to this object's RectTransform.")]
        [SerializeField] private RectTransform cardRoot;

        [Tooltip("Toggled by Show and Clear. Defaults to this object.")]
        [SerializeField] private GameObject visualRoot;

        [Header("Content")]
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private GraphicFallbackSettings portraitFallback = new GraphicFallbackSettings();

        [Header("Theme surfaces")]
        [SerializeField] private Image surfaceImage;
        [SerializeField] private Outline borderOutline;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image portraitFrameImage;
        [SerializeField] private Image portraitMaskImage;
        [SerializeField] private Image[] cornerImages = System.Array.Empty<Image>();
        [SerializeField] private Image[] temporaryBorderImages = System.Array.Empty<Image>();
        [SerializeField] private PortraitFallbackView portraitFallbackView;
        [SerializeField] private GameObject nextCardRoot;
        [SerializeField] private Image nextCardSurface;
        [SerializeField] private Image nextCardFrame;

        [Header("Choice previews")]
        [SerializeField] private ChoicePreviewView leftPreview;
        [SerializeField] private ChoicePreviewView rightPreview;

        /// <summary>True between a successful <see cref="Show"/> and the next <see cref="Clear"/>.</summary>
        public bool HasCard { get; private set; }

        public RectTransform CardRoot => cardRoot != null ? cardRoot : transform as RectTransform;

        public GraphicFallbackMode PortraitMode { get; private set; } = GraphicFallbackMode.HideGraphic;

        /// <summary>Renders the card and makes it visible. A null card clears instead of throwing.</summary>
        public void Show(CardDefinition card)
        {
            Render(card);
            SetVisible(HasCard);
        }

        /// <summary>Re-renders without touching visibility — for a card whose content changed.</summary>
        public void UpdateCard(CardDefinition card)
        {
            Render(card);
        }

        /// <summary>Blanks every field, drops the card, and hides the view.</summary>
        public void Clear()
        {
            Render(null);
            ClearChoicePreviews();
            SetVisible(false);
        }

        public void SetChoicePreview(ChoiceSide side, float strength)
        {
            ChoicePreviewView preview = side == ChoiceSide.Left ? leftPreview : rightPreview;

            if (preview != null)
            {
                preview.SetStrength(strength);
            }
        }

        public void SetChoicePreviews(float leftStrength, float rightStrength)
        {
            SetChoicePreview(ChoiceSide.Left, leftStrength);
            SetChoicePreview(ChoiceSide.Right, rightStrength);
        }

        public void ClearChoicePreviews()
        {
            SetChoicePreviews(0f, 0f);
        }

        public void ApplyTheme(GameUITheme theme)
        {
            if (theme == null)
            {
                return;
            }

            if (surfaceImage != null)
            {
                surfaceImage.color = theme.CardSurface;
            }

            if (borderOutline != null)
            {
                borderOutline.effectColor = theme.BorderGold;
                borderOutline.enabled = theme.CardFrameSprite == null;
            }

            for (int i = 0; i < temporaryBorderImages.Length; i++)
            {
                Image border = temporaryBorderImages[i];
                if (border == null)
                {
                    continue;
                }

                border.color = theme.BorderGold;
                border.raycastTarget = false;
                border.enabled = theme.CardFrameSprite == null;
            }

            ConfigureOptional(frameImage, theme.CardFrameSprite, theme.BorderGold);
            ConfigureOptional(portraitFrameImage, theme.PortraitFrameSprite, theme.BorderGold, true);
            ConfigureOptional(portraitMaskImage, theme.PortraitMaskSprite, Color.white, true);
            ConfigureOptional(nextCardFrame, theme.NextCardFrameSprite, theme.BorderGold);

            if (nextCardSurface != null)
            {
                nextCardSurface.color = theme.CardSurface;
                nextCardSurface.raycastTarget = false;
            }

            for (int i = 0; i < cornerImages.Length; i++)
            {
                ConfigureOptional(cornerImages[i], theme.CornerDecorationSprite, theme.BorderGold);
            }

            ConfigureText(speakerText, theme.HighlightGold, theme.TitleFont);
            ConfigureText(bodyText, theme.PrimaryText, theme.BodyFont);
            portraitFallbackView?.ApplyTheme(theme);
            leftPreview?.ApplyTheme(theme);
            rightPreview?.ApplyTheme(theme);
        }

        public float GetChoicePreviewStrength(ChoiceSide side)
        {
            ChoicePreviewView preview = side == ChoiceSide.Left ? leftPreview : rightPreview;
            return preview != null ? preview.Strength : 0f;
        }

        private void Render(CardDefinition card)
        {
            CardPresentation presentation = CardPresenter.Create(card);

            SetText(speakerText, presentation.Speaker);
            SetText(bodyText, presentation.BodyText);

            PortraitMode = GraphicFallback.Apply(portraitImage, presentation.Portrait, portraitFallback);
            bool hasPortraitArtwork = PortraitMode == GraphicFallbackMode.UseSource
                || PortraitMode == GraphicFallbackMode.UseFallbackSprite;
            if (portraitFallbackView != null)
            {
                portraitFallbackView.SetVisible(!hasPortraitArtwork);
                if (!hasPortraitArtwork && portraitImage != null)
                {
                    portraitImage.enabled = false;
                }
            }

            // Preview labels come from the card, so replacing a card cannot leave the previous
            // card's wording behind on either side.
            if (leftPreview != null)
            {
                leftPreview.SetText(presentation.LeftPreviewText);
            }

            if (rightPreview != null)
            {
                rightPreview.SetText(presentation.RightPreviewText);
            }

            HasCard = presentation.HasCard;
        }

        private void SetVisible(bool visible)
        {
            GameObject root = visualRoot != null ? visualRoot : gameObject;
            root.SetActive(visible);
            if (nextCardRoot != null)
            {
                nextCardRoot.SetActive(visible);
            }
        }

        private static void ConfigureOptional(
            Image image,
            Sprite sprite,
            Color color,
            bool enabledWithoutSprite = false)
        {
            if (image == null)
            {
                return;
            }

            if (sprite != null || !enabledWithoutSprite)
            {
                image.sprite = sprite;
            }
            image.color = color;
            image.raycastTarget = false;
            image.enabled = sprite != null || enabledWithoutSprite;
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

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor-only wiring hook shared by prefab setup and tests.</summary>
        public void SetAuthoringReferences(
            TMP_Text speaker,
            TMP_Text body,
            Image portrait,
            ChoicePreviewView left,
            ChoicePreviewView right,
            GraphicFallbackSettings fallback = null,
            GameObject root = null,
            Image surface = null,
            Outline outline = null,
            Image frame = null,
            Image portraitFrame = null,
            Image portraitMask = null,
            Image[] corners = null,
            GameObject queuedCard = null,
            Image queuedSurface = null,
            Image queuedFrame = null,
            Image[] generatedBorders = null,
            PortraitFallbackView generatedPortraitFallback = null)
        {
            speakerText = speaker;
            bodyText = body;
            portraitImage = portrait;
            leftPreview = left;
            rightPreview = right;

            if (fallback != null)
            {
                portraitFallback = fallback;
            }

            visualRoot = root;
            surfaceImage = surface;
            borderOutline = outline;
            frameImage = frame;
            portraitFrameImage = portraitFrame;
            portraitMaskImage = portraitMask;
            cornerImages = corners ?? System.Array.Empty<Image>();
            nextCardRoot = queuedCard;
            nextCardSurface = queuedSurface;
            nextCardFrame = queuedFrame;
            temporaryBorderImages = generatedBorders ?? System.Array.Empty<Image>();
            portraitFallbackView = generatedPortraitFallback;
        }
#endif
    }
}
