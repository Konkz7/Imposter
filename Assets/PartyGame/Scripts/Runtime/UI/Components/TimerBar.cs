using PartyGame.Core.Services;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.Components
{
    /// <summary>
    /// Countdown display bound to a <see cref="GameTimer"/>. The timer owns the time, this
    /// only draws it, so timing logic stays testable.
    /// </summary>
    public class TimerBar : MonoBehaviour
    {
        private RectTransform _fill;
        private Image _fillImage;
        private TextMeshProUGUI _label;
        private GameTimer _timer;
        private int _lastWholeSecond = -1;

        public static TimerBar Create(Transform parent)
        {
            var holder = UIFactory.VerticalGroup(parent, "TimerBar", Theme.SpaceXs);
            var bar = holder.gameObject.AddComponent<TimerBar>();
            bar.Construct(holder.transform);
            UIFactory.SetSize(holder.gameObject, 140f, 140f);
            return bar;
        }

        private void Construct(Transform holder)
        {
            _label = UIFactory.CreateFittedText(holder, "0", Theme.FontDisplay, Theme.FontHeading,
                Theme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold, "Remaining");
            UIFactory.SetSize(_label.gameObject, 96f, 96f);

            var track = UIFactory.CreatePanel("Track", holder, Theme.SurfaceSunken, Theme.RadiusPill);
            UIFactory.SetSize(track.gameObject, 18f, 18f);

            _fill = UIFactory.CreateRect("Fill", track.transform);
            _fill.anchorMin = new Vector2(0f, 0f);
            _fill.anchorMax = new Vector2(1f, 1f);
            _fill.pivot = new Vector2(0f, 0.5f);
            _fillImage = _fill.gameObject.AddComponent<Image>();
            _fillImage.sprite = UIGraphics.RoundedRect(Theme.RadiusPill);
            _fillImage.type = Image.Type.Sliced;
            _fillImage.color = Theme.Primary;
            _fillImage.raycastTarget = false;
            _fillImage.pixelsPerUnitMultiplier = 1f;
        }

        public void Bind(GameTimer timer)
        {
            _timer = timer;
            _lastWholeSecond = -1;
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_timer == null) return;

            var remaining = _timer.Remaining;
            var whole = Mathf.CeilToInt(remaining);
            if (whole != _lastWholeSecond)
            {
                _lastWholeSecond = whole;
                if (_label != null) _label.text = GameTimer.Format(remaining);
                if (whole <= 5 && whole > 0) UiFeedback.Sound(Core.Audio.SoundId.CountdownTick);
            }

            var normalised = _timer.NormalisedRemaining;
            if (_fill != null) _fill.anchorMax = new Vector2(Mathf.Clamp01(normalised), 1f);

            var colour = normalised > 0.4f ? Theme.Primary : normalised > 0.15f ? Theme.Warning : Theme.Danger;
            if (_fillImage != null) _fillImage.color = colour;
            if (_label != null) _label.color = normalised > 0.15f ? Theme.TextPrimary : Theme.Danger;
        }
    }
}
