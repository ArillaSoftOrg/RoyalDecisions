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
                    "Yeni dönem sakin bir günle açılır. Ali artık hangi alanda ilerlemek " +
                    "istediğine karar verecek yaştadır.",
                    Choice("Tarımı seçmesini destekle", flagsAdd: Flags("ali_yol_tarim"), forcedNext: 155),
                    Choice("Savunmayı seçmesini destekle", flagsAdd: Flags("ali_yol_savunma"),
                        forcedNext: 152)),

                Card(152, "Veli (Halktan)",
                    "Veli, ikizinin önünde açılan yolu sessizce izler. Kendi yerinin hâlâ belli " +
                    "olmaması onu rahatsız etmeye başlamıştır.",
                    Choice("Onunla açıkça konuş", authority: 1, forcedNext: 155),
                    Choice("Kendi zamanını bulmasına izin ver", forcedNext: 153)),

                Card(153, "Kemal (Mühendis)",
                    "Kemal çevrede “Karakol” adıyla bilinen, düzenli ve silahlı bir yerleşimden " +
                    "söz eder. Şimdiye kadar doğrudan temas kurulmamıştır.",
                    Choice("Temas kurmayı dene", flagsAdd: Flags("karakol_temas_evet"), forcedNext: 155),
                    Choice("Mesafeyi koru", forcedNext: 154)),

                Card(154, "Fatma (Halktan)",
                    "Fatma çocuklara resim yaptırır. Masaların üzeri boya, kâğıt ve eski dergi " +
                    "parçalarıyla doludur.",
                    Choice("Derse katıl", forcedNext: 157),
                    Choice("Kenardan izle", forcedNext: 155)),

                Card(155, "Mete (Asker)",
                    "Mete devriye sırasında Karakol’dan bir ekiple karşılaşır. İki taraf da " +
                    "birbirini önceden fark etmiştir.",
                    Choice("Resmî biçimde selam ver", forcedNext: 156),
                    Choice("Teması uzatmadan geri çekil", forcedNext: 156),
                    variants: new[]
                    {
                        VariantIfFlag("karakol_temas_evet",
                            "İsmet Karakol’la radyo bağlantısı kurar. Karşı taraf düzenli konuşur " +
                            "ama tonları emre alışkın olduklarını belli eder.",
                            Choice("İşbirliği öner", flagsAdd: Flags("karakol_isbirligi"),
                                forcedNext: 156),
                            Choice("Mesafeli bir ilişki kur", forcedNext: 156),
                            speaker: "İsmet (Telsizci)")
                    }),

                Card(156, "Anlatıcı",
                    "Karşılaşma kısa ve olaysız biter. İki taraf da diğerini artık tanımaktadır.",
                    Choice("Olayı kayda geçir", forcedNext: 158),
                    Choice("Üzerinde durma", forcedNext: 158),
                    variants: new[]
                    {
                        new CardVariant(
                            new CardConditions(Flags("karakol_temas_evet", "karakol_isbirligi"), null, null),
                            bodyText: "Karakol teklifi kabul eder ama erzak ve geçiş hakkı " +
                                "konusunda ağır şartlar öne sürer.",
                            leftChoice: Choice("Şartları kabul et", wealth: 2, authority: -1,
                                forcedNext: 158),
                            rightChoice: Choice("Daha dengeli şartlar için pazarlık et", wealth: 1,
                                authority: 1, forcedNext: 158)),
                        VariantIfFlag("karakol_temas_evet",
                            "Karakol mesafeli tavrınıza karşılık verir; ilişki açık bir çatışmaya " +
                            "dönüşmez ama soğukluk hissedilir.",
                            Choice("Tavırlarını resmî olarak eleştir", authority: -1,
                                flagsAdd: Flags("karakol_gerginlik"), forcedNext: 158),
                            Choice("Konuyu büyütme", flagsAdd: Flags("karakol_gerginlik"),
                                forcedNext: 158))
                    }),

                Card(157, "Necati (Halktan)",
                    "Necati Karakol hakkında çevreden duyduğu söylentileri anlatmaya başlar. " +
                    "Hangisinin doğru olduğunu kendisi de bilmiyordur.",
                    Choice("Bildiklerini dinle", forcedNext: 160),
                    Choice("Söylentilere kulak asma", forcedNext: 158)),

                Card(158, "Ömer (Gözcü)",
                    "Ömer, enfektelerin artık rastgele dolaşmadığını fark eder. Aynı bölgelerde " +
                    "toplanıyor, birbirlerine göre hareket ediyor gibidirler.",
                    Choice("Davranışlarını yakından izle", flagsAdd: Flags("zombi_izle_evet"), forcedNext: 160),
                    Choice("Uzaktan gözlemle yetin", forcedNext: 159)),

                Card(159, "Sibel (Halktan)",
                    "Sibel müzik derslerine çocukları da almaya başlar. Eski notalar, tahtaya " +
                    "çizilmiş birkaç çizgiyle yeniden anlam kazanır.",
                    Choice("Bir derse katıl", forcedNext: 163),
                    Choice("Kapıdan izle", forcedNext: 160)),

                Card(160, "Kemal (Mühendis)",
                    "Kemal eski onarım noktalarını gösterir. Bazıları yeniden açılmıştır; " +
                    "özellikle geçici yamalar artık yük taşımamaktadır.",
                    Choice("Büyük bir onarım başlat", wealth: -2, security: 2, forcedNext: 163),
                    Choice("Bir kez daha ertele", conditionalEffect: ReignIfCritical(
                        StatType.Security, atOrBelow: 3, deltasWhenSafe: new StatDeltas(0, 0, -1, 0),
                        resetStat: StatType.Security), forcedNext: 161)),

                Card(161, "Gül (Halktan)",
                    "Gül’ün çocuğu artık sığınağın içinde kendi başına dolaşmaktadır. Peşinden " +
                    "koşan yetişkinler ona yetişmekte zorlanır.",
                    Choice("Onlarla birlikte sevincini paylaş", forcedNext: 165),
                    Choice("İşine devam et", forcedNext: 162)),

                Card(162, "Anlatıcı",
                    "Uzaktan yapılan gözlemler kesin bir sonuç vermez. Enfektelerin ne kadar " +
                    "bilinçli hareket ettiği hâlâ belirsizdir.",
                    Choice("Konuyu aklında tut", forcedNext: 165),
                    Choice("Günlük işlere dön", forcedNext: 165),
                    variants: new[]
                    {
                        VariantIfFlag("zombi_izle_evet",
                            "Ömer’in gözlemleri enfektelerin gerçekten örgütlü hareket ettiğini " +
                            "doğrular. Bu artık tek bir rastlantıyla açıklanamaz.",
                            Choice("Bulguları Zeynep’e aktar", authority: -1,
                                flagsAdd: Flags("bilimsel_gozlem"), forcedNext: 164),
                            Choice("Şimdilik bilgiyi sakla", forcedNext: 163),
                            speaker: "Ömer (Gözcü)")
                    }),

                Card(163, "Ali (Halktan)",
                    "Ali’ye ilk kez tek başına sorumluluk taşıyacağı büyük bir görev verilir. " +
                    "Artık yanında sürekli bir yetişkin olmadan da karar vermesi beklenmektedir.",
                    Choice("Görevi bağımsız yürütmesine izin ver", flagsAdd: Flags("ali_bagimsiz"),
                        forcedNext: 166),
                    Choice("Yakınında deneyimli biri bulunsun", forcedNext: 164)),

                Card(164, "Anlatıcı",
                    "Görev sırasında Ali beklenmedik bir tehlikeyle karşılaşır. Haber sığınağa " +
                    "ulaştığında hâlâ kendi başına çözüm aramaktadır.",
                    Choice("Yardım ekibi gönder", wealth: -1, authority: 1, forcedNext: 166),
                    Choice("Müdahale etmeyip kendi çözmesini bekle", authority: 1,
                        flagsAdd: Flags("ali_sinandi"), forcedNext: 165)),

                Card(165, "Yusuf & Cem",
                    "Cem ile Yusuf’un oyunu artık gençler arasında senden habersiz oynanacak " +
                    "kadar yayılmıştır. Yeni kurallar bile çıkarmışlardır.",
                    Choice("Bir oyuna katıl", forcedNext: 168),
                    Choice("Uzaktan izle", forcedNext: 166)),

                Card(166, "Sabiha (Erzakçı)",
                    "Sabiha yeni bir ticaret rotası çıkarır. Kısa yol daha tehlikeli, uzun yol " +
                    "daha güvenlidir.",
                    Choice("Riskli rotayı kullan", flagsAdd: Flags("rota_riskli"), forcedNext: 168),
                    Choice("Güvenli rotayı kullan", forcedNext: 167),
                    variants: new[]
                    {
                        VariantIfFlag("karakol_gerginlik",
                            "Kemal sınır işaretlerinin giderek sığınağa yaklaştığını gösterir. " +
                            "“Karakol bunu bilerek yapıyor olabilir.”",
                            Choice("Resmî uyarı gönder", flagsAdd: Flags("karakol_uyari"),
                                forcedNext: 168),
                            Choice("Bir süre daha izle", forcedNext: 167),
                            speaker: "Kemal (Mühendis)")
                    }),

                Card(167, "Emine Teyze",
                    "Emine Teyze’nin bahçesi bir kez daha çiçek açar. Aziz her yıl aynı düzeni " +
                    "korumaya özen göstermiştir.",
                    Choice("Bahçede biraz dur", forcedNext: 170),
                    Choice("Yoluna devam et", forcedNext: 168)),

                Card(168, "Ömer (Gözcü)",
                    "Çitteki “Lider” artık düzenli aralıklarla gelmektedir. Bu kez uzun süre " +
                    "bekler ve doğrudan sana bakar.",
                    Choice("Zeynep’i de çağır", forcedNext: 171),
                    Choice("Onu tek başına dinle", forcedNext: 169)),

                Card(169, "İsmet (Telsizci)",
                    "İsmet eski bir frekansta yıllardır duymadığınız türden bir yayın yakalar: " +
                    "zayıf ama gerçek bir müzik istasyonu.",
                    Choice("Bir süre dinle", forcedNext: 172),
                    Choice("Frekansı kapat", forcedNext: 170)),

                Card(170, "Anlatıcı",
                    "Karakol hakkında dolaşan söylentiler sığınağı ikiye böler. Bir grup ilişkiyi " +
                    "sürdürmek, diğer grup tüm teması kesmek ister.",
                    Choice("Herkesin konuşabileceği açık toplantı yap", authority: 1, forcedNext: 173),
                    Choice("Tartışmayı zorla bastır", conditionalEffect: ReignIfCritical(
                        StatType.Authority, atOrBelow: 3, deltasWhenSafe: new StatDeltas(-1, 0, 0, 0),
                        resetStat: StatType.Authority), forcedNext: 171)),

                Card(171, "Ali (Halktan)",
                    "Ali ilk kez önemli bir görevi başarıyla tamamlar. Döndüğünde bunu belli " +
                    "etmemeye çalışsa da yüzündeki ifade değişmiştir.",
                    Choice("Başarısını kutla", forcedNext: 173),
                    Choice("Görevin doğal sonucu gibi karşıla", forcedNext: 172)),

                Card(172, "Anlatıcı",
                    "Yolculuk olaysız geçer. Kazanç büyük değildir ama düzenlidir.",
                    Choice("Bu istikrarı yeterli bul", wealth: 1, forcedNext: 175),
                    Choice("Sonraki sefer daha fazlasını hedefle", wealth: 1, forcedNext: 175),
                    variants: new[]
                    {
                        VariantIfFlag("karakol_uyari",
                            "Karakol, Kemal’in gönderdiği uyarının ardından sınır işaretlerini " +
                            "geri çeker. Buna karşılık iki taraf arasındaki güven daha da azalır.",
                            Choice("Tansiyonu düşürmek için özür dile", authority: -1, forcedNext: 175),
                            Choice("Uyarının gerekli olduğunu açıkça söyle", authority: -1,
                                forcedNext: 175)),
                        VariantIfFlag("karakol_gerginlik",
                            "Sınır birkaç gün içinde daha da yaklaşır. Artık bunun tesadüf " +
                            "olmadığı açıktır.",
                            Choice("Kendi sınırınızı belirgin biçimde işaretle", security: -1,
                                people: -1, forcedNext: 175),
                            Choice("Şimdilik karşılık verme", security: -1, people: -1, forcedNext: 175)),
                        VariantIfFlag("rota_riskli",
                            "Ekip büyük bir yükle döner ama yol boyunca birkaç kez ölümden " +
                            "dönmüştür.",
                            Choice("Ekibin riskini takdir et", wealth: 2, forcedNext: 175),
                            Choice("Bir daha bu kadar ileri gitmemelerini söyle", wealth: 2,
                                forcedNext: 175))
                    }),

                Card(173, "Anlatıcı",
                    "Hasat ve inşaat çalışmalarının aynı dönemde tamamlanması küçük bir bayrama " +
                    "dönüşür. İnsanlar buna kendiliğinden bir isim bile takar.",
                    Choice("Kutlamaya katıl", forcedNext: 177),
                    Choice("Kalabalığın dışında kal", forcedNext: 174)),

                Card(174, "Karakol Temsilcisi",
                    "Karakol’dan doğrudan görüşme daveti gelir. Yer ve saat onlar tarafından " +
                    "belirlenmiştir.",
                    Choice("Görüşmeye kendin git", conditionalEffect: LeaderRisk(leaderHealthDeltaWhenFalse: -3),
                        forcedNext: 176),
                    Choice("Bir temsilci gönder", forcedNext: 175)),

                Card(175, "Anlatıcı",
                    "Gergin günlerin ardından bir hafta boyunca önemli hiçbir şey olmaz. Bu kadar " +
                    "sessizlik bile artık garip gelmektedir.",
                    Choice("İnsanlarla vakit geçir", forcedNext: 177),
                    Choice("İşine dön", forcedNext: 176)),

                Card(176, "İsmet (Telsizci)",
                    "*(K135-136'daki karara göre)* Vertak’la kurduğunuz eski ilişkinin izi " +
                    "yeniden belirir. Korumasını kabul ettiyseniz yeni bir talep, reddettiyseniz " +
                    "eski bir frekanstan yeni bir sinyal gelir.",
                    Choice("Mesajı incele", flagsAdd: Flags("vertak_yanki_evet"), forcedNext: 179),
                    Choice("Yok say", forcedNext: 177)),

                Card(177, "Anlatıcı",
                    "Necati bir sabah uyanmaz. Ölümü ani bir saldırının değil, yılların ve " +
                    "yorgunluğun sonucudur. *(Not: nüfus bir azalır.)*",
                    Choice("Anısını birlikte anın", authority: 1, forcedNext: 180),
                    Choice("Sessizce işlere devam edin", forcedNext: 178)),

                Card(178, "Kemal (Mühendis)",
                    "Kemal sığınağın artık mevcut sınırlarına sığmadığını söyler. Yeni bölmeler " +
                    "açmak mümkündür ama bunun bedeli vardır.",
                    Choice("Büyük bir genişleme başlat", flagsAdd: Flags("genisleme_buyuk"),
                        conditionalEffect: AlwaysLeaderHealth(-1), forcedNext: 181),
                    Choice("Bölgeyi kademeli genişlet", forcedNext: 179)),

                Card(179, "Anlatıcı",
                    "Yeni alan yavaş yavaş büyür. Gösterişli değildir ama her bölüm sağlam " +
                    "biçimde tamamlanır.",
                    Choice("Sabırlı ilerleyişi takdir et", security: 2, forcedNext: 181),
                    Choice("Bunu işin doğal parçası say", security: 2, forcedNext: 181),
                    variants: new[]
                    {
                        VariantIfFlag("genisleme_buyuk",
                            "Genişleme kısa sürede tamamlanır. Yeni alan etkileyicidir ama " +
                            "çalışma ekibini fazlasıyla yormuştur.",
                            Choice("Tamamlanışı kutla", security: 3, forcedNext: 181),
                            Choice("Ekibi dinlenmeye gönder", security: 3, forcedNext: 181))
                    }),

                Card(180, "Anlatıcı",
                    "Yeni açılan bölgede ilk geceyi geçirecek kadar yer hazırlanmıştır. Eski " +
                    "bölüm hâlâ daha tanıdık ve güvenli hissettirir.",
                    Choice("Yeni bölgede kal", forcedNext: 183),
                    Choice("Eski bölümde kal", forcedNext: 181)),

                Card(181, "Anlatıcı",
                    "“Lider” çitin ötesindeki boş araziyi gösterip anlaşılır birkaç kelime kurar. " +
                    "Enfekteler o bölgeyi sizinle paylaşmayı teklif ediyor gibidir.",
                    Choice("Teklifi kabul et", flagsAdd: Flags("zombi_anlasma_evet"), forcedNext: 183),
                    Choice("Teklifi reddet", forcedNext: 182)),

                Card(182, "Anlatıcı",
                    "İki taraf arasında belirgin bir sınır çizilir. Mesafe arttıkça güvenlik de " +
                    "artar.",
                    Choice("Sınırı açıkça işaretle", security: 1, forcedNext: 184),
                    Choice("İşaret koymadan mesafeyi koru", security: 1, forcedNext: 184),
                    variants: new[]
                    {
                        VariantIfFlag("zombi_anlasma_evet",
                            "İlk günler garip geçse de iki taraf aynı bölgede birbirine " +
                            "saldırmadan yaşamayı başarır.",
                            Choice("Anlaşmayı kadroya açıkça anlat", authority: 1, people: -1,
                                flagsAdd: Flags("zombi_komsuluk"), forcedNext: 184),
                            Choice("Ayrıntıları gizli tut", authority: 1, people: -1,
                                flagsAdd: Flags("zombi_komsuluk"), forcedNext: 184))
                    }),

                Card(183, "Ali (Halktan)",
                    "Ali artık kendi çırağını yetiştirecek kadar deneyimlidir. İlk kez bir " +
                    "başkasının hatalarından da sorumlu olacaktır.",
                    Choice("Bu gelişmeyi takdir et", forcedNext: 187),
                    Choice("Bunu doğal bir geçiş say", forcedNext: 184)),

                Card(184, "Zeynep (Doktor)",
                    "Yeni açılan bölgede birkaç kişide aynı belirtiler görülür. Zeynep bunun " +
                    "yayılmadan durdurulabilecek bir hastalık olabileceğini söyler.",
                    Choice("Sıkı karantina uygula", wealth: -1, people: 1, forcedNext: 186),
                    Choice("Hayatı normal sürdür", conditionalEffect: ReignIfCritical(
                        StatType.People, atOrBelow: 3, deltasWhenSafe: new StatDeltas(0, -1, 0, 0),
                        resetStat: StatType.People), forcedNext: 185)),

                Card(185, "Sibel (Halktan)",
                    "Sibel ve öğrencileri yeni bölgede ilk konserlerini verir. Çalanların bir " +
                    "kısmı yıllar önce notayı bile bilmiyordu.",
                    Choice("Konseri dinle", forcedNext: 188),
                    Choice("Uzaktan izle", forcedNext: 186)),

                Card(186, "İsmet (Telsizci)",
                    "İsmet eski bir askerî frekansta kodlanmış, tekrar eden bir mesaj yakalar. " +
                    "Kaynağı oldukça uzaktadır.",
                    Choice("Mesajı çözmeye çalış", flagsAdd: Flags("mesaj_cozuldu_evet"), forcedNext: 188),
                    Choice("Frekansı yok say", forcedNext: 187)),

                Card(187, "Fatma (Halktan)",
                    "Fatma’nın resimleri artık diğer topluluklara da hediye edilmektedir. " +
                    "Bazılarının duvarlarında sığınağın çizimleri görülmeye başlar.",
                    Choice("Bu geleneği destekle", forcedNext: 190),
                    Choice("Üzerinde durma", forcedNext: 188)),

                Card(188, "Gül (Halktan)",
                    "Gül’ün çocuğu “anne” dışında yeni bir kelime söyler. Odanın yarısı ne " +
                    "dediğini anlamaz, diğer yarısı farklı bir kelime duyduğunu iddia eder.",
                    Choice("Gülümseyip kutla", forcedNext: 192),
                    Choice("Şaşkınlığını belli et", forcedNext: 189)),

                Card(189, "Anlatıcı",
                    "Sinyal günler içinde zayıflayıp tamamen kaybolur. Ne olduğu hiçbir zaman " +
                    "öğrenilemez.",
                    Choice("Kaydını arşivde tut", forcedNext: 192),
                    Choice("Konuyu kapat", forcedNext: 192),
                    variants: new[]
                    {
                        VariantIfFlag("mesaj_cozuldu_evet",
                            "Kod çözülünce mesajın uzak bir topluluktan gönderilmiş SOS çağrısı " +
                            "olduğu anlaşılır.",
                            Choice("Yardım göndermek için harekete geç", wealth: -1, authority: 1,
                                flagsAdd: Flags("uzak_topluluk_evet"), forcedNext: 192),
                            Choice("Mesafeyi koru", forcedNext: 190),
                            speaker: "İsmet (Telsizci)")
                    }),

                Card(190, "Anlatıcı",
                    "Haftalık toplantılar artık sığınağın olağan düzeninin bir parçasıdır. " +
                    "İnsanlar sorunlarını doğrudan burada dile getirir.",
                    Choice("Tartışmaya katıl", forcedNext: 193),
                    Choice("Bu kez yalnızca dinle", forcedNext: 191)),

                Card(191, "Anlatıcı",
                    "O gün olağan dışı hiçbir gelişme olmaz. Nöbet çizelgesi bile sakindir.",
                    Choice("Devriyeye çık", forcedNext: 192),
                    Choice("Dinlen", forcedNext: 192),
                    variants: new[]
                    {
                        VariantIfFlag("uzak_topluluk_evet",
                            "SOS çağrısının geldiği bölgeye ulaşmak tehlikelidir. Yolun bir kısmı " +
                            "enfekte bölgelerden geçmektedir.",
                            Choice("Ekibe kendin liderlik et", conditionalEffect: LeaderRisk(
                                leaderHealthDeltaWhenFalse: -3), forcedNext: 194),
                            Choice("Bir ekip gönder", forcedNext: 192),
                            speaker: "Mustafa (Asker)")
                    }),

                Card(192, "Anlatıcı",
                    "Sinyal zamanla tamamen kaybolur ve geride doğrulanabilir hiçbir iz bırakmaz.",
                    Choice("Devam et", forcedNext: 195),
                    Choice("Unut", forcedNext: 195),
                    variants: new[]
                    {
                        VariantIfFlag("vertak_yanki_evet",
                            "Vertak sinyalinin içinde önceki kayıtlarla bağlantılı yeni bir " +
                            "ayrıntı bulunur.",
                            Choice("Bulguyu paylaş", authority: -1,
                                counterDeltas: Counter(CounterPharmaArastirma, 1), forcedNext: 195),
                            Choice("Arşivde sakla",
                                counterDeltas: Counter(CounterPharmaArastirma, 1), forcedNext: 195))
                    }),

                Card(193, "Anlatıcı",
                    "Dış görevde olanlar geri döner. O akşam uzun zamandır ilk kez herkes aynı " +
                    "çatı altındadır.",
                    Choice("Dinlen", forcedNext: 196),
                    Choice("İşlere dön", forcedNext: 194)),

                Card(194, "İsmet (Telsizci)",
                    "İsmet’in tarih arşivi artık birkaç defterden çok daha fazlasıdır. Eski " +
                    "liderlerin kararları bile ayrı ayrı kaydedilmiştir.",
                    Choice("Kendi bildiklerini ekle", forcedNext: 198),
                    Choice("Arşivi ona bırak", forcedNext: 195)),

                Card(195, "Anlatıcı",
                    "*(zombi_komsuluk=evet ise)* Enfektelerle kurulan komşuluk ilk kez ciddi " +
                    "biçimde sınanır. Sınırda beklenmedik bir hareketlilik başlar.",
                    Choice("Sakin kalıp önce ne olduğunu anlamaya çalış", authority: 1, forcedNext: 198),
                    Choice("Sert biçimde karşılık ver", authority: -1, flagsAdd: Flags("zombi_komsuluk_gergin"),
                        forcedNext: 196)),

                Card(196, "Anlatıcı",
                    "Sığınakta yeni bir çocuk doğar. İsim koyma günü yıllar önce Gül’ün bebeğinde " +
                    "olduğu gibi yine küçük bir törene dönüşür.",
                    Choice("Törene katıl", forcedNext: 200),
                    Choice("Kısa bir tebrikle yetin", forcedNext: 197)),

                Card(197, "Kemal (Mühendis)",
                    "Kemal seni yeni kurduğu küçük elektrik şebekesinin başına götürür. Birkaç " +
                    "bölme artık birbirinden bağımsız enerji alabilmektedir.",
                    Choice("Ekibi tebrik et", forcedNext: 201),
                    Choice("Çalışmayı normal bir gelişme say", forcedNext: 198)),

                Card(198, "Ali (Halktan)",
                    "Ali’nin yetiştirdiği çırak artık kendi yanında birini eğitmeye hazırlanır. " +
                    "Bilgi ilk kez üçüncü ele geçmektedir.",
                    Choice("Bu gelişmeyi takdir et", forcedNext: 200),
                    Choice("Zamanın ne kadar geçtiğine şaşır", forcedNext: 199)),

                Card(199, "Mete (Asker)",
                    "Mete Karakol’la ilgili raporları önüne koyar. “Bize söyledikleriyle " +
                    "yaptıkları tam örtüşmüyor. Kendi hesapları olabilir.”",
                    Choice("Şüphesini araştır", flagsAdd: Flags("son_kusku_evet"), forcedNext: 201),
                    Choice("Karakol’a güven", forcedNext: 201)),

                Card(200, "Anlatıcı",
                    "Üçüncü dönemin bu noktasında sığınak artık çevresinden kopuk değildir; " +
                    "komşuları, ticaret yolları ve düşmanları vardır. Hikâye burada bitmez, " +
                    "yalnızca başka bir ölçeğe geçer.",
                    Choice("Devam et", forcedNext: 201),
                    Choice("Yeni döneme geç", forcedNext: 201)),
            };
        }
    }
}
