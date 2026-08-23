using System.Collections.Generic;
using RoyalDecisions.Data;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Bölüm II — K26 to K60. See <see cref="StoryContentLibrary"/> for shared conventions.
    /// </summary>
    /// <remarks>
    /// This chapter introduces the specification's "*Eğer flag ise:* ... *değilse:*" pattern: a
    /// card whose text and choices differ depending on an earlier decision. Each base card below is
    /// authored as the specification's "otherwise" branch, with the flagged branch as a single
    /// <see cref="CardVariant"/> — see each card's comment for which flag selects it.
    /// </remarks>
    public static partial class StoryContentLibrary
    {
        internal static List<CardDefinition> CreateChapter2Cards()
        {
            return new List<CardDefinition>(35)
            {
                Card(26, "Semra (Halktan)",
                    "Semra tozlu bir gitar bulur. Tamir edeyim mi, bırakayım mı?",
                    Choice("Tamir et", forcedNext: 29),
                    Choice("Bırak", forcedNext: 27)),

                // K27-A sets cit_yaklastik_evet, selecting K28's variant below.
                Card(27, "Ömer (Gözcü)",
                    "Ömer nöbette gelir. Çitin ötesinde bir şey konuşuyor gibi, der. Yaklaşalım mı, " +
                    "uzaktan mı izleyelim?",
                    Choice("Yaklaş", authority: -1, flagsAdd: Flags("cit_yaklastik_evet"), forcedNext: 28),
                    Choice("Uzaktan izle", forcedNext: 28)),

                Card(28, "Ömer (Gözcü)",
                    "Ses kesildi. Devriyeyi artır mı, gerek yok mu?",
                    Choice("Artır", security: 1, forcedNext: 30),
                    Choice("Gerek yok", forcedNext: 31),
                    variants: new[]
                    {
                        VariantIfFlag("cit_yaklastik_evet",
                            "\"Yardım\" diyor ama gözleri insan gözü değil. Ateş mi, dinle mi?",
                            Choice("Ateş et", authority: -1, forcedNext: 31),
                            Choice("Dinle", authority: -2, flagsAdd: Flags("zombi_konustu"), forcedNext: 31),
                            speaker: "Ömer (Gözcü)")
                    }),

                Card(29, "Zeynep (Doktor)",
                    "Zeynep gelir. Bu ses Vertak'ın notlarındaki bir şeye benziyor, der. Araştıralım " +
                    "mı, unutalım mı?",
                    Choice("Araştır", authority: -1, counterDeltas: Counter(CounterPharmaArastirma, 1),
                        forcedNext: 32),
                    Choice("Unut", forcedNext: 30)),

                Card(30, "Emine Teyze",
                    "Emine Teyze eski bir anısını anlatır. Dinler misin, vaktin yok mu?",
                    Choice("Dinle", forcedNext: 33),
                    Choice("Vaktim yok", forcedNext: 33)),

                Card(31, "Sabiha (Erzakçı)",
                    "Sabiha harita açar. 3 kişi mi göndereyim, 5 kişi mi?",
                    Choice("3 kişi", forcedNext: 34),
                    Choice("5 kişi", security: -1, flagsAdd: Flags("sefer_ekip_buyuk"), forcedNext: 35)),

                // K32-B's outcome depends on sefer_ekip_buyuk (set at K31); the variant overrides
                // only the right choice, leaving the left choice and body text as authored on both.
                Card(32, "İsmet (Telsizci)",
                    "İsmet telsizden bağırır — sürüyle karşılaşmışlar. Geri mi, riske mi?",
                    Choice("Geri çekil", authority: 1, wealth: 1, forcedNext: 35),
                    Choice("Riske gir", wealth: 1, people: -1, forcedNext: 35),
                    variants: new[]
                    {
                        VariantIfFlag("sefer_ekip_buyuk", null, null,
                            Choice("Riske gir", wealth: 3, authority: -2, forcedNext: 35))
                    }),

                Card(33, "Aziz (Tarımcı)",
                    "Aziz topladığı sebzelerden bir yemek çıkarır. Ye mi, sakla mı?",
                    Choice("Ye", forcedNext: 36),
                    Choice("Sakla", forcedNext: 36)),

                // K34-A sets catlak_onarildi, selecting K37's variant below.
                Card(34, "Kemal (Mühendis)",
                    "Kemal duvara vurur. Temelde çatlak var, der. Şimdi mi, bekle mi?",
                    Choice("Şimdi onar", wealth: -1, flagsAdd: Flags("catlak_onarildi"), forcedNext: 37),
                    Choice("Bekle", forcedNext: 36)),

                Card(35, "Ali & Veli",
                    "Ali ve Veli gitarla \"konser\" verir. Alkışla mı, izle mi?",
                    Choice("Alkışla", forcedNext: 38),
                    Choice("İzle", forcedNext: 38)),

                Card(36, "İsmet (Telsizci)",
                    "İsmet eski bir rapor bulur — Vertak'ın Suş-7 deneyi kontrolden çıkmış. Herkese " +
                    "mi, kadroya mı?",
                    Choice("Herkese açıkla", authority: -2, forcedNext: 39),
                    Choice("Kadroya söyle", forcedNext: 39)),

                Card(37, "Kemal (Mühendis)",
                    "Çatlak büyüdü, su alıyor! Onar mı, boşalt mı?",
                    Choice("Onar", security: -1, wealth: -1, forcedNext: 40),
                    Choice("Boşalt", security: -2, authority: -1, forcedNext: 39),
                    variants: new[]
                    {
                        VariantIfFlag("catlak_onarildi", "Duvar sağlam. Dinlen mi, kontrol mü et?",
                            Choice("Dinlen", authority: 1, forcedNext: 40),
                            Choice("Kontrol et", security: 1, conditionalEffect: AlwaysLeaderHealth(-1),
                                forcedNext: 40))
                    }),

                Card(38, "Rıza / Tarık",
                    "Rıza ve Tarık tartışıyor. Atilla araya girer. Sen mi, o mu?",
                    Choice("Ben hallederim", forcedNext: 41),
                    Choice("Atilla'ya bırak", forcedNext: 40)),

                Card(39, "Necati (Halktan)",
                    "Necati bağırır: \"Lider bizi kandırıyor!\" Açıkla mı, sustur mu?",
                    Choice("Açıkla", authority: 1, forcedNext: 42),
                    Choice("Sustur", conditionalEffect: ReignIfCritical(
                        StatType.Authority, atOrBelow: 3, deltasWhenSafe: new StatDeltas(-1, 0, 0, 0),
                        resetStat: StatType.Authority), forcedNext: 43)),

                // K40-A sets sinyal_cevaplandi, selecting K41's variant below.
                Card(40, "İsmet (Telsizci)",
                    "İsmet tuhaf bir sinyal yakalar. Cevap ver mi, tuzak mı?",
                    Choice("Cevap ver", authority: 1, flagsAdd: Flags("sinyal_cevaplandi"), forcedNext: 43),
                    Choice("Verme", forcedNext: 42)),

                Card(41, "Ömer (Gözcü)",
                    "Sinyal sıklaşıyor. Kapat mı, açık mı bırak?",
                    Choice("Kapat", wealth: -1, forcedNext: 44),
                    Choice("Açık bırak", forcedNext: 44),
                    variants: new[]
                    {
                        VariantIfFlag("sinyal_cevaplandi", "Koordinat istiyorlar. Ver mi, verme mi?",
                            Choice("Ver", flagsAdd: Flags("konum_paylasildi"), forcedNext: 44),
                            Choice("Verme", forcedNext: 44),
                            speaker: "İsmet (Telsizci)")
                    }),

                Card(42, "Gül (Halktan)",
                    "Gül'ün bebeği ilk kez güler. Gülümse mi, işine mi dön?",
                    Choice("Gülümse", forcedNext: 45),
                    Choice("İşe dön", forcedNext: 43)),

                Card(43, "Ömer (Gözcü)",
                    "Ömer gelir — dışarıda bir araç var. Karşıla mı, kilitle mi?",
                    Choice("Karşıla", forcedNext: 46),
                    Choice("Kilitle", forcedNext: 44)),

                // Variant if konum_paylasildi: the arriving group really is Vertak, not an unclear one.
                Card(44, "Anlatıcı",
                    "Kimliği belirsiz küçük bir grup — belki de kimse — belirmiyor net biçimde. " +
                    "Konuş mu, mesafeli mi?",
                    Choice("Konuş", authority: 1, forcedNext: 47),
                    Choice("Mesafeli kal", forcedNext: 45),
                    variants: new[]
                    {
                        VariantIfFlag("konum_paylasildi",
                            "Paylaşılan konuma gelen gerçekten Vertak. Konuş mu, mesafeli mi?",
                            Choice("Konuş", authority: -2, forcedNext: 47),
                            Choice("Mesafeli kal", forcedNext: 45),
                            speaker: "Vertak Temsilcisi")
                    }),

                // K45-A sets ates_ilac_evet, selecting K47's variant below.
                Card(45, "Zeynep (Doktor)",
                    "Zeynep endişeli gelir. Bebek ateşleniyor, der. Son ilacı mı, bekle mi?",
                    Choice("Kullan", wealth: -1, flagsAdd: Flags("ates_ilac_evet"), forcedNext: 48),
                    Choice("Bekle", forcedNext: 46)),

                Card(46, "Sibel (Halktan)",
                    "Sibel sessizce ayakkabıları onarıyor. Teşekkür et mi, sessiz mi kal?",
                    Choice("Teşekkür et", forcedNext: 49),
                    Choice("Sessiz kal", forcedNext: 47)),

                Card(47, "Zeynep (Doktor)",
                    "Ateş yükseldi, şimdi vermek zorunda.",
                    Choice("Anla", wealth: -1, people: -1, forcedNext: 48),
                    Choice("Bekle ve izle", wealth: -1, people: -1, forcedNext: 48),
                    variants: new[]
                    {
                        VariantIfFlag("ates_ilac_evet", "Ateş düştü.",
                            Choice("Rahatla", authority: 1, forcedNext: 50),
                            Choice("Devam et", authority: 1, forcedNext: 50))
                    }),

                Card(48, "Mustafa (Asker)",
                    "Mustafa gelir. Birkaç enfekteli çok yaklaştı, der. Sen mi liderlik et, o mu " +
                    "alsın komutayı?",
                    Choice("Ben ederim", conditionalEffect: LeaderRisk(
                        leaderHealthDeltaWhenFalse: -2, deltasWhenFalse: new StatDeltas(0, 0, 1, 0)),
                        forcedNext: 51),
                    Choice("Mustafa alsın", security: 1, authority: -1, forcedNext: 49)),

                Card(49, "Cem & Yusuf",
                    "Cem ve Yusuf zar oynuyor. Katıl mı, gülümse mi?",
                    Choice("Katıl", forcedNext: 52),
                    Choice("Gülümse", forcedNext: 50)),

                Card(50, "Kemal (Mühendis)",
                    "Kemal ciddi gelir. Kapının menteşeleri güvenilir değil, der. Tüm kaynağı mı, " +
                    "idare mi et?",
                    Choice("Tüm kaynak ver", wealth: -2, security: 2, forcedNext: 53),
                    Choice("İdare et", conditionalEffect: ReignIfCritical(
                        StatType.Security, atOrBelow: 3, deltasWhenSafe: new StatDeltas(0, 0, -1, 0),
                        resetStat: StatType.Security), forcedNext: 51)),

                Card(51, "İsmet (Telsizci)",
                    "İsmet gelir. Konuş mu, reddet mi?",
                    Choice("Konuş", forcedNext: 54),
                    Choice("Reddet", forcedNext: 52)),

                Card(52, "Tarık (Halktan)",
                    "Tarık liderliğini sorguluyor. Sakin kal mı, sert mi?",
                    Choice("Sakin kal", authority: 1, forcedNext: 55),
                    Choice("Sert karşılık ver", authority: -1, forcedNext: 53)),

                Card(53, "Emine Teyze",
                    "Emine Teyze garip bir tarif dener. Tadına bak mı, reddet mi?",
                    Choice("Tadına bak", forcedNext: 56),
                    Choice("Reddet", forcedNext: 54)),

                // K54-A sets duman_arastir_evet, selecting K59's variant later.
                Card(54, "Sabiha (Erzakçı)",
                    "Sabiha uzakta bir duman görür. Araştır mı, girmeyelim mi?",
                    Choice("Araştır", flagsAdd: Flags("duman_arastir_evet"), forcedNext: 57),
                    Choice("Girmeyelim", forcedNext: 55)),

                Card(55, "Ali (Halktan)",
                    "Ali'nin doğum günü. Kutla mı, sade mi?",
                    Choice("Kutla", forcedNext: 58),
                    Choice("Sade geç", forcedNext: 56)),

                // K56-A sets yabanci_temas_evet, selecting K57's variant below.
                Card(56, "Ömer (Gözcü)",
                    "Ömer yaklaşan bir grup görür. Temas mı, izle mi?",
                    Choice("Temas", flagsAdd: Flags("yabanci_temas_evet"), forcedNext: 59),
                    Choice("İzle", forcedNext: 57)),

                Card(57, "Ömer (Gözcü)",
                    "Yakında konaklıyorlar. Nöbet artır mı, sessiz mi?",
                    Choice("Artır", forcedNext: 60),
                    Choice("Sessiz kal", randomOutcome: new RandomStatOutcome(
                        new StatDeltas(0, 0, -1, 0), new StatDeltas(0, 0, 0, 0)), forcedNext: 58),
                    variants: new[]
                    {
                        VariantIfFlag("yabanci_temas_evet", "Ticaret öneriyorlar. Ticaret mi, ret mi?",
                            Choice("Ticaret", wealth: 1, authority: 1, forcedNext: 60),
                            Choice("Ret", authority: -1, forcedNext: 58))
                    }),

                Card(58, "Fatma (Halktan)",
                    "Fatma duvara gökkuşağı çiziyor. İzle mi, geç mi?",
                    Choice("İzle", forcedNext: 61),
                    Choice("Geç", forcedNext: 59)),

                Card(59, "Anlatıcı",
                    "İyi ki gitmediniz — orası tuzakmış.",
                    Choice("Rahatla", authority: 1, forcedNext: 62),
                    Choice("Devam et", authority: 1, forcedNext: 62),
                    variants: new[]
                    {
                        VariantIfFlag("duman_arastir_evet",
                            "Küçük bir grup bulunur, katılmak istiyor. Al mı, ret mi?",
                            Choice("Al", wealth: -1, authority: 1, forcedNext: 62),
                            Choice("Ret", authority: -1, forcedNext: 60),
                            speaker: "Sabiha (Erzakçı)")
                    }),

                Card(60, "Zeynep (Doktor)",
                    "Zeynep suyun kirli olabileceğini söylüyor. Test et mi, hemen iç mi?",
                    Choice("Test et", wealth: -1, forcedNext: 63),
                    Choice("Hemen iç", conditionalEffect: ReignIfCritical(
                        StatType.People, atOrBelow: 3, deltasWhenSafe: default,
                        resetStat: StatType.People), forcedNext: 61)),
            };
        }
    }
}
