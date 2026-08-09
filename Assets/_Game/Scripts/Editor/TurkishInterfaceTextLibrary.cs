using RoyalDecisions.Data;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    /// <summary>Builds the temporary Turkish MVP interface text in memory.</summary>
    public static class TurkishInterfaceTextLibrary
    {
        public static InterfaceTextDefinition Create()
        {
            InterfaceTextDefinition text = ScriptableObject.CreateInstance<InterfaceTextDefinition>();
            text.name = "TurkishInterfaceText";
            text.SetAuthoringData(
                "tr",
                "Royal Decisions",
                "Yeni Oyun",
                "Devam Et",
                "Yeniden Başlat",
                "Hükümdarlık Sona Erdi",
                "Hükümdarlığınız sona erdi. Tarih, son kararınızı sessizce kaydetti.",
                "Otorite",
                "Halk",
                "Güvenlik",
                "Servet",
                "Yıl",
                "Tur",
                "Sol Karar",
                "Sağ Karar",
                "Kayıt dosyası okunamadı.",
                "Bu kayıt oyunun daha yeni bir sürümüyle oluşturulmuş.");
            return text;
        }
    }
}
