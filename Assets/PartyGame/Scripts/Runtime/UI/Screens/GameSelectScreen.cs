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
    /// The game picker. Cards come straight from the content library, so adding a mode is an
    /// asset change rather than a UI change.
    /// </summary>
    public class GameSelectScreen : ScreenBase
    {
        public override string Title => "Choose a game";
        public override string Subtitle => App.Services.Roster.Count + " players ready";

        protected override void Build()
        {
            var content = CreateScrollBody(Theme.SpaceS);
            var modes = App.Services.Content.GameModes;

            if (modes.Count == 0)
            {
                var empty = UIFactory.CreateText(content, "No games are installed.\nRun Party Game > Rebuild Content in the editor.",
                    Theme.FontBody, Theme.TextMuted, TextAlignmentOptions.Center, FontStyles.Normal, "Empty");
                UIFactory.SetSize(empty.gameObject, 300f, 300f);
                return;
            }

            var playerCount = App.Services.Roster.Count;

            foreach (var definition in modes.OrderBy(m => (int)m.ModeId))
            {
                var mode = definition;
                var implemented = GameModeFactory.IsImplemented(mode.ModeId);
                var fits = playerCount >= mode.MinPlayers && playerCount <= mode.MaxPlayers;

                var meta = mode.PlayerRangeLabel + "   -   " + mode.DurationLabel;
                if (!fits)
                {
                    meta = playerCount < mode.MinPlayers
                        ? "Needs " + mode.MinPlayers + " players - you have " + playerCount
                        : "Supports up to " + mode.MaxPlayers + " players";
                }

                var card = SelectionCard.Create(content, mode.DisplayName, mode.Tagline, meta, mode.Glyph,
                    Theme.Accent(mode.AccentIndex), null, 230f, false);

                card.Interactable = implemented;
                card.Clicked += () =>
                {
                    if (!fits)
                    {
                        App.Toast(playerCount < mode.MinPlayers
                            ? mode.DisplayName + " needs at least " + mode.MinPlayers + " players."
                            : mode.DisplayName + " supports up to " + mode.MaxPlayers + " players.");
                        App.ShowPlayers();
                        return;
                    }
                    App.ShowGameSetup(mode);
                };
            }

            UIFactory.Spacer(content, Theme.SpaceL);
        }
    }
}
