using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>Non-interactive atmospheric backdrop with safe null-art fallbacks.</summary>
    public sealed class BackgroundView : MonoBehaviour
    {
        [SerializeField] private Image fallbackSurface;
        [SerializeField] private Image artwork;
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
            Configure(darkOverlay, null, new Color(0f, 0f, 0f, 0.28f), true);
            Configure(vignette, theme.VignetteSprite, Color.white, false);
            if (proceduralVignette != null)
            {
                proceduralVignette.SetStyle(Color.black, 0.22f, 0.42f);
                proceduralVignette.enabled = theme.VignetteSprite == null;
            }
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
            ProceduralVignetteGraphic generatedVignette = null)
        {
            fallbackSurface = surface;
            artwork = art;
            darkOverlay = overlay;
            vignette = vignetteImage;
            proceduralVignette = generatedVignette;
        }
#endif
    }
}
