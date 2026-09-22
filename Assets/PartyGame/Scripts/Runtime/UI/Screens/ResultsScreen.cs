using System.Linq;
using PartyGame.Core.Audio;
using PartyGame.Core.Modes;
using PartyGame.Core.Session;
using PartyGame.UI.Components;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;

namespace PartyGame.UI.Screens
{
    /// <summary>Final standings for a finished game, plus the two ways out.</summary>
    public class ResultsScreen : ScreenBase
    {
        private GameSession _session;
        private RoundSummary _summary;
        private bool _usesScoring = true;

        public override string Title => _usesScoring ? "Final scores" : "Result";
        public override string Subtitle => _session != null && _session.Definition != null
            ? _session.Definition.DisplayName
            : string.Empty;
        public override bool ShowBackButton => false;

        public void Configure(GameSession session, RoundSummary summary, bool usesScoring = true)
        {
            _session = session;
            _summary = summary;
            _usesScoring = usesScoring;
        }

        protected override void Build()
        {
            var content = CreateScrollBody();

            if (_session == null)
            {
                var empty = UIFactory.CreateText(content, "No results to show.", Theme.FontBody, Theme.TextMuted,
                    TextAlignmentOptions.Center, FontStyles.Normal, "Empty");
                UIFactory.FitHeight(empty.gameObject);
                AddFooterButton("Main menu", ButtonStyle.Primary, App.FinishAndGoHome);
                return;
            }

            if (!_usesScoring)
            {
                BuildOutcome(content);
                AddFooterButton("Play again", ButtonStyle.Primary, App.PlayAgain);
                AddFooterButton("Main menu", ButtonStyle.Secondary, App.FinishAndGoHome);
                return;
            }

            var leaderboard = _session.Scores.GetLeaderboard();
            var winners = leaderboard.Where(e => e.Rank == 1).ToList();

            var banner = UIFactory.CreatePaddedCard(content, "Winner", Theme.WithAlpha(Theme.Warning, 0.2f), Theme.SpaceL);

            var label = UIFactory.CreateText(banner, winners.Count > 1 ? "JOINT WINNERS" : "WINNER",
                Theme.FontLabel, Theme.Warning, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
            label.characterSpacing = 8f;
            UIFactory.FitHeight(label.gameObject);

            var names = winners.Count == 0
                ? "Nobody"
                : string.Join("  &  ", winners.Select(w => _session.NameOf(w.PlayerId)));
            var winnerText = UIFactory.CreateFittedText(banner, names, Theme.FontDisplay, Theme.FontHeading,
                Theme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold, "Winner");
            UIFactory.SetSize(winnerText.gameObject, 150f, 150f);

            if (winners.Count > 0)
            {
                var score = UIFactory.CreateText(banner, winners[0].Score + " points", Theme.FontBody,
                    Theme.TextSecondary, TextAlignmentOptions.Center, FontStyles.Normal, "Score");
                UIFactory.FitHeight(score.gameObject);
            }

            if (_summary != null && !string.IsNullOrEmpty(_summary.GameOverReason))
            {
                var reason = UIFactory.CreateText(content, _summary.GameOverReason, Theme.FontBody,
                    Theme.TextSecondary, TextAlignmentOptions.Center, FontStyles.Italic, "Reason");
                UIFactory.FitHeight(reason.gameObject);
            }

            foreach (var entry in leaderboard)
            {
                var player = _session.PlayerOf(entry.PlayerId);
                if (player == null) continue;

                var card = UIFactory.CreateCard("Row-" + entry.PlayerId, content,
                    entry.Rank == 1 ? Theme.WithAlpha(Theme.Warning, 0.16f) : Theme.Surface, Theme.RadiusMedium);

                var row = UIFactory.HorizontalGroup(card, "Row", Theme.SpaceS,
                    new RectOffset((int)Theme.SpaceM, (int)Theme.SpaceM, (int)Theme.SpaceXs, (int)Theme.SpaceXs),
                    TextAnchor.MiddleLeft);
                UIFactory.Stretch((RectTransform)row.transform);

                var rank = UIFactory.CreateFittedText(row.transform, Core.Util.TextUtility.Ordinal(entry.Rank),
                    Theme.FontSubheading, Theme.FontCaption,
                    entry.Rank == 1 ? Theme.Warning : Theme.TextMuted,
                    TextAlignmentOptions.Center, FontStyles.Bold, "Rank");
                UIFactory.SetSize(rank.gameObject, preferredWidth: 120f, minWidth: 120f);

                var name = UIFactory.CreateText(row.transform, player.DisplayName, Theme.FontBody, Theme.TextPrimary,
                    TextAlignmentOptions.Left, FontStyles.Bold, "Name");
                UIFactory.SetSize(name.gameObject, flexibleWidth: 1f);

                var total = UIFactory.CreateText(row.transform, entry.Score.ToString(), Theme.FontSubheading,
                    Theme.TextPrimary, TextAlignmentOptions.Right, FontStyles.Bold, "Total");
                UIFactory.SetSize(total.gameObject, preferredWidth: 180f, minWidth: 180f);

                UIFactory.SetSize(card.gameObject, 120f, 120f);
            }

            UIFactory.Spacer(content, Theme.SpaceL);

            AddFooterButton("Play again", ButtonStyle.Primary, App.PlayAgain);
            AddFooterButton("Main menu", ButtonStyle.Secondary, App.FinishAndGoHome);
        }

        /// <summary>
        /// A side won or lost, so there is no ranking to show. The roles come out here instead,
        /// which is the part the table actually wants at the end of a deduction game.
        /// </summary>
        private void BuildOutcome(Transform content)
        {
            var suspects = _session.Players.Where(p => p.RoleId == PlayerRoles.Imposter).ToList();
            var groupWon = suspects.All(p => !p.IsAlive);

            var banner = UIFactory.CreatePaddedCard(content, "Outcome",
                Theme.WithAlpha(groupWon ? Theme.Success : Theme.Danger, 0.2f), Theme.SpaceL);

            var headline = string.IsNullOrEmpty(_summary?.OutcomeHeadline)
                ? (groupWon ? "The group wins" : "The suspects win")
                : _summary.OutcomeHeadline;

            var headlineText = UIFactory.CreateFittedText(banner, headline, Theme.FontTitle, Theme.FontSubheading,
                Theme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold, "Headline");
            UIFactory.SetSize(headlineText.gameObject, 120f, 120f);

            if (!string.IsNullOrEmpty(_summary?.GameOverReason))
            {
                var reason = UIFactory.CreateText(banner, _summary.GameOverReason, Theme.FontBody,
                    Theme.TextSecondary, TextAlignmentOptions.Center, FontStyles.Normal, "Reason");
                UIFactory.FitHeight(reason.gameObject);
            }

            var label = UIFactory.CreateText(content, "WHO WAS WHO", Theme.FontCaption, Theme.TextMuted,
                TextAlignmentOptions.Left, FontStyles.Bold, "RolesLabel");
            label.characterSpacing = 6f;
            UIFactory.SetSize(label.gameObject, 60f, 60f);

            foreach (var player in _session.Players.OrderBy(p => p.SeatIndex))
            {
                var wasSuspect = player.RoleId == PlayerRoles.Imposter;
                var card = UIFactory.CreateCard("Role-" + player.Id, content,
                    wasSuspect ? Theme.WithAlpha(Theme.Danger, 0.16f) : Theme.Surface, Theme.RadiusMedium);

                var row = UIFactory.HorizontalGroup(card, "Row", Theme.SpaceS,
                    new RectOffset((int)Theme.SpaceM, (int)Theme.SpaceM, (int)Theme.SpaceXs, (int)Theme.SpaceXs),
                    TextAnchor.MiddleLeft);
                UIFactory.Stretch((RectTransform)row.transform);

                var name = UIFactory.CreateText(row.transform, player.DisplayName, Theme.FontBody,
                    Theme.TextPrimary, TextAlignmentOptions.Left, FontStyles.Bold, "Name");
                UIFactory.SetSize(name.gameObject, flexibleWidth: 1f);

                var role = UIFactory.CreateText(row.transform, DescribeRole(player.RoleId),
                    Theme.FontLabel, wasSuspect ? Theme.Danger : Theme.TextSecondary,
                    TextAlignmentOptions.Right, FontStyles.Bold, "Role");
                UIFactory.SetSize(role.gameObject, preferredWidth: 300f, minWidth: 300f);

                UIFactory.SetSize(card.gameObject, 116f, 116f);
            }

            UIFactory.Spacer(content, Theme.SpaceL);
        }

        private static string DescribeRole(string roleId)
        {
            switch (roleId)
            {
                case PlayerRoles.Imposter: return "Suspect";
                case PlayerRoles.Investigator: return "Investigator";
                case PlayerRoles.Witness: return "Witness";
                default: return "Clean";
            }
        }

        public override void OnShown()
        {
            UiFeedback.Sound(SoundId.Fanfare);
        }

        public override bool HandleBack()
        {
            App.FinishAndGoHome();
            return true;
        }
    }
}
