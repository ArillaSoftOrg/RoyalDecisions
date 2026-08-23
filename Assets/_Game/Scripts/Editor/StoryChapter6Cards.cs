using System.Collections.Generic;
using RoyalDecisions.Data;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Bölüm VI — K201 to K250, the end of the authored specification. See
    /// <see cref="StoryContentLibrary"/> for shared conventions.
    /// </summary>
    /// <remarks>
    /// K228 and K234 each gesture at a "branch on the sum of every flag so far" without specifying
    /// what that sum means or where its thresholds fall; both are authored with their one concretely
    /// described branch and no invented aggregate logic — see the project report's "Story Spec
    /// Ambiguities" section. K250 is the specification's actual final card: unlike every other card
    /// in the story, neither of its choices sets a forced-next card, since there is nothing left to
    /// force. Reaching it and confirming either side is therefore this vertical slice's genuine,
    /// intentional end — see <see cref="CardDefinition.ForcedChainOnly"/> for why that reliably
    /// produces "no eligible card" rather than normal selection picking up an unrelated branch.
    /// </remarks>
    public static partial class StoryContentLibrary
    {
        internal static List<CardDefinition> CreateChapter6Cards()
        {
            return new List<CardDefinition>(50)
            {
                Card(201, "Anlatıcı",
                    "Yeni bir mevsim başlıyor. Kutla mı, sıradan mı geç?",
                    Choice("Kutla", forcedNext: 203),
                    Choice("Sıradan geç", forcedNext: 202)),

                Card(202, "Anlatıcı",
                    "Gereksiz bir kaygıydı.",
                    Choice("Rahatla", authority: 1, forcedNext: 205),
                    Choice("Devam et", authority: 1, forcedNext: 205),
                    variants: new[]
                    {
                        VariantIfFlag("son_kusku_evet",
                            "Şüphe doğrulanır ya da yanlış çıkar — belirsizlik sürüyor.",
                            Choice("Kabullen", randomOutcome: new RandomStatOutcome(
                                new StatDeltas(-1, 0, 1, 0), new StatDeltas(-1, 0, 0, 0)), forcedNext: 205),
                            Choice("Devam et", randomOutcome: new RandomStatOutcome(
                                new StatDeltas(-1, 0, 1, 0), new StatDeltas(-1, 0, 0, 0)), forcedNext: 205))
                    }),

                Card(203, "Veli (Halktan)",
                    "Veli kendi yolunu buluyor — mühendislik mi, telsizcilik mi. Destekle mi, kendi " +
                    "haline mi bırak?",
                    Choice("Destekle", forcedNext: 205),
                    Choice("Kendi haline bırak", forcedNext: 204)),

                Card(204, "Mustafa / Mete",
                    "Mustafa ve Mete ufukta hareketlilik fark ediyor. Erken uyarı mı, gözlem mi?",
                    Choice("Erken uyarı kur", security: -1, flagsAdd: Flags("erken_uyari_evet"),
                        forcedNext: 206),
                    Choice("Gözlemeye devam", forcedNext: 205)),

                Card(205, "Anlatıcı",
                    "Karakol'da iç karışıklık çıktığı haberi geliyor. Değerlendir mi, karışma mı?",
                    Choice("Değerlendir", flagsAdd: Flags("karakol_yeni_yonetim"), forcedNext: 207),
                    Choice("Karışma", forcedNext: 206)),

                Card(206, "Anlatıcı",
                    "Karışılmadığı için belirsizlik sürüyor, ama en azından güvenlik hazırlığı " +
                    "yapıldı.",
                    Choice("Hazırlan", randomOutcome: new RandomStatOutcome(
                        new StatDeltas(0, 0, 1, 0), new StatDeltas(-1, 0, -1, 0)), forcedNext: 208),
                    Choice("Bekle", randomOutcome: new RandomStatOutcome(
                        new StatDeltas(0, 0, 1, 0), new StatDeltas(-1, 0, -1, 0)), forcedNext: 208)),

                Card(207, "Ali (Halktan)",
                    "Ali'nin çırağı ilk bağımsız görevini tamamlıyor. Gurur duy mu, doğal mı karşıla?",
                    Choice("Gurur duy", forcedNext: 210),
                    Choice("Doğal karşıla", forcedNext: 208)),

                Card(208, "Kemal (Mühendis)",
                    "Genişleyen sığınağın yapısal karmaşıklığı bir soruna yol açıyor. Acil mi, göze " +
                    "al mı?",
                    Choice("Acil müdahale", wealth: -2, security: 1, forcedNext: 211),
                    Choice("Göze al", conditionalEffect: ReignIfCritical(
                        StatType.Security, atOrBelow: 3, deltasWhenSafe: new StatDeltas(0, 0, -1, 0),
                        resetStat: StatType.Security), forcedNext: 209)),

                Card(209, "Anlatıcı",
                    "Erken uyarı kurulmadığı için sürü fark edilmeden yaklaştı.",
                    Choice("Toparlan", people: -1, authority: -1, forcedNext: 211),
                    Choice("Devam et", people: -1, authority: -1, forcedNext: 211),
                    variants: new[]
                    {
                        VariantIfFlag("erken_uyari_evet", "Erken uyarı sayesinde herkes hazırdı.",
                            Choice("Rahatla", authority: 1, forcedNext: 211),
                            Choice("Devam et", authority: 1, forcedNext: 211))
                    }),

                Card(210, "Anlatıcı",
                    "Kriz sonrası dayanışma güçleniyor. Kutla mı, sessizce hisset mi?",
                    Choice("Kutla", forcedNext: 212),
                    Choice("Sessizce hisset", forcedNext: 211)),

                Card(211, "Anlatıcı",
                    "Sıradan bir nöbet günü.",
                    Choice("Devam et", forcedNext: 212),
                    Choice("İlerle", forcedNext: 212),
                    variants: new[]
                    {
                        VariantIfFlag("zombi_komsuluk",
                            "\"Lider\" zombi karmaşık bir şey ifade etmeye çalışıyor. Zeynep'le mi, " +
                            "tek mi?",
                            Choice("Zeynep'le dinle", forcedNext: 213),
                            Choice("Tek başına dinle", forcedNext: 212),
                            speaker: "Ömer (Gözcü)")
                    }),

                Card(212, "İsmet (Telsizci)",
                    "İsmet'in arşivi sığınağın gururu. Katkı ver mi, izle mi?",
                    Choice("Katkı ver", forcedNext: 215),
                    Choice("İzle", forcedNext: 213)),

                Card(213, "Ali / Veli",
                    "Ali ya da Veli ilk kez resmi bir karar toplantısına katılıyor. Söz hakkı ver mi, " +
                    "izle mi?",
                    Choice("Söz hakkı ver", authority: 1, forcedNext: 215),
                    Choice("İzle", forcedNext: 214)),

                Card(214, "Sabiha (Erzakçı)",
                    "Sabiha'nın ticaret ağı birden fazla topluluğu kapsıyor. Genişlet mi, sınırlı mı?",
                    Choice("Genişlet", wealth: 1, flagsAdd: Flags("ticaret_agi_genis"), forcedNext: 216),
                    Choice("Sınırlı tut", forcedNext: 215)),

                Card(215, "Karakol Krizi",
                    "Karakol krizi doğrudan sığınağa sıçrıyor. Bizzat mı, ekibe mi bırak?",
                    Choice("Bizzat git", conditionalEffect: LeaderRisk(leaderHealthDeltaWhenFalse: -3),
                        forcedNext: 218),
                    Choice("Ekibe bırak", forcedNext: 216)),

                Card(216, "Anlatıcı",
                    "Sakinlik geri geliyor. Dinlen mi, işe mi dön?",
                    Choice("Dinlen", forcedNext: 218),
                    Choice("İşe dön", forcedNext: 217)),

                Card(217, "Aziz (Tarımcı)",
                    "Tarım alanı genişletildiyse rekor bir hasat mümkün. Riske gir mi, güvenli mi?",
                    Choice("Riske gir", flagsAdd: Flags("hasat_riskli"), forcedNext: 219),
                    Choice("Güvenli ilerle", forcedNext: 218)),

                Card(218, "Anlatıcı",
                    "İstikrarlı bir hasat toplanır.",
                    Choice("Sevin", wealth: 2, forcedNext: 221),
                    Choice("Devam et", wealth: 2, forcedNext: 221),
                    variants: new[]
                    {
                        VariantIfFlag("hasat_riskli", "Riskli hasat sonuçlanıyor.",
                            Choice("Sevin", randomOutcome: new RandomStatOutcome(
                                new StatDeltas(0, 0, 0, 4), new StatDeltas(0, 0, 0, 1)), forcedNext: 221),
                            Choice("Devam et", randomOutcome: new RandomStatOutcome(
                                new StatDeltas(0, 0, 0, 4), new StatDeltas(0, 0, 0, 1)), forcedNext: 221))
                    }),

                Card(219, "Anlatıcı",
                    "Sığınakta ilk kez fazla erzak \"ihraç\" ediliyor. Kutla mı, tedbirli mi?",
                    Choice("Kutla", forcedNext: 223),
                    Choice("Tedbirli ol", forcedNext: 220)),

                Card(220, "İsmet (Telsizci)",
                    "Vertak hikâyesi netleşiyor. Kutla/rahatla mı, temkinli mi?",
                    Choice("Kutla/rahatla", authority: 2, forcedNext: 223),
                    Choice("Temkinli kal", security: 1, forcedNext: 221)),

                Card(221, "Anlatıcı",
                    "Sığınağın en yaşlısı geçmişi genç nesile anlatıyor. Dinle mi, işine mi dön?",
                    Choice("Dinle", forcedNext: 224),
                    Choice("İşe dön", forcedNext: 222)),

                Card(222, "Anlatıcı",
                    "Yeni nesille eski nesil arasında değerler çatışması. Ortak karar mı, otorite mi?",
                    Choice("Ortak karar ara", authority: 1, forcedNext: 226),
                    Choice("Otorite kullan", conditionalEffect: ReignIfCritical(
                        StatType.Authority, atOrBelow: 3, deltasWhenSafe: new StatDeltas(-1, 0, 0, 0),
                        resetStat: StatType.Authority), forcedNext: 223)),

                Card(223, "Anlatıcı",
                    "Uzlaşma ya da gerginlik sonrası sakin bir hafta. Vakit geçir mi, yalnız mı?",
                    Choice("Vakit geçir", forcedNext: 226),
                    Choice("Yalnız kal", forcedNext: 224)),

                Card(224, "Anlatıcı",
                    "Sığınak artık kurulduğu günden çok farklı bir yer. Devam ediyor.",
                    Choice("Devam et", forcedNext: 225),
                    Choice("İlerle", forcedNext: 225)),

                Card(225, "Anlatıcı",
                    "Sığınakta bir \"gelenek günü\" var, ilk günden beri hayatta kalanlar anılıyor. " +
                    "Katıl mı, izle mi?",
                    Choice("Katıl", forcedNext: 229),
                    Choice("İzle", forcedNext: 226)),

                Card(226, "Anlatıcı",
                    "Kemal'in mühendislik mirası artık kalıcı bir yapı taşı. Değerlendir mi, sıradan " +
                    "mı?",
                    Choice("Değerlendir", forcedNext: 230),
                    Choice("Sıradan gör", forcedNext: 227)),

                Card(227, "Anlatıcı",
                    "\"Lider\" zombi son kez net bir şekilde konuşuyor — uyarı mı, veda mı, teklif " +
                    "mi. Dikkatle dinle mi, mesafede mi kal?",
                    Choice("Dikkatle dinle", flagsAdd: Flags("zombi_finali_dinlendi"), forcedNext: 230),
                    Choice("Mesafede kal", forcedNext: 228)),

                Card(228, "Anlatıcı",
                    "Belirsizlik sürer.",
                    Choice("Kabullen", forcedNext: 230),
                    Choice("Devam et", forcedNext: 230),
                    variants: new[]
                    {
                        VariantIfFlag("zombi_finali_dinlendi", "Önemli bir bilgi ve uyarı alınır.",
                            Choice("Not al", flagsAdd: Flags("zombi_son_mesaj"), forcedNext: 230),
                            Choice("Paylaş", flagsAdd: Flags("zombi_son_mesaj"), forcedNext: 230))
                    }),

                Card(229, "Anlatıcı",
                    "Sakin bir akşam, sığınağın artık bir \"ev\" olduğu hissediliyor. Yansıt mı, " +
                    "sessizce yaşa mı?",
                    Choice("Yansıt", forcedNext: 231),
                    Choice("Sessizce yaşa", forcedNext: 230)),

                Card(230, "Mustafa / Mete",
                    "Yıllardır biriken tüm gerilimler bir araya gelip en büyük krizi yaratıyor. Öne " +
                    "çık mı, kadroya güven mi?",
                    Choice("Öne çık", conditionalEffect: LeaderRisk(leaderHealthDeltaWhenFalse: -4),
                        forcedNext: 234),
                    Choice("Kadroya güven", authority: 1, forcedNext: 231)),

                Card(231, "Anlatıcı",
                    "Fırtına dinmiş, sığınak bir kez daha ayakta. Nefes al mı, işe mi dön?",
                    Choice("Nefes al", forcedNext: 234),
                    Choice("İşe dön", forcedNext: 232)),

                Card(232, "Anlatıcı",
                    "Sabiha, Aziz, Kemal, İsmet — hepsinin mirası sığınağın kimliğini oluşturuyor. " +
                    "Fark et mi, sıradan mı?",
                    Choice("Fark et", forcedNext: 236),
                    Choice("Sıradan gün", forcedNext: 233)),

                Card(233, "Anlatıcı",
                    "Ali, Veli ve yeni nesil geleceği kendi elleriyle şekillendiriyor. Güven mi, " +
                    "temkinli mi?",
                    Choice("Güven", forcedNext: 235),
                    Choice("Temkinli ol", forcedNext: 234)),

                Card(234, "Anlatıcı",
                    "Tüm ilişkiler (Vertak, Karakol, zombiler) bir arada değerlendiriliyor — sığınak " +
                    "bölgede kendi başına bir güç mü, hâlâ kırılgan mı?",
                    Choice("Değerlendir", forcedNext: 238),
                    Choice("Sıradan gör", forcedNext: 238)),

                Card(235, "İsmet (Telsizci)",
                    "İsmet'in arşivinde kaçıncı lider olduğunuz, kaç gündür ayakta olduğunuz yazılı. " +
                    "Oku mu, ona mı bırak?",
                    Choice("Oku", forcedNext: 237),
                    Choice("Ona bırak", forcedNext: 236)),

                Card(236, "Anlatıcı",
                    "Büyük bir toplantı yapılıyor, artık gerçek bir \"topluluk\" gibi karar veriliyor. " +
                    "Söz al mı, dinle mi?",
                    Choice("Söz al", forcedNext: 238),
                    Choice("Dinle", forcedNext: 237)),

                Card(237, "Anlatıcı",
                    "Zeynep'in eğittiği halef artık kendi başına yeterli. Gurur duy mu, doğal mı?",
                    Choice("Gurur duy", forcedNext: 239),
                    Choice("Doğal karşıla", forcedNext: 238)),

                Card(238, "Anlatıcı",
                    "Ömer'in güvenliği, Mustafa ve Mete'nin savunması — kalıcı yapı taşları. " +
                    "Değerlendir mi, sıradan mı?",
                    Choice("Değerlendir", forcedNext: 242),
                    Choice("Sıradan gör", forcedNext: 239)),

                Card(239, "Anlatıcı",
                    "Son bir sakin akşam, herkes bir arada. Teşekkür et mi, sessizce mi otur?",
                    Choice("Teşekkür et", forcedNext: 242),
                    Choice("Sessizce otur", forcedNext: 240)),

                Card(240, "Anlatıcı",
                    "Yıllardır süren yolculuk, ilk günün korkusundan çok uzakta bir yere gelmiş. " +
                    "Geriye mi, ileriye mi?",
                    Choice("Geriye bak", forcedNext: 242),
                    Choice("İleriye bak", forcedNext: 241)),

                Card(241, "Anlatıcı",
                    "Sığınağın halk arasında oluşmuş bir ismi bile var. Resmi mi yap, doğal mı bırak?",
                    Choice("Resmi yap", forcedNext: 244),
                    Choice("Doğal bırak", forcedNext: 242)),

                Card(242, "Emine Teyze",
                    "Emine Teyze'nin bahçesi hâlâ çiçek açıyor, ilk günden beri süren bir sembol. " +
                    "İzle mi, geç mi?",
                    Choice("İzle", forcedNext: 245),
                    Choice("Geç", forcedNext: 243)),

                Card(243, "Gül (Halktan)",
                    "Gül'ün çocuğu artık okula benzer bir derse katılıyor, Atilla'nın mirası sürüyor. " +
                    "Katıl mı, izle mi?",
                    Choice("Katıl", forcedNext: 246),
                    Choice("İzle", forcedNext: 244)),

                Card(244, "Aziz (Tarımcı)",
                    "Aziz'in tarım mirası artık sığınağın temel geçim kaynağı. Fark et mi, sıradan mı?",
                    Choice("Fark et", forcedNext: 248),
                    Choice("Sıradan gör", forcedNext: 245)),

                Card(245, "Anlatıcı",
                    "Son kart öncesi, herkes bir arada, sessiz bir gurur var havada. Hisset mi, " +
                    "geleceğe mi odaklan?",
                    Choice("Hisset", forcedNext: 248),
                    Choice("Geleceğe odaklan", forcedNext: 246)),

                Card(246, "Anlatıcı",
                    "Kadronun hepsi (Zeynep, Sabiha, Ömer, Kemal, Atilla, Aziz, İsmet, Mustafa, Mete) " +
                    "bir arada son bir toplantı yapıyor. Katıl mı, dinle mi?",
                    Choice("Katıl", forcedNext: 250),
                    Choice("Dinle", forcedNext: 247)),

                Card(247, "İsmet (Telsizci)",
                    "Sığınağın günlüğüne son bir kayıt düşülüyor. Sen mi yaz, İsmet mi?",
                    Choice("Sen yaz", forcedNext: 250),
                    Choice("İsmet yazsın", forcedNext: 248)),

                Card(248, "Anlatıcı",
                    "Gece çöküyor, sığınak sessizleşiyor — ama huzurlu bir sessizlik bu sefer. Dışarı " +
                    "bak mı, içeri dön mü?",
                    Choice("Dışarı bak", forcedNext: 250),
                    Choice("İçeri dön", forcedNext: 249)),

                Card(249, "Anlatıcı",
                    "Son an — kaç lider geldi geçti, kaç gün geçti, kimin hatırladığı önemli değil " +
                    "artık; sığınak ayakta. Düşün mü, sadece hisset mi?",
                    Choice("Düşün", forcedNext: 250),
                    Choice("Sadece hisset", forcedNext: 250)),

                // The specification's true end: no forced-next on either side. See class remarks.
                Card(250, "Anlatıcı",
                    "Sığınağın kaderi K1'den beri birikmiş 250 kartlık tüm kararların toplamına " +
                    "bağlı: kaç lider geldi geçti, hangi ittifaklar kuruldu ya da yıkıldı, konuşan " +
                    "zombilerle ilişki nasıl şekillendi — hepsi burada birleşiyor. Bu hâlâ bir final " +
                    "değildir; sistem aynı kurallarla sonsuza dek üretilebilir, ama bu sığınağın " +
                    "günlüğü burada kapanıyor.",
                    Choice("Günlüğü kapat"),
                    Choice("Sessizce otur")),
            };
        }
    }
}
