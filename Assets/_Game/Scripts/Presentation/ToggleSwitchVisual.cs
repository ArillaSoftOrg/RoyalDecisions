using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Drives a track/knob pill switch's colour and knob position from a Toggle's state. Unity's
    /// built-in Toggle only supports a single show/hide graphic, not a sliding two-position
    /// control, so this small component owns that purely-visual behaviour.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public sealed class ToggleSwitchVisual : MonoBehaviour
    {
        [SerializeField] private Graphic track;
        [SerializeField] private RectTransform knob;
        [SerializeField] private Color onColour = new Color(0.78f, 0.58f, 0.18f, 1f);
        [SerializeField] private Color offColour = new Color32(0x2A, 0x2F, 0x3A, 0xFF);
        [SerializeField] private float knobInset = 4f;

        private Toggle toggle;

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
            toggle.onValueChanged.AddListener(Apply);
            Apply(toggle.isOn);
        }

        private void OnDisable()
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(Apply);
            }
        }

        private void Apply(bool isOn)
        {
            if (track != null)
            {
                track.color = isOn ? onColour : offColour;
            }
            if (knob != null && track != null)
            {
                float travel = Mathf.Max(
                    0f, (track.rectTransform.rect.width - knob.rect.width) * 0.5f - knobInset);
                knob.anchoredPosition = new Vector2(isOn ? travel : -travel, knob.anchoredPosition.y);
            }
        }
    }
}
