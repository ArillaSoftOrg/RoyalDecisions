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
                    "Semra depoda tozlanmış bir gitar bulur. Tellerini yoklayıp sana bakar. " +
                    "“Biraz uğraşırsam yine ses verir.”",
                    Choice("Tamir etmesine izin ver", forcedNext: 29),
                    Choice("Şimdilik olduğu yerde kalsın", forcedNext: 27)),

                // K27-A sets cit_yaklastik_evet, selecting K28's variant below.
                Card(27, "Ömer (Gözcü)",
                    "Ömer nöbetten inerken duraksar. “Çitin ötesinden bir ses geliyor. Kelimelere " +
                    "benziyor.”",
                    Choice("Yakından bak", authority: -1, flagsAdd: Flags("cit_yaklastik_evet"),
                        forcedNext: 31),
                    Choice("Uzaktan izlemeye devam et", forcedNext: 28)),

                Card(28, "Ömer (Gözcü)",
                    "Ses bir anda kesilir. Ömer karanlığa bakar. “Yerini kaybettim.”",
                    Choice("Devriyeyi artır", security: 1, forcedNext: 30),
                    Choice("Nöbet düzenini değiştirme", forcedNext: 31),
                    variants: new[]
                    {
                        VariantIfFlag("cit_yaklastik_evet",
                            "Çitin ötesindeki yaratık boğuk bir sesle “Yardım” der. Gözleri artık " +
                            "insana ait görünmüyor.",
                            Choice("Ateş et", authority: -1, forcedNext: 31),
                            Choice("Ne söyleyeceğini dinle", authority: -2,
                                flagsAdd: Flags("zombi_konustu"), forcedNext: 31),
                            speaker: "Ömer (Gözcü)")
                    }),

                Card(29, "Zeynep (Doktor)",
                    "Zeynep duyduklarını düşünür. “Vertak notlarında buna benzeyen bir vaka " +
                    "vardı. Aynı şeyse bilmemiz gereken çok şey var.”",
                    Choice("İzini araştır", authority: -1,
                        counterDeltas: Counter(CounterPharmaArastirma, 1), forcedNext: 34),
                    Choice("Konuyu kapat", forcedNext: 30)),

                Card(30, "Emine Teyze",
                    "Emine Teyze eski günlerden bir hikâye anlatmaya başlar. Bir süreliğine " +
                    "sığınağın duvarları yokmuş gibi olur.",
                    Choice("Yanına oturup dinle", authority: 1, forcedNext: 33),
                    Choice("İşine dön", forcedNext: 33)),

                Card(31, "Sabiha (Erzakçı)",
                    "Sabiha haritayı masaya açar. “Yakındaki depoya ulaşabiliriz. Üç kişi daha " +
                    "sessiz olur; beş kişi daha çok yük taşır.”",
                    Choice("Üç kişilik ekip gönder", forcedNext: 32),
                    Choice("Beş kişilik ekip gönder", security: -1, flagsAdd: Flags("sefer_ekip_buyuk"),
                        forcedNext: 32)),

                // K32-B's outcome depends on sefer_ekip_buyuk (set at K31); the variant overrides
                // only the right choice, leaving the left choice and body text as authored on both.
                Card(32, "İsmet (Telsizci)",
                    "İsmet telsizi sana uzatır. Hattın öbür ucunda bağrışmalar vardır: ekip bir " +
                    "sürüye yakalanmıştır.",
                    Choice("Hemen geri çekilmelerini emret", authority: 1, wealth: 1, forcedNext: 35),
                    Choice("Görevi tamamlamalarını iste", wealth: 1, people: -1, forcedNext: 35),
                    variants: new[]
                    {
                        VariantIfFlag("sefer_ekip_buyuk", null, null,
                            Choice("Görevi tamamlamalarını iste", wealth: 3, authority: -2,
                                forcedNext: 35))
                    }),

                Card(33, "Aziz (Tarımcı)",
                    "Aziz topladığı sebzelerden sıcak bir yemek çıkarır. “Bugün yiyebiliriz. Ya " +
                    "da yarına bırakırız.”",
                    Choice("Bugün ye", forcedNext: 36),
                    Choice("Sakla", forcedNext: 36)),

                // K34-A sets catlak_onarildi, selecting K37's variant below.
                Card(34, "Kemal (Mühendis)",
                    "Kemal duvara birkaç kez vurup sesi dinler. “Temelde çatlak var. Beklersek " +
                    "büyüyebilir.”",
                    Choice("Şimdi onar", wealth: -1, flagsAdd: Flags("catlak_onarildi"), forcedNext: 37),
                    Choice("Şimdilik bekle", forcedNext: 36)),

                Card(35, "Ali & Veli",
                    "Ali ile Veli gitarı ele geçirip küçük bir “konser” verir. Sığınakta ilk kez " +
                    "birkaç kişi gerçekten güler.",
                    Choice("Alkışla", forcedNext: 38),
                    Choice("Kenardan izle", forcedNext: 38)),

                Card(36, "İsmet (Telsizci)",
                    "İsmet eski bir Vertak raporunu masaya bırakır. Suş-7 deneyinin kontrolden " +
                    "çıktığı yazılıdır. Kemal’in sözünü ettiği nemli çatlak da rapordaki koşullarla " +
                    "ürkütücü biçimde örtüşür.",
                    Choice("Bildiklerini herkese anlat", authority: -2, forcedNext: 37),
                    Choice("Şimdilik yalnızca kadroyla paylaş", forcedNext: 37)),

                Card(37, "Kemal (Mühendis)",
                    "Çatlak büyümüş, içeri su almaya başlamıştır. Kemal küfreder. “Artık " +
                    "erteleyemeyiz.”",
                    Choice("Hasarlı bölümü onar", security: -1, wealth: -1, forcedNext: 40),
                    Choice("Bölmeyi boşalt", security: -2, authority: -1, forcedNext: 39),
                    variants: new[]
                    {
                        VariantIfFlag("catlak_onarildi",
                            "Kemal duvarı yeniden kontrol eder. “Şimdilik sağlam. İstersen burada " +
                            "bırakırız, istersen son bir kez baştan sona bakarım.”",
                            Choice("İşi burada bitir", authority: 1, forcedNext: 40),
                            Choice("Ayrıntılı kontrol yap", security: 1,
                                conditionalEffect: AlwaysLeaderHealth(-1), forcedNext: 40))
                    }),

                Card(38, "Rıza (Halktan)",
                    "Rıza ile Tarık birbirine girmiştir. Atilla ikisinin arasında durup sana " +
                    "bakar. “İstersen sen konuş. İstersen ben halledeyim.”",
                    Choice("Araya kendin gir", forcedNext: 42),
                    Choice("Atilla’ya bırak", forcedNext: 40)),

                Card(39, "Necati (Halktan)",
                    "Necati kalabalığın ortasında sesini yükseltir: “Bize her şeyi anlatmıyor!” " +
                    "İnsanlar dönüp sana bakar.",
                    Choice("Bildiklerini açıkça anlat", authority: 1, forcedNext: 42),
                    Choice("Tartışmayı zorla kes", conditionalEffect: ReignIfCritical(
                        StatType.Authority, atOrBelow: 3, deltasWhenSafe: new StatDeltas(-1, 0, 0, 0),
                        resetStat: StatType.Authority), forcedNext: 43)),

                // K40-A sets sinyal_cevaplandi, selecting K41's variant below.
                Card(40, "İsmet (Telsizci)",
                    "İsmet kulaklığını çıkarmaz. “Aynı frekanstan tekrar tekrar sinyal geliyor. " +
                    "Bizi özellikle arıyor olabilirler.”",
                    Choice("Cevap ver", authority: 1, flagsAdd: Flags("sinyal_cevaplandi"), forcedNext: 41),
                    Choice("Sessiz kal", forcedNext: 41)),

                Card(41, "Ömer (Gözcü)",
                    "Sinyal daha sık gelmeye başlar. Karşı taraf cevap almadan vazgeçmiyordur.",
                    Choice("Cihazı kapat", wealth: -1, forcedNext: 43),
                    Choice("Frekansı açık bırak", forcedNext: 43),
                    variants: new[]
                    {
                        VariantIfFlag("sinyal_cevaplandi",
                            "Karşı taraf doğrudan koordinat ister. İsmet eli vericinin üzerinde " +
                            "bekler.",
                            Choice("Konumu paylaş", flagsAdd: Flags("konum_paylasildi"), forcedNext: 43),
                            Choice("Konumu verme", forcedNext: 43),
                            speaker: "İsmet (Telsizci)")
                    }),

                Card(42, "Gül (Halktan)",
                    "Gül’ün bebeği ilk kez kahkaha atar. Tartışmaların ortasında herkes birkaç " +
                    "saniyeliğine susar.",
                    Choice("Gülümse", forcedNext: 45),
                    Choice("İşine dön", forcedNext: 43)),

                Card(43, "Ömer (Gözcü)",
                    "Ömer kapıdan haber verir. “Dışarıda bir araç durdu. İçindekiler bekliyor.”",
                    Choice("Kapıda karşıla", forcedNext: 46),
                    Choice("Kapıları kilitle", forcedNext: 44)),

                // Variant if konum_paylasildi: the arriving group really is Vertak, not an unclear one.
                Card(44, "Anlatıcı",
                    "Kapıda kimliği belirsiz, gergin bir grup bekler.",
                    Choice("Konuşmayı kabul et", authority: -2, forcedNext: 47),
                    Choice("Mesafeyi koru", forcedNext: 45),
                    variants: new[]
                    {
                        VariantIfFlag("konum_paylasildi",
                            "Kapıda gerçekten bir Vertak temsilcisi vardır; sakin, temiz ve " +
                            "hazırlıklıdır.",
                            Choice("Konuşmayı kabul et", authority: 1, forcedNext: 47),
                            Choice("Mesafeyi koru", forcedNext: 45),
                            speaker: "Vertak Temsilcisi")
                    }),

                // K45-A sets ates_ilac_evet, selecting K47's variant below.
                Card(45, "Zeynep (Doktor)",
                    "Zeynep bebeğin başında bekler. “Ateşi yükseliyor. Elimizde bir doz ilaç " +
                    "kaldı.”",
                    Choice("Son ilacı kullan", wealth: -1, flagsAdd: Flags("ates_ilac_evet"),
                        forcedNext: 47),
                    Choice("Bir süre daha gözlemle", forcedNext: 46)),

                Card(46, "Sibel (Halktan)",
                    "Sibel köşede sessizce yıpranmış ayakkabıları onarır. Bebeğin ateşi " +
                    "konuşulurken bile elindeki işe devam eder.",
                    Choice("Emeği için teşekkür et", forcedNext: 49),
                    Choice("Sessizce geç", forcedNext: 47)),

                Card(47, "Zeynep (Doktor)",
                    "Ateş daha da yükselince son ilaç yine kullanılır. Zeynep yorgun gözlerle " +
                    "sana bakar.",
                    Choice("Kimseyi suçlama", wealth: -1, people: -1, forcedNext: 48),
                    Choice("Kararını sorgula", wealth: -1, people: -1,
                        conditionalEffect: AlwaysLeaderHealth(-1), forcedNext: 48),
                    variants: new[]
                    {
                        VariantIfFlag("ates_ilac_evet",
                            "Sabaha karşı bebeğin ateşi düşer. Zeynep sonunda omuzlarını gevşetir.",
                            Choice("Tehlikenin geçtiğini kabul et", authority: 1, forcedNext: 50),
                            Choice("Bir gece daha gözlem altında tut", authority: 1, forcedNext: 50))
                    }),

                Card(48, "Mustafa (Asker)",
                    "Mustafa aceleyle gelir. “Birkaç enfekteli dış hatta kadar sokuldu. Komutayı " +
                    "biri almalı.”",
                    Choice("Savunmayı kendin yönet", conditionalEffect: LeaderRisk(
                        leaderHealthDeltaWhenFalse: -2, deltasWhenFalse: new StatDeltas(0, 0, 1, 0)),
                        forcedNext: 51),
                    Choice("Komutayı Mustafa’ya bırak", security: 1, authority: -1, forcedNext: 49)),

                Card(49, "Cem & Yusuf",
                    "Cem ile Yusuf bir çift zar bulmuş, kendi kurallarını uydurmuşlardır. Sana da " +
                    "yer açarlar.",
                    Choice("Oyuna katıl", forcedNext: 52),
                    Choice("İzleyip geç", forcedNext: 50)),

                Card(50, "Kemal (Mühendis)",
                    "Kemal kapının menteşesini söküp önüne bırakır. “Bununla bir saldırı daha " +
                    "karşılamayız. Ya düzgünce yenileriz ya da şansımıza güveniriz.”",
                    Choice("Kaynak ayırıp tamamen yenile", wealth: -2, security: 2, forcedNext: 53),
                    Choice("Şimdilik idare et", conditionalEffect: ReignIfCritical(
                        StatType.Security, atOrBelow: 3, deltasWhenSafe: new StatDeltas(0, 0, -1, 0),
                        resetStat: StatType.Security), forcedNext: 51)),

                Card(51, "İsmet (Telsizci)",
                    "İsmet Vertak frekansını yeniden açar. “Hat açık. İstersek konuşabiliriz.”",
                    Choice("Teması sürdür", forcedNext: 54),
                    Choice("Bağlantıyı reddet", forcedNext: 52)),

                Card(52, "Tarık (Halktan)",
                    "Tarık bu kez herkesin önünde konuşur. “Bu kararları neden hep sen " +
                    "veriyorsun?” Oda sessizleşir.",
                    Choice("Sakin kalıp cevap ver", authority: 1, forcedNext: 55),
                    Choice("Sert karşılık ver", authority: -1, forcedNext: 53)),

                Card(53, "Emine Teyze",
                    "Emine Teyze elde kalanlarla ne olduğu pek anlaşılmayan bir yemek yapar. " +
                    "Kaşığı sana uzatır.",
                    Choice("Tadına bak", forcedNext: 56),
                    Choice("Bu kez pas geç", forcedNext: 54)),

                // K54-A sets duman_arastir_evet, selecting K59's variant later.
                Card(54, "Sabiha (Erzakçı)",
                    "Sabiha uzakta yükselen ince bir duman sütununu gösterir. “Ateşse insan " +
                    "vardır. Tuzaksa da bizi bekliyor olabilir.”",
                    Choice("Dumanın kaynağını araştır", flagsAdd: Flags("duman_arastir_evet"), forcedNext: 57),
                    Choice("O bölgeden uzak dur", forcedNext: 55)),

                Card(55, "Ali (Halktan)",
                    "Ali’nin doğum günü gelir. Büyük bir kutlama yapacak hâliniz yoktur ama " +
                    "herkes günü hatırlar.",
                    Choice("Küçük de olsa kutla", forcedNext: 58),
                    Choice("Sade bir tebrikle geç", forcedNext: 56)),

                // K56-A sets yabanci_temas_evet, selecting K57's variant below.
                Card(56, "Ömer (Gözcü)",
                    "Ömer uzakta ilerleyen bir grup görür. “Bizi fark ettiler mi emin değilim.”",
                    Choice("Temas kur", flagsAdd: Flags("yabanci_temas_evet"), forcedNext: 59),
                    Choice("Uzaktan izle", forcedNext: 57)),

                // "Düzeni değiştirme" is written in the spec as costing 🏠-1 only when the watch is
                // already loose (nobet_gevsek); CardVariant supports one flag gate per card, and
                // this slot is already gated on yabanci_temas_evet, so the nested nobet condition
                // is approximated here as its more common, costed outcome rather than left as RNG.
                Card(57, "Ömer (Gözcü)",
                    "Grup yakınlarda kamp kurar. Ömer, birkaç gün burada kalabileceklerini " +
                    "düşünür.",
                    Choice("Nöbeti artır", forcedNext: 60),
                    Choice("Düzeni değiştirme", security: -1, forcedNext: 58),
                    variants: new[]
                    {
                        VariantIfFlag("yabanci_temas_evet",
                            "Grup yiyecek ve malzeme takası teklif eder. Sabiha malları hızlıca " +
                            "gözden geçirir.",
                            Choice("Takası kabul et", wealth: 1, authority: 1, forcedNext: 60),
                            Choice("Teklifi reddet", authority: -1, forcedNext: 58))
                    }),

                Card(58, "Fatma (Halktan)",
                    "Fatma duvara kocaman bir gökkuşağı çizer. Gri betonun ortasında fazlasıyla " +
                    "canlı durur.",
                    Choice("Bir süre yanında dur", forcedNext: 61),
                    Choice("Yoluna devam et", forcedNext: 59)),

                Card(59, "Anlatıcı",
                    "Sonradan gelen haber, dumanın bir tuzağın parçası olduğunu doğrular. Uzak " +
                    "durmak doğru karar olmuştur.",
                    Choice("Rahatla", authority: 1, forcedNext: 62),
                    Choice("Devam et", authority: 1, forcedNext: 62),
                    variants: new[]
                    {
                        VariantIfFlag("duman_arastir_evet",
                            "Dumanın yanında küçük bir grup bulunur. Sığınağa katılmak " +
                            "istediklerini söylerler.",
                            Choice("İçeri al", wealth: -1, authority: 1, forcedNext: 62),
                            Choice("Geri çevir", authority: -1, forcedNext: 60),
                            speaker: "Sabiha (Erzakçı)")
                    }),

                Card(60, "Zeynep (Doktor)",
                    "Zeynep su kabını ışığa tutar. “Kokusu normal değil. İçmeden önce test " +
                    "etmemiz gerek.”",
                    Choice("Suyu test et", wealth: -1, forcedNext: 63),
                    Choice("Beklemeden kullan", conditionalEffect: ReignIfCritical(
                        StatType.People, atOrBelow: 3, deltasWhenSafe: default,
                        resetStat: StatType.People), forcedNext: 61)),
            };
        }
    }
}
