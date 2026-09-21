using System.Linq;
using PartyGame.Core.Audio;
using PartyGame.Core.Modes;
using PartyGame.Core.Services;
using PartyGame.UI.Components;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;

namespace PartyGame.UI.Gameplay
{
    /// <summary>Plain information, a spoken-turn prompt or a question shown to the whole table.</summary>
    public class MessageStepView : StepView
    {
        protected override void Build()
        {
            var scroll = UIFactory.CreateScrollView(Content, "Scroll", Theme.SpaceS,
                new RectOffset(0, 0, (int)Theme.SpaceS, (int)Theme.SpaceM), out var scrollRect);
            UIFactory.Stretch((RectTransform)scrollRect.transform);

            var step = (MessageStep)Step;
            BuildHeadingBlock(scroll, step.Title, step.Body, step.Feature, step.Accent);

            if (step.Bullets.Count > 0)
            {
                var card = UIFactory.CreatePaddedCard(scroll, "Bullets", Theme.Surface);
                foreach (var bullet in step.Bullets)
                {
                    var text = UIFactory.CreateText(card, "-  " + bullet, Theme.FontLabel, Theme.TextSecondary,
                        TextAlignmentOptions.TopLeft, FontStyles.Normal, "Bullet");
                    UIFactory.FitHeight(text.gameObject);
                }
            }

            BuildPrimaryAction(step.ContinueLabel, FinishAcknowledged);
        }
    }

    /// <summary>Group discussion with a shared countdown.</summary>
    public class DiscussionStepView : StepView
    {
        private GameTimer _timer;
        private UiButton _action;
        private bool _finished;

        protected override void Build()
        {
            var step = (DiscussionStep)Step;

            var column = UIFactory.VerticalGroup(Content, "Discussion", Theme.SpaceM, null, TextAnchor.MiddleCenter);
            UIFactory.Stretch((RectTransform)column.transform);

            BuildHeadingBlock(column.transform, step.Title, step.Body, null, step.Accent);

            _timer = new GameTimer();
            var bar = TimerBar.Create(column.transform);
            bar.Bind(_timer);

            if (step.Bullets.Count > 0)
            {
                var card = UIFactory.CreatePaddedCard(column.transform, "Tips", Theme.Surface);
                foreach (var bullet in step.Bullets)
                {
                    var text = UIFactory.CreateText(card, "-  " + bullet, Theme.FontLabel, Theme.TextMuted,
                        TextAlignmentOptions.TopLeft, FontStyles.Normal, "Tip");
                    UIFactory.FitHeight(text.gameObject);
                }
            }

            _timer.Completed += OnTimeUp;
            _timer.Start(step.Seconds);

            _action = BuildPrimaryAction(step.CanSkip ? step.ContinueLabel : "Waiting for the timer",
                () => FinishDiscussion(false), step.CanSkip ? ButtonStyle.Primary : ButtonStyle.Secondary);
            _action.Interactable = step.CanSkip;
        }

        private void Update()
        {
            _timer?.Tick(Time.unscaledDeltaTime);
        }

        private void OnTimeUp()
        {
            UiFeedback.Sound(SoundId.TimeUp);
            UiFeedback.Haptic();
            if (_action != null)
            {
                _action.Interactable = true;
                _action.SetLabel("Time is up - continue");
                _action.SetStyle(ButtonStyle.Primary);
                _action.SetOnClick(() => FinishDiscussion(true));
            }
        }

        private void FinishDiscussion(bool timedOut)
        {
            if (_finished) return;
            _finished = true;
            _timer?.Stop();
            var result = StepResult.Acknowledged(Step);
            result.TimedOut = timedOut;
            Finish(result);
        }

        private void OnDestroy()
        {
            if (_timer != null) _timer.Completed -= OnTimeUp;
        }
    }

    /// <summary>End of round reveal.</summary>
    public class RevealStepView : StepView
    {
        protected override void Build()
        {
            var step = (RevealStep)Step;

            var scroll = UIFactory.CreateScrollView(Content, "Scroll", Theme.SpaceS,
                new RectOffset(0, 0, (int)Theme.SpaceS, (int)Theme.SpaceM), out var scrollRect);
            UIFactory.Stretch((RectTransform)scrollRect.transform);

            BuildHeadingBlock(scroll, step.Title, null, step.Headline, step.Accent);

            foreach (var entry in step.Entries)
            {
                var card = UIFactory.CreatePaddedCard(scroll, "Entry",
                    entry.Highlight
                        ? Theme.WithAlpha(Theme.ForAccent(entry.Accent), 0.18f)
                        : Theme.Surface,
                    Theme.SpaceS);

                var title = UIFactory.CreateText(card, entry.Title, Theme.FontSubheading,
                    entry.Highlight ? Theme.ForAccent(entry.Accent) : Theme.TextPrimary,
                    TextAlignmentOptions.Left, FontStyles.Bold, "Title");
                UIFactory.FitHeight(title.gameObject);

                if (!string.IsNullOrEmpty(entry.Detail))
                {
                    var detail = UIFactory.CreateText(card, entry.Detail, Theme.FontLabel, Theme.TextSecondary,
                        TextAlignmentOptions.TopLeft, FontStyles.Normal, "Detail");
                    UIFactory.FitHeight(detail.gameObject);
                }
            }

            BuildPrimaryAction(step.ContinueLabel, FinishAcknowledged);
        }

        private void Start()
        {
            UiFeedback.Sound(SoundId.Reveal);
            UiFeedback.Haptic();
        }
    }

    /// <summary>Leaderboard, used between rounds and at the end of a game.</summary>
    public class ScoreboardStepView : StepView
    {
        protected override void Build()
        {
            var step = (ScoreboardStep)Step;

            var scroll = UIFactory.CreateScrollView(Content, "Scroll", Theme.SpaceXs,
                new RectOffset(0, 0, (int)Theme.SpaceS, (int)Theme.SpaceM), out var scrollRect);
            UIFactory.Stretch((RectTransform)scrollRect.transform);

            BuildHeadingBlock(scroll, step.Title, null, null, step.Accent);

            foreach (var row in step.Rows.OrderBy(r => r.Rank))
                BuildRow(scroll, row, step.IsFinal);

            BuildPrimaryAction(step.ContinueLabel, FinishAcknowledged);
        }

        private void BuildRow(Transform parent, ScoreRow row, bool isFinal)
        {
            var highlight = isFinal && row.Rank == 1;
            var card = UIFactory.CreateCard("Row-" + row.PlayerId, parent,
                highlight ? Theme.WithAlpha(Theme.Warning, 0.2f) : Theme.Surface, Theme.RadiusMedium);

            var group = UIFactory.HorizontalGroup(card, "Row", Theme.SpaceS,
                new RectOffset((int)Theme.SpaceM, (int)Theme.SpaceM, (int)Theme.SpaceXs, (int)Theme.SpaceXs),
                TextAnchor.MiddleLeft);
            UIFactory.Stretch((RectTransform)group.transform);

            var rank = UIFactory.CreateFittedText(group.transform, row.Rank.ToString(), Theme.FontSubheading,
                Theme.FontCaption, highlight ? Theme.Warning : Theme.TextMuted,
                TextAlignmentOptions.Center, FontStyles.Bold, "Rank");
            UIFactory.SetSize(rank.gameObject, preferredWidth: 70f, minWidth: 70f);

            var name = UIFactory.CreateText(group.transform,
                row.Name + (string.IsNullOrEmpty(row.Note) ? string.Empty : "  (" + row.Note + ")"),
                Theme.FontBody, Theme.TextPrimary, TextAlignmentOptions.Left, FontStyles.Bold, "Name");
            UIFactory.SetSize(name.gameObject, flexibleWidth: 1f);

            if (row.Delta != 0)
            {
                var delta = UIFactory.CreateText(group.transform, (row.Delta > 0 ? "+" : string.Empty) + row.Delta,
                    Theme.FontLabel, row.Delta > 0 ? Theme.Success : Theme.Danger,
                    TextAlignmentOptions.Right, FontStyles.Bold, "Delta");
                UIFactory.SetSize(delta.gameObject, preferredWidth: 130f, minWidth: 130f);
            }

            var total = UIFactory.CreateText(group.transform, row.Total.ToString(), Theme.FontSubheading,
                Theme.TextPrimary, TextAlignmentOptions.Right, FontStyles.Bold, "Total");
            UIFactory.SetSize(total.gameObject, preferredWidth: 170f, minWidth: 170f);

            UIFactory.SetSize(card.gameObject, 116f, 116f);
        }

        private void Start()
        {
            var step = (ScoreboardStep)Step;
            UiFeedback.Sound(step.IsFinal ? SoundId.Fanfare : SoundId.RoundComplete);
        }
    }
}
