using System;
using PartyGame.Core.Audio;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PartyGame.UI.Components
{
    public enum ButtonStyle
    {
        Primary = 0,
        Secondary = 1,
        Ghost = 2,
        Danger = 3,
        Success = 4,
        Subtle = 5
    }

    /// <summary>
    /// The one button in the app. Owns its own styling, press feedback and click sound so no
    /// screen has to remember to add them.
    /// </summary>
    public class UiButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Image _background;
        private Image _outline;
        private TextMeshProUGUI _label;
        private TextMeshProUGUI _sublabel;
        private Button _button;
        private ButtonStyle _style;
        private bool _pressed;

        public event Action Clicked;

        public RectTransform Rect => (RectTransform)transform;
        public Button Button => _button;

        public bool Interactable
        {
            get => _button != null && _button.interactable;
            set
            {
                if (_button == null) return;
                _button.interactable = value;
                ApplyStyle(_style);
            }
        }

        public static UiButton Create(Transform parent, string label, ButtonStyle style, Action onClick,
            float height = Theme.ButtonHeight, string sublabel = null, string name = null)
        {
            var root = UIFactory.CreatePanel(name ?? ("Button-" + label), parent, Color.white, Theme.RadiusMedium);
            var button = root.gameObject.AddComponent<UiButton>();
            button.Construct(root, label, sublabel, style, onClick, height);
            return button;
        }

        private void Construct(Image background, string label, string sublabel, ButtonStyle style, Action onClick, float height)
        {
            _background = background;
            _background.raycastTarget = true;

            _outline = UIFactory.CreateOutline("Outline", transform, Color.clear, Theme.RadiusMedium);
            UIFactory.Stretch(_outline.rectTransform);

            var content = UIFactory.VerticalGroup(transform, "Content", 2f,
                new RectOffset((int)Theme.SpaceM, (int)Theme.SpaceM, 0, 0), TextAnchor.MiddleCenter);
            UIFactory.Stretch((RectTransform)content.transform);
            content.childForceExpandHeight = false;

            _label = UIFactory.CreateText(content.transform, label, Theme.FontSubheading, Color.white,
                TextAlignmentOptions.Center, FontStyles.Bold, "Label");
            _label.enableAutoSizing = true;
            _label.fontSizeMin = Theme.FontLabel;
            _label.fontSizeMax = Theme.FontSubheading;
            UIFactory.FitHeight(_label.gameObject);

            if (!string.IsNullOrEmpty(sublabel))
            {
                _sublabel = UIFactory.CreateText(content.transform, sublabel, Theme.FontCaption,
                    Theme.TextSecondary, TextAlignmentOptions.Center, FontStyles.Normal, "Sublabel");
                UIFactory.FitHeight(_sublabel.gameObject);
            }

            _button = gameObject.AddComponent<Button>();
            _button.targetGraphic = _background;
            _button.transition = Selectable.Transition.None;
            _button.onClick.AddListener(HandleClick);
            if (onClick != null) Clicked += onClick;

            UIFactory.SetSize(gameObject, height, height);
            ApplyStyle(style);
        }

        private void HandleClick()
        {
            UiFeedback.Sound(_style == ButtonStyle.Primary ? SoundId.Confirm : SoundId.Tap);
            Clicked?.Invoke();
        }

        public void SetOnClick(Action onClick)
        {
            Clicked = null;
            if (onClick != null) Clicked += onClick;
        }

        public void SetLabel(string label)
        {
            if (_label != null) _label.text = label ?? string.Empty;
        }

        public void SetSublabel(string sublabel)
        {
            if (_sublabel != null) _sublabel.text = sublabel ?? string.Empty;
        }

        public void SetStyle(ButtonStyle style)
        {
            ApplyStyle(style);
        }

        private void ApplyStyle(ButtonStyle style)
        {
            _style = style;
            var enabled = _button == null || _button.interactable;

            Color fill;
            Color text;
            Color border = Color.clear;

            switch (style)
            {
                case ButtonStyle.Secondary:
                    fill = Theme.SurfaceRaised;
                    text = Theme.TextPrimary;
                    border = Theme.Outline;
                    break;
                case ButtonStyle.Ghost:
                    fill = Theme.WithAlpha(Theme.SurfaceRaised, 0f);
                    text = Theme.TextSecondary;
                    border = Theme.Outline;
                    break;
                case ButtonStyle.Danger:
                    fill = Theme.Danger;
                    text = Theme.OnColour(Theme.Danger);
                    break;
                case ButtonStyle.Success:
                    fill = Theme.Success;
                    text = Theme.OnColour(Theme.Success);
                    break;
                case ButtonStyle.Subtle:
                    fill = Theme.Surface;
                    text = Theme.TextSecondary;
                    break;
                default:
                    fill = Theme.Primary;
                    text = Theme.OnColour(Theme.Primary);
                    break;
            }

            if (!enabled)
            {
                fill = Theme.WithAlpha(Theme.SurfaceSunken, 0.9f);
                text = Theme.TextMuted;
                border = Theme.WithAlpha(Theme.Outline, 0.6f);
            }

            if (_background != null) _background.color = fill;
            if (_outline != null) _outline.color = border;
            if (_label != null) _label.color = text;
            if (_sublabel != null) _sublabel.color = Theme.WithAlpha(text, 0.75f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Interactable || _pressed) return;
            _pressed = true;
            if (UiFeedback.AnimationsEnabled) transform.localScale = Vector3.one * 0.96f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pressed) return;
            _pressed = false;
            transform.localScale = Vector3.one;
        }

        private void OnDisable()
        {
            _pressed = false;
            transform.localScale = Vector3.one;
        }
    }
}
