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
                    "Necati zar oyununda üst üste kaybeder. Masadakiler onun söylenmesine gülmeye " +
                    "başlar.",
                    Choice("Sen de gül", forcedNext: 64),
                    Choice("Ciddiyetini koru", forcedNext: 62)),

                Card(62, "Mete (Asker)",
                    "Mete haritayı önüne koyar. “İhtiyacımız olan malzeme burada olabilir. Yol " +
                    "kötü, bölge daha da kötü.”",
                    Choice("Keşfe kendin çık", conditionalEffect: LeaderRisk(leaderHealthDeltaWhenFalse: -2),
                        forcedNext: 65),
                    Choice("Bir ekip gönder", wealth: -1, forcedNext: 63)),

                Card(63, "İsmet (Telsizci)",
                    "İsmet eski Vertak kayıtlarından “Faz 4” başlıklı bir dosya çıkarır. İçeriği, " +
                    "bildiklerinizi biraz daha karanlık bir yere bağlar. *(pharma_arastirma+1)*",
                    Choice("Dosyayı herkesle paylaş", authority: -1, counterDeltas: Counter(CounterPharmaArastirma, 1),
                        forcedNext: 66),
                    Choice("Şimdilik arşivde tut", counterDeltas: Counter(CounterPharmaArastirma, 1), forcedNext: 64)),

                Card(64, "Yusuf (Halktan)",
                    "Yusuf derede balık tutmaya uğraşır. Oltası sürekli bir yerlere takılır ama " +
                    "vazgeçmez.",
                    Choice("Yardım et", forcedNext: 65),
                    Choice("Kenardan izle", forcedNext: 65)),

                Card(65, "Kemal (Mühendis)",
                    "Kemal çatının en güneş alan bölümünü işaretler. “Yeterli malzeme bulursak " +
                    "kendi elektriğimizi üretebiliriz.”",
                    Choice("Projeyi hemen başlat", flagsAdd: Flags("proje_baslatildi"), forcedNext: 68),
                    Choice("Şimdilik ertele", flagsAdd: Flags("proje_ertelendi"), forcedNext: 66)),

                Card(66, "Kemal (Mühendis)",
                    "Kemal parça listesini uzatır. “Elimizdekiler yetmiyor. Ya komşulardan " +
                    "isteyeceğiz ya da başka yerlerden söküp kullanacağız.”",
                    Choice("Başka bir sığınaktan yardım iste", wealth: -1, flagsAdd: Flags("ittifak_baslangic"),
                        forcedNext: 69),
                    Choice("Kendi kaynaklarımızla devam et", security: -1, forcedNext: 67)),

                // Only ever reached via K66-B, where the project was postponed — see class remarks.
                Card(67, "Kemal (Mühendis)",
                    "Eldeki parçalarla kurulan panel kusursuz değildir ama çalışır. Gecikmenin " +
                    "bedeli, daha düşük kapasitedir.",
                    Choice("Bu hâliyle yeterli say", security: 1, wealth: -1, forcedNext: 70),
                    Choice("İleride genişletmek üzere kayda geçir", security: 1, wealth: -1,
                        forcedNext: 70),
                    variants: new[]
                    {
                        VariantIfFlag("proje_baslatildi",
                            "Panel sonunda tam kapasite çalışır. Sığınakta ilk kez kesintisiz bir " +
                            "elektrik kaynağı vardır.",
                            Choice("Kemal’in emeğini takdir et", security: 2, wealth: -1, forcedNext: 70),
                            Choice("Vakit kaybetmeden sıradaki işe geç", security: 2, wealth: -1,
                                forcedNext: 70))
                    }),

                Card(68, "Sibel (Halktan)",
                    "Panel konuşulurken Sibel, elektriğin ona eski bir şeyi hatırlattığını " +
                    "söyler. Meğer salgından önce piyanistmiş.",
                    Choice("Bir gün çalmasını iste", forcedNext: 71),
                    Choice("Konuyu uzatma", forcedNext: 67)),

                Card(69, "Anlatıcı",
                    "Komşu sığınaktan resmi bir teklif gelir: kaynak ve haber paylaşımı " +
                    "karşılığında karşılıklı destek.",
                    Choice("İttifakı kabul et", flagsAdd: Flags("ittifak_kabul"), forcedNext: 72),
                    Choice("Teklifi reddet", forcedNext: 70)),

                Card(70, "Tarık / Rıza",
                    "Tarık ile Rıza günlerdir ilk kez aynı masada kavga etmeden oturur. Kimse " +
                    "bunun nasıl olduğunu tam anlayamaz.",
                    Choice("Barışmalarını kutla", forcedNext: 73),
                    Choice("Üzerinde durma", forcedNext: 71)),

                Card(71, "Sabiha (Erzakçı)",
                    "Sabiha büyük miktarda erzak getirebilecek bir takas fırsatı bulur. Güvenli " +
                    "seçenek az kazandırır; diğerinde kayıp ihtimali çok daha büyüktür.",
                    Choice("Güvenli takası seç", wealth: -1, people: 1, forcedNext: 74),
                    Choice("Büyük riski al", conditionalEffect: ReignIfCritical(
                        StatType.Wealth, atOrBelow: 3, deltasWhenSafe: new StatDeltas(0, 0, 0, 3),
                        resetStat: StatType.Wealth), forcedNext: 72)),

                Card(72, "Gül (Halktan)",
                    "Gül bebeğine isim koyacağı gün herkesi yanına çağırır. Sığınakta uzun " +
                    "zamandır böyle bir şey için toplanılmamıştır.",
                    Choice("Törene katıl", forcedNext: 75),
                    Choice("Kısa bir tebrikle yetin", forcedNext: 73)),

                Card(73, "Ömer (Gözcü)",
                    "Ömer çitin yanında yine aynı boğuk sesi duyar. Bu kez kelimeler daha nettir: " +
                    "“Biz de... insandık.”",
                    Choice("Dinlemeye devam et", authority: -1, flagsAdd: Flags("zombi_ikinci_temas"), forcedNext: 76),
                    Choice("Çitten uzaklaş", forcedNext: 74)),

                Card(74, "Aziz (Tarımcı)",
                    "Aziz telaşla tohum defterini arar. Yıllardır tuttuğu bütün ekim notları o " +
                    "defterdedir.",
                    Choice("Aramasına yardım et", forcedNext: 77),
                    Choice("Kendi işine dön", forcedNext: 75)),

                Card(75, "Anlatıcı",
                    "Komşu sığınaktan gelen haberler, teklifin göründüğü kadar masum olmadığını " +
                    "doğrular. Reddetmek sizi bir yükten kurtarmıştır.",
                    Choice("Rahatla", authority: 1, forcedNext: 77),
                    Choice("Devam et", authority: 1, forcedNext: 77),
                    variants: new[]
                    {
                        VariantIfFlag("ittifak_kabul",
                            "İttifakın şartları zamanla tek taraflı hâle gelir. Karşı taraf daha " +
                            "çok isterken verdiği destek azalır.",
                            Choice("Şartlara itiraz et", authority: -1, forcedNext: 78),
                            Choice("Anlaşmayı bozmamak için boyun eğ", wealth: -2, authority: -1,
                                forcedNext: 76))
                    }),

                Card(76, "Ömer (Gözcü)",
                    "Ömer sabaha karşı seni kenara çeker. “Birisi sana ulaşmaya çalıştı. Tesadüf " +
                    "değildi.”",
                    Choice("Şüphelinin peşine düş", conditionalEffect: LeaderRisk(
                        leaderHealthDeltaWhenFalse: -1, deltasWhenFalse: new StatDeltas(-1, 0, 0, 0)),
                        forcedNext: 79),
                    Choice("Şimdilik üstünü kapat", forcedNext: 77)),

                Card(77, "Ali (Halktan)",
                    "Ali ilk kez gerçek nöbete çıkmak istediğini söyler. Çocukluğundan kalan " +
                    "hâliyle ona bakmak artık giderek zorlaşmaktadır.",
                    Choice("Nöbete katılmasına izin ver", forcedNext: 80),
                    Choice("Biraz daha beklemesini söyle", forcedNext: 78)),

                Card(78, "Kemal (Mühendis)",
                    "Kemal kış için iki ısınma planı çıkarır. Odun daha güvenilir ama dumanlıdır; " +
                    "elektrik daha temiz ama sisteme yük bindirir.",
                    Choice("Odunla ısın", wealth: -1, flagsAdd: Flags("kis_hazirlik_odun"), forcedNext: 81),
                    Choice("Elektrikli sistemi kullan", security: -1,
                        flagsAdd: Flags("kis_hazirlik_elektrik"), forcedNext: 79)),

                Card(79, "Anlatıcı",
                    "Kışın ilk haftasında sığınak sıcak kalır ama baca kurum bağlar, hava " +
                    "ağırlaşır.",
                    Choice("Bacayı düzenli temizlet", security: 1, forcedNext: 80),
                    Choice("Sezonu böyle çıkarmaya çalış", security: 1, people: -1, forcedNext: 80),
                    variants: new[]
                    {
                        VariantIfFlag("kis_hazirlik_elektrik",
                            "Elektrikli sistem çalışır ama yük altında sık sık kararsızlaşır. " +
                            "Buna karşılık içerideki hava temizdir.",
                            Choice("Kemal’e sürekli kontrol ettir", people: 1, forcedNext: 80),
                            Choice("Arızalar çıkana kadar müdahale etme", security: -1, people: 1,
                                forcedNext: 80))
                    }),

                Card(80, "Semra (Halktan)",
                    "Semra’nın küçük konserleri artık sığınağın alışkanlıklarından biri olmuştur. " +
                    "O akşam yine gitarını çıkarır.",
                    Choice("Dinlemeye git", forcedNext: 84),
                    Choice("Bu kez çalışmaya devam et", forcedNext: 81)),

                Card(81, "İsmet (Telsizci)",
                    "*(pharma_arastirma≥2 ise İsmet, Vertak'ın asıl planını çözer: sığınakları " +
                    "toplamak; düşükse yalnızca dağınık, tedirgin edici ipuçları bulur.)* İsmet " +
                    "bulduklarını önüne dizer. Ne kadarının halka açıklanacağına karar vermek " +
                    "gerekir.",
                    Choice("Bildiklerini yayımla", authority: -1, forcedNext: 83),
                    Choice("Bilgiyi kadroyla sınırla", forcedNext: 82)),

                Card(82, "Anlatıcı",
                    "“Vertak’a katılalım” diyenlerin sayısı artar. Tartışma artık birkaç kişinin " +
                    "homurdanmasından çıkıp açık bir bölünmeye dönüşmüştür.",
                    Choice("Gitmek isteyenleri serbest bırak", authority: 1, forcedNext: 84),
                    Choice("Kimsenin ayrılmasına izin verme", conditionalEffect: ReignIfCritical(
                        StatType.Authority, atOrBelow: 3, deltasWhenSafe: new StatDeltas(-1, 0, 0, 0),
                        resetStat: StatType.Authority), forcedNext: 83)),

                Card(83, "Emine Teyze",
                    "Emine Teyze o gün alışılmadık derecede sakindir. Bahçesinin yanında oturup " +
                    "eski günlerden konuşur; bu, onun son iyi günüdür. *(Not: bu günden sonra " +
                    "nüfus 20'ye düşer.)*",
                    Choice("Yanında otur", authority: 1, forcedNext: 87),
                    Choice("Dinlenmesi için yalnız bırak", forcedNext: 84)),

                Card(84, "Zeynep (Doktor)",
                    "Zeynep’in elleri titremeye başlamıştır. Günlerdir herkese bakmış, kendisi " +
                    "neredeyse hiç uyumamıştır.",
                    Choice("Dinlenmesini zorunlu tut", flagsAdd: Flags("zeynep_zorla_dinlendirildi"), forcedNext: 86),
                    Choice("Kararı ona bırak", forcedNext: 85)),

                Card(85, "Cem & Yusuf",
                    "Cem ile Yusuf yeni bir masa oyunu uydurmuştur. Kuralları her tur değişiyor " +
                    "ama kimsenin umurunda değildir.",
                    Choice("Oyuna katıl", forcedNext: 87),
                    Choice("Kenardan izle", forcedNext: 86)),

                Card(86, "Anlatıcı",
                    "Sınırda yorgun ve bitkin bir mülteci grubu belirir. Yanlarında çocuklar da " +
                    "vardır.",
                    Choice("Grubu içeri al", flagsAdd: Flags("multeci_kabul"), forcedNext: 89),
                    Choice("Sığınaktan uzaklaştır", forcedNext: 87)),

                Card(87, "Anlatıcı",
                    "Grup çevreden ayrılmaz. Yakınlarda dolaşmaları içerideki huzursuzluğu " +
                    "artırır.",
                    Choice("Bölgeden uzaklaştır", authority: -1, forcedNext: 90),
                    Choice("Görmezden gel", conditionalEffect: new ConditionalChoiceEffect(
                        new NumericCondition(NumericSource.Stat, NumericComparison.LessOrEqual, 4,
                            stat: StatType.Security),
                        deltasWhenTrue: new StatDeltas(0, 0, -1, 0)), forcedNext: 88),
                    variants: new[]
                    {
                        VariantIfFlag("multeci_kabul",
                            "İçeri alınanlardan birinde kısa süre sonra hastalık belirtisi görülür.",
                            Choice("Hemen karantinaya al", people: 1, authority: -1, forcedNext: 90),
                            Choice("Belirti ağırlaşana kadar bekle", people: -1, forcedNext: 88))
                    }),

                Card(88, "Anlatıcı",
                    "Zeynep tam mülteci krizi sırasında hastalanır. Revirin başında artık kimin " +
                    "duracağı belirsizdir.",
                    Choice("Geçici birini görevlendir", people: -2, forcedNext: 89),
                    Choice("Zeynep’in işi sürdürmesine izin ver", people: -2,
                        conditionalEffect: AlwaysLeaderHealth(-1), forcedNext: 89),
                    variants: new[]
                    {
                        VariantIfFlag("zeynep_zorla_dinlendirildi",
                            "Zeynep birkaç gün sonra belirgin biçimde toparlanmış döner. Mülteci " +
                            "meselesine yeniden el atabilecek durumdadır.",
                            Choice("Tam görevine dönsün", authority: 1, forcedNext: 90),
                            Choice("İş yükünü kademeli artır", authority: 1, people: 1, forcedNext: 90))
                    }),

                Card(89, "Anlatıcı",
                    "Sığınağın kuruluşunun üzerinden bir yıl geçmiştir. Kimse bunu tam olarak " +
                    "kutlama saymasa da tarih herkesin aklındadır.",
                    Choice("Küçük bir yıl dönümü düzenle", forcedNext: 91),
                    Choice("Günü sıradan geçir", forcedNext: 90)),

                Card(90, "Mustafa (Asker)",
                    "Mustafa dışarıdan gelen uğultuyu dinler. “Şimdiye kadarki en büyük sürü bu. " +
                    "Hatları ben tutarım ama senin kararın lazım.”",
                    Choice("Cepheye çıkıp savunmayı yönet", conditionalEffect: LeaderRisk(
                        leaderHealthDeltaWhenFalse: -3, deltasWhenFalse: new StatDeltas(0, 0, 2, 0)),
                        forcedNext: 93),
                    Choice("Komutayı Mustafa’ya bırak", security: 1, authority: -1, forcedNext: 91)),

                Card(91, "Ali (Halktan)",
                    "Ali artık çocuk değildir. İlk kez uzmanlık isteyen gerçek bir görev için " +
                    "adını yazdırır.",
                    Choice("Görevi ona ver", forcedNext: 94),
                    Choice("Bir süre daha beklet", forcedNext: 92)),

                Card(92, "Kemal (Mühendis)",
                    "Kemal yapısal raporu masaya bırakır. “Bu bina bizi daha ne kadar taşır, emin " +
                    "değilim. Taşınmak pahalı; kalmak da riskli.”",
                    Choice("Yeni bir yere taşın", wealth: -2, security: 1, forcedNext: 94),
                    Choice("Mevcut sığınakta kal", conditionalEffect: ReignIfCritical(
                        StatType.Security, atOrBelow: 3, deltasWhenSafe: new StatDeltas(0, 0, -1, 0),
                        resetStat: StatType.Security), forcedNext: 93)),

                Card(93, "İsmet (Telsizci)",
                    "*(pharma_arastirma≥3 ise İsmet, Vertak'ın içeriden bölündüğünü öğrenir — bir " +
                    "hizip barış istiyor; düşükse yalnızca çelişkili söylentiler duyar.)* " +
                    "İsmet’in getirdiği bilgiler ilk kez Vertak’ın tek parça olmadığını " +
                    "düşündürür.",
                    Choice("Barış isteyenlerle temas ara", authority: 1, forcedNext: 95),
                    Choice("Kimseye güvenme", forcedNext: 94)),

                Card(94, "İsmet (Telsizci)",
                    "İsmet eski bir kaset bulur. Cızırtıların arasından salgın öncesi bir şarkı " +
                    "ve insanların sıradan konuşmaları duyulur.",
                    Choice("Hep birlikte dinleyin", authority: 1, forcedNext: 97),
                    Choice("İşine dön", forcedNext: 95)),

                Card(95, "Ömer (Gözcü)",
                    "Ömer, konuşan enfektelerden gelen ilk açık temas teklifini iletir. Bu kez " +
                    "çitin ötesinde bekleyip cevap vermenizi istemektedirler.",
                    Choice("Ateşkes görüşmesi yap", flagsAdd: Flags("ateskes_evet"), forcedNext: 97),
                    Choice("Önce saldır", forcedNext: 96)),

                Card(96, "Anlatıcı",
                    "Çatışma büyür ve iki tarafta da kayıplar olur.",
                    Choice("Kayıplar için anma düzenle", authority: -2, people: -1, forcedNext: 97),
                    Choice("Savunmayı toparlayıp devam et", authority: -2, people: -1, forcedNext: 97),
                    variants: new[]
                    {
                        VariantIfFlag("ateskes_evet",
                            "Görüşmeler beklenmedik biçimde sonuç verir. Sınırın öte yanı ilk kez " +
                            "yalnızca bir tehdit değil, konuşulabilen bir komşu gibi görünür.",
                            Choice("Anlaşmayı törenle duyur", authority: 2, people: 1, forcedNext: 100),
                            Choice("Gösterişsiz biçimde yürürlüğe koy", authority: 2, people: 1,
                                forcedNext: 100))
                    }),

                Card(97, "Anlatıcı",
                    "O akşam sığınakta alışılmadık bir sessizlik vardır. Herkes bir şekilde hâlâ " +
                    "burada olduğunun farkındadır.",
                    Choice("Bir süre oturup olanları düşün", forcedNext: 100),
                    Choice("Uyumaya git", forcedNext: 98)),

                Card(98, "Gül (Halktan)",
                    "Gül’ün çocuğu ilk kez kendi başına birkaç adım atar. Yakındakiler istemsizce " +
                    "alkışlar.",
                    Choice("Onlarla birlikte kutla", forcedNext: 102),
                    Choice("İşine devam et", forcedNext: 99)),

                Card(99, "Fatma (Halktan)",
                    "Fatma yeni gelen çocuklara duvarın kenarında resim yaptırır. Birkaç " +
                    "dakikalığına sığınak okul gibi görünür.",
                    Choice("Derse katıl", forcedNext: 101),
                    Choice("Kenardan izle", forcedNext: 100)),

                Card(100, "Anlatıcı",
                    "İlk büyük dönemin sonunda sığınak hâlâ ayaktadır. Buraya kadar gelen yol; " +
                    "verdiğin kararlar, kurduğun ilişkiler ve geride bıraktığın sonuçlarla " +
                    "şekillenmiştir. Bu bir final değildir.",
                    Choice("Devam et", forcedNext: 101),
                    Choice("Yeni döneme geç", forcedNext: 101)),
            };
        }
    }
}
