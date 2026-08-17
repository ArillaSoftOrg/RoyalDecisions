using System;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Static credits/version panel. Purely presentational — its text is authored content, not
    /// code, so replacing the copy never requires a script change.
    /// </summary>
    public sealed class AboutPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;

        [Tooltip("Optional. Animates opening/closing; falls back to an instant SetActive when absent.")]
        [SerializeField] private PanelFadeAnimator panelAnimator;

        public event Action CloseRequested;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void OnEnable()
        {
            if (closeButton != null) closeButton.onClick.AddListener(HandleClose);
        }

        private void OnDisable()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(HandleClose);
        }

        public void Show()
        {
            if (panelAnimator != null)
            {
                panelAnimator.Show();
            }
            else
            {
                panelRoot?.SetActive(true);
            }
        }

        public void Hide()
        {
            if (panelAnimator != null)
            {
                panelAnimator.Hide();
            }
            else
            {
                panelRoot?.SetActive(false);
            }
        }

        private void HandleClose()
        {
            Hide();
            CloseRequested?.Invoke();
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(GameObject root, Button close, PanelFadeAnimator transition = null)
        {
            panelRoot = root;
            closeButton = close;
            panelAnimator = transition;
        }
#endif
    }
}
