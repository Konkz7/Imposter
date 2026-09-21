using System;
using PartyGame.Core.Audio;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.Components
{
    /// <summary>Label, description and a minus / value / plus control. Used by every stepper setting.</summary>
    public class StepperControl : MonoBehaviour
    {
        private TextMeshProUGUI _value;
        private UiButton _minus;
        private UiButton _plus;
        private int _current;
        private int _min;
        private int _max;
        private int _step = 1;
        private string _suffix = string.Empty;

        public event Action<int> ValueChanged;

        public int Value => _current;

        public static StepperControl Create(Transform parent, string label, string description,
            int value, int min, int max, int step, string suffix, Action<int> onChanged)
        {
            var card = UIFactory.CreateCard("Stepper-" + label, parent);
            var control = card.gameObject.AddComponent<StepperControl>();
            control.Construct(card, label, description, value, min, max, step, suffix, onChanged);
            return control;
        }

        private void Construct(RectTransform card, string label, string description, int value,
            int min, int max, int step, string suffix, Action<int> onChanged)
        {
            _min = min;
            _max = Mathf.Max(min, max);
            _step = Mathf.Max(1, step);
            _suffix = suffix ?? string.Empty;
            _current = Mathf.Clamp(value, _min, _max);
            if (onChanged != null) ValueChanged += onChanged;

            var row = UIFactory.HorizontalGroup(card, "Row", Theme.SpaceS,
                new RectOffset((int)Theme.SpaceM, (int)Theme.SpaceM, (int)Theme.SpaceS, (int)Theme.SpaceS),
                TextAnchor.MiddleLeft);
            UIFactory.Stretch((RectTransform)row.transform);

            var column = UIFactory.VerticalGroup(row.transform, "Text", 2f, null, TextAnchor.MiddleLeft);
            UIFactory.SetSize(column.gameObject, flexibleWidth: 1f);

            var title = UIFactory.CreateText(column.transform, label, Theme.FontBody, Theme.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Bold, "Label");
            UIFactory.FitHeight(title.gameObject);

            if (!string.IsNullOrEmpty(description))
            {
                var caption = UIFactory.CreateText(column.transform, description, Theme.FontCaption,
                    Theme.TextMuted, TextAlignmentOptions.TopLeft, FontStyles.Normal, "Description");
                UIFactory.FitHeight(caption.gameObject);
            }

            _minus = UiButton.Create(row.transform, "-", ButtonStyle.Secondary, () => Adjust(-_step), 96f, null, "Minus");
            UIFactory.SetSize(_minus.gameObject, 96f, 96f, 96f, 96f);

            var valueHolder = UIFactory.CreateRect("Value", row.transform);
            UIFactory.SetSize(valueHolder.gameObject, 96f, 96f, 130f, 130f);
            _value = UIFactory.CreateFittedText(valueHolder, string.Empty, Theme.FontSubheading, Theme.FontCaption,
                Theme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold, "ValueText");
            UIFactory.Stretch(_value.rectTransform);

            _plus = UiButton.Create(row.transform, "+", ButtonStyle.Secondary, () => Adjust(_step), 96f, null, "Plus");
            UIFactory.SetSize(_plus.gameObject, 96f, 96f, 96f, 96f);

            UIFactory.SetSize(gameObject, 180f, 180f);
            Refresh();
        }

        /// <summary>Re-clamps when the player count changes an option's ceiling.</summary>
        public void SetRange(int min, int max)
        {
            _min = min;
            _max = Mathf.Max(min, max);
            var clamped = Mathf.Clamp(_current, _min, _max);
            if (clamped != _current)
            {
                _current = clamped;
                ValueChanged?.Invoke(_current);
            }
            Refresh();
        }

        private void Adjust(int delta)
        {
            var next = Mathf.Clamp(_current + delta, _min, _max);
            if (next == _current)
            {
                UiFeedback.Sound(SoundId.Error);
                return;
            }
            _current = next;
            Refresh();
            ValueChanged?.Invoke(_current);
        }

        private void Refresh()
        {
            if (_value != null) _value.text = _current + _suffix;
            if (_minus != null) _minus.Interactable = _current > _min;
            if (_plus != null) _plus.Interactable = _current < _max;
        }
    }

    /// <summary>Label, description and an on/off switch.</summary>
    public class ToggleSwitch : MonoBehaviour
    {
        private Image _track;
        private RectTransform _knob;
        private Image _knobImage;
        private TextMeshProUGUI _stateLabel;
        private bool _isOn;

        public event Action<bool> Changed;

        public bool IsOn => _isOn;

        public static ToggleSwitch Create(Transform parent, string label, string description, bool value,
            Action<bool> onChanged)
        {
            var card = UIFactory.CreateCard("Toggle-" + label, parent);
            var toggle = card.gameObject.AddComponent<ToggleSwitch>();
            toggle.Construct(card, label, description, value, onChanged);
            return toggle;
        }

        private void Construct(RectTransform card, string label, string description, bool value, Action<bool> onChanged)
        {
            _isOn = value;
            if (onChanged != null) Changed += onChanged;

            var row = UIFactory.HorizontalGroup(card, "Row", Theme.SpaceM,
                new RectOffset((int)Theme.SpaceM, (int)Theme.SpaceM, (int)Theme.SpaceS, (int)Theme.SpaceS),
                TextAnchor.MiddleLeft);
            UIFactory.Stretch((RectTransform)row.transform);

            var column = UIFactory.VerticalGroup(row.transform, "Text", 2f, null, TextAnchor.MiddleLeft);
            UIFactory.SetSize(column.gameObject, flexibleWidth: 1f);

            var title = UIFactory.CreateText(column.transform, label, Theme.FontBody, Theme.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Bold, "Label");
            UIFactory.FitHeight(title.gameObject);

            if (!string.IsNullOrEmpty(description))
            {
                var caption = UIFactory.CreateText(column.transform, description, Theme.FontCaption,
                    Theme.TextMuted, TextAlignmentOptions.TopLeft, FontStyles.Normal, "Description");
                UIFactory.FitHeight(caption.gameObject);
            }

            // The state is written out as well as shown, so the control never relies on colour alone.
            _stateLabel = UIFactory.CreateText(row.transform, string.Empty, Theme.FontCaption, Theme.TextSecondary,
                TextAlignmentOptions.Right, FontStyles.Bold, "State");
            UIFactory.SetSize(_stateLabel.gameObject, preferredWidth: 70f, minWidth: 70f);

            _track = UIFactory.CreatePanel("Track", row.transform, Theme.SurfaceSunken, Theme.RadiusPill);
            UIFactory.SetSize(_track.gameObject, 76f, 76f, 132f, 132f);

            // Anchored to the middle of the track so the on and off positions are symmetrical.
            _knob = UIFactory.CreateRect("Knob", _track.transform);
            _knob.anchorMin = new Vector2(0.5f, 0.5f);
            _knob.anchorMax = new Vector2(0.5f, 0.5f);
            _knob.pivot = new Vector2(0.5f, 0.5f);
            _knob.sizeDelta = new Vector2(56f, 56f);
            _knobImage = _knob.gameObject.AddComponent<Image>();
            _knobImage.sprite = UIGraphics.Circle(60);
            _knobImage.raycastTarget = false;

            var button = gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(Toggle);

            UIFactory.SetSize(gameObject, 172f, 172f);
            Refresh();
        }

        public void Toggle()
        {
            _isOn = !_isOn;
            UiFeedback.Sound(_isOn ? SoundId.Confirm : SoundId.Tap);
            Refresh();
            Changed?.Invoke(_isOn);
        }

        public void SetValue(bool value, bool notify = false)
        {
            _isOn = value;
            Refresh();
            if (notify) Changed?.Invoke(_isOn);
        }

        private void Refresh()
        {
            if (_track != null) _track.color = _isOn ? Theme.WithAlpha(Theme.Primary, 0.45f) : Theme.SurfaceSunken;
            if (_knobImage != null) _knobImage.color = _isOn ? Theme.Primary : Theme.TextMuted;
            if (_knob != null) _knob.anchoredPosition = new Vector2(_isOn ? 34f : -34f, 0f);
            if (_stateLabel != null)
            {
                _stateLabel.text = _isOn ? "ON" : "OFF";
                _stateLabel.color = _isOn ? Theme.Primary : Theme.TextMuted;
            }
        }
    }

    /// <summary>Label, description and a row of mutually exclusive option chips.</summary>
    public class OptionPicker : MonoBehaviour
    {
        private readonly System.Collections.Generic.List<SelectionCard> _cards =
            new System.Collections.Generic.List<SelectionCard>();

        private string _selectedId = string.Empty;

        public event Action<string> Changed;

        public string SelectedId => _selectedId;

        public static OptionPicker Create(Transform parent, string label, string description,
            System.Collections.Generic.IReadOnlyList<Core.Modes.SettingOption> options, string selectedId,
            Action<string> onChanged)
        {
            var holder = UIFactory.VerticalGroup(parent, "Options-" + label, Theme.SpaceXs);
            var picker = holder.gameObject.AddComponent<OptionPicker>();
            picker.Construct(holder.transform, label, description, options, selectedId, onChanged);
            UIFactory.FitHeight(holder.gameObject);
            return picker;
        }

        private void Construct(Transform holder, string label, string description,
            System.Collections.Generic.IReadOnlyList<Core.Modes.SettingOption> options, string selectedId,
            Action<string> onChanged)
        {
            _selectedId = selectedId ?? string.Empty;
            if (onChanged != null) Changed += onChanged;

            var title = UIFactory.CreateText(holder, label, Theme.FontBody, Theme.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Bold, "Label");
            UIFactory.FitHeight(title.gameObject);

            if (!string.IsNullOrEmpty(description))
            {
                var caption = UIFactory.CreateText(holder, description, Theme.FontCaption, Theme.TextMuted,
                    TextAlignmentOptions.TopLeft, FontStyles.Normal, "Description");
                UIFactory.FitHeight(caption.gameObject);
            }

            foreach (var option in options)
            {
                var captured = option;
                var card = SelectionCard.Create(holder, option.Label, option.Description, null, null,
                    Theme.Primary, null, 128f);
                card.Key = option.Id;
                card.Clicked += () => Select(captured.Id);
                _cards.Add(card);
            }

            Refresh();
        }

        public void Select(string optionId)
        {
            if (_selectedId == optionId) return;
            _selectedId = optionId;
            Refresh();
            Changed?.Invoke(_selectedId);
        }

        private void Refresh()
        {
            foreach (var card in _cards) card.Selected = card.Key == _selectedId;
        }
    }
}
