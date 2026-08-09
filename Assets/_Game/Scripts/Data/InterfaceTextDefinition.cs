using UnityEngine;

namespace RoyalDecisions.Data
{
    /// <summary>Reusable interface wording for one language. Narrative remains in card assets.</summary>
    [CreateAssetMenu(
        menuName = "Royal Decisions/Interface Text",
        fileName = "InterfaceText")]
    public sealed class InterfaceTextDefinition : ScriptableObject
    {
        [SerializeField] private string languageCode = "tr";
        [SerializeField] private string mainMenuTitle = "Royal Decisions";
        [SerializeField] private string newGame = "Yeni Oyun";
        [SerializeField] private string continueGame = "Devam Et";
        [SerializeField] private string restart = "Yeniden Başlat";
        [SerializeField] private string gameOverTitle = "Hükümdarlık Sona Erdi";
        [TextArea(2, 5)]
        [SerializeField] private string gameOverBody = "Hükümdarlığınız sona erdi.";
        [SerializeField] private string authority = "Otorite";
        [SerializeField] private string people = "Halk";
        [SerializeField] private string security = "Güvenlik";
        [SerializeField] private string wealth = "Servet";
        [SerializeField] private string year = "Yıl";
        [SerializeField] private string turn = "Tur";
        [SerializeField] private string leftPlaceholderChoice = "Sol Karar";
        [SerializeField] private string rightPlaceholderChoice = "Sağ Karar";
        [TextArea(2, 4)]
        [SerializeField] private string corruptSave = "Kayıt dosyası okunamadı.";
        [TextArea(2, 4)]
        [SerializeField] private string unsupportedSave =
            "Bu kayıt oyunun daha yeni bir sürümüyle oluşturulmuş.";

        public string LanguageCode => languageCode ?? string.Empty;
        public string MainMenuTitle => mainMenuTitle ?? string.Empty;
        public string NewGame => newGame ?? string.Empty;
        public string ContinueGame => continueGame ?? string.Empty;
        public string Restart => restart ?? string.Empty;
        public string GameOverTitle => gameOverTitle ?? string.Empty;
        public string GameOverBody => gameOverBody ?? string.Empty;
        public string Authority => authority ?? string.Empty;
        public string People => people ?? string.Empty;
        public string Security => security ?? string.Empty;
        public string Wealth => wealth ?? string.Empty;
        public string Year => year ?? string.Empty;
        public string Turn => turn ?? string.Empty;
        public string LeftPlaceholderChoice => leftPlaceholderChoice ?? string.Empty;
        public string RightPlaceholderChoice => rightPlaceholderChoice ?? string.Empty;
        public string CorruptSave => corruptSave ?? string.Empty;
        public string UnsupportedSave => unsupportedSave ?? string.Empty;

        public string GetStatLabel(StatType stat)
        {
            return stat switch
            {
                StatType.Authority => Authority,
                StatType.People => People,
                StatType.Security => Security,
                StatType.Wealth => Wealth,
                _ => string.Empty
            };
        }

#if UNITY_EDITOR
        public void SetAuthoringData(
            string code,
            string menuTitle,
            string newGameLabel,
            string continueLabel,
            string restartLabel,
            string fallbackTitle,
            string fallbackBody,
            string authorityLabel,
            string peopleLabel,
            string securityLabel,
            string wealthLabel,
            string yearLabel,
            string turnLabel,
            string leftChoiceLabel,
            string rightChoiceLabel,
            string corruptSaveMessage,
            string unsupportedSaveMessage)
        {
            languageCode = code ?? string.Empty;
            mainMenuTitle = menuTitle ?? string.Empty;
            newGame = newGameLabel ?? string.Empty;
            continueGame = continueLabel ?? string.Empty;
            restart = restartLabel ?? string.Empty;
            gameOverTitle = fallbackTitle ?? string.Empty;
            gameOverBody = fallbackBody ?? string.Empty;
            authority = authorityLabel ?? string.Empty;
            people = peopleLabel ?? string.Empty;
            security = securityLabel ?? string.Empty;
            wealth = wealthLabel ?? string.Empty;
            year = yearLabel ?? string.Empty;
            turn = turnLabel ?? string.Empty;
            leftPlaceholderChoice = leftChoiceLabel ?? string.Empty;
            rightPlaceholderChoice = rightChoiceLabel ?? string.Empty;
            corruptSave = corruptSaveMessage ?? string.Empty;
            unsupportedSave = unsupportedSaveMessage ?? string.Empty;
        }
#endif
    }
}
