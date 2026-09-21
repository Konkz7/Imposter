using PartyGame.UI.Components;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;

namespace PartyGame.UI.Screens
{
    /// <summary>Front door of the app: brand, player count and the main routes.</summary>
    public class MainMenuScreen : ScreenBase
    {
        private UiButton _playersButton;

        public override string Title => string.Empty;
        public override bool ShowBackButton => false;

        protected override void Build()
        {
            var column = UIFactory.VerticalGroup(Body, "Menu", Theme.SpaceS, null, TextAnchor.MiddleCenter);
            UIFactory.Stretch((RectTransform)column.transform);

            // Two weighted gaps balance the brand against the buttons instead of pinning
            // either to an edge.
            UIFactory.Spacer(column.transform, Theme.SpaceL, true, 0.55f);

            var brand = UIFactory.CreateText(column.transform, "ODD\nONE\nOUT", Theme.FontDisplay, Theme.TextPrimary,
                TextAlignmentOptions.Center, FontStyles.Bold, "Brand");
            brand.characterSpacing = 8f;
            brand.lineSpacing = -18f;
            UIFactory.SetSize(brand.gameObject, 380f, 380f);

            var strapline = UIFactory.CreateText(column.transform,
                "Five party games for one phone and a room full of people",
                Theme.FontLabel, Theme.TextSecondary, TextAlignmentOptions.Center, FontStyles.Normal, "Strapline");
            UIFactory.FitHeight(strapline.gameObject);

            UIFactory.Spacer(column.transform, Theme.SpaceM, true);

            UiButton.Create(column.transform, "Play", ButtonStyle.Primary, App.ShowGameSelect, 170f, "Pick a game");

            _playersButton = UiButton.Create(column.transform, "Players", ButtonStyle.Secondary, App.ShowPlayers,
                Theme.CompactButtonHeight, PlayerSummary());

            var row = UIFactory.HorizontalGroup(column.transform, "SecondaryRow", Theme.SpaceS, evenColumns: true);
            UIFactory.SetSize(row.gameObject, Theme.CompactButtonHeight, Theme.CompactButtonHeight);
            UiButton.Create(row.transform, "How to play", ButtonStyle.Ghost, App.ShowHelpIndex, Theme.CompactButtonHeight);
            UiButton.Create(row.transform, "Settings", ButtonStyle.Ghost, App.ShowSettings, Theme.CompactButtonHeight);

            UiButton.Create(column.transform, "About", ButtonStyle.Ghost, App.ShowAbout, 84f);

            UIFactory.Spacer(column.transform, Theme.SpaceM);
        }

        private string PlayerSummary()
        {
            var count = App.Services.Roster.Count;
            return count + (count == 1 ? " player added" : " players added");
        }

        public override void OnShown()
        {
            if (_playersButton != null) _playersButton.SetSublabel(PlayerSummary());
        }
    }
}
