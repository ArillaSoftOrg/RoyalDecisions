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
                    "Yeni bir mevsim başlar. Hava değişirken sığınağın günlük düzeni şaşırtıcı " +
                    "ölçüde aynı kalır.",
                    Choice("Mevsimin gelişini küçük bir kutlamayla karşıla", forcedNext: 203),
                    Choice("Günü olağan şekilde geçir", forcedNext: 202)),

                Card(202, "Anlatıcı",
                    "Günler geçer, Karakol’dan şüpheyi doğrulayacak bir hareket gelmez. Mete’nin " +
                    "kaygısı şimdilik yersiz görünür.",
                    Choice("Rahatla", authority: 1, forcedNext: 205),
                    Choice("Devam et", authority: 1, forcedNext: 205),
                    variants: new[]
                    {
                        VariantIfFlag("son_kusku_evet",
                            "Mete’nin şüphesi kısmen doğrulanır: Karakol, ittifakları kendi " +
                            "çıkarına göre yönlendirmeyi planlamaktadır; henüz açık bir hamle " +
                            "yapmamıştır.",
                            Choice("Kadroyu durumdan haberdar et", authority: -1,
                                flagsAdd: Flags("karakol_niyet_bilindi"), forcedNext: 205),
                            Choice("Bilgiyi şimdilik dar bir çevrede tut",
                                flagsAdd: Flags("karakol_niyet_bilindi"), forcedNext: 205))
                    }),

                Card(203, "Veli (Halktan)",
                    "Veli sonunda kendi alanını seçmeye hazırlanır. Kemal’in atölyesiyle İsmet’in " +
                    "telsiz odası arasında gidip gelmektedir.",
                    Choice("Mühendisliğe yönelmesini destekle", flagsAdd: Flags("veli_yol_muhendislik"),
                        forcedNext: 205),
                    Choice("Telsizciliği kendi başına seçmesine izin ver",
                        flagsAdd: Flags("veli_yol_telsizcilik"), forcedNext: 204)),

                Card(204, "Mustafa / Mete",
                    "Mustafa ile Mete ufukta alışılmadık bir hareketlilik fark eder. Ne " +
                    "yaklaştığını henüz seçememektedirler.",
                    Choice("Erken uyarı düzeni kur", security: -1, flagsAdd: Flags("erken_uyari_evet"),
                        forcedNext: 206),
                    Choice("Daha fazla bilgi gelene kadar izle", forcedNext: 205)),

                Card(205, "Anlatıcı",
                    "Karakol’dan dağınık haberler gelmeye başlar: içeride yönetim kavgası çıkmış, " +
                    "eski düzen çözülmektedir.",
                    Choice("Değişimi yakından değerlendir", flagsAdd: Flags("karakol_yeni_yonetim"),
                        forcedNext: 206),
                    Choice("İç işlerine karışma", forcedNext: 206)),

                Card(206, "Anlatıcı",
                    "Karakol’daki belirsizliğe karşı sığınağın sınırları sıkılaştırılır.",
                    Choice("Yeni önlemleri kadroya açıkla", security: 1, forcedNext: 208),
                    Choice("Önlemleri sessizce uygula", security: 1, forcedNext: 208),
                    variants: new[]
                    {
                        VariantIfFlag("karakol_yeni_yonetim",
                            "Karakol’daki yeni yönetimle önceki dönemden daha dengeli bir ilişki " +
                            "kurulur.",
                            Choice("Şartları yazılı anlaşmaya bağla", wealth: 1, authority: 1,
                                forcedNext: 207),
                            Choice("Sözlü mutabakatla yetin", wealth: 1, authority: 1, forcedNext: 207))
                    }),

                Card(207, "Ali (Halktan)",
                    "Ali’nin yetiştirdiği çırak ilk görevini tek başına tamamlayıp geri döner. " +
                    "Artık yalnızca bir öğrenciden söz etmek zordur.",
                    Choice("Başarısını takdir et", forcedNext: 210),
                    Choice("Görevin doğal sonucu gibi karşıla", forcedNext: 208)),

                Card(208, "Kemal (Mühendis)",
                    "Sığınak büyüdükçe eski yapıya eklenen bölmeler birbirini zorlamaya başlar. " +
                    "Kemal bir taşıyıcı noktadaki sorunu gösterir. “Bunu ertelemek artık kumar.”",
                    Choice("Acil müdahale başlat", wealth: -2, security: 1, forcedNext: 211),
                    Choice("Riski göze alıp bekle", conditionalEffect: ReignIfCritical(
                        StatType.Security, atOrBelow: 3, deltasWhenSafe: new StatDeltas(0, 0, -1, 0),
                        resetStat: StatType.Security), forcedNext: 209)),

                Card(209, "Anlatıcı",
                    "Kriz sığınağı hazırlıksız yakalar. Zarar sınırlanır ama bunun bedeli ağır olur.",
                    Choice("Hazırlıksızlığın sorumluluğunu kabul et", people: -1, authority: -1,
                        forcedNext: 211),
                    Choice("Kararı savunup hızla toparlanmaya geç", people: -1, authority: -1,
                        forcedNext: 211),
                    variants: new[]
                    {
                        VariantIfFlag("erken_uyari_evet",
                            "Yaklaşan kriz hazırlıklar sayesinde beklenenden daha hafif atlatılır. " +
                            "Mustafa’nın kurduğu düzen ilk alarmda çalışır.",
                            Choice("Mustafa’nın hazırlığını özellikle takdir et", authority: 1,
                                forcedNext: 211),
                            Choice("Başarıyı bütün ekibe mal et", authority: 1, forcedNext: 211))
                    }),

                Card(210, "Anlatıcı",
                    "Krizden sonra insanlar birbirlerinin işine kendiliğinden yardım etmeye " +
                    "başlar. Birkaç gün boyunca görev listesine bakmaya bile gerek kalmaz.",
                    Choice("Bu dayanışmayı birlikte kutla", forcedNext: 212),
                    Choice("Sessizce sürmesine izin ver", forcedNext: 211)),

                Card(211, "Anlatıcı",
                    "Nöbet günü olaysız geçer. Çitin ötesinde yalnızca rüzgâr ve uzaktaki " +
                    "hareketler vardır.",
                    Choice("Devam et", forcedNext: 212),
                    Choice("İlerle", forcedNext: 212),
                    variants: new[]
                    {
                        VariantIfFlag("zombi_komsuluk",
                            "“Lider” çite gelir ve bu kez uzun, parçalı cümlelerle bir şey " +
                            "anlatmaya çalışır. Söylediğinin önemli olduğu bellidir.",
                            Choice("Zeynep’i çağırıp birlikte dinle", forcedNext: 213),
                            Choice("Onu tek başına dinle", forcedNext: 212),
                            speaker: "Ömer (Gözcü)")
                    }),

                Card(212, "İsmet (Telsizci)",
                    "İsmet’in arşivi artık yalnızca onun işi sayılmaz. İnsanlar kendi notlarını, " +
                    "haritalarını ve hatıralarını da buraya bırakmaktadır.",
                    Choice("Arşive kendi katkını ekle", forcedNext: 215),
                    Choice("Kaydı İsmet’e bırak", forcedNext: 213)),

                // Speaker: whichever of the twins leans toward the security/decision track — Ali if
                // ali_yol_savunma was chosen at K151, Veli otherwise (base card).
                Card(213, "Veli (Halktan)",
                    "Yeni nesilden biri ilk kez resmî karar toplantısında masaya oturur. Bu kez " +
                    "yalnızca dinleyen bir çırak değildir.",
                    Choice("Görüşünü açıkça söylemesini iste", authority: 1, forcedNext: 215),
                    Choice("İlk toplantıda gözlemlemesine izin ver", forcedNext: 214),
                    variants: new[]
                    {
                        VariantIfFlag("ali_yol_savunma", null, null, null, speaker: "Ali (Halktan)")
                    }),

                Card(214, "Sabiha (Erzakçı)",
                    "Sabiha’nın kurduğu ticaret ağı artık birden fazla topluluğu birbirine " +
                    "bağlamaktadır. Yeni bir rota daha eklemek mümkündür ama ağ büyüdükçe denetim " +
                    "zorlaşır.",
                    Choice("Ağı daha da genişlet", wealth: 1, flagsAdd: Flags("ticaret_agi_genis"), forcedNext: 216),
                    Choice("Mevcut ölçekte tut", forcedNext: 215)),

                Card(215, "Karakol Krizi",
                    "Karakol’daki kriz doğrudan sığınağın çevresine sıçrar. Silahlı grupların " +
                    "hareket ettiği haberi gelir ve hızlı karar vermek gerekir.",
                    Choice("Duruma kendin müdahale et", conditionalEffect: LeaderRisk(leaderHealthDeltaWhenFalse: -3),
                        forcedNext: 218),
                    Choice("Müdahaleyi ekibe bırak", forcedNext: 216)),

                Card(216, "Anlatıcı",
                    "Karakol’daki hareketlilik yatışınca sığınağa yeniden gündelik sessizlik " +
                    "döner.",
                    Choice("Bir gün dinlen", forcedNext: 218),
                    Choice("İşlere dön", forcedNext: 217)),

                Card(217, "Aziz (Tarımcı)",
                    "Aziz genişleyen tarlaları gösterir. “Hava böyle giderse rekor kırabiliriz. " +
                    "Ama sonuna kadar zorlarsak bir terslikte daha çok kaybederiz.”",
                    Choice("Verimi zorlayıp riske gir", flagsAdd: Flags("hasat_riskli"), forcedNext: 219),
                    Choice("Güvenli yöntemle ilerle", forcedNext: 218)),

                Card(218, "Anlatıcı",
                    "Hasat beklendiği gibi gelir: büyük değildir ama kayıp da yoktur.",
                    Choice("Aziz’in planını takdir et", wealth: 2, forcedNext: 221),
                    Choice("Sonucu olağan kabul et", wealth: 2, forcedNext: 221),
                    variants: new[]
                    {
                        new CardVariant(
                            new CardConditions(Flags("hasat_riskli", "ali_yol_tarim"), null, null),
                            bodyText: "Ali’nin tarım bilgisiyle alınan risk karşılığını verir; " +
                                "depolar yıllardır görülmemiş ölçüde dolar.",
                            leftChoice: Choice("Hasadı şenlikle kutla", wealth: 4, forcedNext: 221),
                            rightChoice: Choice("Fazlayı doğrudan depola", wealth: 4, forcedNext: 221)),
                        VariantIfFlag("hasat_riskli",
                            "Hava son anda döner. Ürünün yalnızca bir kısmı kurtarılabilir.",
                            Choice("Kurtarılan ürünle yetin", wealth: 1, forcedNext: 221),
                            Choice("Gelecek sezon aynı yöntemi yeniden denemeyi planla", wealth: 1,
                                forcedNext: 221))
                    }),

                Card(219, "Anlatıcı",
                    "Depolar ilk kez sığınağın ihtiyacından fazlasını verir. Sabiha, artan " +
                    "erzağın başka topluluklarla düzenli takasa çıkarılmasını önerir.",
                    Choice("İlk büyük dış satışı kutla", forcedNext: 223),
                    Choice("Fazlayı verirken temkinli davran", forcedNext: 220)),

                Card(220, "İsmet (Telsizci)",
                    "*(pharma_arastirma ve K135-136'daki karara göre)* Yıllardır süren Vertak " +
                    "meselesi sonunda net bir biçim alır: ya içeriden çözülür ya da gücünü " +
                    "kaybedip bölgeden çekilir. İlk kez adı günlük bir tehdit gibi anılmaz.",
                    Choice("Tehlikenin geçtiğini kabul et", authority: 2, forcedNext: 223),
                    Choice("Yine de savunmayı gevşetme", security: 1, forcedNext: 221)),

                Card(221, "Anlatıcı",
                    "Sığınaktaki en yaşlı kişi gençleri etrafına toplayıp ilk yılları anlatır. " +
                    "Bazıları anlattığı olaylar yaşanırken henüz doğmamıştır.",
                    Choice("Oturup birlikte dinle", forcedNext: 224),
                    Choice("Günlük işine dön", forcedNext: 222)),

                Card(222, "Anlatıcı",
                    "Yeni yetişenlerle eski kuşak arasında ilk kez açık bir değer çatışması " +
                    "yaşanır. Mesele tek bir karar değil, sığınağın bundan sonra nasıl " +
                    "yönetileceğidir.",
                    Choice("Ortak bir karar arayın", authority: 1, forcedNext: 226),
                    Choice("Son sözü otoriteyle ver", conditionalEffect: ReignIfCritical(
                        StatType.Authority, atOrBelow: 3, deltasWhenSafe: new StatDeltas(-1, 0, 0, 0),
                        resetStat: StatType.Authority), forcedNext: 223)),

                Card(223, "Anlatıcı",
                    "Tartışma çözülmüş olsa da etkisi hemen geçmez. Sığınak bir hafta boyunca " +
                    "alınan kararın havasını taşır.",
                    Choice("İnsanların arasında kal", forcedNext: 226),
                    Choice("Bir süre yalnız kal", forcedNext: 224)),

                Card(224, "Anlatıcı",
                    "İlk günkü sığınakla bugünkü yer arasında neredeyse yalnızca duvarların adı " +
                    "ortaktır. Ali ile Veli’nin yolları, Karakol’la kurulan ilişki ve Vertak’tan " +
                    "kalan izler artık bu hayatın parçasıdır.",
                    Choice("Değişimi arşive kaydet", authority: 1, forcedNext: 225),
                    Choice("Üzerinde konuşmadan kabul et", forcedNext: 225)),

                Card(225, "Anlatıcı",
                    "Yıllar içinde kendiliğinden bir “gelenek günü” oluşmuştur. İlk dönemden beri " +
                    "yaşamış olanlar ve artık aranızda bulunmayanlar o gün isimleriyle anılır; " +
                    "Necati de onlardan biridir.",
                    Choice("Anmaya katıl", forcedNext: 229),
                    Choice("Kenardan izle", forcedNext: 226)),

                Card(226, "Anlatıcı",
                    "Kemal’in yaptığı işler artık tek tek projeler olmaktan çıkmıştır. Bölmeler, " +
                    "güneş panelleri, onarımlar ve genişleme sığınağın kalıcı altyapısına " +
                    "dönüşmüştür.",
                    Choice("Yaptıklarının değerini ona söyle", authority: 1, forcedNext: 230),
                    Choice("Bunları artık düzenin doğal parçası say", forcedNext: 227)),

                Card(227, "Anlatıcı",
                    "“Lider” son kez çitin önünde belirir. Bu kez birkaç kelimeyi açıkça " +
                    "söyleyebilir ama mesajının uyarı mı, veda mı yoksa teklif mi olduğu henüz " +
                    "anlaşılmaz.",
                    Choice("Sonuna kadar dinle", flagsAdd: Flags("zombi_finali_dinlendi"), forcedNext: 230),
                    Choice("Mesafeyi koru", forcedNext: 228)),

                Card(228, "Anlatıcı",
                    "“Lider” bir süre çite yaslanır, sonra tek kelime etmeden uzaklaşır. Mesajın " +
                    "ne olduğu öğrenilemez.",
                    Choice("Karşılaşmayı kayda geçir", forcedNext: 230),
                    Choice("Konuyu kapat", forcedNext: 230),
                    variants: new[]
                    {
                        VariantIfFlag("zombi_finali_dinlendi",
                            "Parçalar bir araya gelince mesaj anlaşılır: enfekte topluluğu kendi " +
                            "içinde bölünmektedir ve “Lider” yaklaşan ayrışma konusunda sizi " +
                            "uyarmaktadır.",
                            Choice("Kadroyu olası çatışmaya hazırla", security: 1,
                                flagsAdd: Flags("zombi_son_mesaj"), forcedNext: 230),
                            Choice("İkinci bir işaret gelene kadar bekle",
                                flagsAdd: Flags("zombi_son_mesaj"), forcedNext: 230))
                    }),

                Card(229, "Anlatıcı",
                    "Akşam olduğunda sığınak ilk günkü gibi geçici bir barınak değil, insanların " +
                    "geri döndüğü bir ev gibi görünür.",
                    Choice("Bir süre oturup bunu düşün", forcedNext: 231),
                    Choice("Düşünmeden günlük hayatına devam et", forcedNext: 230)),

                Card(230, "Mustafa / Mete",
                    "Yıllardır ertelenen, bastırılan ve çözülen gerilimlerin bir kısmı aynı anda " +
                    "yeniden yüzeye çıkar. Bu, sığınağın karşılaştığı son büyük sınavlardan " +
                    "biridir.",
                    Choice("Krizin önüne kendin çık", conditionalEffect: LeaderRisk(leaderHealthDeltaWhenFalse: -4),
                        forcedNext: 234),
                    Choice("Yetiştirdiğin kadroya güven", authority: 1, forcedNext: 231)),

                Card(231, "Anlatıcı",
                    "Kriz sona erdiğinde duvarlar hâlâ ayaktadır. Alarm seslerinin ardından gelen " +
                    "sessizlik bu kez yenilgi değil, rahatlamadır.",
                    Choice("Biraz soluklan", forcedNext: 234),
                    Choice("Hemen işlere dön", forcedNext: 232)),

                Card(232, "Anlatıcı",
                    "Sabiha’nın ticareti, Aziz’in tarımı, Kemal’in yapıları ve İsmet’in arşivi " +
                    "artık kişilerden bağımsız işleyen düzenlere dönüşmüştür. Her biri sığınakta " +
                    "kalıcı bir iz bırakmıştır.",
                    Choice("Hepsine açıkça teşekkür et", authority: 1, forcedNext: 236),
                    Choice("İşlerin artık böyle yürümesini doğal karşıla", forcedNext: 233)),

                Card(233, "Anlatıcı",
                    "Ali, Veli ve onların ardından gelenler artık yalnızca öğrenmiyor; tarımı, " +
                    "savunmayı, mühendisliği ve haberleşmeyi kendileri yürütüyor.",
                    Choice("Sorumluluğu yeni nesle bırakmaya güven", forcedNext: 235),
                    Choice("Denetimi bir süre daha sıkı tut", forcedNext: 234)),

                Card(234, "Anlatıcı",
                    "Vertak, Karakol ve enfektelerle kurulan bütün ilişkiler artık aynı tabloda " +
                    "görülebilir. Sığınağın bölgede ne kadar güçlü ya da kırılgan olduğu, yıllar " +
                    "boyunca verilen kararların toplamıyla belirlenmiştir.",
                    Choice("Elde edilen gücü sahiplen", authority: 1, forcedNext: 238),
                    Choice("Kırılganlığı unutmadan savunmayı koru", security: 1, forcedNext: 236)),

                Card(235, "İsmet (Telsizci)",
                    "İsmet’in arşivinde kaç liderin görev yaptığı ve sığınağın kaç gündür ayakta " +
                    "olduğu bile yazılıdır. Sayılar, hatırladığından daha büyüktür.",
                    Choice("Kayıtları kendin oku", forcedNext: 237),
                    Choice("Arşivi İsmet’e bırak", forcedNext: 236)),

                Card(236, "Anlatıcı",
                    "Büyük toplantıda artık tek bir kişinin sözü belirleyici değildir. Uzmanlar, " +
                    "gençler ve eski sakinler aynı masada konuşur.",
                    Choice("Görüşünü söyle", authority: 1, forcedNext: 238),
                    Choice("Bu kez yalnızca dinle", forcedNext: 237)),

                Card(237, "Anlatıcı",
                    "Zeynep’in yetiştirdiği halef artık reviri tek başına yönetebilecek kadar " +
                    "deneyimlidir. Sağlık hizmeti ilk kez tek bir kişiye bağlı değildir.",
                    Choice("Başardıklarını takdir et", forcedNext: 239),
                    Choice("Bunu sistemin doğal sonucu say", forcedNext: 238)),

                Card(238, "Anlatıcı",
                    "Ömer’in kurduğu nöbet düzeniyle Mustafa ve Mete’nin savunma sistemi artık " +
                    "kişiler değişse bile işleyecek kadar yerleşmiştir.",
                    Choice("Bu düzenin değerini açıkça vurgula", forcedNext: 242),
                    Choice("Günlük hayatın parçası say", forcedNext: 239)),

                Card(239, "Anlatıcı",
                    "Bir akşam herkes aynı yerde toplanır. Kimse bunun “son sakin akşam” olduğunu " +
                    "söylemez; yalnızca uzun zamandır ilk kez masada boşluk azdır.",
                    Choice("Oradakilere teşekkür et", forcedNext: 242),
                    Choice("Sessizce onlarla otur", forcedNext: 240)),

                Card(240, "Anlatıcı",
                    "İsmet’in eski kayıtları ilk günün korkusunu hatırlatır. Bugünkü sığınak, o " +
                    "kapının önündeki birkaç saatten çok uzakta bir yerdedir.",
                    Choice("İlk günü yeniden hatırla", forcedNext: 242),
                    Choice("Gelecek yıllara odaklan", forcedNext: 241)),

                Card(241, "Anlatıcı",
                    "İnsanlar sığınağa yıllardır aynı adı takmaktadır. İsim artık haritalarda ve " +
                    "ticaret notlarında bile görünmeye başlar.",
                    Choice("Adı resmî olarak kabul et", authority: 1, forcedNext: 244),
                    Choice("Halkın kullandığı biçimiyle bırak", forcedNext: 242)),

                Card(242, "Emine Teyze",
                    "Emine Teyze’nin bahçesi yine çiçektedir. Kendisinden sonra da her yıl " +
                    "birileri toprağı havalandırmış, tohumları yenilemiştir.",
                    Choice("Bahçede biraz kal", forcedNext: 245),
                    Choice("Yoluna devam et", forcedNext: 243)),

                Card(243, "Gül (Halktan)",
                    "Gül’ün çocuğu artık düzenli derslere katılır. Atilla’nın yıllar önce " +
                    "başlattığı eğitim düzeni, onu kuranlardan bağımsız biçimde sürmektedir.",
                    Choice("Bir derse katıl", forcedNext: 246),
                    Choice("Kapıdan izle", forcedNext: 244)),

                Card(244, "Aziz (Tarımcı)",
                    "Aziz’in kurduğu tarım düzeni artık sığınağın ana geçim kaynağıdır. Bir " +
                    "zamanlar her porsiyonun hesabı yapılırken şimdi ekim takvimleri " +
                    "konuşulmaktadır.",
                    Choice("Bu değişimi özellikle takdir et", forcedNext: 248),
                    Choice("Artık olağan kabul et", forcedNext: 245)),

                Card(245, "Anlatıcı",
                    "Gece yaklaşırken kadro bir kez daha aynı yerde toplanır. Ortamdaki sessizlik " +
                    "yorgunluktan çok, uzun bir işi tamamlamış insanların sessizliğidir.",
                    Choice("O anın içinde kal", forcedNext: 248),
                    Choice("Bir sonraki güne odaklan", forcedNext: 246)),

                Card(246, "Anlatıcı",
                    "Zeynep, Sabiha, Ömer, Kemal, Atilla, Aziz, İsmet, Mustafa ve Mete aynı " +
                    "masada son bir geniş toplantıya katılır. Yıllarca farklı krizlerde verilen " +
                    "kararlar artık ortak bir geçmişe dönüşmüştür.",
                    Choice("Tartışmaya katıl", forcedNext: 250),
                    Choice("Bu kez yalnızca dinle", forcedNext: 247)),

                Card(247, "İsmet (Telsizci)",
                    "İsmet sığınak günlüğünün son boş sayfalarından birini açar. Kalemi masanın " +
                    "ortasına bırakır.",
                    Choice("Son kaydı kendin yaz", forcedNext: 250),
                    Choice("Kaydı İsmet yazsın", forcedNext: 248)),

                Card(248, "Anlatıcı",
                    "Gece çöker ve sığınak yavaşça sessizleşir. İlk yıllardaki sessizlik tehlike " +
                    "beklemek demekti; bu kez insanlar yalnızca uyuyordur.",
                    Choice("Bir süre dışarıyı izle", forcedNext: 250),
                    Choice("İçeri dön", forcedNext: 249)),

                Card(249, "Anlatıcı",
                    "Kaç liderin gelip geçtiği, kaç günün sayıldığı artık tek başına önemli " +
                    "değildir. Duvarların içinde hayat sürmekte ve sığınak hâlâ ayaktadır.",
                    Choice("Geçen yılları düşün", forcedNext: 250),
                    Choice("Hiçbir şey söylemeden o anı yaşa", forcedNext: 250)),

                // The specification's true end: no forced-next on either side. See class remarks.
                Card(250, "Anlatıcı",
                    "K1’den bu yana verilen 250 kararın izi sığınağın bugünkü hâlinde görünür: " +
                    "kaç liderin görev yaptığı, Karakol ve Vertak’la kurulan ilişkiler, konuşan " +
                    "enfektelerle savaş mı yoksa birlikte yaşam mı seçildiği burada birleşir. Bu " +
                    "bir son değildir; sığınağın tarihi buradan sonra da aynı kararların " +
                    "ağırlığıyla devam edebilir.",
                    Choice("Günlüğü kapat"),
                    Choice("Sessizce otur")),
            };
        }
    }
}
