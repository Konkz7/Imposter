using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PartyGame.Core.Content;
using PartyGame.Core.Modes;
using PartyGame.Core.Services;
using PartyGame.Core.Session;

namespace PartyGame.Games.Wavelength
{
    /// <summary>
    /// Everyone is given the same secret number on a 1-10 scale, except the imposter. The players
    /// take turns writing a question with a scale, everyone else answers it at the strength of
    /// their number, and the group works out whose answers do not fit.
    /// </summary>
    public class WavelengthMode : ImposterModeBase
    {
        public const string SettingDeviation = "minDeviation";
        public const string SettingImposterKnows = "imposterKnows";
        public const string SettingQuestionsPerRound = "questionsPerRound";
        public const string SettingAnswers = "answers";

        public const string AnswersAloud = "aloud";
        public const string AnswersTyped = "typed";

        /// <summary>The top of the questions stepper, shown as "Everyone": every player asks one.</summary>
        public const int EveryoneAsks = 7;
        public const int FewestQuestions = 2;

        public const string FieldQuestion = "question";
        public const string FieldLow = "low";
        public const string FieldHigh = "high";

        private const string AskPrefix = "ask-";
        private const string AnswerPrefix = "answer-";
        private const int SuggestionsPerAsker = 12;

        private const int ScaleMin = 1;
        private const int ScaleMax = 10;

        /// <summary>The lowest gap: the odd number only has to differ, not to differ by much.</summary>
        public const int MinimumGapForAny = 1;

        private const int DefaultGap = 4;

        private readonly List<AskedQuestion> _questions = new List<AskedQuestion>();
        private List<WavelengthQuestion> _suggestionPool = new List<WavelengthQuestion>();
        private MessageStep _overview;
        private int _crewNumber;
        private int _imposterNumber;

        /// <summary>One player's question this round, and what everyone said back.</summary>
        private sealed class AskedQuestion
        {
            public AskedQuestion(PlayerState asker, int number)
            {
                Asker = asker;
                Number = number;
            }

            public PlayerState Asker { get; }

            /// <summary>Position in the round, from 1.</summary>
            public int Number { get; }

            public string Prompt { get; set; } = string.Empty;
            public string Low { get; set; } = string.Empty;
            public string High { get; set; } = string.Empty;

            /// <summary>Typed answers by player id. Stays empty when answers are said out loud.</summary>
            public Dictionary<int, string> Answers { get; } = new Dictionary<int, string>();

            /// <summary>Every step that shows the question, built before it was written.</summary>
            public List<GameStep> Showing { get; } = new List<GameStep>();

            /// <summary>Typed answers only: the table's view of every answer to this question.</summary>
            public MessageStep AnswerSheet { get; set; }
        }

        public override GameModeId Id => GameModeId.Wavelength;

        private bool AnswersAreTyped => Session.Settings.GetString(SettingAnswers, AnswersAloud) == AnswersTyped;

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
            var imposterCount = SettingDefinition.Stepper(CommonSettingKeys.ImposterCount, "Off-scale players", 1, 1, 3,
                description: "How many players get a different number");
            imposterCount.MaxForPlayerCount = players => Math.Max(1, (players - 1) / 3);

            return new List<SettingDefinition>
            {
                SettingDefinition.CategoryPicker(CommonSettingKeys.Categories, "Ready-made questions",
                    "Packs behind 'Use a ready-made one'. Pick some, or leave empty for all"),
                imposterCount,
                BuildGapSetting(),
                SettingDefinition.Toggle(SettingImposterKnows, "Tell them they are odd", false,
                    "On: the odd player knows. Off: they have no idea - far funnier"),
                BuildQuestionsSetting(),
                SettingDefinition.Choice(SettingAnswers, "Answers", AnswersAloud, new[]
                {
                    new SettingOption(AnswersAloud, "Out loud", "Go round the table answering each question"),
                    new SettingOption(AnswersTyped, "Typed", "Answer in private, then everyone's answers are shown before the vote")
                }),
                SettingDefinition.Stepper(CommonSettingKeys.Rounds, "Rounds", 3, 1, 10),
                SettingDefinition.Stepper(CommonSettingKeys.DiscussionSeconds, "Discussion time", 90, 30, 300, 15, "s"),
                SettingDefinition.Toggle(CommonSettingKeys.VotingEnabled, "Voting", true),
                SettingDefinition.Choice(CommonSettingKeys.TieBehaviour, "If the vote ties", "noResult", TieOptions())
            };
        }

        /// <summary>
        /// How far the odd number must sit from everyone else. A gap of one means "any other
        /// number", which leaks nothing: knowing the setting tells you nothing about the range
        /// the odd number could be in. Anything higher trades that secrecy for a clearer signal.
        /// </summary>
        private static SettingDefinition BuildGapSetting()
        {
            var gap = SettingDefinition.Stepper(SettingDeviation, "Minimum gap", DefaultGap, MinimumGapForAny, 7,
                description: "Any means the odd number can be next door, so the gap gives nothing away");
            gap.ValueLabels[MinimumGapForAny] = "Any";
            return gap;
        }

        /// <summary>
        /// When everyone asks, everyone else answers every question, which grows fast with a big
        /// group. A lower cap keeps rounds short, and the askers then take turns between rounds.
        /// </summary>
        private static SettingDefinition BuildQuestionsSetting()
        {
            var questions = SettingDefinition.Stepper(SettingQuestionsPerRound, "Questions per round", EveryoneAsks,
                FewestQuestions, EveryoneAsks,
                description: "Lower it for big groups - the askers then take turns between rounds");
            questions.ValueLabels[EveryoneAsks] = "Everyone";
            return questions;
        }

        public override IReadOnlyList<string> BuildRulesSummary(GameSettings settings)
        {
            var cap = settings.GetInt(SettingQuestionsPerRound, EveryoneAsks);
            var typed = settings.GetString(SettingAnswers, AnswersAloud) == AnswersTyped;

            return new List<string>
            {
                "Everyone secretly gets the same number from 1 to 10 - except one player.",
                (cap >= EveryoneAsks ? "Each round, everyone" : "Each round, " + cap + " players take turns and")
                    + " write a question with a scale, like 'Name a food: 1 = disgusting, 10 = delicious'.",
                "Writing the ends of the scale is optional - you can explain it out loud instead.",
                "Everyone else answers at the strength of their number, never saying the number.",
                typed
                    ? "Answers are typed in private, then everyone's answers are shown before the vote."
                    : "Answers are said out loud, going round the table.",
                "Spot whose answers keep landing in the wrong place, then vote.",
                settings.GetBool(SettingImposterKnows, false)
                    ? "The odd player knows they are off-scale."
                    : "The odd player has no idea they are off-scale.",
                settings.GetInt(SettingDeviation, DefaultGap) <= MinimumGapForAny
                    ? "The odd number can be anything, even next door - no clue from the gap."
                    : "The odd number is at least " + settings.GetInt(SettingDeviation, DefaultGap) + " away from everyone else."
            };
        }

        protected override void BuildRound()
        {
            _questions.Clear();
            _overview = null;

            var packs = Session.Content.ResolveSelection<WavelengthPackData>(
                GameModeId.Wavelength, Session.Settings.GetList(CommonSettingKeys.Categories));
            _suggestionPool = packs.SelectMany(p => p.Questions).Where(q => q != null && q.IsValid).ToList();

            var gap = Math.Max(MinimumGapForAny, Session.Settings.GetInt(SettingDeviation, DefaultGap));
            AssignNumbers(gap);

            var imposters = AssignImposters(Session.Settings.GetInt(CommonSettingKeys.ImposterCount, 1));
            var imposterKnows = Session.Settings.GetBool(SettingImposterKnows, false);

            Enqueue(new MessageStep
            {
                PhaseLabel = Session.RoundLabel,
                Title = "Secret numbers",
                Body = "Everyone gets a number from 1 to 10. Pass the phone around and keep it to yourself.",
                Feature = ScaleMin + " - " + ScaleMax,
                Accent = StepAccent.Primary,
                ContinueLabel = "Start passing"
            });

            var infoSteps = new List<GameStep>();
            foreach (var player in TurnOrder())
            {
                var isImposter = imposters.Any(i => i.Id == player.Id);
                var number = isImposter ? _imposterNumber : _crewNumber;
                player.SetRoundData(RoundDataKeys.Number, number.ToString(CultureInfo.InvariantCulture));

                var step = new PrivateInfoStep
                {
                    PhaseLabel = "Your number",
                    Title = player.DisplayName,
                    ActorPlayerId = player.Id,
                    Accent = isImposter && imposterKnows ? StepAccent.Danger : StepAccent.Primary,
                    Headline = number.ToString(CultureInfo.InvariantCulture),
                    ContinueLabel = "Hide and pass on",
                    Footnote = "Never say your number out loud."
                };
                step.Lines.Add(new InfoLine("Your number", number + " out of 10", true));
                step.Lines.Add(new InfoLine("Every answer", "Pitch it at " + number + " on that question's scale"));
                if (isImposter && imposterKnows)
                    step.Lines.Add(new InfoLine("Careful", "Your number is not the same as everyone else's."));

                infoSteps.Add(Enqueue(step));
            }
            ApplySequence(infoSteps);

            var askers = AskersThisRound();
            for (var i = 0; i < askers.Count; i++) _questions.Add(new AskedQuestion(askers[i], i + 1));

            if (AnswersAreTyped) BuildTypedAnswers();
            else BuildSpokenAnswers();

            Enqueue(BuildDiscussion(
                "Who is off the scale?",
                "Look back over every answer. Somebody is working from a different number.",
                Session.Settings.GetInt(CommonSettingKeys.DiscussionSeconds, 90),
                "Ask people to explain an answer",
                "A too-strong or too-weak answer is the giveaway"));

            BuildVotingPhase("Whose answers did not fit the scale?");
        }

        /// <summary>
        /// Out loud: each asker writes their question when their go comes round, then everyone
        /// else answers it in turn, starting with the next player along.
        /// </summary>
        private void BuildSpokenAnswers()
        {
            foreach (var question in _questions)
            {
                Enqueue(BuildAskForm(question));

                question.Showing.Add(Enqueue(new MessageStep
                {
                    PhaseLabel = QuestionLabel(question),
                    Title = question.Asker.DisplayName + " asks",
                    Accent = StepAccent.Primary,
                    ContinueLabel = "First answer"
                }));

                var turns = new List<GameStep>();
                foreach (var player in AnswerersFor(question))
                {
                    var turn = Enqueue(new MessageStep
                    {
                        PhaseLabel = AnswersLabel(question),
                        Title = player.DisplayName + ", your answer",
                        ActorPlayerId = player.Id,
                        ContinueLabel = "Next player"
                    });
                    question.Showing.Add(turn);
                    turns.Add(turn);
                }
                ApplySequence(turns);
            }
        }

        /// <summary>
        /// Typed: the phone goes round twice. Everyone writes their question, then each player
        /// answers all of the others in one sitting, so the phone is passed once per player rather
        /// than once per answer. Nothing is shown until everyone has answered, so nobody can copy.
        /// </summary>
        private void BuildTypedAnswers()
        {
            foreach (var question in _questions) Enqueue(BuildAskForm(question));

            _overview = Enqueue(new MessageStep
            {
                PhaseLabel = "This round's questions",
                Title = "The questions",
                Body = "Askers, explain your scale now if you left it blank. Then the phone goes round for everyone to answer in private.",
                Accent = StepAccent.Primary,
                ContinueLabel = "Start answering"
            });

            foreach (var player in TurnOrder())
            {
                var sitting = new List<GameStep>();
                foreach (var question in _questions.Where(q => q.Asker.Id != player.Id))
                {
                    var answer = Enqueue(new TextInputStep
                    {
                        Id = AnswerPrefix + question.Number.ToString(CultureInfo.InvariantCulture) + "-"
                             + player.Id.ToString(CultureInfo.InvariantCulture),
                        PhaseLabel = "Your answers",
                        Title = question.Asker.DisplayName + " asks",
                        Placeholder = "Your answer",
                        MaxLength = 40,
                        ActorPlayerId = player.Id,
                        Accent = StepAccent.Primary,
                        ContinueLabel = "Lock in"
                    });
                    question.Showing.Add(answer);
                    sitting.Add(answer);
                }
                ApplySequence(sitting);
            }

            foreach (var question in _questions)
            {
                question.AnswerSheet = Enqueue(new MessageStep
                {
                    PhaseLabel = AnswersLabel(question),
                    Title = question.Asker.DisplayName + " asked",
                    Accent = StepAccent.Primary,
                    ContinueLabel = question.Number < _questions.Count ? "Next question" : "Talk it over"
                });
                question.Showing.Add(question.AnswerSheet);
            }
        }

        /// <summary>The asker's screen. The two ends of the scale are optional.</summary>
        private FormStep BuildAskForm(AskedQuestion question)
        {
            var form = new FormStep
            {
                Id = AskPrefix + question.Number.ToString(CultureInfo.InvariantCulture),
                PhaseLabel = QuestionLabel(question),
                Title = question.Asker.DisplayName + ", ask a question",
                Body = "Everyone else answers it at the strength of their number.",
                Prompt = "Fill in the ends of the scale, or leave them blank and explain them out loud.",
                ActorPlayerId = question.Asker.Id,
                Accent = StepAccent.Primary,
                ContinueLabel = "Ask this"
            };
            form.Fields.Add(new FormField(FieldQuestion, "Question", "e.g. Name a food", 80, true));
            form.Fields.Add(new FormField(FieldLow, "1 means", "e.g. disgusting", 30, false));
            form.Fields.Add(new FormField(FieldHigh, "10 means", "e.g. delicious", 30, false));

            foreach (var idea in Shuffler.PickDistinct(_suggestionPool, SuggestionsPerAsker, Session.Random))
            {
                form.Suggestions.Add(new Dictionary<string, string>
                {
                    [FieldQuestion] = idea.Prompt,
                    [FieldLow] = idea.LowLabel,
                    [FieldHigh] = idea.HighLabel
                });
            }
            return form;
        }

        protected override void OnStepHandled(GameStep step, StepResult result)
        {
            if (step == null || result == null) return;

            if (step.Id.StartsWith(AskPrefix, StringComparison.Ordinal))
            {
                var question = _questions.FirstOrDefault(q => step.Id == AskPrefix + q.Number.ToString(CultureInfo.InvariantCulture));
                if (question == null) return;

                question.Prompt = result.ValueOf(FieldQuestion);
                if (question.Prompt.Length == 0) question.Prompt = "(asked out loud)";
                question.Low = result.ValueOf(FieldLow);
                question.High = result.ValueOf(FieldHigh);

                ShowQuestion(question);
                RefreshOverview();
                return;
            }

            if (step.Id.StartsWith(AnswerPrefix, StringComparison.Ordinal) && step.ActorPlayerId.HasValue)
            {
                var question = _questions.FirstOrDefault(q => q.Showing.Contains(step));
                if (question == null) return;

                question.Answers[step.ActorPlayerId.Value] = (result.Text ?? string.Empty).Trim();
                RefreshAnswerSheet(question);
            }
        }

        /// <summary>Writes the question into every step that was built before it existed.</summary>
        private void ShowQuestion(AskedQuestion question)
        {
            var scale = ScaleText(question);
            foreach (var step in question.Showing)
            {
                switch (step)
                {
                    case TextInputStep answer:
                        answer.Body = question.Prompt;
                        answer.Prompt = JoinLines(scale,
                            "Your number is " + NumberOf(answer.ActorPlayerId) + ". Answer at that level, without saying it.");
                        break;
                    case MessageStep turn when turn.ActorPlayerId.HasValue:
                        turn.Feature = question.Prompt;
                        turn.Body = JoinLines(scale, "Answer out loud at your number. Never say it.");
                        break;
                    case MessageStep screen:
                        screen.Feature = question.Prompt;
                        screen.Body = scale;
                        break;
                }
            }
        }

        private void RefreshOverview()
        {
            if (_overview == null) return;

            _overview.Bullets.Clear();
            foreach (var question in _questions.Where(q => q.Prompt.Length > 0))
            {
                var scale = ScaleText(question);
                _overview.Bullets.Add(question.Asker.DisplayName + ": " + question.Prompt
                                      + (scale.Length > 0 ? "  (" + scale + ")" : string.Empty));
            }
        }

        private void RefreshAnswerSheet(AskedQuestion question)
        {
            if (question.AnswerSheet == null) return;

            question.AnswerSheet.Bullets.Clear();
            foreach (var player in AnswerersFor(question))
                if (question.Answers.TryGetValue(player.Id, out var answer))
                    question.AnswerSheet.Bullets.Add(player.DisplayName + ": " + answer);
        }

        /// <summary>
        /// Everyone, in this round's turn order, unless the questions are capped. Then the next
        /// block of seats asks each round, so everyone gets a go over the course of the game.
        /// </summary>
        private List<PlayerState> AskersThisRound()
        {
            var order = TurnOrder();
            var cap = Session.Settings.GetInt(SettingQuestionsPerRound, EveryoneAsks);
            if (cap >= EveryoneAsks || cap >= order.Count) return order;

            cap = Math.Max(1, cap);
            var seats = Session.ActivePlayers.OrderBy(p => p.SeatIndex).ToList();
            var start = (Session.RoundNumber - 1) * cap % seats.Count;
            return Enumerable.Range(0, cap).Select(i => seats[(start + i) % seats.Count]).ToList();
        }

        /// <summary>Everyone but the asker, starting with the next player along from them.</summary>
        private List<PlayerState> AnswerersFor(AskedQuestion question)
        {
            var seats = Session.ActivePlayers.OrderBy(p => p.SeatIndex).ToList();
            var start = Math.Max(0, seats.FindIndex(p => p.Id == question.Asker.Id));
            return Enumerable.Range(1, seats.Count - 1).Select(i => seats[(start + i) % seats.Count]).ToList();
        }

        private string QuestionLabel(AskedQuestion question)
        {
            return _questions.Count == 1 ? "The question" : "Question " + question.Number + " of " + _questions.Count;
        }

        private string AnswersLabel(AskedQuestion question)
        {
            return _questions.Count == 1 ? "Answers" : "Answers - question " + question.Number + " of " + _questions.Count;
        }

        /// <summary>Whichever ends the asker wrote. Empty when they wrote neither.</summary>
        private static string ScaleText(AskedQuestion question)
        {
            var ends = new List<string>();
            if (question.Low.Length > 0) ends.Add("1 = " + question.Low);
            if (question.High.Length > 0) ends.Add("10 = " + question.High);
            return string.Join("   |   ", ends);
        }

        private static string JoinLines(string first, string second)
        {
            return first.Length == 0 ? second : first + "\n" + second;
        }

        private int NumberOf(int? playerId)
        {
            var player = playerId.HasValue ? Session.PlayerOf(playerId.Value) : null;
            return player != null ? player.GetRoundDataInt(RoundDataKeys.Number) : 0;
        }

        /// <summary>
        /// Picks the shared number, then the odd one. At the lowest gap every other number on the
        /// scale is a candidate, so the odd number carries no information about where it sits.
        /// </summary>
        private void AssignNumbers(int minimumGap)
        {
            _crewNumber = Session.Random.Range(ScaleMin, ScaleMax + 1);

            var candidates = new List<int>();
            for (var n = ScaleMin; n <= ScaleMax; n++)
                if (Math.Abs(n - _crewNumber) >= minimumGap) candidates.Add(n);

            if (candidates.Count == 0)
            {
                // The scale is too small for the requested gap: fall back to the far end.
                _imposterNumber = _crewNumber <= (ScaleMin + ScaleMax) / 2 ? ScaleMax : ScaleMin;
                return;
            }
            _imposterNumber = candidates[Session.Random.Range(0, candidates.Count)];
        }

        protected override void BuildReveal()
        {
            var imposters = Imposters;
            var reveal = new RevealStep
            {
                PhaseLabel = "Reveal",
                Title = "The numbers",
                Headline = imposters.Count == 1
                    ? NameOf(imposters[0].Id) + " was on " + _imposterNumber
                    : JoinNames(imposters) + " were on " + _imposterNumber,
                Accent = ImposterWasCaught ? StepAccent.Success : StepAccent.Danger
            };

            reveal.Entries.Add(new RevealEntry
            {
                Title = "Everyone else had",
                Detail = _crewNumber + " out of 10",
                Accent = StepAccent.Primary,
                Highlight = true
            });
            reveal.Entries.Add(new RevealEntry
            {
                Title = "The odd number was",
                Detail = _imposterNumber + " out of 10",
                Accent = StepAccent.Danger,
                Highlight = true
            });

            // With typed answers this is the full record of who said what, odd answers marked.
            var odd = imposters.Select(p => p.Id).ToHashSet();
            foreach (var question in _questions)
            {
                var lines = new List<string>();
                var scale = ScaleText(question);
                if (scale.Length > 0) lines.Add(scale);
                foreach (var player in AnswerersFor(question))
                    if (question.Answers.TryGetValue(player.Id, out var answer))
                        lines.Add(player.DisplayName + ": " + answer + (odd.Contains(player.Id) ? "  (odd number)" : string.Empty));

                reveal.Entries.Add(new RevealEntry
                {
                    Title = question.Asker.DisplayName + ": " + question.Prompt,
                    Detail = string.Join("\n", lines)
                });
            }

            AppendVoteEntries(reveal);
            Enqueue(reveal);
        }
    }
}
