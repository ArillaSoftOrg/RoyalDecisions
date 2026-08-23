using System.Collections.Generic;
using RoyalDecisions.Data;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Bölüm III — K61 to K100. See <see cref="StoryContentLibrary"/> for shared conventions.
    /// </summary>
    /// <remarks>
    /// K67, K81 and K93 each carry a specification condition ("(proje=baslatildi ise)",
    /// "(pharma_arastirma≥2 ise özel metin)", "(pharma_arastirma≥3 ise özel yol)") whose alternate
    /// content the specification never actually writes out. K67's condition is additionally
    /// unreachable in its true state given how K65/K66 route into it. All three are authored as a
    /// single, unconditional card rather than an invented variant — see the project report's "Story
    /// Spec Ambiguities" section.
    /// </remarks>
    public static partial class StoryContentLibrary
    {
        internal static List<CardDefinition> CreateChapter3Cards()
        {
            return new List<CardDefinition>(40)
            {
                Card(61, "Necati (Halktan)",
                    "Necati kumarda kaybediyor, herkes gülüyor. Gül mü, ciddi mi kal?",
                    Choice("Gül", forcedNext: 64),
                    Choice("Ciddi kal", forcedNext: 62)),

                Card(62, "Mete (Asker)",
                    "Mete, kritik malzeme için tehlikeli bir keşif gerektiğini söylüyor. Bizzat mı, " +
                    "gönder mi?",
                    Choice("Bizzat git", conditionalEffect: LeaderRisk(leaderHealthDeltaWhenFalse: -2),
                        forcedNext: 65),
                    Choice("Gönder", wealth: -1, forcedNext: 63)),

                Card(63, "İsmet (Telsizci)",
                    "İsmet eski bir Vertak dosyası buluyor. Paylaş mı, sakla mı?",
                    Choice("Paylaş", authority: -1, counterDeltas: Counter(CounterPharmaArastirma, 1),
                        forcedNext: 66),
                    Choice("Sakla", counterDeltas: Counter(CounterPharmaArastirma, 1), forcedNext: 64)),

                Card(64, "Yusuf (Halktan)",
                    "Yusuf derede balık tutmaya çalışıyor. Yardım et mi, izle mi?",
                    Choice("Yardım et", forcedNext: 67),
                    Choice("İzle", forcedNext: 65)),

                Card(65, "Kemal (Mühendis)",
                    "Kemal büyük bir güneş paneli projesi öneriyor. Başla mı, ertele mi?",
                    Choice("Başla", forcedNext: 68),
                    Choice("Ertele", forcedNext: 66)),

                Card(66, "Kemal (Mühendis)",
                    "Malzeme eksik. Başka sığınaktan mı, kendi kaynağımızla mı?",
                    Choice("Başka sığınaktan iste", wealth: -1, flagsAdd: Flags("ittifak_baslangic"),
                        forcedNext: 69),
                    Choice("Kendi kaynağımız", security: -1, forcedNext: 67)),

                // Only ever reached via K66-B, where the project was postponed — see class remarks.
                Card(67, "Kemal (Mühendis)",
                    "Elde kalan malzemeyle panel nihayet tamamlanır.",
                    Choice("Sevin", security: 2, wealth: -1, forcedNext: 70),
                    Choice("Devam et", security: 2, wealth: -1, forcedNext: 70)),

                Card(68, "Sibel (Halktan)",
                    "Sibel'in eskiden piyanist olduğu ortaya çıkıyor. İste mi, bırak mı?",
                    Choice("İste", forcedNext: 71),
                    Choice("Bırak", forcedNext: 69)),

                Card(69, "Anlatıcı",
                    "Komşu bir sığınaktan ittifak teklifi geliyor. Kabul mü, ret mi?",
                    Choice("Kabul", flagsAdd: Flags("ittifak_kabul"), forcedNext: 72),
                    Choice("Ret", forcedNext: 70)),

                Card(70, "Tarık / Rıza",
                    "Tarık ve Rıza beklenmedik şekilde barışıyor. Kutla mı, fark etme mi?",
                    Choice("Kutla", forcedNext: 73),
                    Choice("Fark etme", forcedNext: 71)),

                Card(71, "Sabiha (Erzakçı)",
                    "Sabiha, riskli bir toptan takas fırsatı buluyor. Güvenli mi, büyük riskli mi?",
                    Choice("Güvenli takas", wealth: -1, people: 1, forcedNext: 74),
                    Choice("Büyük riskli takas", conditionalEffect: ReignIfCritical(
                        StatType.Wealth, atOrBelow: 3, deltasWhenSafe: new StatDeltas(0, 0, 0, 3),
                        resetStat: StatType.Wealth), forcedNext: 72)),

                Card(72, "Gül (Halktan)",
                    "Gül bebeğine isim koyuyor. Katıl mı, kısa tebrik mi?",
                    Choice("Katıl", forcedNext: 75),
                    Choice("Kısa tebrik", forcedNext: 73)),

                Card(73, "Ömer (Gözcü)",
                    "Ömer çitte yine bir ses duyar: \"Biz de... insandık.\" Dinle mi, uzaklaş mı?",
                    Choice("Dinle", authority: -1, flagsAdd: Flags("zombi_ikinci_temas"), forcedNext: 76),
                    Choice("Uzaklaş", forcedNext: 74)),

                Card(74, "Aziz (Tarımcı)",
                    "Aziz'in tohum defteri kayboluyor. Yardım et mi, boşver mi?",
                    Choice("Yardım et", forcedNext: 77),
                    Choice("Boşver", forcedNext: 75)),

                Card(75, "Anlatıcı",
                    "İyi ki reddettik.",
                    Choice("Rahatla", authority: 1, forcedNext: 77),
                    Choice("Devam et", authority: 1, forcedNext: 77),
                    variants: new[]
                    {
                        VariantIfFlag("ittifak_kabul", "İttifak sizi sömürmek istiyormuş. Karşı çık " +
                            "mı, boyun eğ mi?",
                            Choice("Karşı çık", authority: -1, forcedNext: 78),
                            Choice("Boyun eğ", wealth: -2, authority: -1, forcedNext: 76))
                    }),

                Card(76, "Ömer (Gözcü)",
                    "Ömer bir suikast girişimi fark ediyor. Soruştur mu, görmezden mi?",
                    Choice("Soruştur, şüpheliyle yüzleş", conditionalEffect: LeaderRisk(
                        leaderHealthDeltaWhenFalse: -1, deltasWhenFalse: new StatDeltas(-1, 0, 0, 0)),
                        forcedNext: 79),
                    Choice("Görmezden gel", forcedNext: 77)),

                Card(77, "Ali (Halktan)",
                    "Ali ilk kez nöbete katılmak istiyor. İzin ver mi, erken mi bul?",
                    Choice("İzin ver", forcedNext: 80),
                    Choice("Erken bul", forcedNext: 78)),

                Card(78, "Kemal (Mühendis)",
                    "Kemal, ısınma sorunu için iki çözüm sunuyor. Odun mu, elektrik mi?",
                    Choice("Odun", wealth: -1, forcedNext: 81),
                    Choice("Elektrik", security: -1, forcedNext: 79)),

                Card(79, "Anlatıcı",
                    "Kışın ilk haftası geçiyor.",
                    Choice("Devam et", randomOutcome: new RandomStatOutcome(
                        new StatDeltas(0, 1, 1, 0), new StatDeltas(0, -1, -1, 0)), forcedNext: 80),
                    Choice("Dayan", randomOutcome: new RandomStatOutcome(
                        new StatDeltas(0, 1, 1, 0), new StatDeltas(0, -1, -1, 0)), forcedNext: 80)),

                Card(80, "Semra (Halktan)",
                    "Semra'nın konseri artık gelenek oldu. Katıl mı, kaçır mı?",
                    Choice("Katıl", forcedNext: 84),
                    Choice("Kaçır", forcedNext: 81)),

                Card(81, "İsmet (Telsizci)",
                    "İsmet, Vertak'ın asıl planını çözüyor: sığınakları toplamak. Yay mı, sessiz mi?",
                    Choice("Yay", authority: -1, forcedNext: 83),
                    Choice("Sessiz kal", forcedNext: 82)),

                Card(82, "Anlatıcı",
                    "\"Vertak'a katılalım\" tartışması büyüyor. İzin ver mi, zorla tut mu?",
                    Choice("İzin ver", authority: 1, forcedNext: 84),
                    Choice("Zorla tut", conditionalEffect: ReignIfCritical(
                        StatType.Authority, atOrBelow: 3, deltasWhenSafe: new StatDeltas(-1, 0, 0, 0),
                        resetStat: StatType.Authority), forcedNext: 83)),

                Card(83, "Emine Teyze",
                    "Emine Teyze'nin son güzel günü — doğal seyrinde. Otur mu, yalnız mı bırak?",
                    Choice("Otur", forcedNext: 87),
                    Choice("Yalnız bırak", forcedNext: 84)),

                Card(84, "Zeynep (Doktor)",
                    "Zeynep bitkin görünüyor. Dinlenmesini emret mi, kendi bilsin mi?",
                    Choice("Emret", flagsAdd: Flags("zeynep_zorla_dinlendirildi"), forcedNext: 86),
                    Choice("Kendi bilsin", forcedNext: 85)),

                Card(85, "Cem & Yusuf",
                    "Cem ve Yusuf yeni bir oyun icat ediyor. Katıl mı, izle mi?",
                    Choice("Katıl", forcedNext: 87),
                    Choice("İzle", forcedNext: 86)),

                Card(86, "Anlatıcı",
                    "Sınırdan bir grup mülteci geliyor. Kabul mü, ret mi?",
                    Choice("Kabul", flagsAdd: Flags("multeci_kabul"), forcedNext: 89),
                    Choice("Ret", forcedNext: 87)),

                Card(87, "Anlatıcı",
                    "Grup çevrede kalmış. Dağıt mı, görmezden mi?",
                    Choice("Dağıt", authority: -1, forcedNext: 90),
                    Choice("Görmezden gel", randomOutcome: new RandomStatOutcome(
                        new StatDeltas(0, 0, -1, 0), new StatDeltas(0, 0, 0, 0)), forcedNext: 88),
                    variants: new[]
                    {
                        VariantIfFlag("multeci_kabul", "Biri hasta. Karantina mı, risk mi?",
                            Choice("Karantina", people: 1, authority: -1, forcedNext: 90),
                            Choice("Risk al", people: -1, forcedNext: 88))
                    }),

                Card(88, "Anlatıcı",
                    "Zeynep hastalanır.",
                    Choice("Endişelen", people: -2, forcedNext: 89),
                    Choice("Bekle", people: -2, forcedNext: 89),
                    variants: new[]
                    {
                        VariantIfFlag("zeynep_zorla_dinlendirildi", "Zeynep toparlanmış döner.",
                            Choice("Rahatla", authority: 1, forcedNext: 90),
                            Choice("Devam et", authority: 1, forcedNext: 90))
                    }),

                Card(89, "Anlatıcı",
                    "Sığınağın yıl dönümü kutlanıyor. Kutla mı, sade mi?",
                    Choice("Kutla", forcedNext: 91),
                    Choice("Sade geç", forcedNext: 90)),

                Card(90, "Mustafa (Asker)",
                    "Mustafa, büyük bir sürü saldırısı geldiğini haber veriyor. Cepheye çık mı, ona " +
                    "mı bırak?",
                    Choice("Cepheye çık", conditionalEffect: LeaderRisk(
                        leaderHealthDeltaWhenFalse: -3, deltasWhenFalse: new StatDeltas(0, 0, 2, 0)),
                        forcedNext: 93),
                    Choice("Mustafa'ya bırak", security: 1, authority: -1, forcedNext: 91)),

                Card(91, "Ali (Halktan)",
                    "Ali büyümüş, ilk vasıflı görevini istiyor. Şans ver mi, bekle mi?",
                    Choice("Şans ver", forcedNext: 94),
                    Choice("Bekle", forcedNext: 92)),

                Card(92, "Kemal (Mühendis)",
                    "Kemal, sığınağın taşınması gerekebileceğini söylüyor. Taşın mı, kal mı?",
                    Choice("Taşın", wealth: -2, security: 1, forcedNext: 94),
                    Choice("Kal", conditionalEffect: ReignIfCritical(
                        StatType.Security, atOrBelow: 3, deltasWhenSafe: new StatDeltas(0, 0, -1, 0),
                        resetStat: StatType.Security), forcedNext: 93)),

                Card(93, "İsmet (Telsizci)",
                    "İsmet, Vertak'ın içeriden bölündüğünü öğreniyor. Temas mı, güvenme mi?",
                    Choice("Temas", authority: 1, forcedNext: 95),
                    Choice("Güvenme", forcedNext: 94)),

                Card(94, "İsmet (Telsizci)",
                    "İsmet eski bir kaset buluyor, hep beraber dinliyorlar. Dinle mi, kaçır mı?",
                    Choice("Dinle", forcedNext: 97),
                    Choice("Kaçır", forcedNext: 95)),

                Card(95, "Ömer (Gözcü)",
                    "Ömer, konuşan zombilerle resmi bir temas fırsatı doğduğunu bildiriyor. Ateşkes " +
                    "mi, saldır mı?",
                    Choice("Ateşkes dene", flagsAdd: Flags("ateskes_evet"), forcedNext: 97),
                    Choice("Saldır", forcedNext: 96)),

                Card(96, "Anlatıcı",
                    "Çatışma büyür.",
                    Choice("Toparlan", authority: -2, people: -1, forcedNext: 97),
                    Choice("Devam et", authority: -2, people: -1, forcedNext: 97),
                    variants: new[]
                    {
                        VariantIfFlag("ateskes_evet", "Uzun vadeli bir barış kurulur.",
                            Choice("Sevin", authority: 2, people: 1, forcedNext: 100),
                            Choice("Temkinli sevin", authority: 2, people: 1, forcedNext: 100))
                    }),

                Card(97, "Anlatıcı",
                    "Sessiz bir akşam, herkes hayatta kalmanın farkında. Yansıt mı, uyu mu?",
                    Choice("Yansıt", forcedNext: 100),
                    Choice("Uyu", forcedNext: 98)),

                Card(98, "Gül (Halktan)",
                    "Gül'ün çocuğu ilk adımlarını atıyor. Kutla mı, meşgul mü?",
                    Choice("Kutla", forcedNext: 102),
                    Choice("Meşgul ol", forcedNext: 99)),

                Card(99, "Fatma (Halktan)",
                    "Fatma yeni çocuklara resim dersi veriyor. Katıl mı, izle mi?",
                    Choice("Katıl", forcedNext: 101),
                    Choice("İzle", forcedNext: 100)),

                Card(100, "Anlatıcı",
                    "Sığınağın kaderi o ana kadarki tüm bayrakların toplamına bağlı. Bu bir final " +
                    "değil — hikaye devam ediyor.",
                    Choice("Devam et", forcedNext: 101),
                    Choice("İlerle", forcedNext: 101)),
            };
        }
    }
}
