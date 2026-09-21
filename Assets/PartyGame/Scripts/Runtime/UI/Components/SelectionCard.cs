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
    /// <summary>
    /// A tappable card with an optional selected state. Used for game modes, categories,
    /// vote options and answer choices - one widget instead of four near-identical ones.
    /// </summary>
    public class SelectionCard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Image _background;
        private Image _outline;
        private Image _glyphBadge;
        private TextMeshProUGUI _glyph;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _subtitle;
        private TextMeshProUGUI _meta;
        private Image _tick;
        private Button _button;
        private Color _accent = Theme.Primary;
        private bool _selected;
        private bool _pressed;

        public event Action Clicked;

        public string Key { get; set; } = string.Empty;

        public Button Button => _button;

        public bool Interactable
        {
            get => _button != null && _button.interactable;
            set
            {
                if (_button != null) _button.interactable = value;
                Refresh();
            }
        }

        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                Refresh();
            }
        }

        public static SelectionCard Create(Transform parent, string title, string subtitle, string meta,
            string glyph, Color accent, Action onClick, float height = 200f, bool showTick = true)
        {
            var root = UIFactory.CreatePanel("Card-" + title, parent, Theme.SurfaceRaised, Theme.RadiusLarge);
            var card = root.gameObject.AddComponent<SelectionCard>();
            card.Construct(root, title, subtitle, meta, glyph, accent, onClick, height, showTick);
            return card;
        }

        private void Construct(Image background, string title, string subtitle, string meta, string glyph,
            Color accent, Action onClick, float height, bool showTick)
        {
            _background = background;
            _accent = accent;

            _outline = UIFactory.CreateOutline("Outline", transform, Theme.Outline, Theme.RadiusLarge);
            UIFactory.Stretch(_outline.rectTransform);

            var row = UIFactory.HorizontalGroup(transform, "Row", Theme.SpaceM,
                new RectOffset((int)Theme.SpaceM, (int)Theme.SpaceM,
                    (int)Theme.SpaceS, (int)Theme.SpaceS), TextAnchor.MiddleLeft);
            UIFactory.Stretch((RectTransform)row.transform);

            if (!string.IsNullOrEmpty(glyph))
            {
                _glyphBadge = UIFactory.CreatePanel("Badge", row.transform, Theme.WithAlpha(accent, 0.22f),
                    Theme.RadiusMedium);
                UIFactory.SetSize(_glyphBadge.gameObject, 112f, 112f, 112f, 112f);
                _glyph = UIFactory.CreateFittedText(_glyphBadge.transform, glyph, 56f, 28f, accent,
                    TextAlignmentOptions.Center, FontStyles.Bold, "Glyph");
                UIFactory.Stretch(_glyph.rectTransform, 8f);
            }

            var column = UIFactory.VerticalGroup(row.transform, "Column", 4f, null, TextAnchor.MiddleLeft);
            column.childForceExpandHeight = false;
            UIFactory.SetSize(column.gameObject, flexibleWidth: 1f);

            _title = UIFactory.CreateText(column.transform, title, Theme.FontSubheading, Theme.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Bold, "Title");
            UIFactory.FitHeight(_title.gameObject);

            if (!string.IsNullOrEmpty(subtitle))
            {
                _subtitle = UIFactory.CreateText(column.transform, subtitle, Theme.FontLabel,
                    Theme.TextSecondary, TextAlignmentOptions.TopLeft, FontStyles.Normal, "Subtitle");
                UIFactory.FitHeight(_subtitle.gameObject);
            }

            if (!string.IsNullOrEmpty(meta))
            {
                _meta = UIFactory.CreateText(column.transform, meta, Theme.FontCaption, Theme.TextMuted,
                    TextAlignmentOptions.Left, FontStyles.Normal, "Meta");
                UIFactory.FitHeight(_meta.gameObject);
            }

            if (showTick)
            {
                // A ring that fills in when selected. Deliberately a shape rather than a glyph
                // so it renders identically whatever font the device falls back to.
                var tickHolder = UIFactory.CreateRect("Tick", row.transform);
                UIFactory.SetSize(tickHolder.gameObject, 56f, 56f, 56f, 56f);
                var ring = tickHolder.gameObject.AddComponent<Image>();
                ring.sprite = UIGraphics.Circle(48);
                ring.color = Theme.WithAlpha(Theme.Outline, 0.9f);
                ring.raycastTarget = false;

                _tick = UIFactory.CreateRect("TickFill", tickHolder).gameObject.AddComponent<Image>();
                _tick.sprite = UIGraphics.Circle(48);
                _tick.raycastTarget = false;
                UIFactory.Stretch(_tick.rectTransform, 10f);
            }

            _button = gameObject.AddComponent<Button>();
            _button.targetGraphic = _background;
            _button.transition = Selectable.Transition.None;
            _button.onClick.AddListener(() =>
            {
                UiFeedback.Sound(SoundId.Tap);
                Clicked?.Invoke();
            });
            if (onClick != null) Clicked += onClick;

            UIFactory.SetSize(gameObject, height, height);
            Refresh();
        }

        public void SetAccent(Color accent)
        {
            _accent = accent;
            if (_glyphBadge != null) _glyphBadge.color = Theme.WithAlpha(accent, 0.22f);
            if (_glyph != null) _glyph.color = accent;
            Refresh();
        }

        public void SetTitle(string title)
        {
            if (_title != null) _title.text = title ?? string.Empty;
        }

        public void SetSubtitle(string subtitle)
        {
            if (_subtitle != null) _subtitle.text = subtitle ?? string.Empty;
        }

        public void SetMeta(string meta)
        {
            if (_meta != null) _meta.text = meta ?? string.Empty;
        }

        private void Refresh()
        {
            var enabled = _button == null || _button.interactable;

            if (_background != null)
                _background.color = !enabled
                    ? Theme.WithAlpha(Theme.Surface, 0.6f)
                    : _selected
                        ? Theme.WithAlpha(_accent, 0.2f)
                        : Theme.SurfaceRaised;

            if (_outline != null)
                _outline.color = !enabled
                    ? Theme.WithAlpha(Theme.Outline, 0.4f)
                    : _selected ? _accent : Theme.Outline;

            if (_title != null) _title.color = enabled ? Theme.TextPrimary : Theme.TextMuted;
            if (_tick != null) _tick.color = _selected ? _accent : Color.clear;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Interactable || _pressed) return;
            _pressed = true;
            if (UiFeedback.AnimationsEnabled) transform.localScale = Vector3.one * 0.985f;
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
