using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Audio;
using PartyGame.Core.Modes;
using PartyGame.Core.Util;
using PartyGame.UI.Components;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PartyGame.UI.Gameplay
{
    /// <summary>
    /// Secret information for one player, kept behind a cover until they deliberately reveal it.
    /// In hold mode the card is only visible while the finger is down, so it cannot be left
    /// on screen when the phone is handed on.
    /// </summary>
    public class PrivateInfoStepView : StepView, IPointerDownHandler, IPointerUpHandler
    {
        private CanvasGroup _secretGroup;
        private CanvasGroup _coverGroup;
        private UiButton _continueButton;
        private TextMeshProUGUI _coverHint;
        private bool _holdToReveal = true;
        private bool _revealed;
        private bool _everRevealed;

        public override bool IsShowingSecret => _revealed;

        /// <summary>True once the owner has looked at the card at least once.</summary>
        public bool HasBeenRevealed => _everRevealed;

        public void SetHoldToReveal(bool holdToReveal)
        {
            _holdToReveal = holdToReveal;
        }

        protected override void Build()
        {
            var step = (PrivateInfoStep)Step;

            var stack = UIFactory.CreateRect("Stack", Content);
            UIFactory.Stretch(stack);

            // --- the secret itself
            var secretHolder = UIFactory.CreateRect("Secret", stack);
            UIFactory.Stretch(secretHolder);
            _secretGroup = secretHolder.gameObject.AddComponent<CanvasGroup>();
            _secretGroup.alpha = 0f;

            var column = UIFactory.VerticalGroup(secretHolder, "Column", Theme.SpaceS, null, TextAnchor.MiddleCenter);
            UIFactory.Stretch((RectTransform)column.transform);

            var accent = Theme.ForAccent(step.Accent == StepAccent.Neutral ? StepAccent.Primary : step.Accent);

            var headline = UIFactory.CreateFittedText(column.transform, step.Headline, Theme.FontTitle,
                Theme.FontSubheading, accent, TextAlignmentOptions.Center, FontStyles.Bold, "Headline");
            UIFactory.SetSize(headline.gameObject, 150f, 150f);

            foreach (var line in step.Lines)
            {
                var card = UIFactory.CreatePaddedCard(column.transform, "Line",
                    line.Emphasise ? Theme.WithAlpha(accent, 0.2f) : Theme.Surface, Theme.SpaceM);

                if (!string.IsNullOrEmpty(line.Label))
                {
                    var label = UIFactory.CreateText(card, line.Label.ToUpperInvariant(), Theme.FontCaption,
                        Theme.TextMuted, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
                    label.characterSpacing = 6f;
                    UIFactory.FitHeight(label.gameObject);
                }

                var value = UIFactory.CreateFittedText(card, line.Value,
                    line.Emphasise ? Theme.FontTitle : Theme.FontBody,
                    line.Emphasise ? Theme.FontSubheading : Theme.FontLabel,
                    line.Emphasise ? Theme.TextPrimary : Theme.TextSecondary,
                    TextAlignmentOptions.Center, line.Emphasise ? FontStyles.Bold : FontStyles.Normal, "Value");
                UIFactory.SetSize(value.gameObject, line.Emphasise ? 96f : 60f, line.Emphasise ? 96f : 60f);
            }

            if (!string.IsNullOrEmpty(step.Footnote))
            {
                var footnote = UIFactory.CreateText(column.transform, step.Footnote, Theme.FontLabel,
                    Theme.TextMuted, TextAlignmentOptions.Center, FontStyles.Italic, "Footnote");
                UIFactory.FitHeight(footnote.gameObject);
            }

            // --- the cover
            var cover = UIFactory.CreatePanel("Cover", stack, Theme.SurfaceRaised, Theme.RadiusLarge);
            _coverGroup = cover.gameObject.AddComponent<CanvasGroup>();
            UIFactory.Stretch(cover.rectTransform);
            cover.raycastTarget = true;

            var coverOutline = UIFactory.CreateOutline("Outline", cover.transform, Theme.Outline, Theme.RadiusLarge);
            UIFactory.Stretch(coverOutline.rectTransform);

            var coverColumn = UIFactory.VerticalGroup(cover.transform, "CoverColumn", Theme.SpaceM, null,
                TextAnchor.MiddleCenter);
            UIFactory.Stretch((RectTransform)coverColumn.transform);

            var who = UIFactory.CreateFittedText(coverColumn.transform, step.Title, Theme.FontTitle,
                Theme.FontSubheading, Theme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold, "Who");
            UIFactory.SetSize(who.gameObject, 130f, 130f);

            _coverHint = UIFactory.CreateText(coverColumn.transform, HintText(), Theme.FontBody,
                Theme.TextSecondary, TextAlignmentOptions.Center, FontStyles.Normal, "Hint");
            UIFactory.FitHeight(_coverHint.gameObject);

            var warning = UIFactory.CreateText(coverColumn.transform, "Make sure nobody else can see the screen.",
                Theme.FontCaption, Theme.TextMuted, TextAlignmentOptions.Center, FontStyles.Italic, "Warning");
            UIFactory.FitHeight(warning.gameObject);

            _continueButton = BuildPrimaryAction(step.ContinueLabel, HideAndContinue);
            _continueButton.Interactable = false;
        }

        private string HintText()
        {
            return _holdToReveal ? "Press and hold anywhere to reveal" : "Tap to reveal";
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_holdToReveal) SetRevealed(true);
            else SetRevealed(!_revealed);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_holdToReveal) SetRevealed(false);
        }

        private void SetRevealed(bool revealed)
        {
            if (_revealed == revealed) return;
            _revealed = revealed;

            if (_secretGroup != null) _secretGroup.alpha = revealed ? 1f : 0f;
            if (_coverGroup != null) _coverGroup.alpha = revealed ? 0f : 1f;

            if (revealed)
            {
                if (!_everRevealed)
                {
                    _everRevealed = true;
                    if (_continueButton != null) _continueButton.Interactable = true;
                }
                UiFeedback.Sound(SoundId.Reveal);
                UiFeedback.Haptic();
            }
            else
            {
                UiFeedback.Sound(SoundId.Hide);
                if (_coverHint != null)
                    _coverHint.text = _everRevealed
                        ? (_holdToReveal ? "Hold again to look once more" : "Tap to look again")
                        : HintText();
            }
        }

        private void HideAndContinue()
        {
            SetRevealed(false);
            FinishAcknowledged();
        }

        private void OnDisable()
        {
            // Never leave a secret on screen if this view is torn down mid-reveal.
            SetRevealed(false);
        }
    }

    /// <summary>Free text entry from one player.</summary>
    public class TextInputStepView : StepView
    {
        private TMP_InputField _field;
        private TextMeshProUGUI _error;
        private UiButton _submit;

        public override bool IsShowingSecret => true;

        protected override void Build()
        {
            var step = (TextInputStep)Step;

            var scroll = UIFactory.CreateScrollView(Content, "Scroll", Theme.SpaceS,
                new RectOffset(0, 0, (int)Theme.SpaceXs, (int)Theme.SpaceM), out var scrollRect);
            UIFactory.Stretch((RectTransform)scrollRect.transform);

            BuildHeadingBlock(scroll, step.Title, null, step.Body, step.Accent);

            var prompt = UIFactory.CreateText(scroll, step.Prompt, Theme.FontBody, Theme.TextSecondary,
                TextAlignmentOptions.Center, FontStyles.Normal, "Prompt");
            UIFactory.FitHeight(prompt.gameObject);

            _field = UIFactory.CreateInputField(scroll, step.Placeholder, step.MaxLength);
            UIFactory.SetSize(_field.gameObject, 140f, 140f);
            _field.onSubmit.AddListener(_ => Submit());
            _field.onValueChanged.AddListener(OnTextChanged);

            _error = UIFactory.CreateText(scroll, string.Empty, Theme.FontLabel, Theme.Danger,
                TextAlignmentOptions.Center, FontStyles.Normal, "Error");
            UIFactory.FitHeight(_error.gameObject);

            _submit = BuildPrimaryAction(step.ContinueLabel, Submit);
            _submit.Interactable = step.AllowEmpty;

            // Typing is the one place a phone keyboard can hide the thing being typed into.
            KeyboardAvoider.Attach((RectTransform)transform);
        }

        private void Start()
        {
            if (_field != null) _field.ActivateInputField();
        }

        private void OnTextChanged(string value)
        {
            if (_error != null) _error.text = string.Empty;
            var step = (TextInputStep)Step;
            if (_submit != null) _submit.Interactable = step.AllowEmpty || !string.IsNullOrWhiteSpace(value);
        }

        private void Submit()
        {
            var step = (TextInputStep)Step;
            var text = _field != null ? _field.text : string.Empty;

            if (!step.AllowEmpty && string.IsNullOrWhiteSpace(text))
            {
                ShowError("Write something first.");
                return;
            }

            if (TextUtility.AnyMatch(text, step.RejectedValues))
            {
                ShowError(step.RejectionMessage);
                if (_field != null)
                {
                    _field.text = string.Empty;
                    _field.ActivateInputField();
                }
                return;
            }

            if (_field != null) _field.DeactivateInputField();
            UiFeedback.Sound(SoundId.Confirm);
            Finish(StepResult.ForText(Step, text));
        }

        private void ShowError(string message)
        {
            if (_error != null) _error.text = message;
            UiFeedback.Sound(SoundId.Error);
        }
    }

    /// <summary>One player picks one option: a vote, a guess or a stance.</summary>
    public class ChoiceStepView : StepView
    {
        private readonly List<SelectionCard> _cards = new List<SelectionCard>();
        private UiButton _confirm;
        private string _selectedId = string.Empty;

        public override bool IsShowingSecret => true;

        protected override void Build()
        {
            var step = (ChoiceStep)Step;

            var scroll = UIFactory.CreateScrollView(Content, "Scroll", Theme.SpaceXs,
                new RectOffset(0, 0, (int)Theme.SpaceXs, (int)Theme.SpaceM), out var scrollRect);
            UIFactory.Stretch((RectTransform)scrollRect.transform);

            BuildHeadingBlock(scroll, step.Title, step.Prompt, step.Body, step.Accent);

            var options = step.ShuffleOptions
                ? step.Options.OrderBy(_ => Random.value).ToList()
                : step.Options.ToList();

            foreach (var option in options)
            {
                var captured = option;
                var accent = option.Accent == StepAccent.Neutral
                    ? Theme.Primary
                    : Theme.ForAccent(option.Accent);

                var card = SelectionCard.Create(scroll, option.Label, option.SubLabel, null, null, accent, null, 150f);
                card.Key = option.Id;
                card.Interactable = option.Enabled;
                card.Clicked += () => Select(captured.Id);
                _cards.Add(card);
            }

            if (_cards.Count == 0)
            {
                var empty = UIFactory.CreateText(scroll, "No options available.", Theme.FontBody, Theme.TextMuted,
                    TextAlignmentOptions.Center, FontStyles.Normal, "Empty");
                UIFactory.FitHeight(empty.gameObject);
            }

            _confirm = BuildPrimaryAction(step.ContinueLabel, Confirm);
            _confirm.Interactable = false;
        }

        private void Select(string optionId)
        {
            _selectedId = optionId;
            foreach (var card in _cards) card.Selected = card.Key == optionId;
            if (_confirm != null) _confirm.Interactable = true;
            UiFeedback.Sound(SoundId.Vote);
        }

        private void Confirm()
        {
            if (string.IsNullOrEmpty(_selectedId))
            {
                UiFeedback.Sound(SoundId.Error);
                return;
            }
            UiFeedback.Haptic();
            Finish(StepResult.ForChoice(Step, _selectedId));
        }
    }
}
