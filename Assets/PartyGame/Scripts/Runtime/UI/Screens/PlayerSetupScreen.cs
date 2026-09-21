using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Session;
using PartyGame.UI.Components;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;

namespace PartyGame.UI.Screens
{
    /// <summary>
    /// Add, rename, reorder and remove players. The roster lives in the app services, so every
    /// game mode shares it and nothing is entered twice.
    /// </summary>
    public class PlayerSetupScreen : ScreenBase
    {
        private RectTransform _list;
        private TextMeshProUGUI _counter;
        private UiButton _addButton;
        private readonly List<PlayerRowView> _rows = new List<PlayerRowView>();

        public override string Title => "Players";
        public override string Subtitle => "Everyone who is playing on this phone";

        protected override void Build()
        {
            var column = UIFactory.VerticalGroup(Body, "Column", Theme.SpaceS);
            UIFactory.Stretch((RectTransform)column.transform);

            var header = UIFactory.HorizontalGroup(column.transform, "Counter", Theme.SpaceS, null, TextAnchor.MiddleLeft);
            UIFactory.SetSize(header.gameObject, 90f, 90f);

            _counter = UIFactory.CreateText(header.transform, string.Empty, Theme.FontLabel, Theme.TextSecondary,
                TextAlignmentOptions.Left, FontStyles.Normal, "Count");
            UIFactory.SetSize(_counter.gameObject, flexibleWidth: 1f);

            var shuffle = UiButton.Create(header.transform, "Shuffle order", ButtonStyle.Ghost, ShuffleOrder, 90f);
            UIFactory.SetSize(shuffle.gameObject, 90f, 90f, 320f, 320f);

            var scrollHolder = UIFactory.CreateRect("ListHolder", column.transform);
            UIFactory.SetSize(scrollHolder.gameObject, flexibleHeight: 1f, minHeight: 300f);
            _list = UIFactory.CreateScrollView(scrollHolder, "Scroll", Theme.SpaceXs,
                new RectOffset(0, 0, 0, (int)Theme.SpaceL), out var scrollRect);
            UIFactory.Stretch((RectTransform)scrollRect.transform);

            _addButton = UiButton.Create(Footer, "Add player", ButtonStyle.Secondary, AddPlayer, Theme.CompactButtonHeight);
            AddFooterButton("Done", ButtonStyle.Primary, Done);

            Rebuild();
        }

        private void Rebuild()
        {
            foreach (Transform child in _list) Destroy(child.gameObject);
            _rows.Clear();

            var roster = App.Services.Roster;
            var canRemove = roster.Count > PlayerRoster.AbsoluteMinPlayers;

            foreach (var player in roster.Players)
            {
                var row = PlayerRowView.Create(_list, player, Rename, Remove, canRemove);
                _rows.Add(row);
            }

            RefreshCounter();
        }

        private void RefreshCounter()
        {
            var roster = App.Services.Roster;
            if (_counter != null)
                _counter.text = roster.Count + " of " + PlayerRoster.AbsoluteMaxPlayers + " players";
            if (_addButton != null)
                _addButton.Interactable = roster.Count < PlayerRoster.AbsoluteMaxPlayers;
        }

        private void AddPlayer()
        {
            var roster = App.Services.Roster;
            if (roster.Count >= PlayerRoster.AbsoluteMaxPlayers)
            {
                App.Toast("That is the maximum number of players.");
                return;
            }
            roster.Add(null);
            Rebuild();
        }

        private void Remove(int playerId)
        {
            var roster = App.Services.Roster;
            if (roster.Count <= PlayerRoster.AbsoluteMinPlayers)
            {
                App.Toast("You need at least " + PlayerRoster.AbsoluteMinPlayers + " players.");
                return;
            }
            roster.Remove(playerId);
            Rebuild();
        }

        private void Rename(int playerId, string value)
        {
            App.Services.Roster.Rename(playerId, value);
            var player = App.Services.Roster.Get(playerId);
            var index = _rows.FindIndex(r => r.PlayerId == playerId);
            if (index >= 0 && player != null)
                _rows[index].Refresh(player, App.Services.Roster.Count > PlayerRoster.AbsoluteMinPlayers);
        }

        private void ShuffleOrder()
        {
            App.Services.Roster.Shuffle(App.Services.Random);
            Rebuild();
            App.Toast("Seating order shuffled.", false);
        }

        private void Done()
        {
            var roster = App.Services.Roster;
            var duplicates = roster.Players
                .GroupBy(p => (p.DisplayName ?? string.Empty).Trim().ToLowerInvariant())
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicates != null)
            {
                App.Toast("Two players are called " + duplicates.First().DisplayName + ". Make the names different.");
                return;
            }

            App.Services.PersistPlayers();
            App.Back();
        }

        public override void OnHidden()
        {
            App.Services.PersistPlayers();
        }
    }
}
