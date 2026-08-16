using System;
using System.Collections;
using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Fades and gently scales a panel in and out instead of an instant SetActive pop.
    /// </summary>
    /// <remarks>
    /// Purely visual: it never decides whether a panel should be open, only how getting there
    /// looks. Mirrors <see cref="CardSwipeController"/>'s animation shape — unscaled time, an
    /// <see cref="AnimationCurve"/> ease, and a reduced-motion mode that shortens rather than
    /// removes the transition — so every animated presentation in the game behaves the same way.
    /// </remarks>
    public sealed class PanelFadeAnimator : MonoBehaviour
    {
        private const float RestingScale = 0.94f;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float showDuration = 0.16f;
        [SerializeField] private float hideDuration = 0.12f;
        [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Coroutine runningAnimation;
        private float defaultShowDuration;
        private float defaultHideDuration;
        private bool accessibilityDefaultsCaptured;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        public void Show()
        {
            if (IsVisible && CurrentAlpha() >= 1f)
            {
                return;
            }

            float from = CurrentAlpha();
            panelRoot?.SetActive(true);
            StopRunningAnimation();

            if (!CanRunCoroutines() || showDuration <= 0f)
            {
                ApplyProgress(1f);
                return;
            }

            runningAnimation = StartCoroutine(FadeRoutine(from, 1f, showDuration, null));
        }

        public void Hide(Action onComplete = null)
        {
            if (!IsVisible)
            {
                onComplete?.Invoke();
                return;
            }

            float from = CurrentAlpha();
            StopRunningAnimation();

            if (!CanRunCoroutines() || hideDuration <= 0f)
            {
                ApplyProgress(0f);
                panelRoot.SetActive(false);
                onComplete?.Invoke();
                return;
            }

            runningAnimation = StartCoroutine(FadeRoutine(from, 0f, hideDuration, onComplete));
        }

        /// <summary>Shortens the transition instead of removing it, matching the swipe card's mode.</summary>
        public void SetReducedMotion(bool enabled)
        {
            if (!accessibilityDefaultsCaptured)
            {
                defaultShowDuration = showDuration;
                defaultHideDuration = hideDuration;
                accessibilityDefaultsCaptured = true;
            }

            showDuration = enabled ? Mathf.Min(defaultShowDuration, 0.05f) : defaultShowDuration;
            hideDuration = enabled ? Mathf.Min(defaultHideDuration, 0.05f) : defaultHideDuration;
        }

        private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // Unscaled: menu feedback must keep moving even if gameplay time is paused.
                elapsed += Time.unscaledDeltaTime;
                float t = Evaluate(ease, elapsed / duration);
                ApplyProgress(Mathf.Lerp(from, to, t));
                yield return null;
            }

            ApplyProgress(to);
            runningAnimation = null;

            if (to <= 0f && panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            onComplete?.Invoke();
        }

        private void ApplyProgress(float alpha)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = alpha;
            canvasGroup.interactable = alpha >= 1f;
            canvasGroup.blocksRaycasts = alpha > 0f;
            canvasGroup.transform.localScale = Vector3.one * Mathf.Lerp(RestingScale, 1f, alpha);
        }

        private float CurrentAlpha()
        {
            return canvasGroup != null ? canvasGroup.alpha : (IsVisible ? 1f : 0f);
        }

        private void OnDisable()
        {
            StopRunningAnimation();
        }

        private void StopRunningAnimation()
        {
            if (runningAnimation == null)
            {
                return;
            }

            if (CanRunCoroutines())
            {
                StopCoroutine(runningAnimation);
            }

            runningAnimation = null;
        }

        private bool CanRunCoroutines()
        {
            return Application.isPlaying && isActiveAndEnabled;
        }

        private static float Evaluate(AnimationCurve curve, float rawProgress)
        {
            float t = Mathf.Clamp01(rawProgress);
            return curve != null && curve.length > 0 ? curve.Evaluate(t) : t;
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(GameObject root, CanvasGroup group)
        {
            panelRoot = root;
            canvasGroup = group;
        }
#endif
    }
}
