using System.Collections.Generic;
using RoyalDecisions.Data;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// Builds the five placeholder prologue slides in memory, with no asset I/O.
    /// </summary>
    /// <remarks>
    /// Deliberately separate from <c>PrologueSequenceSetup</c>: keeping the slide text and order free
    /// of <c>AssetDatabase</c> lets tests build and inspect the full set without writing a single
    /// file. This is disposable placeholder narrative — replacing it (or changing the slide count)
    /// must never require a code change once it lives in <c>DefaultPrologue.asset</c> (see
    /// <see cref="RoyalDecisions.Data.PrologueSequenceData"/>).
    /// </remarks>
    public static class PrologueDefaultContent
    {
        public const int SlideCount = 5;

        // Motion choices below reflect the real Prologue_01–05 illustrations (see
        // PrologueSequenceSetup's real-art sync), not an arbitrary alternating pattern:
        // - Slide 1/2/4: Zoom. Each has an important subject away from dead centre (the foreground
        //   couple bottom-left on 1, the doorway group right-of-centre on 2, the window threat
        //   top-left opposite the table on 4) — zooming around the frame centre keeps every subject
        //   proportionally in place instead of drifting toward an edge.
        // - Slide 3/5: Pan. Slide 3's standing figure sits on the vertical centreline, so a small
        //   horizontal drift adds life to an otherwise static, symmetric composition without
        //   approaching either edge. Slide 5 is the wide, roughly centred finale shot, where a slow
        //   drift reads as more cinematic than another zoom.
        private static readonly (string Subtitle, PrologueSlideMotion Motion)[] Slides =
        {
            ("Dünya, birkaç yıl içinde sessizliğe gömüldü.", PrologueSlideMotion.Zoom),
            ("Hayatta kalanlar, güvenli olduğunu düşündükleri son sığınaklara çekildi.", PrologueSlideMotion.Zoom),
            ("Fakat duvarlar açlığı, korkuyu ve insanların birbirine olan güvensizliğini durduramadı.",
                PrologueSlideMotion.Pan),
            ("Şimdi onların geleceğini belirleyecek kararlar senin elinde.", PrologueSlideMotion.Zoom),
            ("Her seçim bir hayat kurtarabilir... ya da her şeyi sona erdirebilir.", PrologueSlideMotion.Pan),
        };

        /// <summary>
        /// Builds the five slides, pairing them positionally with <paramref name="illustrations"/>
        /// when supplied. Passing null (or a shorter list) is a fully supported configuration — the
        /// remaining slides simply have no illustration, exactly like a hand-authored slide left
        /// blank in the Inspector.
        /// </summary>
        public static PrologueSlideData[] CreateSlides(IReadOnlyList<Sprite> illustrations = null)
        {
            PrologueSlideData[] result = new PrologueSlideData[Slides.Length];

            for (int i = 0; i < Slides.Length; i++)
            {
                Sprite sprite = illustrations != null && i < illustrations.Count ? illustrations[i] : null;
                result[i] = new PrologueSlideData(sprite, Slides[i].Subtitle, Slides[i].Motion);
            }

            return result;
        }
    }
}
