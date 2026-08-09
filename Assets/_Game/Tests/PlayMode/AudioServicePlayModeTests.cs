using System.Collections;
using NUnit.Framework;
using RoyalDecisions.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoyalDecisions.Tests.PlayMode
{
    /// <summary>
    /// Confirms audio behaviour against a real AudioSource, which only exists at runtime.
    /// </summary>
    [TestFixture]
    public class AudioServicePlayModeTests
    {
        private GameObject host;
        private AudioCueLibrary library;

        [TearDown]
        public void TearDown()
        {
            if (host != null)
            {
                Object.Destroy(host);
                host = null;
            }

            if (library != null)
            {
                Object.Destroy(library);
                library = null;
            }
        }

        private AudioService CreateService(bool withSource, bool withClip)
        {
            host = new GameObject("Audio");

            AudioSource source = null;
            if (withSource)
            {
                source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
            }

            library = ScriptableObject.CreateInstance<AudioCueLibrary>();
            library.SetAuthoringData(new[]
            {
                new AudioCue("sfx_seal", withClip ? AudioClip.Create("sfx_seal", 4410, 1, 44100, false) : null)
            });

            AudioService service = host.AddComponent<AudioService>();
            service.SetAuthoringReferences(source, library);
            return service;
        }

        [UnityTest]
        public IEnumerator AKnownCuePlaysThroughTheAudioSource()
        {
            AudioService service = CreateService(withSource: true, withClip: true);
            yield return null;

            Assert.That(service.Play("sfx_seal"), Is.EqualTo(AudioPlayResult.Played));
        }

        [UnityTest]
        public IEnumerator AMissingCueLeavesTheSourceSilent()
        {
            AudioService service = CreateService(withSource: true, withClip: true);
            AudioSource source = host.GetComponent<AudioSource>();
            yield return null;

            Assert.That(service.Play("sfx_absent"), Is.EqualTo(AudioPlayResult.UnknownCue));
            yield return null;

            Assert.That(source.isPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator MutingSuppressesPlayback()
        {
            AudioService service = CreateService(withSource: true, withClip: true);
            AudioSource source = host.GetComponent<AudioSource>();
            yield return null;

            service.SetMuted(true);
            Assert.That(service.Play("sfx_seal"), Is.EqualTo(AudioPlayResult.Muted));
            yield return null;

            Assert.That(source.isPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator NoAudioSourceIsSurvivableAtRuntime()
        {
            AudioService service = CreateService(withSource: false, withClip: true);
            yield return null;

            Assert.That(service.Play("sfx_seal"), Is.EqualTo(AudioPlayResult.NoAudioSource));
        }

        [UnityTest]
        public IEnumerator VolumeReachesTheAudioSource()
        {
            AudioService service = CreateService(withSource: true, withClip: true);
            AudioSource source = host.GetComponent<AudioSource>();
            yield return null;

            service.SetVolume(0.25f);

            Assert.That(source.volume, Is.EqualTo(0.25f).Within(0.0001f));
        }
    }
}
