using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    public sealed class TutorialOverlayView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private string[] titles =
        {
            "Karar Ver", "Dengeyi Koru", "Sınırları İzle"
        };
        [SerializeField] private string[] bodies =
        {
            "Kartı sola veya sağa sürükle. Seçimin, bırakmadan önce görünür.",
            "Otorite, Halk, Güvenlik ve Servet değerlerini dengede tut.",
            "Bir değer 0 veya 100 olduğunda hükümranlık sona erer."
        };

        public event Action NextRequested;
        public event Action SkipRequested;

        public int StepCount => Math.Min(titles?.Length ?? 0, bodies?.Length ?? 0);

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void OnEnable()
        {
            if (nextButton != null) nextButton.onClick.AddListener(HandleNext);
            if (skipButton != null) skipButton.onClick.AddListener(HandleSkip);
        }

        private void OnDisable()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(HandleNext);
            if (skipButton != null) skipButton.onClick.RemoveListener(HandleSkip);
        }

        public void ShowStep(int index)
        {
            if (StepCount == 0)
            {
                return;
            }
            int safe = Mathf.Clamp(index, 0, StepCount - 1);
            if (titleText != null) titleText.text = titles[safe] ?? string.Empty;
            if (bodyText != null) bodyText.text = bodies[safe] ?? string.Empty;
            panelRoot?.SetActive(true);
        }

        public void Hide() => panelRoot?.SetActive(false);

        private void HandleNext() => NextRequested?.Invoke();
        private void HandleSkip() => SkipRequested?.Invoke();

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            GameObject root,
            TMP_Text title,
            TMP_Text body,
            Button next,
            Button skip)
        {
            panelRoot = root;
            titleText = title;
            bodyText = body;
            nextButton = next;
            skipButton = skip;
        }
#endif
    }
}
