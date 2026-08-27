using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Drives a track/knob pill switch's colour and knob position from a Toggle's state. Unity's
    /// built-in Toggle only supports a single show/hide graphic, not a sliding two-position
    /// control, so this small component owns that purely-visual behaviour.
    /// </summary>
    /// <remarks>
    /// The slide/colour change animates on a user-triggered flip, but snaps instantly when the
    /// panel first renders the toggle's starting state (<see cref="OnEnable"/>) — the same
    /// distinction real OS toggles make. This animation is intentionally never gated behind
    /// "reduced motion": a toggle's slide communicates its new state rather than decorating the
    /// screen, and platform reduced-motion settings exempt exactly this kind of functional,
    /// state-communicating control.
    /// </remarks>
    [RequireComponent(typeof(Toggle))]
    public sealed class ToggleSwitchVisual : MonoBehaviour
    {
        [SerializeField] private Graphic track;
        [SerializeField] private RectTransform knob;
        [SerializeField] private Color onColour = new Color(0.78f, 0.58f, 0.18f, 1f);
        [SerializeField] private Color offColour = new Color32(0x2A, 0x2F, 0x3A, 0xFF);
        [SerializeField] private float knobInset = 4f;

        [Tooltip("Seconds for the knob slide/colour crossfade on a user-triggered flip. Zero snaps "
            + "instantly.")]
        [Min(0f)]
        [SerializeField] private float animationDuration = 0.15f;

        private Toggle toggle;
        private bool animating;
        private float animationTimer;
        private Vector2 knobStartPosition;
        private Vector2 knobTargetPosition;
        private Color trackStartColour;
        private Color trackTargetColour;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
        }

        private void OnEnable()
        {
            if (toggle == null)
            {
                return;
            }
            toggle.onValueChanged.AddListener(HandleValueChanged);
            ApplyImmediate(toggle.isOn);
        }

        private void OnDisable()
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(HandleValueChanged);
            }
            animating = false;
        }

        private void HandleValueChanged(bool isOn)
        {
            if (track == null)
            {
                return;
            }

            trackStartColour = track.color;
            trackTargetColour = isOn ? onColour : offColour;
            if (knob != null)
            {
                knobStartPosition = knob.anchoredPosition;
                float travel = Travel();
                knobTargetPosition = new Vector2(isOn ? travel : -travel, knob.anchoredPosition.y);
            }

            animationTimer = 0f;
            animating = animationDuration > 0f;
            if (!animating)
            {
                ApplyImmediate(isOn);
            }
        }

        private void Update()
        {
            // Returns on the first line for every frame the knob is at rest, which is nearly all
            // of them. No allocation on either path.
            if (!animating)
            {
                return;
            }

            animationTimer += Time.deltaTime;
            float t = Mathf.Clamp01(animationTimer / animationDuration);
            if (track != null)
            {
                track.color = Color.Lerp(trackStartColour, trackTargetColour, t);
            }
            if (knob != null)
            {
                knob.anchoredPosition = Vector2.Lerp(knobStartPosition, knobTargetPosition, t);
            }

            if (t >= 1f)
            {
                animating = false;
            }
        }

        private void ApplyImmediate(bool isOn)
        {
            animating = false;
            if (track != null)
            {
                track.color = isOn ? onColour : offColour;
            }
            if (knob != null && track != null)
            {
                float travel = Travel();
                knob.anchoredPosition = new Vector2(isOn ? travel : -travel, knob.anchoredPosition.y);
            }
        }

        private float Travel()
        {
            if (track == null || knob == null)
            {
                return 0f;
            }
            return Mathf.Max(
                0f, (track.rectTransform.rect.width - knob.rect.width) * 0.5f - knobInset);
        }
    }
}
