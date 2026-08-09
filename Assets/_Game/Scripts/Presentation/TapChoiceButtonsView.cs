using RoyalDecisions.Data;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Optional on-screen alternative to the swipe gesture: two buttons that confirm a choice
    /// directly. This view owns no rule or story logic — it only forwards a tap to
    /// <see cref="CardSwipeController.ConfirmSide"/>, which is the same confirmation path a
    /// completed drag uses, so a decision still resolves exactly once (CLAUDE.md §5, §9).
    /// </summary>
    public sealed class TapChoiceButtonsView : MonoBehaviour
    {
        [SerializeField] private CardSwipeController swipeController;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        private void OnEnable()
        {
            if (leftButton != null) leftButton.onClick.AddListener(HandleLeftClicked);
            if (rightButton != null) rightButton.onClick.AddListener(HandleRightClicked);
        }

        private void OnDisable()
        {
            if (leftButton != null) leftButton.onClick.RemoveListener(HandleLeftClicked);
            if (rightButton != null) rightButton.onClick.RemoveListener(HandleRightClicked);
        }

        private void HandleLeftClicked() => swipeController?.ConfirmSide(ChoiceSide.Left);

        private void HandleRightClicked() => swipeController?.ConfirmSide(ChoiceSide.Right);

#if UNITY_EDITOR
        public void SetAuthoringReferences(CardSwipeController swipe, Button left, Button right)
        {
            swipeController = swipe;
            leftButton = left;
            rightButton = right;
        }
#endif
    }
}
