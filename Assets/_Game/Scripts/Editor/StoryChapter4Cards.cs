using System.Collections.Generic;
using RoyalDecisions.Data;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Bölüm IV — K101 to K150. See <see cref="StoryContentLibrary"/> for shared conventions.
    /// </summary>
    /// <remarks>
    /// K118, K129 and K138 each carry a specification condition (respectively
    /// "(ayaklanma_riski=evet ise)", "(vertak_gozlem=evet ise)", "(ateskes=evet ise)") with no
    /// alternate content given for when the condition does not hold; each is authored to apply
    /// unconditionally, the same call made for K67/K81/K93 in Chapter III. K133's four-way outcome
    /// (two prior binary decisions) is expressed as three ordered, compound-condition variants over
    /// a base case, most-specific first. K136's "Kabul / Ret / Araştırma" three-way outcome is
    /// collapsed to two (K135 only ever offers two choices) — see the project report's "Story Spec
    /// Ambiguities" section for both.
    /// </remarks>
    public static partial class StoryContentLibrary
    {
        internal static List<CardDefinition> CreateChapter4Cards()
        {
            return new List<CardDefinition>(50)
            {
                Card(101, "Necati (Halktan)",
                    "Yeni sezon sakin bir sabahla açılıyor. Necati eski radyoyu tamir ediyor. " +
                    "Yardım et mi, izle mi?",
                    Choice("Yardım et", forcedNext: 104),
                    Choice("İzle", forcedNext: 102)),

                Card(102, "İsmet (Telsizci)",
                    "Vertak sinyalleri sıklaştı. Karart mı, açık mı bırak?",
                    Choice("Karart", flagsAdd: Flags("vertak_karartma_evet"), forcedNext: 104),
                    Choice("Açık bırak", forcedNext: 103)),

                Card(103, "Fatma (Halktan)",
                    "Fatma duvara yeni resimler yapıyor. Katkı ver mi, izle mi?",
                    Choice("Katkı ver", forcedNext: 106),
                    Choice("İzle", forcedNext: 104)),

                Card(104, "Tarık (Halktan)",
                    "Tarık liderliğini açıkça sorguluyor: \"Oy yapalım.\" İzin ver mi, bastır mı?",
                    Choice("İzin ver", flagsAdd: Flags("meydan_okuma_evet"), forcedNext: 107),
                    Choice("Bastır", flagsAdd: Flags("gizli_gerginlik"), forcedNext: 105)),

                Card(105, "Anlatıcı",
                    "Tarık gizliden destek topluyor. Ömer'e izlet mi, görmezden mi?",
                    Choice("İzlet", authority: -1, forcedNext: 108),
                    Choice("Görmezden gel", flagsAdd: Flags("ayaklanma_riski"), forcedNext: 106),
                    variants: new[]
                    {
                        VariantIfFlag("meydan_okuma_evet", "Açık tartışma. Açık konuş mu, sessiz mi?",
                            Choice("Açık konuş", authority: 2, forcedNext: 108),
                            Choice("Sessiz kal", authority: 1, forcedNext: 106))
                    }),

                Card(106, "Anlatıcı",
                    "Vertak konumu bulur.",
                    Choice("Kaygılan", flagsAdd: Flags("vertak_yolda"), forcedNext: 107),
                    Choice("Devam et", flagsAdd: Flags("vertak_yolda"), forcedNext: 107),
                    variants: new[]
                    {
                        VariantIfFlag("vertak_karartma_evet", "Sinyal kaybolur.",
                            Choice("Rahatla", authority: 1, forcedNext: 108),
                            Choice("Devam et", authority: 1, forcedNext: 108))
                    }),

                Card(107, "Sibel (Halktan)",
                    "Sibel'in piyano konserleri artık düzenli. Dinle mi, işe mi dön?",
                    Choice("Dinle", forcedNext: 110),
                    Choice("İşe dön", forcedNext: 108)),

                Card(108, "Ali (Halktan)",
                    "Ali artık genç bir yetişkin, \"çırak nöbetçi\" oldu. Gurur duy mu, sıradan mı " +
                    "davran?",
                    Choice("Gurur duy", forcedNext: 111),
                    Choice("Sıradan davran", forcedNext: 109)),

                Card(109, "Ömer (Gözcü)",
                    "Ömer, birinin düzenli olarak çite yaklaşıp konuşmaya çalıştığını bildiriyor. " +
                    "İsim ver mi, mesafeli mi?",
                    Choice("İsim ver", flagsAdd: Flags("zombi_isimlendirildi"), forcedNext: 112),
                    Choice("Mesafeli kal", forcedNext: 110)),

                Card(110, "Sabiha (Erzakçı)",
                    "Sabiha yeni bir bölge öneriyor. Git mi, kal mı?",
                    Choice("Git", forcedNext: 112),
                    Choice("Kal", forcedNext: 111),
                    variants: new[]
                    {
                        VariantIfFlag("vertak_yolda",
                            "Bir araç yakında duruyor, kapıyı çalıyorlar. Aç mı, silahlan mı?",
                            Choice("Aç", forcedNext: 112),
                            Choice("Silahlan", forcedNext: 111))
                    }),

                Card(111, "Anlatıcı",
                    "Eski bir depo bulunur, kilitli — açılır, orta düzey erzak.",
                    Choice("Paylaştır", wealth: 2, forcedNext: 112),
                    Choice("Sakla", wealth: 2, forcedNext: 112),
                    variants: new[]
                    {
                        VariantIfFlag("vertak_yolda",
                            "Temsilci ayrılır ama \"gözlemleneceksiniz\" der.",
                            Choice("Endişelen", authority: -1, flagsAdd: Flags("vertak_gozlem"),
                                forcedNext: 115),
                            Choice("Görmezden gel", authority: -1, flagsAdd: Flags("vertak_gozlem"),
                                forcedNext: 115))
                    }),

                Card(112, "Aziz (Tarımcı)",
                    "Aziz yeni bir hasat tarifi dener. Tadına bak mı, mütevazı mı kal?",
                    Choice("Tadına bak", forcedNext: 116),
                    Choice("Mütevazı kal", forcedNext: 113)),

                Card(113, "Zeynep (Doktor)",
                    "Bir gıda zehirlenmesi vakası çıkıyor. Test et mi, görmezden mi?",
                    Choice("Test et", wealth: -1, people: 1, forcedNext: 116),
                    Choice("Görmezden gel", conditionalEffect: ReignIfCritical(
                        StatType.People, atOrBelow: 3, deltasWhenSafe: default,
                        resetStat: StatType.People), forcedNext: 114)),

                Card(114, "Cem & Yusuf",
                    "Cem ve Yusuf'un oyunu artık gelenek. Oyna mı, izle mi?",
                    Choice("Oyna", forcedNext: 117),
                    Choice("İzle", forcedNext: 115)),

                Card(115, "Kemal (Mühendis)",
                    "Kemal büyük bir onarım projesi öneriyor. Tam mı, minimal mi?",
                    Choice("Tam proje", flagsAdd: Flags("onarim_tam"), forcedNext: 118),
                    Choice("Minimal", forcedNext: 116)),

                Card(116, "Anlatıcı",
                    "Minimal onarım tamamlanır; ileride yine sorun çıkabilir.",
                    Choice("Kabullen", security: 1, forcedNext: 118),
                    Choice("Devam et", security: 1, forcedNext: 118),
                    variants: new[]
                    {
                        VariantIfFlag("onarim_tam", "Tam kapsamlı onarım tamamlanır.",
                            Choice("Sevin", security: 3, conditionalEffect: AlwaysLeaderHealth(-1),
                                forcedNext: 118),
                            Choice("Devam et", security: 3, conditionalEffect: AlwaysLeaderHealth(-1),
                                forcedNext: 118))
                    }),

                Card(117, "Anlatıcı",
                    "Küçük bir pazar kuruluyor, millet eşya takas ediyor. Katıl mı, gözlemle mi?",
                    Choice("Katıl", forcedNext: 120),
                    Choice("Gözlemle", forcedNext: 118)),

                Card(118, "Anlatıcı",
                    "Gizli gerginlik patlıyor. Yüzleş mi, kaç mı?",
                    Choice("Yüzleş", conditionalEffect: LeaderRisk(
                        leaderHealthDeltaWhenFalse: -3, deltasWhenFalse: new StatDeltas(2, 0, 0, 0)),
                        forcedNext: 120),
                    Choice("Kaç/saklan", authority: -2, forcedNext: 119)),

                Card(119, "Anlatıcı",
                    "Olaylardan sonra sakin bir akşam. Paylaş mı, yalnız mı kal?",
                    Choice("Paylaş", forcedNext: 122),
                    Choice("Yalnız kal", forcedNext: 120)),

                Card(120, "Anlatıcı",
                    "Yaralı bir kadın kapıya geliyor, eski bir Vertak çalışanı. İçeri al mı, uzak " +
                    "tut mu?",
                    Choice("İçeri al", flagsAdd: Flags("eski_vertak_calisan"), forcedNext: 124),
                    Choice("Uzak tut", forcedNext: 121)),

                Card(121, "Anlatıcı",
                    "Kadın gider, bir not bırakır — kısmi bilgi.",
                    Choice("Oku", forcedNext: 122),
                    Choice("Sakla", forcedNext: 122),
                    variants: new[]
                    {
                        VariantIfFlag("eski_vertak_calisan", "İsmet sorguluyor. Güven mi, şüphe mi?",
                            Choice("Güven", authority: -1,
                                counterDeltas: Counter(CounterPharmaArastirma, 2),
                                flagsAdd: Flags("icerden_bilgi"), forcedNext: 124),
                            Choice("Şüphe", authority: -1, forcedNext: 124),
                            speaker: "İsmet (Telsizci)")
                    }),

                Card(122, "Ali (Halktan)",
                    "Ali \"tam nöbetçi\"liğe terfi ediyor. Gurur duy mu, sade mi geç?",
                    Choice("Gurur duy", forcedNext: 124),
                    Choice("Sade geç", forcedNext: 123)),

                Card(123, "Zeynep (Doktor)",
                    "Zeynep kendinden sonrasını eğitmek istiyor. Atilla mı, Sibel mi?",
                    Choice("Atilla", flagsAdd: Flags("halef_atilla"), forcedNext: 125),
                    Choice("Sibel", flagsAdd: Flags("halef_sibel"), forcedNext: 124)),

                Card(124, "Anlatıcı",
                    "Büyüyen bir \"sığınak kütüphanesi\" oluşuyor. Katkı ver mi, izle mi?",
                    Choice("Katkı ver", forcedNext: 127),
                    Choice("İzle", forcedNext: 125)),

                Card(125, "Ömer (Gözcü)",
                    "Ömer, zombinin düzenli ziyaret edip bir yön işaret ettiğini fark ediyor. " +
                    "Takip et mi, görmezden mi?",
                    Choice("Takip et", flagsAdd: Flags("zombi_takip"), forcedNext: 128),
                    Choice("Görmezden gel", forcedNext: 126)),

                Card(126, "Anlatıcı",
                    "Zombi kayboluyor, gizem çözülmeden kalır.",
                    Choice("Kabullen", forcedNext: 127),
                    Choice("Devam et", forcedNext: 127),
                    variants: new[]
                    {
                        VariantIfFlag("zombi_takip", "Eski bir Vertak tesisine yönlendiriyor.",
                            Choice("İncele", authority: -1, flagsAdd: Flags("vertak_tesis_bulundu"),
                                counterDeltas: Counter(CounterPharmaArastirma, 2), forcedNext: 129),
                            Choice("Uzaklaş", authority: -1, flagsAdd: Flags("vertak_tesis_bulundu"),
                                counterDeltas: Counter(CounterPharmaArastirma, 2), forcedNext: 129))
                    }),

                Card(127, "Anlatıcı",
                    "Halef eğitimi tamamlıyor, ikinci bir sağlıkçı var.",
                    Choice("Sevin", people: 1, flagsAdd: Flags("ikinci_saglikci"), forcedNext: 131),
                    Choice("Devam et", people: 1, flagsAdd: Flags("ikinci_saglikci"), forcedNext: 131)),

                Card(128, "Sibel (Halktan)",
                    "Sibel'in konserine dışarıdan katılanlar da oluyor. Katıl mı, izle mi?",
                    Choice("Katıl", forcedNext: 132),
                    Choice("İzle", forcedNext: 129)),

                Card(129, "Anlatıcı",
                    "\"Gözlem\"in aslında sürekli takip olduğu anlaşılıyor. Sakinleştir mi, gerçeği " +
                    "kabul et mi?",
                    Choice("Sakinleştir", authority: 1, forcedNext: 133),
                    Choice("Gerçeği kabul et", conditionalEffect: ReignIfCritical(
                        StatType.Authority, atOrBelow: 3, deltasWhenSafe: new StatDeltas(-1, 0, 0, 0),
                        resetStat: StatType.Authority), forcedNext: 130)),

                Card(130, "Gül (Halktan)",
                    "Gül'ün çocuğu ilk kelimelerini söylüyor. Kutla mı, meşgul mü?",
                    Choice("Kutla", forcedNext: 133),
                    Choice("Meşgul ol", forcedNext: 131)),

                Card(131, "Mustafa (Asker)",
                    "Mustafa: en büyük sürü yaklaşıyor. Seferberlik mi, tahliye mi?",
                    Choice("Seferberlik", flagsAdd: Flags("kriz_seferberlik"), forcedNext: 134),
                    Choice("Tahliye", forcedNext: 132)),

                Card(132, "Mustafa / Mete",
                    "Sürü artık görünür mesafede. Mustafa ve Mete pozisyon alıyor. Cepheye lider mi, " +
                    "geride mi?",
                    Choice("Cepheye çık", flagsAdd: Flags("kriz_cephede"), conditionalEffect: LeaderRisk(
                        leaderHealthDeltaWhenFalse: -3), forcedNext: 135),
                    Choice("Geride kal", forcedNext: 133)),

                Card(133, "Anlatıcı",
                    "Tahliye tamamlanamadan iptal edilir.",
                    Choice("Kabullen", authority: -1, forcedNext: 136),
                    Choice("Devam et", authority: -1, forcedNext: 136),
                    variants: new[]
                    {
                        new CardVariant(
                            new CardConditions(Flags("kriz_seferberlik", "kriz_cephede"), null, null),
                            bodyText: "Seferberlik cephede karşılanır; hasar büyük ama moral yüksek.",
                            leftChoice: Choice("Sevin", security: -1, authority: 3, forcedNext: 136),
                            rightChoice: Choice("Devam et", security: -1, authority: 3, forcedNext: 136)),
                        VariantIfFlag("kriz_seferberlik",
                            "Seferberlik geriden yönetilir; hasar da moral de ölçülü.",
                            Choice("Sevin", security: -2, authority: 1, forcedNext: 136),
                            Choice("Devam et", security: -2, authority: 1, forcedNext: 136)),
                        VariantIfFlag("kriz_cephede",
                            "Tahliye cepheden korunarak sürdürülür; kayıplar oldu.",
                            Choice("Kabullen", security: -2, wealth: -1, forcedNext: 136),
                            Choice("Devam et", security: -2, wealth: -1, forcedNext: 136))
                    }),

                Card(134, "Anlatıcı",
                    "Kriz sonrası sakin bir gün. Vakit geçir mi, yalnız mı kal?",
                    Choice("Vakit geçir", forcedNext: 137),
                    Choice("Yalnız kal", forcedNext: 135)),

                Card(135, "İsmet (Telsizci)",
                    "Vertak hâlâ gizemli. Devam mı, unut mu?",
                    Choice("Devam et", flagsAdd: Flags("vertak_yuzlesildi"), forcedNext: 139),
                    Choice("Unut", forcedNext: 136),
                    variants: new[]
                    {
                        new CardVariant(RequiresCounterAtLeast(CounterPharmaArastirma, 3),
                            bodyText: "Vertak'ın gerçek yüzü gizlenemiyor. Yüzleş mi, kaçın mı?",
                            leftChoice: Choice("Yüzleş", flagsAdd: Flags("vertak_yuzlesildi"), forcedNext: 139),
                            rightChoice: Choice("Kaçın", forcedNext: 136))
                    }),

                Card(136, "Anlatıcı",
                    "Tam bağımsızlık sürer, ama tehlike de sürer; belirsizlik büyüyor.",
                    Choice("Kabullen", counterDeltas: Counter(CounterPharmaArastirma, 1), forcedNext: 139),
                    Choice("Devam et", counterDeltas: Counter(CounterPharmaArastirma, 1), forcedNext: 139),
                    variants: new[]
                    {
                        VariantIfFlag("vertak_yuzlesildi",
                            "Vertak korumasına girilir — güvenlik artar, özgürlük azalır.",
                            Choice("Kabullen", security: 2, authority: -1, forcedNext: 139),
                            Choice("Devam et", security: 2, authority: -1, forcedNext: 139))
                    }),

                Card(137, "Anlatıcı",
                    "Sığınakta büyük bir toplantı yapılıyor. Söz al mı, dinle mi?",
                    Choice("Söz al", forcedNext: 140),
                    Choice("Dinle", forcedNext: 138)),

                Card(138, "Ömer (Gözcü)",
                    "Ömer, zombilerle \"sınır anlaşması\" önerildiğini iletir. Kabul mü, mesafe mi?",
                    Choice("Kabul et", authority: 1, forcedNext: 141),
                    Choice("Mesafe koy", forcedNext: 139)),

                Card(139, "Ali (Halktan)",
                    "Ali artık sığınağın en genç vasıflı üyesi. Gurur duy mu, mütevazı mı kal?",
                    Choice("Gurur duy", forcedNext: 143),
                    Choice("Mütevazı kal", forcedNext: 140)),

                Card(140, "Mustafa / Mete",
                    "Mustafa ve Mete en büyük tehdidin geldiğini haber veriyor. Öne çık mı, arkada " +
                    "dur mu?",
                    Choice("Öne çık", conditionalEffect: LeaderRisk(leaderHealthDeltaWhenFalse: -4),
                        forcedNext: 142),
                    Choice("Arkada dur", authority: -1, forcedNext: 141)),

                Card(141, "Anlatıcı",
                    "Fırtına dinmiş gibi, sığınak bir kez daha ayakta. Nefes al mı, işe mi dön?",
                    Choice("Nefes al", forcedNext: 143),
                    Choice("İşe dön", forcedNext: 142)),

                Card(142, "İsmet (Telsizci)",
                    "İsmet eski günlükleri düzenliyor, sığınağın tarihini yazmaya karar veriyor. " +
                    "Anlat mı, ona mı bırak?",
                    Choice("Anlat", forcedNext: 144),
                    Choice("Ona bırak", forcedNext: 143)),

                Card(143, "Anlatıcı",
                    "Son bir sakin akşam, hayatta kalan kadronun hepsi bir arada. Teşekkür et mi, " +
                    "sessizce mi otur?",
                    Choice("Teşekkür et", forcedNext: 146),
                    Choice("Sessizce otur", forcedNext: 144)),

                Card(144, "Aziz (Tarımcı)",
                    "Emine Teyze'nin bahçesi (Aziz'in eseri) çiçek açıyor. İzle mi, geç mi?",
                    Choice("İzle", forcedNext: 147),
                    Choice("Geç", forcedNext: 145)),

                Card(145, "Necati (Halktan)",
                    "Necati eski dostlarını anıyor, sessiz bir akşam. Dinle mi, boşver mi?",
                    Choice("Dinle", forcedNext: 149),
                    Choice("Boşver", forcedNext: 146)),

                Card(146, "Aziz (Tarımcı)",
                    "Aziz yeni bir tarif üzerinde çalışıyor. Katkı ver mi, izle mi?",
                    Choice("Katkı ver", forcedNext: 148),
                    Choice("İzle", forcedNext: 147)),

                Card(147, "Anlatıcı",
                    "Sığınağın nüfusu artık istikrarlı. Değerlendir mi, sıradan mı gör?",
                    Choice("Değerlendir", forcedNext: 151),
                    Choice("Sıradan gör", forcedNext: 148)),

                Card(148, "Sabiha (Erzakçı)",
                    "Sabiha'nın ticaret ağı büyüyor. Destekle mi, sınırlı mı tut?",
                    Choice("Destekle", forcedNext: 152),
                    Choice("Sınırlı tut", forcedNext: 149)),

                Card(149, "İsmet (Telsizci)",
                    "İsmet arşivine yeni kayıtlar ekliyor. Katkı ver mi, izle mi?",
                    Choice("Katkı ver", forcedNext: 150),
                    Choice("İzle", forcedNext: 150)),

                Card(150, "Anlatıcı",
                    "Sığınağın kaderi K1'den beri birikmiş tüm bayrakların toplamına bağlı: kaçıncı " +
                    "liderdesiniz, hangi ittifaklar kuruldu, Vertak'la ilişki nasıl, zombilerle " +
                    "ateşkes mi savaş mı — hepsi burada birleşiyor. Bu bir final değildir.",
                    Choice("Devam et", forcedNext: 151),
                    Choice("İlerle", forcedNext: 151)),
            };
        }
    }
}
