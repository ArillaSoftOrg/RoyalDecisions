using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Keeps a RectTransform inside the device safe area.
    /// </summary>
    /// <remarks>
    /// Player Settings already enable rendering outside the safe area, so the app draws under
    /// notches and cutouts. This is not optional polish — without it the HUD sits under the camera
    /// hole on most modern phones.
    ///
    /// <see cref="Update"/> compares three structs and returns; it allocates nothing and does no
    /// work while the safe area is unchanged, which is most frames of most sessions.
    /// </remarks>
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("Defaults to this object's own RectTransform when left empty.")]
        [SerializeField] private RectTransform target;

        private Rect lastSafeArea;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private bool hasApplied;

        public RectTransform Target => target;

        private void Awake()
        {
            EnsureTarget();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            ApplyIfChanged();
        }

        /// <summary>Applies the current screen's safe area unconditionally.</summary>
        public bool Apply()
        {
            return ApplyTo(Screen.safeArea, Screen.width, Screen.height);
        }

        /// <summary>Applies only when the safe area or screen dimensions have actually moved.</summary>
        public bool ApplyIfChanged()
        {
            Rect safeArea = Screen.safeArea;
            int width = Screen.width;
            int height = Screen.height;

            if (hasApplied
                && safeArea == lastSafeArea
                && width == lastScreenWidth
                && height == lastScreenHeight)
            {
                return false;
            }

            return ApplyTo(safeArea, width, height);
        }

        /// <summary>
        /// The seam tests drive: supplies the safe area explicitly instead of reading the device.
        /// </summary>
        public bool ApplyTo(Rect safeArea, int screenWidth, int screenHeight)
        {
            EnsureTarget();

            if (target == null)
            {
                return false;
            }

            if (!SafeAreaMath.TryCalculateAnchors(
                    safeArea, screenWidth, screenHeight, out Vector2 min, out Vector2 max))
            {
                return false;
            }

            target.anchorMin = min;
            target.anchorMax = max;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;

            lastSafeArea = safeArea;
            lastScreenWidth = screenWidth;
            lastScreenHeight = screenHeight;
            hasApplied = true;
            return true;
        }

        private void EnsureTarget()
        {
            if (target == null)
            {
                target = transform as RectTransform;
            }
        }
    }
}
