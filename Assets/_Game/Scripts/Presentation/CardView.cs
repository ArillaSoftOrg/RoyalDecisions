using RoyalDecisions.Data;
using RoyalDecisions.Domain;
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
        // Multiplied onto the queued card's frame/surface tint so it reads as behind and less
        // prominent than the active card while still sharing the same art.
        private static readonly Color NextCardDimTint = new Color(0.62f, 0.62f, 0.62f, 1f);

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
        [Tooltip("Dark scrim behind the name label at the bottom of the portrait, for legibility.")]
        [SerializeField] private Image nameScrimImage;
        [Tooltip("Dark scrim behind the situation/body text near the top of the portrait.")]
        [SerializeField] private Image bodyScrimImage;
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

        /// <summary>
        /// Renders <paramref name="card"/> as resolved for the current run (variant text, effective
        /// choices, availability) and makes it visible.
        /// </summary>
        public void Show(CardDefinition card, ResolvedCard resolved)
        {
            Render(card, resolved);
            SetVisible(HasCard);
        }

        /// <summary>Re-renders without touching visibility — for a card whose content changed.</summary>
        public void UpdateCard(CardDefinition card)
        {
            Render(card);
        }

        /// <summary>As <see cref="UpdateCard(CardDefinition)"/>, but from a resolved presentation.</summary>
        public void UpdateCard(CardDefinition card, ResolvedCard resolved)
        {
            Render(card, resolved);
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

            // White, not BorderGold: this is real painted frame art (multiple colours), not a flat
            // gold fallback shape, so it must render untinted rather than colour-multiplied.
            ConfigureOptional(frameImage, theme.CardFrameSprite, Color.white);
            ConfigureOptional(portraitFrameImage, theme.PortraitFrameSprite, theme.BorderGold, true);
            ConfigureOptional(portraitMaskImage, theme.PortraitMaskSprite, Color.white, true);
            // Dimmed, not full white: the queued card must read as behind/less prominent than the
            // active one even though it shares the same frame art.
            ConfigureOptional(nextCardFrame, theme.NextCardFrameSprite, NextCardDimTint);

            if (nextCardSurface != null)
            {
                nextCardSurface.color = theme.CardSurface * NextCardDimTint;
                nextCardSurface.raycastTarget = false;
            }

            for (int i = 0; i < cornerImages.Length; i++)
            {
                ConfigureOptional(cornerImages[i], theme.CornerDecorationSprite, theme.BorderGold);
            }

            ConfigureText(speakerText, theme.HighlightGold, theme.TitleFont);
            // bodyText now renders directly over the bottom of the portrait (the parchment
            // situation panel above the card was removed), so it needs a light colour readable
            // over painted art, not the parchment ink colour SituationText used.
            ConfigureText(bodyText, theme.PrimaryText, theme.BodyFont);

            if (nameScrimImage != null)
            {
                // The themed plaque tried here read as a washed-out brown box behind the name, not
                // the visual interest it was meant to add — the user asked for it gone. Disabled,
                // not deleted, same as bodyScrimImage below: cheap to bring back with a better
                // colour/graphic later.
                nameScrimImage.raycastTarget = false;
                nameScrimImage.enabled = false;
            }

            if (bodyScrimImage != null)
            {
                // At the user's explicit request: no dark scrim behind the story text any more —
                // it now reads directly against the (now full-bleed) portrait. Left in place
                // disabled, not deleted, so it is one flag flip to bring back if legibility over
                // busy art turns out to need it.
                bodyScrimImage.enabled = false;
            }

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
            RenderPresentation(CardPresenter.Create(card));
        }

        private void Render(CardDefinition card, ResolvedCard resolved)
        {
            RenderPresentation(CardPresenter.Create(card, resolved));
        }

        private void RenderPresentation(CardPresentation presentation)
        {
            SetText(speakerText, presentation.Speaker);
            SetText(bodyText, presentation.BodyText);

            PortraitMode = GraphicFallback.Apply(portraitImage, presentation.Portrait, portraitFallback);
            bool hasPortraitArtwork = PortraitMode == GraphicFallbackMode.UseSource
                || PortraitMode == GraphicFallbackMode.UseFallbackSprite;
            ApplyPortraitCoverFit(hasPortraitArtwork);
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

        // Portrait art is not authored at a fixed aspect relative to the frame's opening (see
        // PortraitCoverFitMath). Rather than let Image stretch it non-uniformly, size the
        // Portrait rect itself to the sprite's true aspect ratio, oversized just enough to fully
        // cover the mask; PortraitMask's existing Mask component crops the overflow, so faces and
        // bodies are always scaled uniformly, never squeezed.
        private void ApplyPortraitCoverFit(bool hasPortraitArtwork)
        {
            if (portraitImage == null)
            {
                return;
            }

            RectTransform portraitRect = portraitImage.rectTransform;
            RectTransform maskRect = portraitMaskImage != null ? portraitMaskImage.rectTransform : null;

            if (!hasPortraitArtwork || portraitImage.sprite == null || maskRect == null)
            {
                // No artwork to fit against: restore the authored full-stretch rect so a later
                // card with real art does not inherit a stale cropped size.
                portraitRect.anchorMin = Vector2.zero;
                portraitRect.anchorMax = Vector2.one;
                portraitRect.offsetMin = Vector2.zero;
                portraitRect.offsetMax = Vector2.zero;
                return;
            }

            Rect sourceRect = portraitImage.sprite.rect;
            float spriteAspect = sourceRect.height > 0f ? sourceRect.width / sourceRect.height : 1f;
            Vector2 coverSize = PortraitCoverFitMath.ComputeCoverSize(maskRect.rect.size, spriteAspect);

            portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = Vector2.zero;
            portraitRect.sizeDelta = coverSize;
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
            PortraitFallbackView generatedPortraitFallback = null,
            Image nameScrim = null,
            Image bodyScrim = null)
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
            nameScrimImage = nameScrim;
            bodyScrimImage = bodyScrim;
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
