using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Content;
using PartyGame.Core.Modes;
using PartyGame.UI.Components;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;

namespace PartyGame.UI.Screens
{
    /// <summary>
    /// Pre-game configuration. The controls are generated from the mode's declared settings,
    /// so a new option appears here as soon as a mode declares it.
    /// </summary>
    public class GameSetupScreen : ScreenBase
    {
        private GameModeDefinition _definition;
        private IGameMode _mode;
        private GameSettings _settings;

        private readonly List<BoundStepper> _steppers = new List<BoundStepper>();
        private readonly Dictionary<string, SelectionCard> _categoryCards = new Dictionary<string, SelectionCard>();
        private TextMeshProUGUI _categorySummary;

        public override string Title => _definition != null ? _definition.DisplayName : "Game setup";
        public override string Subtitle => "Set it up, then start";

        public void Configure(GameModeDefinition definition)
        {
            _definition = definition;
        }

        protected override void Build()
        {
            _mode = GameModeFactory.Create(_definition.ModeId);
            if (_mode == null)
            {
                var message = UIFactory.CreateText(Body, "This game is not playable yet.", Theme.FontBody,
                    Theme.TextMuted, TextAlignmentOptions.Center, FontStyles.Normal, "Unavailable");
                UIFactory.Stretch(message.rectTransform);
                return;
            }

            _settings = App.ModeSettings.Load(_mode, App.Services.Content);
            _settings.ClampAll(App.Services.Roster.Count);

            var content = CreateScrollBody();

            if (_definition.IsBelowRecommended(App.Services.Roster.Count))
            {
                var note = UIFactory.CreatePaddedCard(content, "SizeNote",
                    Theme.WithAlpha(Theme.Warning, 0.14f), Theme.SpaceM);
                var noteText = UIFactory.CreateText(note,
                    "This works with " + App.Services.Roster.Count + " players - it is just " +
                    _definition.RecommendationLabel + ".",
                    Theme.FontLabel, Theme.TextSecondary, TextAlignmentOptions.TopLeft, FontStyles.Normal, "Note");
                UIFactory.FitHeight(noteText.gameObject);
            }

            // Category pickers are long lists, so the game options come first and the packs
            // go underneath - otherwise the settings that matter are buried.
            foreach (var definition in _settings.Definitions.Where(d => d.Type != SettingType.Categories))
            {
                switch (definition.Type)
                {
                    case SettingType.Toggle:
                        BuildToggle(content, definition);
                        break;
                    case SettingType.Stepper:
                        BuildStepper(content, definition);
                        break;
                    case SettingType.Options:
                        BuildOptions(content, definition);
                        break;
                }
            }

            foreach (var definition in _settings.Definitions.Where(d => d.Type == SettingType.Categories))
                BuildCategories(content, definition);

            UIFactory.Spacer(content, Theme.SpaceL);

            AddFooterButton("Start game", ButtonStyle.Primary, StartPressed);
        }

        private void BuildToggle(Transform parent, SettingDefinition definition)
        {
            ToggleSwitch.Create(parent, definition.Label, definition.Description,
                _settings.GetBool(definition.Key, definition.DefaultValue == "1"),
                value => _settings.SetBool(definition.Key, value));
        }

        private void BuildStepper(Transform parent, SettingDefinition definition)
        {
            var max = definition.ResolveMax(App.Services.Roster.Count);
            var stepper = StepperControl.Create(parent, definition.Label, definition.Description,
                _settings.GetInt(definition.Key, definition.Min), definition.Min, max, definition.Step,
                definition.FormatValue, value => _settings.SetInt(definition.Key, value));
            _steppers.Add(new BoundStepper { Definition = definition, Control = stepper });
        }

        private void BuildOptions(Transform parent, SettingDefinition definition)
        {
            OptionPicker.Create(parent, definition.Label, definition.Description, definition.Options,
                _settings.GetString(definition.Key, definition.DefaultValue),
                value => _settings.SetString(definition.Key, value));
        }

        private void BuildCategories(Transform parent, SettingDefinition definition)
        {
            var packs = App.Services.Content.PacksFor(_definition.ModeId);

            var title = UIFactory.CreateText(parent, definition.Label, Theme.FontBody, Theme.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Bold, "CategoriesLabel");
            UIFactory.FitHeight(title.gameObject);

            _categorySummary = UIFactory.CreateText(parent, string.Empty, Theme.FontCaption, Theme.TextMuted,
                TextAlignmentOptions.TopLeft, FontStyles.Normal, "CategoriesSummary");
            UIFactory.FitHeight(_categorySummary.gameObject);

            if (packs.Count == 0)
            {
                _categorySummary.text = "No content packs found for this game.";
                return;
            }

            var actions = UIFactory.HorizontalGroup(parent, "CategoryActions", Theme.SpaceXs, evenColumns: true);
            UIFactory.SetSize(actions.gameObject, 92f, 92f);
            UiButton.Create(actions.transform, "Select all", ButtonStyle.Ghost, () => SetAllCategories(true), 92f);
            UiButton.Create(actions.transform, "Clear", ButtonStyle.Ghost, () => SetAllCategories(false), 92f);
            UiButton.Create(actions.transform, "Surprise me", ButtonStyle.Ghost, PickRandomCategories, 92f);

            var selected = new HashSet<string>(_settings.GetList(definition.Key));

            foreach (var pack in packs)
            {
                var id = pack.Id;
                var card = SelectionCard.Create(parent, pack.DisplayName, pack.Description,
                    pack.EntryCount + " entries", pack.Glyph, Theme.Accent(pack.AccentIndex), null, 170f);
                card.Key = id;
                card.Selected = selected.Contains(id);
                card.Clicked += () =>
                {
                    card.Selected = !card.Selected;
                    CommitCategories(definition.Key);
                };
                _categoryCards[id] = card;
            }

            UpdateCategorySummary();
        }

        private void SetAllCategories(bool selected)
        {
            foreach (var card in _categoryCards.Values) card.Selected = selected;
            CommitCategories(CommonSettingKeys.Categories);
        }

        private void PickRandomCategories()
        {
            if (_categoryCards.Count == 0) return;
            var ids = _categoryCards.Keys.ToList();
            var take = Mathf.Clamp(ids.Count / 2, 1, ids.Count);
            var chosen = new HashSet<string>(Core.Services.Shuffler.PickDistinct(ids, take, App.Services.Random));
            foreach (var pair in _categoryCards) pair.Value.Selected = chosen.Contains(pair.Key);
            CommitCategories(CommonSettingKeys.Categories);
        }

        private void CommitCategories(string key)
        {
            var selected = _categoryCards.Where(p => p.Value.Selected).Select(p => p.Key).ToList();
            _settings.SetList(key, selected);
            UpdateCategorySummary();
        }

        private void UpdateCategorySummary()
        {
            if (_categorySummary == null) return;
            var selected = _categoryCards.Count(p => p.Value.Selected);
            _categorySummary.text = selected == 0
                ? "Nothing picked - every pack will be used"
                : selected + " of " + _categoryCards.Count + " packs selected";
        }

        private void StartPressed()
        {
            _settings.ClampAll(App.Services.Roster.Count);
            App.ModeSettings.Save(_definition.ModeId, _settings);

            var scores = new Core.Services.ScoreManager();
            var probe = new Core.Session.GameSession(App.Services.Roster, scores, App.Services.Random,
                App.Services.Content, _definition, _settings);
            probe.ConfigureRounds(_settings.GetInt(CommonSettingKeys.Rounds, 3));

            var validation = _mode.Validate(probe);
            if (!validation.IsValid)
            {
                App.Toast(validation.Message);
                return;
            }

            App.ShowHowToPlay(_definition, _settings, () => App.StartGame(_definition, _settings));
        }

        public override void OnShown()
        {
            // The player list can change while this screen sits in the stack, so any option
            // whose ceiling depends on it is re-clamped here rather than at build time only.
            foreach (var bound in _steppers)
            {
                if (bound.Definition.MaxForPlayerCount == null) continue;
                bound.Control.SetRange(bound.Definition.Min,
                    bound.Definition.ResolveMax(App.Services.Roster.Count));
            }
        }

        private class BoundStepper
        {
            public SettingDefinition Definition;
            public StepperControl Control;
        }
    }
}
