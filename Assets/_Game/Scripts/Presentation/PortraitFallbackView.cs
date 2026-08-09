using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>Shows a neutral, texture-free silhouette while portrait artwork is absent.</summary>
    public sealed class PortraitFallbackView : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Image backdrop;
        [SerializeField] private Image head;
        [SerializeField] private Image shoulders;
        [SerializeField] private Image torso;

        public bool IsVisible => visualRoot != null && visualRoot.activeSelf;

        public void SetVisible(bool visible)
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(visible);
            }
        }

        public void ApplyTheme(GameUITheme theme)
        {
            if (theme == null)
            {
                return;
            }

            Configure(backdrop, theme.PortraitFallbackBackground);
            Configure(head, theme.PortraitFallbackForeground);
            Configure(shoulders, theme.PortraitFallbackForeground);
            Configure(torso, theme.PortraitFallbackForeground);
        }

        private static void Configure(Image image, Color colour)
        {
            if (image == null)
            {
                return;
            }

            image.color = colour;
            image.raycastTarget = false;
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            GameObject root,
            Image background,
            Image headImage,
            Image shoulderImage,
            Image torsoImage)
        {
            visualRoot = root;
            backdrop = background;
            head = headImage;
            shoulders = shoulderImage;
            torso = torsoImage;
        }
#endif
    }
}
