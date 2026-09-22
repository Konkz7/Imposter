using System;
using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Modes;
using PartyGame.UI.Components;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;

namespace PartyGame.UI.Screens
{
    /// <summary>
    /// Short, numbered rules for one game. The steps come from the mode itself and reflect the
    /// settings actually chosen, so the rules never describe a different game to the one about
    /// to be played.
    /// </summary>
    public class HowToPlayScreen : ScreenBase
    {
        private GameModeDefinition _definition;
        private GameSettings _settings;
        private Action _onStart;

        public override string Title => _definition != null ? _definition.DisplayName : "How to play";
        public override string Subtitle => "How it works";

        public void Configure(GameModeDefinition definition, GameSettings settings, Action onStart)
        {
            _definition = definition;
            _settings = settings;
            _onStart = onStart;
        }

        protected override void Build()
        {
            var content = CreateScrollBody(Theme.SpaceS);

            if (_definition != null && !string.IsNullOrEmpty(_definition.Description))
            {
                var card = UIFactory.CreatePaddedCard(content, "Intro",
                    Theme.WithAlpha(Theme.Accent(_definition.AccentIndex), 0.16f));
                var text = UIFactory.CreateText(card, _definition.Description, Theme.FontBody, Theme.TextPrimary,
                    TextAlignmentOptions.TopLeft, FontStyles.Normal, "IntroText");
                UIFactory.FitHeight(text.gameObject);
            }

            foreach (var line in BuildSteps().Select((text, index) => new { text, index }))
                BuildStepRow(content, line.index + 1, line.text);

            if (_definition != null)
            {
                var meta = UIFactory.CreateText(content,
                    _definition.PlayerRangeLabel +
                    (_definition.HasRecommendation ? " (" + _definition.RecommendationLabel + ")" : string.Empty) +
                    "   -   " + _definition.DurationLabel,
                    Theme.FontCaption, Theme.TextMuted, TextAlignmentOptions.Center, FontStyles.Normal, "Meta");
                UIFactory.FitHeight(meta.gameObject);
            }

            UIFactory.Spacer(content, Theme.SpaceL);

            if (_onStart != null)
            {
                AddFooterButton("Start playing", ButtonStyle.Primary, () =>
                {
                    var start = _onStart;
                    _onStart = null;
                    start();
                });
            }
        }

        private IReadOnlyList<string> BuildSteps()
        {
            if (_definition == null) return new List<string>();

            var mode = GameModeFactory.Create(_definition.ModeId);
            if (mode != null)
            {
                var settings = _settings ?? App.ModeSettings.Load(mode, App.Services.Content);
                var summary = mode.BuildRulesSummary(settings);
                if (summary != null && summary.Count > 0) return summary;
            }

            return _definition.HowToPlay.Count > 0
                ? _definition.HowToPlay
                : new List<string> { "Rules for this game are coming soon." };
        }

        private void BuildStepRow(Transform parent, int number, string text)
        {
            var row = UIFactory.HorizontalGroup(parent, "Step" + number, Theme.SpaceS, null, TextAnchor.UpperLeft);
            UIFactory.FitHeight(row.gameObject);

            var badge = UIFactory.CreateRect("Number", row.transform);
            UIFactory.SetSize(badge.gameObject, 72f, 72f, 72f, 72f);
            var image = badge.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = UIGraphics.Circle(64);
            image.color = Theme.WithAlpha(Theme.Primary, 0.25f);
            image.raycastTarget = false;

            var numberText = UIFactory.CreateFittedText(badge, number.ToString(), Theme.FontLabel, Theme.FontCaption,
                Theme.Primary, TextAlignmentOptions.Center, FontStyles.Bold, "NumberText");
            UIFactory.Stretch(numberText.rectTransform, 6f);

            var body = UIFactory.CreateText(row.transform, text, Theme.FontBody, Theme.TextSecondary,
                TextAlignmentOptions.TopLeft, FontStyles.Normal, "Body");
            UIFactory.SetSize(body.gameObject, flexibleWidth: 1f);
            UIFactory.FitHeight(body.gameObject);
        }
    }

    /// <summary>Lists every game so the rules can be read without starting anything.</summary>
    public class HelpIndexScreen : ScreenBase
    {
        public override string Title => "How to play";
        public override string Subtitle => "Read the rules for any game";

        protected override void Build()
        {
            var content = CreateScrollBody();

            foreach (var definition in App.Services.Content.GameModes)
            {
                var mode = definition;
                SelectionCard.Create(content, mode.DisplayName, mode.Tagline,
                    mode.PlayerRangeLabel + "   -   " + mode.DurationLabel, mode.Glyph,
                    Theme.Accent(mode.AccentIndex),
                    () => App.ShowHowToPlay(mode, null, null), 210f, false);
            }

            var card = UIFactory.CreatePaddedCard(content, "Passing", Theme.Surface);

            var title = UIFactory.CreateText(card, "Passing the phone", Theme.FontSubheading, Theme.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Bold, "Title");
            UIFactory.FitHeight(title.gameObject);

            var body = UIFactory.CreateText(card,
                "Secret information is always hidden behind a reveal screen. Hand the phone over first, " +
                "then the next player taps to reveal, reads, and hides it again before passing on. " +
                "Nothing secret is ever on screen while the phone is moving.",
                Theme.FontLabel, Theme.TextSecondary, TextAlignmentOptions.TopLeft, FontStyles.Normal, "Body");
            UIFactory.FitHeight(body.gameObject);

            UIFactory.Spacer(content, Theme.SpaceL);
        }
    }
}
