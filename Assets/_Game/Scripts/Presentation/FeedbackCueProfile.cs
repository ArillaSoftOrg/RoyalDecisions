using UnityEngine;

namespace RoyalDecisions.Presentation
{
    [CreateAssetMenu(
        menuName = "Royal Decisions/Presentation/Feedback Cue Profile",
        fileName = "FeedbackCueProfile")]
    public sealed class FeedbackCueProfile : ScriptableObject
    {
        [SerializeField] private string uiClick = string.Empty;
        [SerializeField] private string sliderTick = string.Empty;
        [SerializeField] private string cardEnter = string.Empty;
        [SerializeField] private string threshold = string.Empty;
        [SerializeField] private string cardPreview = string.Empty;
        [SerializeField] private string snapBack = string.Empty;
        [SerializeField] private string leftConfirmation = string.Empty;
        [SerializeField] private string rightConfirmation = string.Empty;
        [SerializeField] private string exit = string.Empty;
        [SerializeField] private string statIncrease = string.Empty;
        [SerializeField] private string statDecrease = string.Empty;
        [SerializeField] private string critical = string.Empty;
        [SerializeField] private string gameOver = string.Empty;
        [SerializeField] private string restart = string.Empty;
        [SerializeField] private string menuMusic = string.Empty;
        [SerializeField] private string gameplayMusic = string.Empty;
        [SerializeField] private string ambientLoop = string.Empty;

        public string UiClick => uiClick ?? string.Empty;
        public string SliderTick => sliderTick ?? string.Empty;
        public string CardEnter => cardEnter ?? string.Empty;
        public string Threshold => threshold ?? string.Empty;
        public string CardPreview => cardPreview ?? string.Empty;
        public string SnapBack => snapBack ?? string.Empty;
        public string LeftConfirmation => leftConfirmation ?? string.Empty;
        public string RightConfirmation => rightConfirmation ?? string.Empty;
        public string Exit => exit ?? string.Empty;
        public string StatIncrease => statIncrease ?? string.Empty;
        public string StatDecrease => statDecrease ?? string.Empty;
        public string Critical => critical ?? string.Empty;
        public string GameOver => gameOver ?? string.Empty;
        public string Restart => restart ?? string.Empty;
        public string MenuMusic => menuMusic ?? string.Empty;
        public string GameplayMusic => gameplayMusic ?? string.Empty;
        public string AmbientLoop => ambientLoop ?? string.Empty;
    }
}
