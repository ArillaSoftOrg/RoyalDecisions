using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Periodically dips the alpha of a set of overlay graphics (e.g. the CRT scanline/vignette
    /// pair) to read as an old-monitor flicker, plus an on-demand burst for syncing to an event
    /// like a settings-tab switch.
    /// </summary>
    /// <remarks>
    /// Purely decorative — never decides what's on screen, only how the CRT overlay flickers.
    /// Mirrors <see cref="PanelFadeAnimator"/>'s shape: unscaled time so menu feedback keeps moving
    /// if gameplay time is paused, and a reduced-motion mode that calms the effect down (longer
    /// gaps, shorter/softer bursts) instead of removing it outright.
    /// </remarks>
    public sealed class CrtFlickerAnimator : MonoBehaviour
    {
        private const float ReducedMotionIntervalScale = 2.5f;
        private const float ReducedMotionDurationScale = 0.5f;
        private const float ReducedMotionDipSoften = 0.6f;

        [SerializeField] private Graphic[] targets = System.Array.Empty<Graphic>();
        [SerializeField] private float minInterval = 2.5f;
        [SerializeField] private float maxInterval = 6f;
        [SerializeField] private float burstDuration = 0.15f;
        [SerializeField] private float dipMultiplier = 0.35f;

        private readonly Dictionary<Graphic, float> baseAlphas = new Dictionary<Graphic, float>();
        private bool baseAlphasCaptured;

        private float defaultMinInterval;
        private float defaultMaxInterval;
        private float defaultBurstDuration;
        private float defaultDipMultiplier;
        private bool reducedMotionDefaultsCaptured;

        private float timeUntilNextBurst;
        private bool nextBurstScheduled;
        private float burstElapsed = -1f;

        /// <summary>Forces an immediate flicker burst, outside the normal random cycle — used to
        /// sync a visible flicker to an event such as a settings-tab switch.</summary>
        public void TriggerBurst()
        {
            EnsureBaseAlphasCaptured();
            burstElapsed = 0f;
        }

        /// <summary>Calms the effect down rather than disabling it: longer gaps between bursts, a
        /// shorter and softer burst.</summary>
        public void SetReducedMotion(bool enabled)
        {
            if (!reducedMotionDefaultsCaptured)
            {
                defaultMinInterval = minInterval;
                defaultMaxInterval = maxInterval;
                defaultBurstDuration = burstDuration;
                defaultDipMultiplier = dipMultiplier;
                reducedMotionDefaultsCaptured = true;
            }

            minInterval = CrtFlickerMath.ScaleForReducedMotion(
                defaultMinInterval, enabled, ReducedMotionIntervalScale);
            maxInterval = CrtFlickerMath.ScaleForReducedMotion(
                defaultMaxInterval, enabled, ReducedMotionIntervalScale);
            burstDuration = CrtFlickerMath.ScaleForReducedMotion(
                defaultBurstDuration, enabled, ReducedMotionDurationScale);
            dipMultiplier = enabled
                ? Mathf.Lerp(defaultDipMultiplier, 1f, ReducedMotionDipSoften)
                : defaultDipMultiplier;
        }

        /// <summary>Advances the flicker state machine by <paramref name="unscaledDeltaTime"/>.
        /// Called from <see cref="Update"/> at runtime; called directly by tests.</summary>
        public void Tick(float unscaledDeltaTime)
        {
            EnsureBaseAlphasCaptured();

            if (burstElapsed >= 0f)
            {
                burstElapsed += unscaledDeltaTime;
                float duration = Mathf.Max(0.01f, burstDuration);

                if (burstElapsed >= duration)
                {
                    burstElapsed = -1f;
                    ApplyAlphaMultiplier(1f);
                    ScheduleNextBurst();
                    return;
                }

                float progress = Mathf.Clamp01(burstElapsed / duration);
                ApplyAlphaMultiplier(CrtFlickerMath.BurstAlphaMultiplier(progress, dipMultiplier));
                return;
            }

            if (!nextBurstScheduled)
            {
                ScheduleNextBurst();
            }

            timeUntilNextBurst -= unscaledDeltaTime;
            if (timeUntilNextBurst <= 0f)
            {
                burstElapsed = 0f;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Tick(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            burstElapsed = -1f;
            if (baseAlphasCaptured)
            {
                ApplyAlphaMultiplier(1f);
            }
        }

        private void ScheduleNextBurst()
        {
            timeUntilNextBurst = Random.Range(minInterval, maxInterval);
            nextBurstScheduled = true;
        }

        private void EnsureBaseAlphasCaptured()
        {
            if (baseAlphasCaptured)
            {
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    baseAlphas[targets[i]] = targets[i].color.a;
                }
            }

            baseAlphasCaptured = true;
        }

        private void ApplyAlphaMultiplier(float multiplier)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                Graphic target = targets[i];
                if (target == null)
                {
                    continue;
                }

                float baseAlpha = baseAlphas.TryGetValue(target, out float captured)
                    ? captured
                    : target.color.a;

                Color colour = target.color;
                colour.a = baseAlpha * multiplier;
                target.color = colour;
            }
        }

#if UNITY_EDITOR
        public void SetAuthoringReferences(
            Graphic[] flickerTargets,
            float minIntervalSeconds = 2.5f,
            float maxIntervalSeconds = 6f,
            float burstDurationSeconds = 0.15f,
            float dipMultiplierValue = 0.35f)
        {
            targets = flickerTargets ?? System.Array.Empty<Graphic>();
            minInterval = minIntervalSeconds;
            maxInterval = maxIntervalSeconds;
            burstDuration = burstDurationSeconds;
            dipMultiplier = dipMultiplierValue;
        }
#endif
    }
}
