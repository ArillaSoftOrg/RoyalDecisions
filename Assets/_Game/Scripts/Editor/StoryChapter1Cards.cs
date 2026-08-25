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
                    "Ömer kapıdan koşarak gelir. “Dışarıda hâlâ insanlar var. Şimdi kapatırsak " +
                    "bazıları dışarıda kalacak.”",
                    Choice("Kapıları kapat", security: 1, authority: -1, forcedNext: 2),
                    Choice("Birkaç kişiyi daha içeri al", authority: 1, wealth: -1, security: -1, forcedNext: 3)),

                Card(2, "Atilla (Sığınak Görevlisi)",
                    "Atilla bir süre sessizce yanında durur. “Kapının dışında bıraktıklarımızı " +
                    "herkes gördü. Kimse konuşmuyor ama unutmuş da değiller.”",
                    Choice("Konuyu kapat", authority: -1, forcedNext: 4),
                    Choice("Herkesi topla, konuşalım", authority: 1, forcedNext: 5)),

                Card(3, "Sabiha (Erzakçı)",
                    "Sabiha elindeki defteri açar. “İçeride planladığımızdan fazla insan var. " +
                    "Depo bu hızla uzun süre dayanmaz.”",
                    Choice("Porsiyonları küçült", wealth: -1, authority: 1,
                        flagsAdd: Flags("k3_yolu"), forcedNext: 6),
                    Choice("Kimseyi kısmadan dağıt", wealth: -2, authority: 2, forcedNext: 7)),

                Card(4, "Mustafa (Asker)",
                    "Mustafa öfkeli bir hâlde gelir. “İki grup birbirine girdi. Biraz daha " +
                    "sürerse yumruklar konuşacak.”",
                    Choice("Araya gir, düzeni sağla", security: 1, authority: -1, forcedNext: 7),
                    Choice("İki tarafı da masaya oturt", authority: 1, security: -1, forcedNext: 8)),

                Card(5, "Sabiha (Erzakçı)",
                    "Sabiha çantasını hazırlamış bekliyor. “Dışarı çıkıp çevreyi tarayabiliriz. " +
                    "Ama içeride toparlanacak çok iş var.”",
                    Choice("Keşif ekibini çıkar", forcedNext: 9),
                    Choice("Önce sığınağı toparla", security: 1, forcedNext: 6)),

                Card(6, "Kemal (Mühendis)",
                    "Kemal krokiyi masaya serer. “Bu kadar kişiyi tek bölmede tutmak güvenli " +
                    "değil. Araya duvar çekebilirim ama yer daha da daralır.”",
                    Choice("Bölmeleri ayır", security: -1, flagsAdd: Flags("bolme_karari"), forcedNext: 7),
                    Choice("Herkesi bir arada tut", authority: 1, people: -1, forcedNext: 13)),

                Card(7, "Mustafa (Asker)",
                    "Mustafa yüzünü asar. “Kısıtlama olmayınca kimi iki pay aldı, kimi aç kaldı. " +
                    "Bir daha olmaması için bir düzen koymamız gerek.”",
                    Choice("Kesin kurallar koy", security: 1, wealth: -1, forcedNext: 10),
                    Choice("Bu kez uyarıyla geçiştir", authority: -1, forcedNext: 10),
                    variants: new[]
                    {
                        VariantIfFlag("bolme_karari",
                            "Kemal ellerindeki tozu silkeler. “Duvar tamam ama ayrılanlar " +
                            "homurdanıyor. Bu düzen kalıcı mı olacak?”",
                            Choice("Kararın arkasında dur", security: 1, wealth: -1, forcedNext: 10),
                            Choice("Geçici olduğunu söyle", authority: -1, forcedNext: 10),
                            speaker: "Kemal (Mühendis)")
                    }),

                Card(8, "Rıza / Sabiha",
                    "Rıza kalabalığın içinden bağırır: “Bir geceliğine de olsa karnımız doysun!” " +
                    "Sabiha hemen karşı çıkar: “Depoyu boşaltırsak yarını çıkaramayız.”",
                    Choice("Dağıtımı kısıtlı tut", wealth: -1, authority: -1, forcedNext: 11),
                    Choice("Depoyu aç, bir gecelik ziyafet ver",
                        conditionalEffect: Reign(NumericCondition.Always(), StatType.Wealth),
                        forcedNext: 11)),

                Card(9, "Ömer (Gözcü)",
                    "Ömer gözlerini ovuşturarak gelir. “Nöbetçi az. Ya çevreyi geniş tutup seyrek " +
                    "gezeceğiz ya da girişlere yığılıp sıkı nöbet tutacağız.”",
                    Choice("Çevreyi geniş tut", authority: 1, flagsAdd: Flags("nobet_gevsek"), forcedNext: 11),
                    Choice("Girişleri sıkı tut", security: 1, flagsAdd: Flags("nobet_siki"), forcedNext: 12)),

                Card(10, "Zeynep (Doktor)",
                    "Zeynep elindeki feneri yüzüne tutar. “Kaç gecedir doğru dürüst uyumadın. " +
                    "Biraz daha zorlarsan ayakta duramayacaksın.”",
                    Choice("Bu gece dinlen", conditionalEffect: AlwaysLeaderHealth(1), forcedNext: 14),
                    Choice("Nöbete katıl", authority: 1, conditionalEffect: AlwaysLeaderHealth(-1),
                        forcedNext: 11)),

                Card(11, "Ömer (Gözcü)",
                    "Ömer nefes nefese gelir. “Birisi içeri sızmış. Henüz kimse fark etmedi.”",
                    Choice("Sessizce etkisiz hâle getir", people: -1, conditionalEffect: AlwaysLeaderHealth(-1),
                        forcedNext: 14),
                    Choice("Herkesi uyandır", authority: -1, people: -1, forcedNext: 15)),

                Card(12, "Ömer (Gözcü)",
                    "Ömer bu kez gururlu ama gergindir. “Saldırıyı püskürttük. Bir nöbetçi " +
                    "yaralandı.”",
                    Choice("Zeynep’i hemen çağır", people: 1, forcedNext: 14),
                    Choice("Şimdilik beklesin", people: -1, forcedNext: 15)),

                Card(13, "Zeynep (Doktor)",
                    "Zeynep aceleyle gelir. “Birinde döküntü başladı. Ne olduğunu bilmiyoruz; " +
                    "diğerlerinden ayırmazsak risk almış oluruz.”",
                    Choice("Karantinaya al", people: 1, authority: -1,
                        flagsAdd: Flags("karantina_evet"), forcedNext: 17),
                    Choice("Şimdilik dokunma", authority: 1, people: -1,
                        flagsAdd: Flags("karantina_hayir"), forcedNext: 18)),

                Card(14, "Mete (Asker)",
                    "Mete kapıda bekler. “Dışarıda bir gölge dolaşıyor. Kim olduğunu göremedim.”",
                    Choice("Kendin kontrol et", conditionalEffect: LeaderRisk(leaderHealthDeltaWhenFalse: -3),
                        forcedNext: 18),
                    Choice("Nöbetçiyi gönder", authority: -1, forcedNext: 15)),

                Card(15, "Zeynep (Doktor)",
                    "Zeynep kapının önündeki yaralı yabancıyı gösterir. “Durumu kötü. İçeri " +
                    "alırsak ne taşıdığını da içeri almış oluruz.”",
                    Choice("İçeri al", people: -1, authority: 1, forcedNext: 19),
                    Choice("Dışarıda gözlem altında tut", forcedNext: 16)),

                Card(16, "Ali & Veli",
                    "Ali ile Veli köşede kart oynuyor. Atilla boş bir sandalye çekip sana bakar. " +
                    "“Bir el sürer, hepsi bu.”",
                    Choice("Bir el otur", forcedNext: 19),
                    Choice("İşinin başına dön", forcedNext: 17)),

                Card(17, "Zeynep (Doktor)",
                    "Zeynep gözlerini kaçırır. “Hastalık yayılmadı. Ama karantinaya aldığımız " +
                    "kişi geceyi çıkaramadı.”",
                    Choice("Küçük bir tören düzenle", authority: 1, forcedNext: 19),
                    Choice("Sessizce gömüp işlere dön", wealth: 1, forcedNext: 20)),

                Card(18, "Zeynep (Doktor)",
                    "Zeynep telaşla gelir. “Döküntü başkalarında da çıktı. Artık bekleyemeyiz.”",
                    Choice("Geç de olsa karantina uygula", people: -2, authority: -1, forcedNext: 20),
                    Choice("Eldekilerle tedavi etmeye çalış", people: -1, forcedNext: 19)),

                Card(19, "İsmet (Telsizci)",
                    "İsmet yaralının eşyaları arasında bir kimlik kartı bulur. Kartın üzerinde " +
                    "tek bir isim okunuyor: “Vertak.”",
                    Choice("Sahibine sorular sor", authority: -1,
                        counterDeltas: Counter(CounterVertakIpucu, 1), forcedNext: 21),
                    Choice("Kartı şimdilik sakla", forcedNext: 20)),

                Card(20, "Ömer (Gözcü)",
                    "Ömer koşarak içeri girer. “Sürü yaklaşıyor. Çok kalabalıklar; hazırlanmak " +
                    "için fazla vaktimiz yok.”",
                    Choice("Barikat kur", security: 1, wealth: -1,
                        flagsAdd: Flags("savunma_barikat"), forcedNext: 23),
                    Choice("Işıkları söndür, herkes saklansın", authority: -1, flagsAdd: Flags("savunma_saklanma"), forcedNext: 21)),

                Card(21, "Aziz (Tarımcı)",
                    "Aziz saklanırken kalan son kahveyi demler. Kupayı uzatır. “Böyle bir gecede " +
                    "işe yarar.”",
                    Choice("Kahveyi iç", forcedNext: 24),
                    Choice("Başkasına ver", forcedNext: 22)),

                Card(22, "Mustafa (Asker)",
                    "Sığınak sessizliğe gömülür. Mustafa fısıldar: “Herkesi tek yerde tutabilirim. " +
                    "Ya da nöbetçileri girişlere dağıtırım.”",
                    Choice("Herkesi içeride topla", authority: 1, forcedNext: 24),
                    Choice("Nöbetçileri girişlere yerleştir", security: 1,
                        conditionalEffect: AlwaysLeaderHealth(-1), forcedNext: 24)),

                Card(23, "Kemal (Mühendis)",
                    "Kemal hasarlı barikata bakar. “İşe yaradı ama bir darbeyi daha kaldırmaz.”",
                    Choice("Hemen onar", security: -1, wealth: -1, forcedNext: 26),
                    Choice("Onarımı ertele", security: -2, forcedNext: 26)),

                Card(24, "Ömer (Gözcü)",
                    "Ömer sesini alçaltır. “Sürü bizi fark etmeden geçti. Ama içeriden biri az " +
                    "daha ses çıkarıyordu.”",
                    Choice("Herkesi açıkça uyar", authority: -1, forcedNext: 25),
                    Choice("Konuyu büyütme", forcedNext: 25),
                    variants: new[]
                    {
                        VariantIfFlag("nobet_gevsek",
                            "Ömer sesini alçaltır. “Sürü bizi fark etmeden geçti. Ama içeriden " +
                            "biri az daha ses çıkarıyordu.”",
                            Choice("Herkesi açıkça uyar", authority: -1, forcedNext: 25),
                            Choice("Konuyu büyütme", people: -2, forcedNext: 25))
                    }),

                Card(25, "İsmet (Telsizci)",
                    "İsmet kulaklığı çıkarıp masaya bırakır. “Vertak frekansı hâlâ açık. İstersek " +
                    "ilk teması şimdi kurabiliriz.”",
                    Choice("Sinyal gönder", forcedNext: 26),
                    Choice("Sessiz kal, kendi yolumuzda devam et", forcedNext: 29)),
            };
        }
    }
}
