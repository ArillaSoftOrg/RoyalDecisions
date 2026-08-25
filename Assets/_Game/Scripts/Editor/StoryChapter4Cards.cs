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
                    "Yeni dönem sakin bir sabahla açılır. Necati eski bir radyoyu söküp önüne " +
                    "dizer. “Belki bundan hâlâ ses alırız.”",
                    Choice("Tamire yardım et", forcedNext: 104),
                    Choice("Kenardan izle", forcedNext: 102)),

                Card(102, "İsmet (Telsizci)",
                    "İsmet kulaklığını çıkarır. “Vertak sinyalleri son günlerde belirgin biçimde " +
                    "arttı. Bizi dinliyor olabilirler.”",
                    Choice("Yayını karart", flagsAdd: Flags("vertak_karartma_evet"), forcedNext: 104),
                    Choice("Frekansı açık bırak", forcedNext: 103)),

                Card(103, "Fatma (Halktan)",
                    "Fatma boş kalan duvara yeni resimler çizmeye başlar. Çocuklar da etrafına " +
                    "toplanır.",
                    Choice("Sen de bir şey ekle", forcedNext: 106),
                    Choice("Bir süre izle", forcedNext: 104)),

                Card(104, "Tarık (Halktan)",
                    "Tarık bu kez kapalı kapılar ardında değil, herkesin önünde konuşur. " +
                    "“Liderliği oylayalım. Kimin ne düşündüğü ortaya çıksın.”",
                    Choice("Oylamaya izin ver", flagsAdd: Flags("meydan_okuma_evet"), forcedNext: 105),
                    Choice("Toplantıyı dağıt", flagsAdd: Flags("gizli_gerginlik"), forcedNext: 105)),

                Card(105, "Anlatıcı",
                    "Tarık yasağa rağmen gizlice destek toplamaya başlar. Ömer bunu kısa sürede " +
                    "fark eder.",
                    Choice("Ömer’e takip ettir", authority: -1, forcedNext: 108),
                    Choice("Şimdilik görmezden gel", flagsAdd: Flags("ayaklanma_riski"), forcedNext: 106),
                    variants: new[]
                    {
                        VariantIfFlag("meydan_okuma_evet",
                            "Toplantı saatler sürer. Herkes ilk kez açıkça söz alabilmektedir.",
                            Choice("Kendi kararlarını açıkça savun", authority: 2, forcedNext: 108),
                            Choice("Dinlemeyi tercih et", authority: 1, forcedNext: 106))
                    }),

                Card(106, "Anlatıcı",
                    "İsmet birkaç gün sonra kötü haberi verir: Vertak konumunuzu belirlemiştir.",
                    Choice("Savunmayı hızla güçlendir", security: 1, authority: -1,
                        flagsAdd: Flags("vertak_yolda"), forcedNext: 107),
                    Choice("Panik yaratmadan bekle", flagsAdd: Flags("vertak_yolda"), forcedNext: 107),
                    variants: new[]
                    {
                        VariantIfFlag("vertak_karartma_evet",
                            "Sinyal kesilir. İsmet yine de rahat değildir. “Kayboldular mı, yoksa " +
                            "sadece sustular mı bilmiyorum.”",
                            Choice("İsmet’e güvenip konuyu kapat", authority: 1, forcedNext: 108),
                            Choice("Frekansı gizlice izlemeyi sürdür",
                                conditionalEffect: AlwaysLeaderHealth(-1), forcedNext: 108))
                    }),

                Card(107, "Sibel (Halktan)",
                    "Sibel’in piyano konserleri artık düzenli hâle gelmiştir. Dışarıdaki " +
                    "belirsizliğe rağmen o akşam da birkaç kişi sandalyeleri dizer.",
                    Choice("Konseri dinle", forcedNext: 110),
                    Choice("İşine dön", forcedNext: 108)),

                Card(108, "Ali (Halktan)",
                    "Ali artık genç bir yetişkindir ve ilk kez resmen “çırak nöbetçi” sayılır. " +
                    "Ömer ona gerçek bir vardiya çizelgesi verir.",
                    Choice("Ali’yi tebrik et", forcedNext: 111),
                    Choice("Bunu görevin doğal parçası say", forcedNext: 109)),

                Card(109, "Ömer (Gözcü)",
                    "Ömer aynı enfektenin artık düzenli aralıklarla çite geldiğini söyler. “Belli " +
                    "ki bizimle konuşmak istiyor.”",
                    Choice("Ona bir isim verip teması kişiselleştir", flagsAdd: Flags("zombi_isimlendirildi"), forcedNext: 112),
                    Choice("Mesafeyi koru", forcedNext: 110)),

                Card(110, "Sabiha (Erzakçı)",
                    "Sabiha haritada daha önce taranmamış bir bölgeyi gösterir. “Malzeme çıkabilir. " +
                    "Yol da temiz görünüyor.”",
                    Choice("Bölgeyi araştır", forcedNext: 112),
                    Choice("Bu kez çıkma", forcedNext: 111),
                    variants: new[]
                    {
                        VariantIfFlag("vertak_yolda",
                            "Yakında bir araç durur. İçindekiler silahsız görünse de Vertak işareti " +
                            "taşımaktadır.",
                            Choice("Kapıyı kontrollü aç", forcedNext: 112),
                            Choice("Herkesi silah başına geçir", forcedNext: 111))
                    }),

                Card(111, "Anlatıcı",
                    "Keşif ekibi kilitli eski bir depo bulur. İçeriden işe yarar miktarda erzak " +
                    "çıkar.",
                    Choice("Erzağı hemen dağıt", wealth: 2, authority: 1, forcedNext: 112),
                    Choice("İhtiyaç için depola", wealth: 2, forcedNext: 112),
                    variants: new[]
                    {
                        VariantIfFlag("vertak_yolda",
                            "Vertak temsilcisi ayrılırken tek bir cümle bırakır: “Gözlemleneceksiniz.”",
                            Choice("Tehdidi ciddiye alıp güvenliği artır", security: 1, authority: -1,
                                flagsAdd: Flags("vertak_gozlem"), forcedNext: 115),
                            Choice("Gözdağı sayıp rutine dön", flagsAdd: Flags("vertak_gozlem"),
                                forcedNext: 115))
                    }),

                Card(112, "Aziz (Tarımcı)",
                    "Aziz yeni hasattan farklı bir yemek dener. Tadı konusunda kendisi bile emin " +
                    "değildir.",
                    Choice("İlk lokmayı sen al", forcedNext: 116),
                    Choice("Başkalarının denemesini bekle", forcedNext: 113)),

                Card(113, "Zeynep (Doktor)",
                    "Revir kısa sürede mide bulantısı ve ateş şikâyetleriyle dolar. Zeynep ortak " +
                    "bir gıda zehirlenmesinden şüphelenir.",
                    Choice("Yiyecekleri test ettir", wealth: -1, people: 1, forcedNext: 116),
                    Choice("Kendiliğinden geçmesini bekle", conditionalEffect: ReignIfCritical(
                        StatType.People, atOrBelow: 3, deltasWhenSafe: default,
                        resetStat: StatType.People), forcedNext: 114)),

                Card(114, "Cem & Yusuf",
                    "Cem ile Yusuf’un uydurduğu oyun artık sığınağın eski alışkanlıklarından biri " +
                    "olmuştur. Yeni gelenler bile kuralları bilir.",
                    Choice("Bir tur oyna", forcedNext: 117),
                    Choice("Kenardan izle", forcedNext: 115)),

                Card(115, "Kemal (Mühendis)",
                    "Kemal yeni bir yapısal rapor getirir. “Yama yaparak gidiyoruz. İstersek bu " +
                    "kez kökten çözebiliriz.”",
                    Choice("Kapsamlı onarım başlat", flagsAdd: Flags("onarim_tam"), forcedNext: 118),
                    Choice("Yalnızca zorunlu yerleri düzelt", forcedNext: 116)),

                Card(116, "Anlatıcı",
                    "Hızlı yamalar işe yarar. Kemal yine de bazı bölgelerin ileride yeniden sorun " +
                    "çıkaracağını not eder.",
                    Choice("Sorunlu noktaları takip listesine al", security: 1, forcedNext: 118),
                    Choice("Şimdilik yeterli say", security: 1, forcedNext: 118),
                    variants: new[]
                    {
                        VariantIfFlag("onarim_tam",
                            "Aylar süren çalışma sonunda sığınak baştan aşağı güçlendirilir. " +
                            "Kemal ilk kez “Bu bina artık uzun süre gider” der.",
                            Choice("Ekiple birlikte kutla", security: 3,
                                conditionalEffect: AlwaysLeaderHealth(-1), forcedNext: 118),
                            Choice("Dinlenmeden sıradaki işe geç", security: 3,
                                conditionalEffect: AlwaysLeaderHealth(-1), forcedNext: 118))
                    }),

                Card(117, "Anlatıcı",
                    "Sığınağın ortak alanında küçük bir takas pazarı kurulmaya başlanır. İnsanlar " +
                    "ihtiyaç fazlasını birbirleriyle değiştirir.",
                    Choice("Pazara katıl", forcedNext: 120),
                    Choice("Uzaktan gözlemle", forcedNext: 118)),

                Card(118, "Anlatıcı",
                    "*(ayaklanma_riski=evet ise)* Gizlice büyüyen huzursuzluk sonunda patlar. " +
                    "Kalabalığın içinde sana doğru ilerleyenler vardır.",
                    Choice("Karşılarına çık", conditionalEffect: LeaderRisk(
                        leaderHealthDeltaWhenFalse: -3, deltasWhenFalse: new StatDeltas(2, 0, 0, 0)),
                        forcedNext: 120),
                    Choice("Güvenli bir yere çekil", authority: -2, forcedNext: 119)),

                Card(119, "Anlatıcı",
                    "Olayların ardından sığınak sessizleşir. O akşam birkaç kişi konuşmak için " +
                    "yanına gelir; kimse ne diyeceğini tam bilemez.",
                    Choice("Onlarla otur", forcedNext: 122),
                    Choice("Yalnız kal", forcedNext: 120)),

                Card(120, "Anlatıcı",
                    "Kapıda yaralı bir kadın belirir. Üzerindeki eski kimlik, bir dönem Vertak " +
                    "için çalıştığını gösterir.",
                    Choice("İçeri al", flagsAdd: Flags("eski_vertak_calisan"), forcedNext: 124),
                    Choice("Sığınağa sokma", forcedNext: 121)),

                Card(121, "Anlatıcı",
                    "Kadın giderken kapının yakınına katlanmış bir not bırakır. İçinde Vertak " +
                    "hakkında parçalı bilgiler vardır.",
                    Choice("Notu hemen incele", forcedNext: 122),
                    Choice("Arşive kaldırıp sonra bak", forcedNext: 122),
                    variants: new[]
                    {
                        VariantIfFlag("eski_vertak_calisan",
                            "İsmet kadını uzun süre sorgular. Hikâyesinde açık bir çelişki bulamaz " +
                            "ama güvenmek için de erken olduğunu söyler.",
                            Choice("Söylediklerine güven", authority: -1, forcedNext: 124),
                            Choice("Ayrıntılı sorgulamayı sürdür", authority: -1,
                                counterDeltas: Counter(CounterPharmaArastirma, 2),
                                flagsAdd: Flags("icerden_bilgi"), forcedNext: 124),
                            speaker: "İsmet (Telsizci)")
                    }),

                Card(122, "Ali (Halktan)",
                    "Ali artık tam yetkili bir nöbetçidir. Ömer vardiya çizelgesinde adının " +
                    "yanındaki “çırak” notunu siler.",
                    Choice("Ali’yi tebrik et", forcedNext: 124),
                    Choice("Tören yapmadan göreve devam et", forcedNext: 123)),

                Card(123, "Zeynep (Doktor)",
                    "Zeynep revirdeki defterleri gösterir. “Bir gün burada olmayacağım. Birini " +
                    "şimdiden yetiştirmeliyiz.”",
                    Choice("Atilla’yı yetiştir", flagsAdd: Flags("halef_atilla"), forcedNext: 125),
                    Choice("Sibel’i yetiştir", flagsAdd: Flags("halef_sibel"), forcedNext: 124)),

                Card(124, "Anlatıcı",
                    "Yıllar içinde biriken kitaplar, notlar ve eski dergiler için ayrı bir köşe " +
                    "oluşmuştur. İnsanlar buraya artık “kütüphane” demektedir.",
                    Choice("Arşive bir şey ekle", forcedNext: 127),
                    Choice("Olduğu gibi bırak", forcedNext: 125)),

                Card(125, "Ömer (Gözcü)",
                    "Ömer, konuşan enfektenin son günlerde hep aynı yöne işaret ettiğini fark " +
                    "eder. “Bizi bir yere götürmeye çalışıyor olabilir.”",
                    Choice("İşaret ettiği yönü takip et", flagsAdd: Flags("zombi_takip"), forcedNext: 128),
                    Choice("Bu kez peşinden gitme", forcedNext: 126)),

                Card(126, "Anlatıcı",
                    "Konuşan enfekte birkaç gün sonra gelmeyi bırakır. Nereye gittiğini kimse " +
                    "öğrenemez.",
                    Choice("Kaydını tut", forcedNext: 127),
                    Choice("Konuyu kapat", forcedNext: 127),
                    variants: new[]
                    {
                        VariantIfFlag("zombi_takip",
                            "İzler eski bir Vertak tesisine çıkar. Dışarıdan terk edilmiş görünür.",
                            Choice("Tesise gir", authority: -1, flagsAdd: Flags("vertak_tesis_bulundu"),
                                counterDeltas: Counter(CounterPharmaArastirma, 2), forcedNext: 129),
                            Choice("Konumu işaretleyip geri dön",
                                flagsAdd: Flags("vertak_tesis_bulundu"),
                                counterDeltas: Counter(CounterPharmaArastirma, 2), forcedNext: 129))
                    }),

                Card(127, "Anlatıcı",
                    "Halef eğitimi tamamlanır. Revirde artık Zeynep dışında gerektiğinde " +
                    "sorumluluk alabilecek ikinci bir sağlıkçı vardır. *(ikinci_saglikci=evet)*",
                    Choice("Zeynep’le birlikte çalışsın", people: 1, flagsAdd: Flags("ikinci_saglikci"),
                        forcedNext: 131),
                    Choice("Kendi vardiyasını yönetsin", people: 1,
                        conditionalEffect: AlwaysLeaderHealth(-1), flagsAdd: Flags("ikinci_saglikci"),
                        forcedNext: 131)),

                Card(128, "Sibel (Halktan)",
                    "Sibel’in konserlerine çevredeki birkaç kişi de gelmeye başlar. İlk kez " +
                    "kapının dışından gelenler yalnızca ticaret veya yardım için değildir.",
                    Choice("Kalabalığa katıl", forcedNext: 132),
                    Choice("Kenardan izle", forcedNext: 129)),

                Card(129, "Anlatıcı",
                    "*(vertak_gozlem=evet ise)* Zamanla Vertak’ın “gözlem” dediği şeyin sürekli " +
                    "takip olduğu anlaşılır. İnsanlar izlendiğini bildikçe huzursuzlanır.",
                    Choice("Durumu sakin biçimde açıkla", authority: 1, forcedNext: 133),
                    Choice("Tehdidi olduğu gibi anlat", conditionalEffect: ReignIfCritical(
                        StatType.Authority, atOrBelow: 3, deltasWhenSafe: new StatDeltas(-1, 0, 0, 0),
                        resetStat: StatType.Authority), forcedNext: 130)),

                Card(130, "Gül (Halktan)",
                    "Gül’ün çocuğu ilk kelimelerini söylemeye başlar. Söylediği şeyin ne olduğu " +
                    "konusunda herkes farklı bir şey duyar.",
                    Choice("Onlarla birlikte kutla", forcedNext: 133),
                    Choice("İşine devam et", forcedNext: 131)),

                Card(131, "Mustafa (Asker)",
                    "Mustafa haritanın üzerine geniş bir yay çizer. “Şimdiye kadar gördüğümüz " +
                    "hiçbir sürü buna benzemiyordu. Doğrudan buraya geliyor.”",
                    Choice("Herkesi savunmaya seferber et", flagsAdd: Flags("kriz_seferberlik"), forcedNext: 134),
                    Choice("Tahliyeyi başlat", forcedNext: 132)),

                Card(132, "Mustafa / Mete",
                    "Sürü artık çıplak gözle seçilebilecek kadar yakındır. Mustafa ile Mete " +
                    "savunma noktalarına geçer.",
                    Choice("Cepheye çıkıp komutayı al", flagsAdd: Flags("kriz_cephede"), conditionalEffect: LeaderRisk(
                        leaderHealthDeltaWhenFalse: -3), forcedNext: 135),
                    Choice("Komutayı geriden yürüt", forcedNext: 133)),

                Card(133, "Anlatıcı",
                    "Tahliye emri geri çekilince insanlar neye güveneceğini şaşırır.",
                    Choice("Kararın sorumluluğunu üstlen", authority: -1, forcedNext: 136),
                    Choice("Konuyu açıklamadan geç", authority: -1, forcedNext: 136),
                    variants: new[]
                    {
                        new CardVariant(
                            new CardConditions(Flags("kriz_seferberlik", "kriz_cephede"), null, null),
                            bodyText: "Savunma hattı kayıp verir ama dayanır. Sürü geri çekilirken " +
                                "içeride ilk kez zafer sesleri yükselir.",
                            leftChoice: Choice("Önce kayıpları an", security: -1, authority: 3,
                                forcedNext: 136),
                            rightChoice: Choice("Zaferi kutla", security: -1, authority: 3,
                                forcedNext: 136)),
                        VariantIfFlag("kriz_seferberlik",
                            "Sürü durdurulur ama emirler birbirine girer; gereğinden fazla hasar " +
                            "oluşur.",
                            Choice("Komuta zincirini sorgula", security: -2, authority: 1,
                                forcedNext: 136),
                            Choice("Kriz geçtiği için konuyu kapat", security: -2, authority: 1,
                                forcedNext: 136)),
                        VariantIfFlag("kriz_cephede",
                            "Tahliye tamamlanır. Herkes çıkamaz; geride bırakılanların adı uzun " +
                            "süre konuşulur.",
                            Choice("Geride kalanları an", security: -2, wealth: -1, forcedNext: 136),
                            Choice("Hayatta kalanlara odaklan", security: -2, wealth: -1,
                                forcedNext: 136))
                    }),

                Card(134, "Anlatıcı",
                    "Büyük krizden sonra ilk kez alarm çalmadan bir gün geçer. İnsanlar ne " +
                    "yapacağını şaşırmış gibidir.",
                    Choice("Bir süre kalabalığın içinde kal", forcedNext: 137),
                    Choice("Yalnız kal", forcedNext: 135)),

                Card(135, "İsmet (Telsizci)",
                    "*(pharma_arastirma yüksekse)* İsmet topladığı belgeleri masaya dizer. “Artık " +
                    "Vertak’ın ne yaptığını biliyoruz. Onlar da bizim bildiğimizi biliyor " +
                    "olabilir.”",
                    Choice("Doğrudan yüzleş", flagsAdd: Flags("vertak_yuzlesildi"), forcedNext: 139),
                    Choice("Temastan kaçın", forcedNext: 136),
                    variants: new[]
                    {
                        new CardVariant(RequiresCounterAtLeast(CounterPharmaArastirma, 3),
                            bodyText: "Vertak'ın gerçek yüzü gizlenemiyor. Yüzleş mi, kaçın mı?",
                            leftChoice: Choice("Araştırmayı sürdür", flagsAdd: Flags("vertak_yuzlesildi"), forcedNext: 139),
                            rightChoice: Choice("Bu dosyayı kapat", forcedNext: 136))
                    }),

                Card(136, "Anlatıcı",
                    "Vertak’la açık bir anlaşma kurulmaz. Sığınak bağımsız kalır ama tehdidin ne " +
                    "kadar yakında olduğu belirsizdir.",
                    Choice("Araştırmayı gizlice sürdür", authority: -1,
                        counterDeltas: Counter(CounterPharmaArastirma, 1), forcedNext: 139),
                    Choice("Konuyu tamamen kapat", forcedNext: 139),
                    variants: new[]
                    {
                        VariantIfFlag("vertak_yuzlesildi",
                            "Vertak, koruması altına girmenizi teklif eder. Daha güvenli bir düzen " +
                            "vaat eder; karşılığında bağımsızlığınızdan vazgeçmenizi ister.",
                            Choice("Koruma teklifini kabul et", security: 1, authority: -1,
                                forcedNext: 139),
                            Choice("Bağımsız kal", authority: 1, forcedNext: 139))
                    }),

                Card(137, "Anlatıcı",
                    "Sığınakta uzun zamandır ilk kez herkesin katıldığı geniş bir toplantı " +
                    "düzenlenir. Sorunlardan çok geleceğin nasıl yönetileceği konuşulur.",
                    Choice("Söz al", forcedNext: 140),
                    Choice("Bu kez dinle", forcedNext: 138)),

                Card(138, "Ömer (Gözcü)",
                    "*(ateskes=evet ise)* Ömer, konuşan enfektelerden yeni bir teklif getirir. " +
                    "Ateşkesin ardından iki taraf arasında açık bir sınır belirlemek " +
                    "istemektedirler.",
                    Choice("Sınır anlaşmasını kabul et", authority: 1, forcedNext: 141),
                    Choice("Ateşkesi koruyup mesafeyi sürdür", forcedNext: 139)),

                Card(139, "Ali (Halktan)",
                    "Ali artık sığınağın en genç uzman üyelerinden biridir. İnsanlar karar " +
                    "verirken onun fikrini de sormaya başlamıştır.",
                    Choice("Başardıklarını açıkça takdir et", forcedNext: 143),
                    Choice("Onu diğer uzmanlardan farklı görme", forcedNext: 140)),

                Card(140, "Mustafa / Mete",
                    "Mustafa ile Mete birlikte gelir. İkisinin de yüzündeki ifade yeterince " +
                    "açıktır: büyük bir tehdit daha yaklaşıyordur.",
                    Choice("Ön hatta çık", conditionalEffect: LeaderRisk(leaderHealthDeltaWhenFalse: -4),
                        forcedNext: 142),
                    Choice("Savunmayı geriden yönet", authority: -1, forcedNext: 141)),

                Card(141, "Anlatıcı",
                    "Tehlike geçtikten sonra sığınak yine ayaktadır. Birkaç saatliğine kimse yeni " +
                    "bir krizden söz etmez.",
                    Choice("Kendine biraz zaman ayır", forcedNext: 143),
                    Choice("Hemen işlere dön", forcedNext: 142)),

                Card(142, "İsmet (Telsizci)",
                    "İsmet yıllardır tuttuğu notları düzenlemeye başlar. “Bunları birileri " +
                    "okumalı. Yoksa burada ne yaşandığını kimse bilmeyecek.”",
                    Choice("Kendi anılarını da anlat", authority: 1, forcedNext: 144),
                    Choice("Kaydı ona bırak", forcedNext: 143)),

                Card(143, "Anlatıcı",
                    "O akşam hayatta kalan kadronun tamamı aynı masadadır. Böyle anların ne kadar " +
                    "seyrek olduğunu herkes bilir.",
                    Choice("Hepsine teşekkür et", forcedNext: 146),
                    Choice("Sessizce onlarla otur", forcedNext: 144)),

                Card(144, "Aziz (Tarımcı)",
                    "Emine Teyze’nin yıllar önce başlattığı bahçe yeniden çiçek açar. Aziz onun " +
                    "bıraktığı düzeni sürdürmektedir.",
                    Choice("Bahçede biraz kal", forcedNext: 147),
                    Choice("Yoluna devam et", forcedNext: 145)),

                Card(145, "Necati (Halktan)",
                    "Necati eski dostlarından söz eder. İsimlerin çoğunu artık yalnızca o " +
                    "hatırlamaktadır.",
                    Choice("Hikâyelerini dinle", forcedNext: 149),
                    Choice("Konuyu uzatma", forcedNext: 146)),

                Card(146, "Aziz (Tarımcı)",
                    "Aziz yeni hasattan başka bir tarif denemektedir. Bu kez senden de fikir " +
                    "ister.",
                    Choice("Yardım et", forcedNext: 148),
                    Choice("Onu kendi hâline bırak", forcedNext: 147)),

                Card(147, "Anlatıcı",
                    "Sığınağın nüfusu uzun süredir ilk kez büyük dalgalanmalar yaşamadan sabit " +
                    "kalır. Bu, eskiden sıradan sayılacak kadar basit bir başarıdır.",
                    Choice("Bu istikrarın değerini vurgula", forcedNext: 151),
                    Choice("Günlük hayatın parçası say", forcedNext: 148)),

                Card(148, "Sabiha (Erzakçı)",
                    "Sabiha artık yalnızca yakın çevreyle değil, birkaç farklı toplulukla düzenli " +
                    "takas yapmaktadır.",
                    Choice("Ticaret ağını destekle", wealth: 1, forcedNext: 152),
                    Choice("Büyümeyi sınırlı tut", forcedNext: 149)),

                Card(149, "İsmet (Telsizci)",
                    "İsmet arşive yeni kayıtlar ekler. Boş kalan birkaç sayfayı sana uzatır.",
                    Choice("Sen de bir kayıt ekle", forcedNext: 150),
                    Choice("Kaydı ona bırak", forcedNext: 150)),

                Card(150, "Anlatıcı",
                    "İkinci büyük dönemin sonunda sığınak artık yalnızca hayatta kalmaya çalışan " +
                    "bir yer değildir. K1’den beri biriken liderlik değişimleri, ittifaklar, " +
                    "Vertak’la kurulan ilişki ve konuşan enfektelerle verilen kararlar burada " +
                    "birlikte ağırlık kazanır. Bu bir final değildir.",
                    Choice("Devam et", forcedNext: 151),
                    Choice("Yeni döneme geç", forcedNext: 151)),
            };
        }
    }
}
