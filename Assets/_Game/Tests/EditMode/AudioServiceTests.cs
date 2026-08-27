using NUnit.Framework;
using RoyalDecisions.Presentation;
using UnityEngine;

namespace RoyalDecisions.Tests.EditMode
{
    [TestFixture]
    public class AudioServiceTests
    {
        private AudioCueLibrary library;

        [TearDown]
        public void TearDown()
        {
            if (library != null)
            {
                Object.DestroyImmediate(library);
                library = null;
            }

            PresentationTestObjects.DestroyAll();
        }

        private AudioCueLibrary Library(params AudioCue[] cues)
        {
            library = ScriptableObject.CreateInstance<AudioCueLibrary>();
            library.SetAuthoringData(cues);
            return library;
        }

        private static AudioService Service(AudioSource source, AudioCueLibrary cueLibrary)
        {
            AudioService service = PresentationTestObjects.CreateComponent<AudioService>("Audio");
            service.SetAuthoringReferences(source, cueLibrary);
            return service;
        }

        private static AudioSource Source()
        {
            return PresentationTestObjects.CreateComponent<AudioSource>("Source");
        }

        private static AudioClip Clip()
        {
            AudioClip clip = AudioClip.Create("cue", 64, 1, 44100, false);
            return clip;
        }

        // --- Every way to have no sound ---------------------------------------

        [Test]
        public void AnEmptyCueIdIsNotAnError()
        {
            AudioService service = Service(Source(), Library());

            // Most cards carry no audio event at all, so this must be quiet and unremarkable.
            Assert.That(service.Play(string.Empty), Is.EqualTo(AudioPlayResult.NoCueId));
            Assert.That(service.Play(null), Is.EqualTo(AudioPlayResult.NoCueId));
            Assert.That(service.WarnedCueIdCount, Is.Zero, "an absent cue must not warn");
        }

        [Test]
        public void NoLibraryIsReported()
        {
            AudioService service = Service(Source(), null);

            Assert.That(service.Play("sfx_seal"), Is.EqualTo(AudioPlayResult.NoLibrary));
        }

        [Test]
        public void AnUnknownCueIsReported()
        {
            AudioService service = Service(Source(), Library(new AudioCue("sfx_other", Clip())));

            Assert.That(service.Play("sfx_seal"), Is.EqualTo(AudioPlayResult.UnknownCue));
        }

        [Test]
        public void ACueWithNoClipIsReported()
        {
            AudioService service = Service(Source(), Library(new AudioCue("sfx_seal", null)));

            Assert.That(service.Play("sfx_seal"), Is.EqualTo(AudioPlayResult.NullClip));
        }

        [Test]
        public void AMissingAudioSourceIsReported()
        {
            AudioService service = Service(null, Library(new AudioCue("sfx_seal", Clip())));

            Assert.That(service.Play("sfx_seal"), Is.EqualTo(AudioPlayResult.NoAudioSource));
        }

        [Test]
        public void NothingConfiguredAtAllStillDoesNotThrow()
        {
            AudioService bare = PresentationTestObjects.CreateComponent<AudioService>("BareAudio");

            Assert.That(() => bare.Play("sfx_seal"), Throws.Nothing);
            Assert.That(() => bare.SetVolume(0.5f), Throws.Nothing);
            Assert.That(() => bare.SetMuted(true), Throws.Nothing);
            Assert.That(() => bare.StopSfx(), Throws.Nothing);
        }

        [Test]
        public void StopSfxWithAnAssignedSourceDoesNotThrow()
        {
            AudioService service = Service(Source(), Library(new AudioCue("sfx_seal", Clip())));
            service.Play("sfx_seal");

            Assert.That(() => service.StopSfx(), Throws.Nothing);
        }

        [Test]
        public void MutedAudioResolvesButDoesNotPlay()
        {
            AudioService service = Service(Source(), Library(new AudioCue("sfx_seal", Clip())));
            service.SetMuted(true);

            Assert.That(service.Play("sfx_seal"), Is.EqualTo(AudioPlayResult.Muted));
        }

        // --- Lookup ---------------------------------------------------------------

        [Test]
        public void CueLookupIsOrdinal()
        {
            AudioService service = Service(Source(), Library(new AudioCue("sfx_seal", Clip())));

            Assert.That(service.Play("SFX_Seal"), Is.EqualTo(AudioPlayResult.UnknownCue),
                "cue IDs must not fold case, on any device locale");
        }

        [Test]
        public void ADuplicateCueIdKeepsTheFirstEntry()
        {
            AudioClip first = Clip();
            AudioCueLibrary cues = Library(
                new AudioCue("sfx_seal", first),
                new AudioCue("sfx_seal", null));

            Assert.That(cues.TryGetCue("sfx_seal", out AudioCue cue), Is.True);
            Assert.That(cue.Clip, Is.SameAs(first));
        }

        [Test]
        public void AnEmptyLibraryResolvesNothing()
        {
            AudioCueLibrary cues = Library();

            Assert.That(cues.TryGetCue("sfx_seal", out _), Is.False);
            Assert.That(cues.TryGetCue(string.Empty, out _), Is.False);
            Assert.That(cues.TryGetCue(null, out _), Is.False);
        }

        // --- Warning deduplication --------------------------------------------------

        [Test]
        public void TheSameMissingCueWarnsOnlyOnce()
        {
            AudioService service = Service(Source(), Library());

            for (int i = 0; i < 25; i++)
            {
                service.Play("sfx_missing");
            }

            Assert.That(service.WarnedCueIdCount, Is.EqualTo(1),
                "a cue missing on every card would otherwise log for the whole run");
        }

        [Test]
        public void DistinctMissingCuesEachWarnOnce()
        {
            AudioService service = Service(Source(), Library());

            service.Play("sfx_a");
            service.Play("sfx_b");
            service.Play("sfx_a");

            Assert.That(service.WarnedCueIdCount, Is.EqualTo(2));
        }

        // --- Volume and mute ----------------------------------------------------------

        [TestCase(-1f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(1f, 1f)]
        [TestCase(3f, 1f)]
        public void VolumeIsClamped(float input, float expected)
        {
            AudioService service = Service(Source(), Library());

            service.SetVolume(input);

            Assert.That(service.Volume, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void MuteIsTracked()
        {
            AudioService service = Service(Source(), Library());

            Assert.That(service.IsMuted, Is.False);
            service.SetMuted(true);
            Assert.That(service.IsMuted, Is.True);
        }

        [Test]
        public void MusicAndSfxVolumesAreIndependentAndClamped()
        {
            AudioService service = Service(Source(), Library());

            service.SetSfxVolume(0.25f);
            service.SetMusicVolume(2f);

            Assert.That(service.Volume, Is.EqualTo(0.25f));
            Assert.That(service.MusicVolume, Is.EqualTo(1f));
        }

        [Test]
        public void MasterVolumeDefaultsToFullSoExistingRawVolumesAreUnaffected()
        {
            AudioService service = Service(Source(), Library());

            service.SetSfxVolume(0.4f);

            Assert.That(service.MasterVolume, Is.EqualTo(1f));
            Assert.That(service.Volume, Is.EqualTo(0.4f),
                "the raw per-channel setting must read back unscaled, independent of the master");
        }

        [Test]
        public void MasterVolumeIsClamped()
        {
            AudioService service = Service(Source(), Library());

            service.SetMasterVolume(3f);
            Assert.That(service.MasterVolume, Is.EqualTo(1f));

            service.SetMasterVolume(-1f);
            Assert.That(service.MasterVolume, Is.EqualTo(0f));
        }

        // --- The silent implementation --------------------------------------------------

        [Test]
        public void SilentServiceAcceptsEverything()
        {
            IAudioService silent = new SilentAudioService();

            Assert.That(silent.Play("anything"), Is.EqualTo(AudioPlayResult.NoLibrary));
            Assert.That(silent.Play(string.Empty), Is.EqualTo(AudioPlayResult.NoCueId));

            silent.SetVolume(5f);
            Assert.That(silent.Volume, Is.EqualTo(1f));

            silent.SetMuted(true);
            Assert.That(silent.IsMuted, Is.True);
            silent.SetMusicVolume(-1f);
            Assert.That(silent.MusicVolume, Is.Zero);
            Assert.That(silent.PlayMusic("music"), Is.EqualTo(AudioPlayResult.NoLibrary));
            Assert.That(() => silent.StopMusic(), Throws.Nothing);
        }
    }
}
