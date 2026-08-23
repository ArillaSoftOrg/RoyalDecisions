using System.Collections.Generic;
using RoyalDecisions.Data;

namespace RoyalDecisions.Editor
{
    /// <summary>Bölüm I — K1 to K25. See <see cref="StoryContentLibrary"/> for shared conventions.</summary>
    public static partial class StoryContentLibrary
    {
        internal static List<CardDefinition> CreateChapter1Cards()
        {
            return new List<CardDefinition>(25)
            {
                Card(1, "Ömer (Gözcü)",
                    "Ömer kapıya koşar. Dışarıda kapıyı çalanlar var, der. Kapatalım mı, son şans mı verelim?",
                    Choice("Kapat", security: 1, authority: -1, forcedNext: 2),
                    Choice("İçeri çek", authority: 1, wealth: -1, security: -1, forcedNext: 3)),

                Card(2, "Atilla (Sığınak Görevlisi)",
                    "Atilla yanına gelir. Dışarıda kalanlar hâlâ akıllarda, der. Konuşalım mı, geçelim mi?",
                    Choice("Geç", authority: -1, forcedNext: 4),
                    Choice("Konuş", authority: 1, forcedNext: 5)),

                Card(3, "Sabiha (Erzakçı)",
                    "Sabiha elinde defterle gelir. Depo hızla azalıyor, der. Payları nasıl bölelim?",
                    Choice("Kısıtlı pay ver", wealth: -1, authority: 1,
                        flagsAdd: Flags("k3_yolu"), forcedNext: 6),
                    Choice("Sınırsız paylaş", wealth: -2, authority: 2, forcedNext: 7)),

                Card(4, "Mustafa (Asker)",
                    "Mustafa gelir, yüzü asık. İki grup birbirine giriyor, der. Otorite mi, uzlaşma mı?",
                    Choice("Otorite kur", security: 1, authority: -1, forcedNext: 7),
                    Choice("Uzlaşma dene", authority: 1, security: -1, forcedNext: 8)),

                Card(5, "Sabiha (Erzakçı)",
                    "Sabiha çantasını hazırlar. Keşfe mi çıkalım, önce mi güçlenelim?",
                    Choice("Keşfe çık", forcedNext: 9),
                    Choice("Önce güçlen", security: 1, forcedNext: 6)),

                Card(6, "Kemal (Mühendis)",
                    "Kemal kroki elinde gelir. Kalabalık tek bölmede tehlikeli, der. Ayıralım mı, " +
                    "bir arada mı tutalım?",
                    Choice("Ayır, duvarlar örülür", security: -1, forcedNext: 7),
                    Choice("Bir arada tut", authority: 1, people: -1, forcedNext: 13)),

                Card(7, "Kemal / Mustafa",
                    "Kemal ya da Mustafa devam eder — ya yeni bölme kararı ya da otorite sonrası " +
                    "gerginlik. Sağlam mı çözelim, geçici mi geçelim?",
                    Choice("Sağlam çöz", security: 1, wealth: -1, forcedNext: 10),
                    Choice("Hafif geç", authority: -1, forcedNext: 10)),

                Card(8, "Rıza / Sabiha",
                    "Rıza bağırır: \"Hepsini yiyelim!\" Sabiha itiraz eder: \"Delilik bu!\"",
                    Choice("Kısıtlamaya devam et", wealth: -1, authority: -1, forcedNext: 11),
                    Choice("Dağıt, bir gecelik ziyafet",
                        conditionalEffect: Reign(NumericCondition.Always(), StatType.Wealth),
                        forcedNext: 11)),

                Card(9, "Ömer (Gözcü)",
                    "Ömer yorgun gelir. Nöbetçi az, der. Geniş-seyrek mi, dar-sıkı mı?",
                    Choice("Geniş-seyrek", authority: 1, flagsAdd: Flags("nobet_gevsek"), forcedNext: 11),
                    Choice("Dar-sıkı", security: 1, flagsAdd: Flags("nobet_siki"), forcedNext: 12)),

                Card(10, "Zeynep (Doktor)",
                    "Zeynep fenerle gelir. Kaç gündür uyumadın, der. Dinlenecek misin, nöbete mi " +
                    "katılacaksın?",
                    Choice("Dinlen", conditionalEffect: AlwaysLeaderHealth(1), forcedNext: 14),
                    Choice("Nöbete katıl", authority: 1, conditionalEffect: AlwaysLeaderHealth(-1),
                        forcedNext: 11)),

                Card(11, "Ömer (Gözcü)",
                    "Ömer koşarak gelir. Biri sızmış, der. Sessizce mi hallederiz, herkesi mi " +
                    "uyandırırız?",
                    Choice("Sessizce hallet", people: -1, conditionalEffect: AlwaysLeaderHealth(-1),
                        forcedNext: 14),
                    Choice("Herkesi uyandır", authority: -1, people: -1, forcedNext: 15)),

                Card(12, "Ömer (Gözcü)",
                    "Ömer gelir, gururlu ama endişeli. Saldırı püskürtüldü ama biri yaralı, der. " +
                    "Zeynep'i çağıralım mı?",
                    Choice("Çağır", people: 1, forcedNext: 14),
                    Choice("Bekler", people: -1, forcedNext: 15)),

                Card(13, "Zeynep (Doktor)",
                    "Zeynep telaşla gelir. Biri döküntülerle uyandı, der. Karantina mı, görmezden " +
                    "mi gelelim?",
                    Choice("Karantina ilan et", people: 1, authority: -1,
                        flagsAdd: Flags("karantina_evet"), forcedNext: 17),
                    Choice("Görmezden gel", authority: 1, people: -1,
                        flagsAdd: Flags("karantina_hayir"), forcedNext: 18)),

                Card(14, "Mete (Asker)",
                    "Mete gelir. Dışarıda bir gölge var, der. Bizzat mı gidiyorsun, nöbetçi mi " +
                    "gönderiyorsun?",
                    Choice("Bizzat git", conditionalEffect: LeaderRisk(leaderHealthDeltaWhenFalse: -3),
                        forcedNext: 18),
                    Choice("Nöbetçi gönder", authority: -1, forcedNext: 15)),

                Card(15, "Zeynep (Doktor)",
                    "Zeynep yaralının yanına koşar. İçeri mi alalım, gözlemleyelim mi?",
                    Choice("Al", people: -1, authority: 1, forcedNext: 19),
                    Choice("Gözlemle", forcedNext: 16)),

                Card(16, "Ali & Veli",
                    "Ali ve Veli kart oyunu oynuyor. Atilla gülümser. Otur musun, işine mi dönersin?",
                    Choice("Otur", forcedNext: 19),
                    Choice("İşe dön", forcedNext: 17)),

                Card(17, "Zeynep (Doktor)",
                    "Zeynep üzgün gelir. Hastalık durdu ama karantinadaki öldü, der. Tören mi, " +
                    "sessiz mi?",
                    Choice("Tören düzenle", authority: 1, forcedNext: 19),
                    Choice("Sessizce göm", forcedNext: 20)),

                Card(18, "Zeynep (Doktor)",
                    "Zeynep telaşla gelir. Hastalık yayıldı, der. Şimdi karantina mı, doğaçlama " +
                    "tedavi mi?",
                    Choice("Karantina uygula", people: -2, authority: -1, forcedNext: 20),
                    Choice("Doğaçlama tedavi et",
                        randomOutcome: new RandomStatOutcome(
                            new StatDeltas(0, 1, 0, 0), new StatDeltas(0, -1, 0, 0)),
                        forcedNext: 19)),

                Card(19, "İsmet (Telsizci)",
                    "İsmet bir kimlik kartı bulur. Vertak yazıyor üstünde, der. Sorgulayalım mı, " +
                    "saklayalım mı?",
                    Choice("Sorgula", authority: -1,
                        counterDeltas: Counter(CounterVertakIpucu, 1), forcedNext: 21),
                    Choice("Sakla", forcedNext: 20)),

                Card(20, "Ömer (Gözcü)",
                    "Ömer koşarak gelir. Sürü sesi geliyor, der. Barikat mı, saklanma mı?",
                    Choice("Barikat kur", security: 1, wealth: -1,
                        flagsAdd: Flags("savunma_barikat"), forcedNext: 23),
                    Choice("Saklan", authority: -1, flagsAdd: Flags("savunma_saklanma"), forcedNext: 21)),

                Card(21, "Aziz (Tarımcı)",
                    "Aziz son kahveyi demler. İçer misin, başkasına mı vereyim?",
                    Choice("İç", forcedNext: 24),
                    Choice("Ver", forcedNext: 22)),

                Card(22, "Mustafa (Asker)",
                    "Mustafa silahını hazırlar. Herkesi içeri mi toplayalım, nöbetçileri mi " +
                    "konumlandırayım?",
                    Choice("Topla", authority: 1, forcedNext: 24),
                    Choice("Konumlandır", security: 1, conditionalEffect: AlwaysLeaderHealth(-1),
                        forcedNext: 23)),

                Card(23, "Kemal (Mühendis)",
                    "Kemal gelir. Barikat tuttu ama hasar gördü, der. Hemen mi onaralım, sonra mı?",
                    Choice("Hemen onar", security: -1, wealth: -1, forcedNext: 26),
                    Choice("Sonra onar", security: -2, forcedNext: 24)),

                Card(24, "Ömer (Gözcü)",
                    "Ömer alçak sesle gelir. Sürü fark etmeden geçti, der. Az kalsın biri ses " +
                    "çıkarıyordu. Uyar mı, şansa mı bırak?",
                    Choice("Uyar", authority: -1, forcedNext: 25),
                    Choice("Şansa bırak",
                        randomOutcome: new RandomStatOutcome(
                            new StatDeltas(0, -2, 0, 0), new StatDeltas(0, 0, 0, 0)),
                        forcedNext: 25)),

                Card(25, "İsmet (Telsizci)",
                    "İsmet kulaklığını çıkarır. Vertak'a sinyal mi, yalnız mı devam?",
                    Choice("Sinyal gönder", forcedNext: 26),
                    Choice("Yalnız devam et", forcedNext: 29)),
            };
        }
    }
}
