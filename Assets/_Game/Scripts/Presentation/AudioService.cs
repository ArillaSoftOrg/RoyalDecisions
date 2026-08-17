using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Plays audio cues through an assigned AudioSource, and stays quiet when it cannot.
    /// </summary>
    /// <remarks>
    /// Not a singleton and not discoverable: it is handed to whoever needs it, exactly like every
    /// other service in this codebase. No <c>Resources.Load</c>, no scene search, no static state.
    ///
    /// Volume and mute are exposed but deliberately not connected to <c>SaveService</c> — Phase 7
    /// owns that wiring.
    /// </remarks>
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        [Tooltip("Optional. Without it every cue resolves to NoAudioSource.")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Optional separate music source. Missing music remains silent.")]
        [SerializeField] private AudioSource musicSource;

        [Tooltip("Optional. Without it every cue resolves to NoLibrary.")]
        [SerializeField] private AudioCueLibrary cueLibrary;

        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 1f;

        [SerializeField] private bool muted;

        /// <summary>
        /// IDs already warned about. Most cards will never have audio, and a missing cue would
        /// otherwise log on every single decision for the whole run.
        /// </summary>
        private readonly HashSet<string> warnedCueIds = new HashSet<string>(StringComparer.Ordinal);

        private bool warnedMissingLibrary;
        private bool warnedMissingSource;

        public float Volume => volume;

        public bool IsMuted => muted;

        public float MusicVolume => musicVolume;

        /// <summary>Diagnostic: how many distinct cue IDs have been warned about.</summary>
        public int WarnedCueIdCount => warnedCueIds.Count;

        public AudioPlayResult Play(string audioEventId) => Play(audioEventId, volume);

        /// <summary>
        /// Plays a cue at an explicit volume instead of the configured SFX volume — used to preview
        /// a setting (e.g. the SFX slider) before it has been applied.
        /// </summary>
        public AudioPlayResult Play(string audioEventId, float volumeOverride)
        {
            // Most specific reason first, so a caller learns the actual problem rather than the
            // first one an arbitrary ordering happened to hit.
            if (string.IsNullOrEmpty(audioEventId))
            {
                return AudioPlayResult.NoCueId;
            }

            if (cueLibrary == null)
            {
                WarnOnce(ref warnedMissingLibrary, "No AudioCueLibrary is assigned; audio is silent.");
                return AudioPlayResult.NoLibrary;
            }

            if (!cueLibrary.TryGetCue(audioEventId, out AudioCue cue))
            {
                WarnOnceForCue(audioEventId, "is not in the cue library");
                return AudioPlayResult.UnknownCue;
            }

            if (cue.Clip == null)
            {
                WarnOnceForCue(audioEventId, "has no clip assigned");
                return AudioPlayResult.NullClip;
            }

            if (audioSource == null)
            {
                WarnOnce(ref warnedMissingSource, "No AudioSource is assigned; audio is silent.");
                return AudioPlayResult.NoAudioSource;
            }

            if (muted)
            {
                return AudioPlayResult.Muted;
            }

            audioSource.PlayOneShot(cue.Clip, PlayOneShotScaleFor(Mathf.Clamp01(volumeOverride)));
            return AudioPlayResult.Played;
        }

        /// <summary>
        /// <c>AudioSource.PlayOneShot</c> multiplies its scale argument by <c>audioSource.volume</c>,
        /// which already carries the configured SFX volume (see <see cref="SetVolume"/>). Passing
        /// <paramref name="desiredAbsoluteVolume"/> straight through would apply that volume twice
        /// (quadratic falloff). Dividing it out here makes the argument mean what callers expect: the
        /// absolute volume the clip should play at, independent of the current baseline.
        /// </summary>
        private float PlayOneShotScaleFor(float desiredAbsoluteVolume)
        {
            float baseline = audioSource.volume;
            return baseline > 0.0001f
                ? Mathf.Clamp01(desiredAbsoluteVolume / baseline)
                : desiredAbsoluteVolume;
        }

        public void SetVolume(float value)
        {
            volume = Mathf.Clamp01(value);

            if (audioSource != null)
            {
                audioSource.volume = volume;
            }
        }

        public void SetSfxVolume(float value) => SetVolume(value);

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }

        public void SetMuted(bool value)
        {
            muted = value;

            if (audioSource != null)
            {
                audioSource.mute = value;
            }
            if (musicSource != null)
            {
                musicSource.mute = value;
            }
        }

        public void SetMasterMuted(bool value) => SetMuted(value);

        public AudioPlayResult PlayMusic(string audioEventId, bool loop = true)
        {
            AudioPlayResult resolution = ResolveCue(audioEventId, out AudioClip clip);
            if (resolution != AudioPlayResult.Played)
            {
                return resolution;
            }
            if (musicSource == null)
            {
                WarnOnce(ref warnedMissingSource, "No music AudioSource is assigned; music is silent.");
                return AudioPlayResult.NoAudioSource;
            }
            if (muted)
            {
                return AudioPlayResult.Muted;
            }
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = musicVolume;
            musicSource.Play();
            return AudioPlayResult.Played;
        }

        public void StopMusic()
        {
            musicSource?.Stop();
        }

        private AudioPlayResult ResolveCue(string audioEventId, out AudioClip clip)
        {
            clip = null;
            if (string.IsNullOrEmpty(audioEventId))
            {
                return AudioPlayResult.NoCueId;
            }
            if (cueLibrary == null)
            {
                WarnOnce(ref warnedMissingLibrary, "No AudioCueLibrary is assigned; audio is silent.");
                return AudioPlayResult.NoLibrary;
            }
            if (!cueLibrary.TryGetCue(audioEventId, out AudioCue cue))
            {
                WarnOnceForCue(audioEventId, "is not in the cue library");
                return AudioPlayResult.UnknownCue;
            }
            if (cue.Clip == null)
            {
                WarnOnceForCue(audioEventId, "has no clip assigned");
                return AudioPlayResult.NullClip;
            }
            clip = cue.Clip;
            return AudioPlayResult.Played;
        }

        private void WarnOnce(ref bool alreadyWarned, string message)
        {
            if (alreadyWarned)
            {
                return;
            }

            alreadyWarned = true;
            Debug.LogWarning("[Audio] " + message, this);
        }

        private void WarnOnceForCue(string cueId, string reason)
        {
            if (!warnedCueIds.Add(cueId))
            {
                return;
            }

            Debug.LogWarning(
                string.Format("[Audio] Cue '{0}' {1}; playing nothing.", cueId, reason), this);
        }

#if UNITY_EDITOR
        /// <summary>Editor-only wiring hook shared by scene setup and tests.</summary>
        public void SetAuthoringReferences(
            AudioSource source,
            AudioCueLibrary library,
            AudioSource music = null)
        {
            audioSource = source;
            cueLibrary = library;
            musicSource = music;
        }
#endif
    }
}
