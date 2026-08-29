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
    /// input. Phase 6 moves <see cref="CardRoot"/> (the swipeable portrait, not the fixed card
    /// shell around it) and drives the preview strengths; Phase 7 decides which card to show.
    /// </remarks>
    public sealed class CardView : MonoBehaviour
    {
        // The character name sits in the fixed band below the portrait area, on the plain
        // NameScrim backing (authored by SceneSetupAutomation). It never moves during a swipe.
        private static readonly Vector2 NameAnchorMin = new Vector2(0.06f, 0.01f);
        private static readonly Vector2 NameAnchorMax = new Vector2(0.94f, 0.115f);

        [Header("Layout")]
        [Tooltip("The transform Phase 6 will drag — the moving portrait root, not the fixed card "
            + "shell. Defaults to this object's RectTransform.")]
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
        [Tooltip("The Mask's own graphic — a flat procedural rounded-rect shape, not a sprite "
            + "Image, so the portrait's corner radius is exact rather than approximated by a "
            + "9-sliced sprite border.")]
        [SerializeField] private Graphic portraitMaskImage;
        [Tooltip("Paper-toned backing behind the fixed name band, matching ContentPanel so the "
            + "name reads as sitting on the same paper surface as SituationText. A flat "
            + "procedural fill, not a sprite Image, so it never reads as a rounded \"pill\".")]
        [SerializeField] private Graphic nameScrimImage;
        [Tooltip("Fixed backdrop behind the swipeable portrait, revealed as it is dragged away.")]
        [SerializeField] private Image cardBackImage;
        [SerializeField] private PortraitFallbackView portraitFallbackView;

        [Header("Choice previews")]
        [SerializeField] private ChoicePreviewView leftPreview;
        [SerializeField] private ChoicePreviewView rightPreview;

        /// <summary>True between a successful <see cref="Show"/> and the next <see cref="Clear"/>.</summary>
        public bool HasCard { get; private set; }

        public RectTransform CardRoot => cardRoot != null ? cardRoot : transform as RectTransform;

        /// <summary>The fixed CardBack container (Card.png), for the card-flip transition. Its
        /// RectTransform is CardBackArt's parent — see <see cref="cardBackImage"/>.</summary>
        public RectTransform CardBackTransform =>
            cardBackImage != null ? cardBackImage.rectTransform.parent as RectTransform : null;

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

        /// <summary>
        /// Makes the card visible without touching its content — for the card-flip transition,
        /// which needs the previous card's CardBack/Speaker/SituationText to stay on screen (and
        /// keep fading out) across the moment a completed decision would otherwise have hidden
        /// them via <see cref="Clear"/>, right up until <see cref="Show(CardDefinition)"/> swaps in
        /// the next card at the flip's midpoint.
        /// </summary>
        public void ForceVisible()
        {
            SetVisible(true);
        }

        /// <summary>
        /// Fades the speaker name and situation text independently of their assigned content — for
        /// the card-flip transition's old-question-out/new-question-in crossfade. Does not affect
        /// the portrait, CardBack, or either choice preview.
        /// </summary>
        public void SetContentAlpha(float speakerAlpha, float bodyAlpha)
        {
            if (speakerText != null)
            {
                speakerText.alpha = Mathf.Clamp01(speakerAlpha);
            }

            if (bodyText != null)
            {
                bodyText.alpha = Mathf.Clamp01(bodyAlpha);
            }
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

            ConfigureOptional(portraitMaskImage, Color.white, true);

            if (cardBackImage != null)
            {
                bool hasArt = theme.CardBackSprite != null;
                if (hasArt)
                {
                    cardBackImage.sprite = theme.CardBackSprite;
                }
                // Untinted when painted art is supplied (it is already the finished card-back
                // artwork); otherwise the flat card surface colour keeps the backdrop from
                // vanishing while placeholder art is missing.
                cardBackImage.color = hasArt ? Color.white : theme.CardSurface;
                cardBackImage.raycastTarget = false;
                cardBackImage.enabled = true;
            }

            // Speaker sits on the paper-toned NameScrim below, not the card's dark surface, so it
            // needs the theme's ink colour (same as bodyText) rather than a gold highlight.
            ConfigureText(speakerText, theme.SituationText, theme.TitleFont);
            // bodyText renders the situation panel above the card (light parchment), not the
            // card's own dark surface, so it needs the theme's ink colour rather than PrimaryText.
            ConfigureText(bodyText, theme.SituationText, theme.BodyFont);
            RepositionName();

            if (nameScrimImage != null)
            {
                // Paper tone matching ContentPanel/SituationPanel (SceneSetupAutomation's
                // SituationPanelColour) so the name band reads as part of the same fixed paper
                // column rather than a dark scrim over background art.
                nameScrimImage.color = new Color32(0xD9, 0xC7, 0x9E, 0xFF);
                nameScrimImage.raycastTarget = false;
                nameScrimImage.enabled = true;
            }

            portraitFallbackView?.ApplyTheme(theme);
            leftPreview?.ApplyTheme(theme);
            rightPreview?.ApplyTheme(theme);
        }

        private void RepositionName()
        {
            RectTransform nameRect = speakerText != null ? speakerText.rectTransform : null;
            if (nameRect == null)
            {
                return;
            }

            nameRect.anchorMin = NameAnchorMin;
            nameRect.anchorMax = NameAnchorMax;
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
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

        /// <summary>As above, for a procedural (spriteless) Graphic such as the portrait mask.</summary>
        private static void ConfigureOptional(Graphic graphic, Color color, bool enabled)
        {
            if (graphic == null)
            {
                return;
            }

            graphic.color = color;
            graphic.raycastTarget = false;
            graphic.enabled = enabled;
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
            Graphic portraitMask = null,
            PortraitFallbackView generatedPortraitFallback = null,
            Graphic nameScrim = null,
            Image cardBack = null)
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
            portraitMaskImage = portraitMask;
            nameScrimImage = nameScrim;
            cardBackImage = cardBack;
            portraitFallbackView = generatedPortraitFallback;
        }
#endif
    }
}
