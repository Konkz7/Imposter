using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Audio;
using PartyGame.Core.Modes;
using PartyGame.UI.Components;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.Gameplay
{
    /// <summary>
    /// Several boxes filled in by one player, e.g. a question and the two ends of its scale.
    /// Whichever box is being typed into is kept in view above the keyboard.
    /// </summary>
    public class FormStepView : StepView
    {
        private static readonly Vector3[] Corners = new Vector3[4];

        private readonly List<TMP_InputField> _fields = new List<TMP_InputField>();
        private ScrollRect _scroll;
        private TextMeshProUGUI _error;
        private UiButton _submit;
        private int _nextSuggestion;
        private TMP_InputField _keptInView;
        private float _keptViewportHeight;

        public override bool IsShowingSecret => Step != null && Step.IsPrivate;

        protected override void Build()
        {
            var step = (FormStep)Step;

            var scroll = UIFactory.CreateScrollView(Content, "Scroll", Theme.SpaceS,
                new RectOffset(0, 0, (int)Theme.SpaceXs, (int)Theme.SpaceM), out _scroll);
            UIFactory.Stretch((RectTransform)_scroll.transform);

            BuildHeadingBlock(scroll, step.Title, step.Body, null, step.Accent);

            if (!string.IsNullOrEmpty(step.Prompt))
            {
                var prompt = UIFactory.CreateText(scroll, step.Prompt, Theme.FontLabel, Theme.TextSecondary,
                    TextAlignmentOptions.Center, FontStyles.Normal, "Prompt");
                UIFactory.FitHeight(prompt.gameObject);
            }

            // Lives with the boxes rather than beside the submit button, so it can never be
            // mistaken for the way forward.
            if (step.Suggestions.Count > 0)
                UiButton.Create(scroll, step.SuggestionLabel, ButtonStyle.Ghost, UseSuggestion,
                    Theme.CompactButtonHeight, null, "Suggestion");

            foreach (var definition in step.Fields)
            {
                var label = UIFactory.CreateText(scroll,
                    definition.Required ? definition.Label : definition.Label + "  (optional)",
                    Theme.FontLabel, Theme.TextSecondary, TextAlignmentOptions.Left, FontStyles.Bold, "Label");
                UIFactory.FitHeight(label.gameObject);

                var field = UIFactory.CreateInputField(scroll, definition.Placeholder, definition.MaxLength);
                UIFactory.SetSize(field.gameObject, 120f, 120f);
                field.onValueChanged.AddListener(_ => OnChanged());
                _fields.Add(field);
            }

            _error = UIFactory.CreateText(scroll, string.Empty, Theme.FontLabel, Theme.Danger,
                TextAlignmentOptions.Center, FontStyles.Normal, "Error");
            UIFactory.FitHeight(_error.gameObject);

            // Only the button submits. Closing the keyboard reports the same "Done" as its key.
            _submit = BuildPrimaryAction(step.ContinueLabel, Submit);
            _submit.Interactable = RequiredFieldsFilled();

            KeyboardAvoider.Attach((RectTransform)transform);
        }

        private void Start()
        {
            if (_fields.Count > 0) _fields[0].ActivateInputField();
        }

        private void LateUpdate()
        {
            var focused = _fields.FirstOrDefault(f => f.isFocused);
            if (focused == null || _scroll == null || _scroll.viewport == null)
            {
                _keptInView = null;
                return;
            }

            // Only react when the box or the space for it changes, so a player scrolling up to
            // re-read something is not dragged straight back down.
            var viewportHeight = _scroll.viewport.rect.height;
            if (focused == _keptInView && Mathf.Abs(viewportHeight - _keptViewportHeight) < 0.5f) return;
            _keptInView = focused;
            _keptViewportHeight = viewportHeight;

            KeepInView((RectTransform)focused.transform);
        }

        /// <summary>Scrolls just far enough for <paramref name="target"/> to sit inside the viewport.</summary>
        private void KeepInView(RectTransform target)
        {
            var viewport = _scroll.viewport;
            var content = _scroll.content;

            viewport.GetWorldCorners(Corners);
            var viewBottom = Corners[0].y;
            var viewTop = Corners[1].y;
            target.GetWorldCorners(Corners);
            var bottom = Corners[0].y;
            var top = Corners[1].y;

            // Positive lifts the content, bringing what is below the viewport into it.
            var shift = bottom < viewBottom ? viewBottom - bottom : top > viewTop ? viewTop - top : 0f;
            var scale = content.lossyScale.y;
            if (Mathf.Abs(shift) < 0.5f || scale <= 0f) return;

            var furthest = Mathf.Max(0f, content.rect.height - viewport.rect.height);
            var position = content.anchoredPosition;
            position.y = Mathf.Clamp(position.y + shift / scale, 0f, furthest);
            content.anchoredPosition = position;
            _scroll.StopMovement();
        }

        private void UseSuggestion()
        {
            var step = (FormStep)Step;
            if (step.Suggestions.Count == 0) return;

            var suggestion = step.Suggestions[_nextSuggestion % step.Suggestions.Count];
            _nextSuggestion++;

            for (var i = 0; i < _fields.Count; i++)
                _fields[i].text = suggestion.TryGetValue(step.Fields[i].Id, out var value) ? value ?? string.Empty : string.Empty;
        }

        private void OnChanged()
        {
            if (_error != null) _error.text = string.Empty;
            if (_submit != null) _submit.Interactable = RequiredFieldsFilled();
        }

        private bool RequiredFieldsFilled()
        {
            var step = (FormStep)Step;
            for (var i = 0; i < _fields.Count; i++)
                if (step.Fields[i].Required && string.IsNullOrWhiteSpace(_fields[i].text)) return false;
            return true;
        }

        private void Submit()
        {
            if (!RequiredFieldsFilled())
            {
                if (_error != null) _error.text = "Write something first.";
                UiFeedback.Sound(SoundId.Error);
                return;
            }

            var step = (FormStep)Step;
            var values = new Dictionary<string, string>();
            for (var i = 0; i < _fields.Count; i++)
            {
                values[step.Fields[i].Id] = _fields[i].text.Trim();
                _fields[i].DeactivateInputField();
            }

            UiFeedback.Sound(SoundId.Confirm);
            Finish(StepResult.ForForm(Step, values));
        }
    }
}
