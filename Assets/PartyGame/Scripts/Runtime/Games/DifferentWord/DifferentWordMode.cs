using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Content;
using PartyGame.Core.Modes;
using PartyGame.Core.Services;
using PartyGame.Core.Session;
using PartyGame.Core.Util;

namespace PartyGame.Games.DifferentWord
{
    /// <summary>How much the imposter is told about the word everyone else received.</summary>
    public static class ImposterHint
    {
        public const string Nothing = "none";
        public const string CategoryOnly = "category";
        public const string SimilarWord = "word";
        public const string SimilarWordAndClue = "wordClue";
    }

    /// <summary>
    /// Everyone gets the same word except the imposters, who get a related one.
    /// Players describe their word without saying it, then vote on who was the odd one out.
    /// </summary>
    public class DifferentWordMode : ImposterModeBase
    {
        public const string SettingHint = "imposterHint";

        private readonly ContentRotation<WordPair> _pairRotation = new ContentRotation<WordPair>(p => p.PairId, 12);
        private readonly ContentRotation<WordCategoryData> _categoryRotation = new ContentRotation<WordCategoryData>(c => c.Id, 3);

        private WordCategoryData _category;
        private WordPair _pair;

        public override GameModeId Id => GameModeId.DifferentWord;

        protected override void OnInitialise()
        {
            Scoring = new ImposterScoring
            {
                CorrectAccusation = 100,
                CrewCaughtImposter = 150,
                ImposterSurvived = 300
            };
        }

        public override IReadOnlyList<SettingDefinition> GetSettingDefinitions(ContentService content)
        {
            var imposterCount = SettingDefinition.Stepper(CommonSettingKeys.ImposterCount, "Imposters", 1, 1, 4,
                description: "How many players get the different word");
            imposterCount.MaxForPlayerCount = players => System.Math.Max(1, (players - 1) / 2);

            return new List<SettingDefinition>
            {
                SettingDefinition.CategoryPicker(CommonSettingKeys.Categories, "Categories",
                    "Pick one, several, or leave empty for all"),
                imposterCount,
                SettingDefinition.Choice(SettingHint, "Imposter hint", ImposterHint.SimilarWord, new[]
                {
                    new SettingOption(ImposterHint.Nothing, "No hint", "The imposter gets no word at all - brutal"),
                    new SettingOption(ImposterHint.CategoryOnly, "Category only", "The imposter only learns the category"),
                    new SettingOption(ImposterHint.SimilarWord, "Similar word", "The imposter gets a related word"),
                    new SettingOption(ImposterHint.SimilarWordAndClue, "Similar word + clue", "Related word plus a written nudge")
                }, "How much help the imposter gets"),
                SettingDefinition.Stepper(CommonSettingKeys.Rounds, "Rounds", 3, 1, 10),
                SettingDefinition.Stepper(CommonSettingKeys.DiscussionSeconds, "Discussion time", 120, 30, 300, 15, "s"),
                SettingDefinition.Toggle(CommonSettingKeys.VotingEnabled, "Voting", true,
                    "Turn off for a purely social round"),
                SettingDefinition.Choice(CommonSettingKeys.TieBehaviour, "If the vote ties", "noResult", TieOptions())
            };
        }

        public override ValidationResult Validate(GameSession session)
        {
            var baseResult = base.Validate(session);
            if (!baseResult.IsValid) return baseResult;

            var playerCount = session.Roster.Count;
            var imposters = session.Settings.GetInt(CommonSettingKeys.ImposterCount, 1);
            if (imposters < 1) return ValidationResult.Fail("There must be at least one imposter.");
            if (imposters > (playerCount - 1) / 2)
                return ValidationResult.Fail("With " + playerCount + " players you can have at most " +
                                             System.Math.Max(1, (playerCount - 1) / 2) + " imposters.");

            var categories = session.Content.ResolveSelection<WordCategoryData>(
                GameModeId.DifferentWord, session.Settings.GetList(CommonSettingKeys.Categories));
            if (categories.Count == 0) return ValidationResult.Fail("No word categories are available.");

            return ValidationResult.Ok();
        }

        public override IReadOnlyList<string> BuildRulesSummary(GameSettings settings)
        {
            var hint = settings.GetString(SettingHint, ImposterHint.SimilarWord);
            var hintLine = hint == ImposterHint.Nothing
                ? "The imposter gets no word at all."
                : hint == ImposterHint.CategoryOnly
                    ? "The imposter only learns the category."
                    : "The imposter gets a different but related word.";

            return new List<string>
            {
                "Everyone secretly receives a word. " + hintLine,
                "Pass the phone around so nobody sees another card.",
                "Take turns saying one word that describes yours - never the word itself.",
                "Talk it out, then vote for whoever sounds like the odd one out.",
                "Imposters score by surviving the vote."
            };
        }

        protected override void BuildRound()
        {
            var categories = Session.Content.ResolveSelection<WordCategoryData>(
                GameModeId.DifferentWord, Session.Settings.GetList(CommonSettingKeys.Categories));

            if (categories.Count == 0)
            {
                Enqueue(new MessageStep
                {
                    Title = "No content",
                    Body = "No word categories are available. Check the content settings.",
                    Accent = StepAccent.Danger,
                    ContinueLabel = "Back"
                });
                return;
            }

            _category = _categoryRotation.Next(categories, Session.Random);
            var pairs = _category.Pairs.Where(p => p != null && p.IsValid).ToList();
            if (pairs.Count == 0)
            {
                Enqueue(new MessageStep
                {
                    Title = "Empty category",
                    Body = _category.DisplayName + " has no word pairs yet.",
                    Accent = StepAccent.Danger,
                    ContinueLabel = "Back"
                });
                return;
            }

            _pair = _pairRotation.Next(pairs, Session.Random);

            // Half the time the roles of the two words are swapped so the "crew" word is not
            // always the more obvious of the pair.
            var swap = Session.Random.Range(0, 2) == 1;
            var crewWord = swap ? _pair.ImposterWord : _pair.CrewWord;
            var imposterWord = swap ? _pair.CrewWord : _pair.ImposterWord;

            var imposters = AssignImposters(Session.Settings.GetInt(CommonSettingKeys.ImposterCount, 1));
            var hintMode = Session.Settings.GetString(SettingHint, ImposterHint.SimilarWord);

            Enqueue(new MessageStep
            {
                PhaseLabel = Session.RoundLabel,
                Title = "Secret words",
                Body = "Everyone gets a word - except " + (imposters.Count == 1 ? "one player" : imposters.Count + " players") +
                       ". Pass the phone around the group and keep your card to yourself.",
                Feature = _category.Glyph + "  " + _category.DisplayName,
                Accent = StepAccent.Primary,
                ContinueLabel = "Start passing"
            });

            var infoSteps = new List<GameStep>();
            foreach (var player in TurnOrder())
            {
                var isImposter = player.RoleId == PlayerRoles.Imposter;
                var step = new PrivateInfoStep
                {
                    PhaseLabel = "Secret word",
                    Title = player.DisplayName,
                    ActorPlayerId = player.Id,
                    Accent = isImposter ? StepAccent.Danger : StepAccent.Primary,
                    Headline = isImposter ? "YOU ARE THE IMPOSTER" : "You are in the group",
                    ContinueLabel = "Hide and pass on"
                };

                if (isImposter)
                {
                    player.SetRoundData(RoundDataKeys.Word, imposterWord);
                    switch (hintMode)
                    {
                        case ImposterHint.Nothing:
                            step.Lines.Add(new InfoLine("Your word", "unknown", true));
                            step.Lines.Add(new InfoLine("", "You get nothing. Listen hard and bluff."));
                            player.SetRoundData(RoundDataKeys.Word, string.Empty);
                            break;
                        case ImposterHint.CategoryOnly:
                            step.Lines.Add(new InfoLine("Category", _category.DisplayName, true));
                            step.Lines.Add(new InfoLine("", "You know the topic but not the word."));
                            player.SetRoundData(RoundDataKeys.Word, string.Empty);
                            break;
                        case ImposterHint.SimilarWordAndClue:
                            step.Lines.Add(new InfoLine("Your word", imposterWord, true));
                            step.Lines.Add(new InfoLine("Category", _category.DisplayName));
                            if (!string.IsNullOrWhiteSpace(_pair.Hint))
                            {
                                step.Lines.Add(new InfoLine("Clue", _pair.Hint));
                                player.SetRoundData(RoundDataKeys.Hint, _pair.Hint);
                            }
                            break;
                        default:
                            step.Lines.Add(new InfoLine("Your word", imposterWord, true));
                            step.Lines.Add(new InfoLine("Category", _category.DisplayName));
                            break;
                    }
                    step.Footnote = "Blend in. Nobody else knows your word is different.";
                }
                else
                {
                    player.SetRoundData(RoundDataKeys.Word, crewWord);
                    step.Lines.Add(new InfoLine("Your word", crewWord, true));
                    step.Lines.Add(new InfoLine("Category", _category.DisplayName));
                    step.Footnote = "Describe it without ever saying it.";
                }

                infoSteps.Add(Enqueue(step));
            }
            ApplySequence(infoSteps);

            Enqueue(new MessageStep
            {
                PhaseLabel = "Describe",
                Title = "Go round the group",
                Body = "Each player says one word that describes their secret word. Never say the word itself.",
                Feature = _category.DisplayName,
                Accent = StepAccent.Primary,
                ContinueLabel = "First player"
            });

            var promptSteps = new List<GameStep>();
            foreach (var player in TurnOrder())
            {
                promptSteps.Add(Enqueue(new MessageStep
                {
                    PhaseLabel = "Describe",
                    Title = player.DisplayName + ", your turn",
                    Body = "Say one word that describes your secret word out loud.",
                    ActorPlayerId = player.Id,
                    Accent = StepAccent.Neutral,
                    ContinueLabel = "Next player"
                }));
            }
            ApplySequence(promptSteps);

            Enqueue(BuildDiscussion(
                "Talk it through",
                "Who sounded slightly off? Challenge each other before the vote.",
                Session.Settings.GetInt(CommonSettingKeys.DiscussionSeconds, 120),
                "Nobody has to say their word",
                "Ask follow-up questions",
                "Watch for someone agreeing a bit too fast"));

            BuildVotingPhase("Who do you think had the different word?");
        }

        protected override void BuildReveal()
        {
            var imposters = Imposters;
            var reveal = new RevealStep
            {
                PhaseLabel = "Reveal",
                Title = "The words",
                Headline = imposters.Count == 1
                    ? NameOf(imposters[0].Id) + " was the imposter"
                    : JoinNames(imposters) + " were the imposters",
                Accent = ImposterWasCaught ? StepAccent.Success : StepAccent.Danger
            };

            var crewWord = Session.ActivePlayers
                .Where(p => p.RoleId != PlayerRoles.Imposter)
                .Select(p => p.GetRoundData(RoundDataKeys.Word))
                .FirstOrDefault(w => !string.IsNullOrEmpty(w));

            var imposterWord = imposters
                .Select(p => p.GetRoundData(RoundDataKeys.Word))
                .FirstOrDefault(w => !string.IsNullOrEmpty(w));

            reveal.Entries.Add(new RevealEntry
            {
                Title = "The group's word",
                Detail = string.IsNullOrEmpty(crewWord) ? "-" : crewWord,
                Accent = StepAccent.Primary,
                Highlight = true
            });

            reveal.Entries.Add(new RevealEntry
            {
                Title = "The imposter's word",
                Detail = string.IsNullOrEmpty(imposterWord) ? "They were given nothing at all" : imposterWord,
                Accent = StepAccent.Danger,
                Highlight = true
            });

            if (_category != null)
                reveal.Entries.Add(new RevealEntry { Title = "Category", Detail = _category.DisplayName });

            AppendVoteEntries(reveal);
            Enqueue(reveal);
        }
    }
}
