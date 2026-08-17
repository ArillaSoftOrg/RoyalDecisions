using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Drives device vibration for <see cref="HapticFeedbackLevel"/> tiers.
    /// </summary>
    /// <remarks>
    /// Prefers Android's amplitude-controlled <c>VibrationEffect</c> (API 26+) so a light UI tick,
    /// a normal confirmation and a critical warning actually feel different, falls back to the
    /// legacy duration-only <c>Vibrator.vibrate(long)</c> on older Android (down to this project's
    /// minSdk 25), and falls back again to <see cref="Handheld.Vibrate"/> everywhere else. The Light
    /// tier stays silent on that last fallback: a full-strength buzz for a subtle drag tick would be
    /// worse than no feedback at all. Every native call is defensive — OEM vibrator services are
    /// known to throw in ways this project cannot reproduce or test — so a failure degrades to the
    /// next fallback (logged once) rather than crashing.
    /// </remarks>
    public sealed class UnityHapticService : IHapticService
    {
        public bool IsEnabled { get; private set; } = true;

        public void SetEnabled(bool enabled) => IsEnabled = enabled;

        public void Pulse(HapticFeedbackLevel level = HapticFeedbackLevel.Standard)
        {
            if (!IsEnabled)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            HapticProfile profile = HapticProfile.For(level);
            if (TryVibrateWithAmplitude(profile) || TryVibrateLegacy(profile))
            {
                return;
            }
#endif
            // Editor, non-Android platforms, or every native attempt above failed.
            if (level != HapticFeedbackLevel.Light)
            {
                Handheld.Vibrate();
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private const int MinAmplitudeControlSdk = 26;
        private const string VibratorServiceName = "vibrator";

        private bool warnedOnce;

        private bool TryVibrateWithAmplitude(HapticProfile profile)
        {
            if (AndroidSdkInt() < MinAmplitudeControlSdk)
            {
                return false;
            }

            try
            {
                using (AndroidJavaObject vibrator = GetVibrator())
                {
                    if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
                    {
                        return false;
                    }

                    using (AndroidJavaClass effectClass =
                        new AndroidJavaClass("android.os.VibrationEffect"))
                    using (AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot", profile.DurationMilliseconds, profile.Amplitude))
                    {
                        vibrator.Call("vibrate", effect);
                        return true;
                    }
                }
            }
            catch (System.Exception exception)
            {
                WarnOnce("Amplitude-controlled vibration failed: " + exception.Message);
                return false;
            }
        }

        private bool TryVibrateLegacy(HapticProfile profile)
        {
            try
            {
                using (AndroidJavaObject vibrator = GetVibrator())
                {
                    if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
                    {
                        return false;
                    }

                    vibrator.Call("vibrate", profile.DurationMilliseconds);
                    return true;
                }
            }
            catch (System.Exception exception)
            {
                WarnOnce("Legacy vibration failed: " + exception.Message);
                return false;
            }
        }

        private static AndroidJavaObject GetVibrator()
        {
            using (AndroidJavaClass unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity =
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                return activity?.Call<AndroidJavaObject>("getSystemService", VibratorServiceName);
            }
        }

        private static int AndroidSdkInt()
        {
            using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
        }

        private void WarnOnce(string message)
        {
            if (warnedOnce)
            {
                return;
            }
            warnedOnce = true;
            Debug.LogWarning("[Haptics] " + message);
        }
#endif
    }
}
