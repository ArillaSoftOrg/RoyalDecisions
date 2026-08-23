using System.Collections.Generic;
using RoyalDecisions.Data;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Bölüm V — K151 to K200. See <see cref="StoryContentLibrary"/> for shared conventions.
    /// </summary>
    /// <remarks>
    /// K156 and K206 each describe more outcome combinations in prose than the graph actually
    /// routes through that card (some of the combinations they describe are reached directly from
    /// the preceding card instead) — both are authored to cover exactly the combinations that do
    /// route through them, with the "which of two unlabelled sub-outcomes" ambiguity resolved by
    /// <see cref="RandomStatOutcome"/> rather than an invented flag. K195's "(zombi_komsuluk=evet
    /// ise)" and K199/K202's "değişken etki" carry the same no-alternate-content and
    /// unspecified-magnitude gaps already documented for earlier chapters. See the project report's
    /// "Story Spec Ambiguities" section.
    /// </remarks>
    public static partial class StoryContentLibrary
    {
        internal static List<CardDefinition> CreateChapter5Cards()
        {
            return new List<CardDefinition>(50)
            {
                Card(151, "Ali (Halktan)",
                    "Sezon 3 sakin bir günle açılıyor. Ali kendi yolunu seçiyor. Tarım mı, savunma mı?",
                    Choice("Tarım", forcedNext: 155),
                    Choice("Savunma", forcedNext: 152)),

                Card(152, "Veli (Halktan)",
                    "Veli, ikizinin seçiminden kıskanıyor. Konuş mu, zaman mı tanı?",
                    Choice("Konuş", forcedNext: 155),
                    Choice("Zaman tanı", forcedNext: 153)),

                Card(153, "Kemal (Mühendis)",
                    "Kemal, \"Karakol\" diye anılan düzenli bir yerleşim olduğunu bildiriyor. Temas " +
                    "mı, uzak mı?",
                    Choice("Temas", flagsAdd: Flags("karakol_temas_evet"), forcedNext: 155),
                    Choice("Uzak dur", forcedNext: 154)),

                Card(154, "Fatma (Halktan)",
                    "Fatma çocuklara resim dersi veriyor. Katıl mı, izle mi?",
                    Choice("Katıl", forcedNext: 157),
                    Choice("İzle", forcedNext: 155)),

                Card(155, "Mete (Asker)",
                    "Mete, devriyeyle karşılaşıyor. Selamla mı, çekil mi?",
                    Choice("Selamla", forcedNext: 158),
                    Choice("Çekil", forcedNext: 156),
                    variants: new[]
                    {
                        VariantIfFlag("karakol_temas_evet",
                            "İsmet radyo bağlantısı kuruyor — otoriter bir yönetim. İşbirliği mi, " +
                            "mesafeli mi?",
                            Choice("İşbirliği öner", randomOutcome: new RandomStatOutcome(
                                new StatDeltas(-1, 0, 0, 2), new StatDeltas(1, 0, 0, 1)), forcedNext: 158),
                            Choice("Mesafeli kal", forcedNext: 156),
                            speaker: "İsmet (Telsizci)")
                    }),

                Card(156, "Anlatıcı",
                    "Karşılaşma sessizce sonlanır.",
                    Choice("Devam et", forcedNext: 158),
                    Choice("Uzaklaş", forcedNext: 158),
                    variants: new[]
                    {
                        VariantIfFlag("karakol_temas_evet",
                            "Mesafeli tavrınız gerginliğe yol açıyor.",
                            Choice("Kabullen", authority: -1, flagsAdd: Flags("karakol_gerginlik"),
                                forcedNext: 158),
                            Choice("Devam et", authority: -1, flagsAdd: Flags("karakol_gerginlik"),
                                forcedNext: 158))
                    }),

                Card(157, "Necati (Halktan)",
                    "Necati, Karakol hakkında bir şeyler duymuş. Dinle mi, boşver mi?",
                    Choice("Dinle", forcedNext: 160),
                    Choice("Boşver", forcedNext: 158)),

                Card(158, "Ömer (Gözcü)",
                    "Ömer, zombilerin \"toplanma\" davranışı sergilediğini fark ediyor. İzle mi, " +
                    "rapor mu?",
                    Choice("Yakından izle", flagsAdd: Flags("zombi_izle_evet"), forcedNext: 160),
                    Choice("Mesafeli rapor", forcedNext: 159)),

                Card(159, "Sibel (Halktan)",
                    "Sibel'in müzik dersleri artık çocuklara da veriliyor. Katıl mı, izle mi?",
                    Choice("Katıl", forcedNext: 163),
                    Choice("İzle", forcedNext: 160)),

                Card(160, "Kemal (Mühendis)",
                    "Kemal, eski onarımların sorun çıkardığını bildiriyor. Büyük tamir mi, ertele mi?",
                    Choice("Büyük tamir", wealth: -2, security: 2, forcedNext: 163),
                    Choice("Ertele", conditionalEffect: ReignIfCritical(
                        StatType.Security, atOrBelow: 3, deltasWhenSafe: new StatDeltas(0, 0, -1, 0),
                        resetStat: StatType.Security), forcedNext: 161)),

                Card(161, "Gül (Halktan)",
                    "Gül'ün çocuğu artık yürüyor. Kutla mı, meşgul mü?",
                    Choice("Kutla", forcedNext: 165),
                    Choice("Meşgul ol", forcedNext: 162)),

                Card(162, "Anlatıcı",
                    "Belirsizlik sürüyor.",
                    Choice("Kabullen", forcedNext: 165),
                    Choice("Devam et", forcedNext: 165),
                    variants: new[]
                    {
                        VariantIfFlag("zombi_izle_evet",
                            "Örgütlendikleri doğrulanıyor. Zeynep'e ilet mi, sakla mı?",
                            Choice("İlet", authority: -1, flagsAdd: Flags("bilimsel_gozlem"), forcedNext: 164),
                            Choice("Sakla", forcedNext: 163),
                            speaker: "Zeynep (Doktor)")
                    }),

                Card(163, "Ali (Halktan)",
                    "Ali'nin ilk büyük görevi geliyor. Bağımsız mı, yanında mı dur?",
                    Choice("Bağımsız bırak", forcedNext: 166),
                    Choice("Yanında dur", forcedNext: 164)),

                Card(164, "Anlatıcı",
                    "Ali beklenmedik bir tehlikeyle karşılaşıyor. Yardım gönder mi, izin ver mi?",
                    Choice("Yardım gönder", wealth: -1, authority: 1, forcedNext: 166),
                    Choice("İzin ver", authority: 1, forcedNext: 165)),

                Card(165, "Yusuf & Cem",
                    "Yusuf ve Cem'in oyunu gençler arasında yayılıyor. Katıl mı, izle mi?",
                    Choice("Katıl", forcedNext: 168),
                    Choice("İzle", forcedNext: 166)),

                Card(166, "Sabiha (Erzakçı)",
                    "Sabiha yeni bir ticaret rotası öneriyor. Riskli mi, güvenli mi?",
                    Choice("Riskli", flagsAdd: Flags("rota_riskli"), forcedNext: 168),
                    Choice("Güvenli", forcedNext: 167),
                    variants: new[]
                    {
                        VariantIfFlag("karakol_gerginlik",
                            "Kemal, Karakol'un sınırı yaklaştırdığını fark ediyor. Uyar mı, izle mi?",
                            Choice("Uyar", flagsAdd: Flags("karakol_uyari"), forcedNext: 168),
                            Choice("İzle", forcedNext: 167),
                            speaker: "Kemal (Mühendis)")
                    }),

                Card(167, "Emine Teyze",
                    "Emine Teyze'nin bahçesi çiçek açıyor. İzle mi, geç mi?",
                    Choice("İzle", forcedNext: 170),
                    Choice("Geç", forcedNext: 168)),

                Card(168, "Ömer (Gözcü)",
                    "\"Lider\" zombi düzenli olarak çite geliyor. Zeynep'i çağır mı, yalnız mı dinle?",
                    Choice("Zeynep'i çağır", forcedNext: 171),
                    Choice("Yalnız dinle", forcedNext: 169)),

                Card(169, "İsmet (Telsizci)",
                    "İsmet eski bir müzik istasyonu sinyali yakalıyor. Dinle mi, boşver mi?",
                    Choice("Dinle", forcedNext: 172),
                    Choice("Boşver", forcedNext: 170)),

                Card(170, "Anlatıcı",
                    "Karakol söylentisi sığınağı ikiye bölüyor. Açık forum mu, bastır mı?",
                    Choice("Açık forum", authority: 1, forcedNext: 173),
                    Choice("Bastır", conditionalEffect: ReignIfCritical(
                        StatType.Authority, atOrBelow: 3, deltasWhenSafe: new StatDeltas(-1, 0, 0, 0),
                        resetStat: StatType.Authority), forcedNext: 171)),

                Card(171, "Ali (Halktan)",
                    "Ali'nin ilk başarısı kutlanıyor. Kutla mı, mütevazı mı kal?",
                    Choice("Kutla", forcedNext: 173),
                    Choice("Mütevazı kal", forcedNext: 172)),

                Card(172, "Anlatıcı",
                    "Güvenli rota istikrarlı bir kazanç sağladı.",
                    Choice("Devam et", wealth: 1, forcedNext: 175),
                    Choice("Genişlet", wealth: 1, forcedNext: 175),
                    variants: new[]
                    {
                        VariantIfFlag("karakol_uyari", "Uyarı, ilişkiyi soğuttu.",
                            Choice("Kabullen", authority: -1, forcedNext: 175),
                            Choice("Devam et", authority: -1, forcedNext: 175)),
                        VariantIfFlag("karakol_gerginlik", "İzlemekle yetinmek hasar bıraktı.",
                            Choice("Kabullen", security: -1, people: -1, forcedNext: 175),
                            Choice("Devam et", security: -1, people: -1, forcedNext: 175)),
                        VariantIfFlag("rota_riskli", "Riskli rota büyük getiri getirdi.",
                            Choice("Sevin", randomOutcome: new RandomStatOutcome(
                                new StatDeltas(0, 0, 0, 2), new StatDeltas(0, 0, 0, -1)), forcedNext: 175),
                            Choice("Devam et", randomOutcome: new RandomStatOutcome(
                                new StatDeltas(0, 0, 0, 2), new StatDeltas(0, 0, 0, -1)), forcedNext: 175))
                    }),

                Card(173, "Anlatıcı",
                    "Büyük bir \"hasat/inşaat bayramı\" kutlanıyor. Katıl mı, arka planda mı?",
                    Choice("Katıl", forcedNext: 177),
                    Choice("Arka planda kal", forcedNext: 174)),

                Card(174, "Karakol Temsilcisi",
                    "Karakol'dan görüşme daveti geliyor. Bizzat mı, temsilci mi?",
                    Choice("Bizzat git", conditionalEffect: LeaderRisk(leaderHealthDeltaWhenFalse: -3),
                        forcedNext: 176),
                    Choice("Temsilci gönder", forcedNext: 175)),

                Card(175, "Anlatıcı",
                    "Sonrasında sakin bir hafta. Vakit geçir mi, işe mi dön?",
                    Choice("Vakit geçir", forcedNext: 177),
                    Choice("İşe dön", forcedNext: 176)),

                Card(176, "İsmet (Telsizci)",
                    "Yeni bir talep ya da eski bir sinyal geliyor. İncele mi, yok say mı?",
                    Choice("İncele", flagsAdd: Flags("vertak_yanki_evet"), forcedNext: 179),
                    Choice("Yok say", forcedNext: 177)),

                Card(177, "Anlatıcı",
                    "Necati doğal bir şekilde vefat ediyor. Anısını an mı, sessizce devam mı?",
                    Choice("An", forcedNext: 180),
                    Choice("Sessizce devam", forcedNext: 178)),

                Card(178, "Kemal (Mühendis)",
                    "Kemal, sığınağı genişletme fikri sunuyor. Büyük mü, kademeli mi?",
                    Choice("Büyük yatırım", flagsAdd: Flags("genisleme_buyuk"),
                        conditionalEffect: AlwaysLeaderHealth(-1), forcedNext: 181),
                    Choice("Kademeli", forcedNext: 179)),

                Card(179, "Anlatıcı",
                    "Kademeli genişleme yavaş ama sağlam ilerliyor.",
                    Choice("Devam et", security: 2, forcedNext: 181),
                    Choice("Sürdür", security: 2, forcedNext: 181),
                    variants: new[]
                    {
                        VariantIfFlag("genisleme_buyuk", "Büyük yatırım hızla şekilleniyor.",
                            Choice("Sevin", security: 3, forcedNext: 181),
                            Choice("Devam et", security: 3, forcedNext: 181))
                    }),

                Card(180, "Anlatıcı",
                    "Yeni bölgede ilk gece. Orada mı kal, eski bölgede mi?",
                    Choice("Yeni bölgede kal", forcedNext: 183),
                    Choice("Eski bölgede kal", forcedNext: 181)),

                Card(181, "Anlatıcı",
                    "\"Lider\" zombi bir bölgeyi paylaşmayı teklif ediyor. Kabul mü, ret mi?",
                    Choice("Kabul", flagsAdd: Flags("zombi_anlasma_evet"), forcedNext: 183),
                    Choice("Ret", forcedNext: 182)),

                Card(182, "Anlatıcı",
                    "Net bir sınır çizilir.",
                    Choice("Kabullen", security: 1, forcedNext: 184),
                    Choice("Devam et", security: 1, forcedNext: 184),
                    variants: new[]
                    {
                        VariantIfFlag("zombi_anlasma_evet",
                            "Garip ama işlevsel bir komşuluk kurulur.",
                            Choice("Kabullen", authority: 1, people: -1, flagsAdd: Flags("zombi_komsuluk"),
                                forcedNext: 184),
                            Choice("Devam et", authority: 1, people: -1, flagsAdd: Flags("zombi_komsuluk"),
                                forcedNext: 184))
                    }),

                Card(183, "Ali (Halktan)",
                    "Ali kendi çırağını eğitmeye başlıyor. Gurur duy mu, doğal mı karşıla?",
                    Choice("Gurur duy", forcedNext: 187),
                    Choice("Doğal karşıla", forcedNext: 184)),

                Card(184, "Zeynep (Doktor)",
                    "Yeni bölgeden bir hastalık riski var. Sıkı karantina mı, devam mı?",
                    Choice("Sıkı karantina", wealth: -1, people: 1, forcedNext: 186),
                    Choice("Devam et", conditionalEffect: ReignIfCritical(
                        StatType.People, atOrBelow: 3, deltasWhenSafe: new StatDeltas(0, -1, 0, 0),
                        resetStat: StatType.People), forcedNext: 185)),

                Card(185, "Sibel (Halktan)",
                    "Sibel ve öğrencileri bir konser daha veriyor. Katıl mı, izle mi?",
                    Choice("Katıl", forcedNext: 188),
                    Choice("İzle", forcedNext: 186)),

                Card(186, "İsmet (Telsizci)",
                    "İsmet eski bir askeri frekansta kodlanmış bir mesaj yakalıyor. Deşifre et mi, " +
                    "yok say mı?",
                    Choice("Deşifre et", flagsAdd: Flags("mesaj_cozuldu_evet"), forcedNext: 188),
                    Choice("Yok say", forcedNext: 187)),

                Card(187, "Fatma (Halktan)",
                    "Fatma'nın resimleri dışarıya da hediye ediliyor. Destekle mi, önemseme mi?",
                    Choice("Destekle", forcedNext: 190),
                    Choice("Önemseme", forcedNext: 188)),

                Card(188, "Gül (Halktan)",
                    "Gül'ün çocuğu ilk kez \"anne\" dışında bir kelime söylüyor. Gülümse mi, şaşır mı?",
                    Choice("Gülümse", forcedNext: 192),
                    Choice("Şaşır", forcedNext: 189)),

                Card(189, "Anlatıcı",
                    "Sinyal zamanla söner.",
                    Choice("Devam et", forcedNext: 192),
                    Choice("Unut", forcedNext: 192),
                    variants: new[]
                    {
                        VariantIfFlag("mesaj_cozuldu_evet",
                            "Uzak bir topluluktan SOS mesajı. Yardıma git mi, mesafeli mi?",
                            Choice("Yardıma git", wealth: -1, authority: 1,
                                flagsAdd: Flags("uzak_topluluk_evet"), forcedNext: 192),
                            Choice("Mesafeli kal", forcedNext: 190),
                            speaker: "İsmet (Telsizci)")
                    }),

                Card(190, "Anlatıcı",
                    "Haftalık toplantı geleneği sürüyor. Katıl mı, dinle mi?",
                    Choice("Katıl", forcedNext: 193),
                    Choice("Dinle", forcedNext: 191)),

                Card(191, "Anlatıcı",
                    "Sıradan, düşük riskli bir gün.",
                    Choice("Devam et", forcedNext: 192),
                    Choice("Rahatla", forcedNext: 192),
                    variants: new[]
                    {
                        VariantIfFlag("uzak_topluluk_evet", "Ulaşmak tehlikeli. Bizzat mı, ekip mi?",
                            Choice("Bizzat git", conditionalEffect: LeaderRisk(leaderHealthDeltaWhenFalse: -3),
                                forcedNext: 194),
                            Choice("Ekip gönder", forcedNext: 192),
                            speaker: "Mustafa (Asker)")
                    }),

                Card(192, "Anlatıcı",
                    "Sinyal söner.",
                    Choice("Devam et", forcedNext: 195),
                    Choice("Unut", forcedNext: 195),
                    variants: new[]
                    {
                        VariantIfFlag("vertak_yanki_evet", "Önemli bulgu çıkar.",
                            Choice("İncele", counterDeltas: Counter(CounterPharmaArastirma, 1), forcedNext: 195),
                            Choice("Kaydet", counterDeltas: Counter(CounterPharmaArastirma, 1), forcedNext: 195))
                    }),

                Card(193, "Anlatıcı",
                    "Herkes döner, sakin bir akşam. Dinlen mi, işe mi dön?",
                    Choice("Dinlen", forcedNext: 196),
                    Choice("İşe dön", forcedNext: 194)),

                Card(194, "İsmet (Telsizci)",
                    "İsmet'in tarih arşivi büyüyor. Katkı ver mi, izle mi?",
                    Choice("Katkı ver", forcedNext: 198),
                    Choice("İzle", forcedNext: 195)),

                Card(195, "Anlatıcı",
                    "Anlaşma ilk kez ciddi sınanıyor. Sakin mi, sert mi?",
                    Choice("Sakin kal", authority: 1, forcedNext: 198),
                    Choice("Sert tepki", authority: -1, flagsAdd: Flags("zombi_komsuluk_gergin"),
                        forcedNext: 196)),

                Card(196, "Anlatıcı",
                    "Yeni bir çocuk doğuyor, isim koyma günü. Katıl mı, kısa tebrik mi?",
                    Choice("Katıl", forcedNext: 200),
                    Choice("Kısa tebrik", forcedNext: 197)),

                Card(197, "Kemal (Mühendis)",
                    "Kemal, küçük bir elektrik şebekesi kurduğunu gösteriyor. Kutla mı, sıradan mı?",
                    Choice("Kutla", forcedNext: 201),
                    Choice("Sıradan karşıla", forcedNext: 198)),

                Card(198, "Ali (Halktan)",
                    "Ali'nin çırağı kendi çırağını almaya hazırlanıyor. Gurur duy mu, şaşır mı?",
                    Choice("Gurur duy", forcedNext: 200),
                    Choice("Şaşır", forcedNext: 199)),

                Card(199, "Mete (Asker)",
                    "Mete, Karakol ilişkilerinin gizli bir ajandası olabileceğinden şüpheleniyor. " +
                    "Araştır mı, güven mi?",
                    Choice("Araştır", flagsAdd: Flags("son_kusku_evet"), forcedNext: 201),
                    Choice("Güven", forcedNext: 200)),

                Card(200, "Anlatıcı",
                    "Sığınak artık büyümüş, komşuları var. Bu bir final değil — hikaye derinleşerek " +
                    "sürüyor.",
                    Choice("Devam et", forcedNext: 201),
                    Choice("İlerle", forcedNext: 201)),
            };
        }
    }
}
