using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>Non-interactive atmospheric backdrop with safe null-art fallbacks.</summary>
    public sealed class BackgroundView : MonoBehaviour
    {
        [SerializeField] private Image fallbackSurface;
        [SerializeField] private Image artwork;
        [Tooltip("Drives cover-fit (fill viewport, preserve aspect, crop overflow) for artwork.")]
        [SerializeField] private AspectRatioFitter artworkFitter;
        [SerializeField] private Image darkOverlay;
        [SerializeField] private Image vignette;
        [SerializeField] private ProceduralVignetteGraphic proceduralVignette;

        public void ApplyTheme(GameUITheme theme)
        {
            if (theme == null)
            {
                return;
            }

            Configure(fallbackSurface, null, theme.OverallBackground, true);
            Configure(artwork, theme.BackgroundSprite, Color.white, false);
            ApplyArtworkCoverFit(theme.BackgroundSprite);
            // Detailed artwork needs more separation from foreground UI than a flat colour did.
            Configure(darkOverlay, null, new Color(0f, 0f, 0f, 0.38f), true);
            Configure(vignette, theme.VignetteSprite, Color.white, false);
            if (proceduralVignette != null)
            {
                proceduralVignette.SetStyle(Color.black, 0.22f, 0.42f);
                proceduralVignette.enabled = theme.VignetteSprite == null;
            }
        }

        private void ApplyArtworkCoverFit(Sprite sprite)
        {
            if (artworkFitter == null)
            {
                return;
            }

            if (sprite == null || sprite.rect.height <= 0f)
            {
                artworkFitter.enabled = false;
                return;
            }

            artworkFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            artworkFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            artworkFitter.enabled = true;
        }

        private static void Configure(Image image, Sprite sprite, Color color, bool enabledWithoutSprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.enabled = sprite != null || enabledWithoutSprite;
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            Image surface,
            Image art,
            Image overlay,
            Image vignetteImage,
            ProceduralVignetteGraphic generatedVignette = null,
            AspectRatioFitter fitter = null)
        {
            fallbackSurface = surface;
            artwork = art;
            darkOverlay = overlay;
            vignette = vignetteImage;
            proceduralVignette = generatedVignette;
            artworkFitter = fitter;
        }
#endif
    }
}
