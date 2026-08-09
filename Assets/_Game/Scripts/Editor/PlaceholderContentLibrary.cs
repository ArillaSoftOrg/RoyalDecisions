using System;
using System.Collections.Generic;
using RoyalDecisions.Data;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Builds the placeholder card and ending set in memory, with no asset I/O.
    /// </summary>
    /// <remarks>
    /// Deliberately separate from the generator: keeping content construction free of
    /// <c>AssetDatabase</c> lets tests build the whole set, validate it, and play a full run
    /// against it without writing a single file.
    ///
    /// Every card exists to prove one capability of the Phase 2 rule engine — see the table in the
    /// Phase 3 plan. All of it is disposable test data; replacing it must never require a code
    /// change (CLAUDE.md §4).
    /// </remarks>
    public static class PlaceholderContentLibrary
    {
        /// <summary>Marks generated text so placeholder content is obvious on screen and in the Inspector.</summary>
        public const string PlaceholderTag = "[GEÇİCİ]";

        public const string OpeningCardId = "card_01_coronation";

        public const string FlagTaxesRaised = "taxes_raised";
        public const string FlagArmyFavoured = "army_favoured";

        private const int LowStatThreshold = 25;

        /// <summary>The twenty placeholder cards, pre-sorted by ordinal ID.</summary>
        public static List<CardDefinition> CreateCards()
        {
            List<CardDefinition> cards = new List<CardDefinition>(20)
            {
                Card("card_01_coronation", "Başmabeyinci",
                    "Taç da saray erkânı da hazır. Taç giyme törenini başlatalım mı?",
                    Choice("Tacını giy", authority: 10, people: 5),
                    Choice("Töreni ertele", authority: -5, security: 5),
                    oncePerRun: true),

                Card("card_02_border_report", "Mareşal",
                    "Atlı gözcüler doğu geçitlerinin ötesinde hareketlilik bildirdi. Henüz kesinleşen bir şey yok.",
                    Choice("Geçitleri güçlendir", security: 10, wealth: -10),
                    Choice("Hazineyi koru", security: -10, wealth: 10)),

                Card("card_03_harvest", "Kâhya",
                    "Hasat sayımı bitti. Ambarlar geçen yıldan biraz daha dolu, ama ancak biraz.",
                    Choice("Aşarı topla", people: -10, wealth: 10),
                    Choice("Aşarı bağışla", people: 10, wealth: -10),
                    weight: 3),

                Card("card_04_tax_reform", "Hazinedar",
                    "Defterler birbirini tutmuyor. Yeni bir vergi açığı bir mevsimde kapatır.",
                    Choice("Vergiyi artır", people: -10, wealth: 15, add: Flags(FlagTaxesRaised)),
                    Choice("Oranları değiştirme", wealth: -5)),

                Card("card_05_tax_backlash", "Lonca Ustası",
                    "Yeni vergi atölyelerimizi boşalttı. Loncalar kararınızı yeniden düşünmenizi istiyor.",
                    Choice("Muhafızları gönder", authority: 5, people: -10, security: 5),
                    Choice("Vergiyi kaldır", people: 10, wealth: -10, remove: Flags(FlagTaxesRaised)),
                    conditions: Conditions(required: Flags(FlagTaxesRaised))),

                Card("card_06_amnesty", "Başkadı",
                    "Zindanlar ağzına kadar dolu. Bir af ilanı haftaya kalmadan hepsini boşaltır.",
                    Choice("Af ilan et", authority: -5, people: 10,
                        remove: Flags(FlagTaxesRaised)),
                    Choice("Kesin biçimde reddet", authority: 5, people: -5)),

                Card("card_07_general_visit", "Başkomutan",
                    "Daimî birlikler iki mevsimdir maaş alamadı, hükümdarım.",
                    Choice("Önce askeri öde", authority: 5, security: 10,
                        add: Flags(FlagArmyFavoured)),
                    Choice("Beklemelerini söyle", authority: -5, security: -5),
                    weight: 2),

                Card("card_08_peace_envoy", "Yabancı Elçi",
                    "Sarayım bir anlaşma öneriyor. Bu çekişmenin bir kış daha sürmesi kimseye yarar sağlamaz.",
                    Choice("Anlaşmayı imzala", people: 10, security: -5),
                    Choice("Elçiyi geri gönder", people: -5, security: 5),
                    conditions: Conditions(forbidden: Flags(FlagArmyFavoured))),

                Card("card_09_bread_riots", "Şehir Muhafızı",
                    "Ekmek kuyrukları bu sabah ayaklanmaya dönüştü. Emrinizi bekliyoruz.",
                    Choice("Ambarları aç", people: 15, wealth: -15),
                    Choice("Meydanı boşalt", authority: 10, people: -5, security: 5),
                    conditions: Conditions(ranges: Ranges(
                        new StatRange(StatType.People, StatBounds.Min, LowStatThreshold)))),

                Card("card_10_empty_vault", "Hazinedar",
                    "Hazinede yalnız yankı var. Duymak yerine görmek isterseniz sizi içeri götürebilirim.",
                    Choice("Mücevherleri erit", authority: -10, wealth: 15),
                    Choice("Dışarıdan borç al", security: -10, wealth: 10),
                    conditions: Conditions(ranges: Ranges(
                        new StatRange(StatType.Wealth, StatBounds.Min, LowStatThreshold)))),

                Card("card_11_spy_master", "Casusbaşı",
                    "Adamlarım çok şey işitiyor. Kulaklarını açık tutmanın bedeli de yüksek oluyor.",
                    Choice("Ağı destekle", security: 10, wealth: -10),
                    Choice("Bütçeyi kıs", security: -10, wealth: 10),
                    cooldown: 3),

                Card("card_12_festival", "Şenlik Nazırı",
                    "Bir şenlik şehrin neşesini yerine getirir. Hazinenin bir kanadını da boşaltır.",
                    Choice("Şenliği düzenle", people: 10, wealth: -10),
                    Choice("Şenliği iptal et", people: -10, wealth: 5),
                    cooldown: 5),

                Card("card_13_royal_wedding", "Başmabeyinci",
                    "İki evlilik teklifi geldi. İki taraf da bu ay içinde yanıt bekliyor.",
                    Choice("İttifak için evlen", authority: 5, people: -5, security: 10),
                    Choice("Aşk için evlen", authority: -10, people: 15),
                    oncePerRun: true),

                Card("card_14_plague", "Saray Hekimi",
                    "Üç mahallede aynı ateşli hastalık görüldü. Karar vermek için belki bir haftamız var.",
                    Choice("Mahalleleri karantinaya al", people: -10, security: 10, wealth: -5),
                    Choice("Yolları açık tut", people: -5, security: -10, wealth: 10),
                    oncePerRun: true),

                Card("card_15_inquisitor", "Sorgucu",
                    "Yalnızca bir soruşturma açmak için izin istiyorum. Bulgular ardından gelecektir.",
                    Choice("Soruşturmaya izin ver", authority: 5, people: -10),
                    Choice("Soruşturmayı reddet", authority: -5, people: 5),
                    forcedNext: "card_16_inquisitor_verdict"),

                Card("card_16_inquisitor_verdict", "Sorgucu",
                    "Soruşturma tamamlandı, hüküm yazıldı. Şimdi yalnızca mührünüzü bekliyor.",
                    Choice("Hükmü onayla", authority: 10, people: -10),
                    Choice("Hükmü boz", authority: -10, people: 10)),

                Card("card_17_ambassador", "Büyükelçi",
                    "Bir elimde antlaşma, diğerinde veda mektubum var. Seçiminizi yapın.",
                    Choice("Antlaşmayı sun", security: 5, wealth: -5,
                        forcedNext: "card_18_ambassador_accord"),
                    Choice("Elçiliği geri gönder", authority: 5, security: -5),
                    forcedNext: "card_19_ambassador_refusal"),

                Card("card_18_ambassador_accord", "Büyükelçi",
                    "Anlaşma imzalandı. Sarayım bunun karşılığında ne kazandığını bilmek isteyecek.",
                    Choice("Tüm koşulları yerine getir", people: 10, security: 5, wealth: -10),
                    Choice("Yalnız metne bağlı kal", authority: 5, people: -5)),

                Card("card_19_ambassador_refusal", "Mareşal",
                    "Elçilik ayrıldı. Fakat muhafızları evlerine değil, doğuya doğru sürdü.",
                    Choice("Birlikleri topla", security: 10, wealth: -10),
                    Choice("Bekle ve izle", security: -5, authority: -5)),

                Card("card_20_wandering_scholar", "Gezgin Bilgin",
                    "Bir oda, bir kandil ve bir yıl istiyorum. Karşılığında hükümdarlığınızın tarihini yazacağım.",
                    Choice("Çalışmayı destekle", authority: 5, wealth: -5),
                    Choice("Yoluna devam etsin", authority: -5, wealth: 5),
                    weight: 5)
            };

            // Sorted here so the generated catalogue has a stable asset diff between runs.
            cards.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
            return cards;
        }

        /// <summary>One ending per statistic per boundary: eight in total.</summary>
        public static List<EndingDefinition> CreateEndings()
        {
            return new List<EndingDefinition>(8)
            {
                Ending(StatType.Authority, StatBoundary.Min, "Sözü Dinlenmeyen Taç",
                    "Emirleriniz saray kapısından çıkmaz oldu. Zamanla siz de çıkamaz oldunuz."),
                Ending(StatType.Authority, StatBoundary.Max, "Sorgulanmayan Taht",
                    "Bir daha kimse size karşı çıkmadı. Bir daha kimse size gerçeği de söylemedi."),
                Ending(StatType.People, StatBoundary.Min, "İçe Açılan Kapılar",
                    "Kalabalık avluda durmadı; onu durdurmaya çalışan da olmadı."),
                Ending(StatType.People, StatBoundary.Max, "Omuzlarda Hükümdar",
                    "Halk sizi yönetilemeyecek kadar sevdi ve sonunda kendi kendini yönetti."),
                Ending(StatType.Security, StatBoundary.Min, "Savunmasız Mevsim",
                    "Surlar yerinde kaldı. Onları savunacak askerler kalmadı."),
                Ending(StatType.Security, StatBoundary.Max, "Gözetlenen Diyar",
                    "Her yol korundu, her muhafız izlendi ve sonunda hiçbir şey kıpırdamadı."),
                Ending(StatType.Wealth, StatBoundary.Min, "Yankılanan Hazine",
                    "Son gümüş tabak da tartıldı, satıldı ve yerine yenisi konmadı."),
                Ending(StatType.Wealth, StatBoundary.Max, "Altın Defter",
                    "Hazine, hizmet etmesi gereken krallıktan daha büyük hâle geldi.")
            };
        }

        public static string EndingId(StatType stat, StatBoundary boundary)
        {
            // Invariant casing: a culture-sensitive lowercase would reshape these IDs under a
            // Turkish locale.
            return string.Format(
                "ending_{0}_{1}",
                stat.ToString().ToLowerInvariant(),
                boundary.ToString().ToLowerInvariant());
        }

        // --- Construction helpers ------------------------------------------------

        private static CardDefinition Card(
            string id,
            string speaker,
            string bodyText,
            ChoiceDefinition left,
            ChoiceDefinition right,
            CardConditions conditions = null,
            int weight = CardDefinition.DefaultSelectionWeight,
            bool oncePerRun = false,
            int cooldown = 0,
            string forcedNext = "")
        {
            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            card.name = id;
            card.SetAuthoringData(
                id,
                PlaceholderTag + " " + speaker,
                bodyText,
                left,
                right,
                conditions,
                weight,
                oncePerRun,
                cooldown,
                forcedNext);

            return card;
        }

        private static ChoiceDefinition Choice(
            string previewText,
            int authority = 0,
            int people = 0,
            int security = 0,
            int wealth = 0,
            string[] add = null,
            string[] remove = null,
            string forcedNext = "")
        {
            return new ChoiceDefinition(
                previewText,
                new StatDeltas(authority, people, security, wealth),
                add,
                remove,
                forcedNext);
        }

        private static EndingDefinition Ending(
            StatType stat,
            StatBoundary boundary,
            string title,
            string bodyText)
        {
            string id = EndingId(stat, boundary);

            EndingDefinition ending = ScriptableObject.CreateInstance<EndingDefinition>();
            ending.name = id;
            ending.SetAuthoringData(id, PlaceholderTag + " " + title, bodyText, stat, boundary);

            return ending;
        }

        private static CardConditions Conditions(
            string[] required = null,
            string[] forbidden = null,
            StatRange[] ranges = null)
        {
            return new CardConditions(required, forbidden, ranges);
        }

        private static string[] Flags(params string[] flags)
        {
            return flags;
        }

        private static StatRange[] Ranges(params StatRange[] ranges)
        {
            return ranges;
        }
    }
}
