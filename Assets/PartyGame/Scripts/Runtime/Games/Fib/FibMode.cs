using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PartyGame.Core.Content;
using PartyGame.Core.Modes;
using PartyGame.Core.Services;
using PartyGame.Core.Session;
using PartyGame.Core.Util;

namespace PartyGame.Games.Fib
{
    /// <summary>
    /// All the point values for Fib in one place so they can be tuned without touching
    /// flow or UI code.
    /// </summary>
    public class FibScoring
    {
        public int CorrectGuess { get; set; } = 1000;
        public int FakeAnswerPicked { get; set; } = 500;
        public int WroteTheRealAnswer { get; set; } = 750;
    }

    /// <summary>One entry on the guessing list.</summary>
    internal class FibAnswer
    {
        public string Id = string.Empty;
        public string Text = string.Empty;
        public bool IsTruth;
        public int AuthorId = -1;
        public readonly List<int> PickedBy = new List<int>();
    }

    /// <summary>
    /// A bluffing game: everyone invents a believable answer to a real question, then the
    /// group tries to spot the genuine one among the fakes.
    /// </summary>
    public class FibMode : GameModeBase
    {
        private const string TruthId = "truth";
        private const string AnswerKeyPrefix = "fib-answer-";
        private const string GuessKeyPrefix = "fib-guess-";

        private readonly ContentRotation<TriviaQuestion> _questionRotation = new ContentRotation<TriviaQuestion>(q => q.Id, 24);
        private readonly List<FibAnswer> _answers = new List<FibAnswer>();

        private TriviaQuestion _question;
        private TriviaPackData _pack;
        private int _phase;

        public FibScoring Scoring { get; set; } = new FibScoring();

        public override GameModeId Id => GameModeId.Fib;

        public override IReadOnlyList<SettingDefinition> GetSettingDefinitions(ContentService content)
        {
            return new List<SettingDefinition>
            {
                SettingDefinition.CategoryPicker(CommonSettingKeys.Categories, "Question packs",
                    "Pick one, several, or leave empty for all"),
                SettingDefinition.Stepper(CommonSettingKeys.Rounds, "Rounds", 4, 1, 12),
                SettingDefinition.Stepper("answerSeconds", "Writing time", 0, 0, 120, 15, "s",
                    "0 means no timer while writing"),
                SettingDefinition.Toggle("showAuthors", "Reveal who wrote what", true,
                    "Show the author of every fake answer at the end")
            };
        }

        public override ValidationResult Validate(GameSession session)
        {
            var baseResult = base.Validate(session);
            if (!baseResult.IsValid) return baseResult;

            var packs = session.Content.ResolveSelection<TriviaPackData>(
                GameModeId.Fib, session.Settings.GetList(CommonSettingKeys.Categories));
            var questions = packs.SelectMany(p => p.Questions).Count(q => q != null && q.IsValid);
            if (questions == 0) return ValidationResult.Fail("No questions are available in the selected packs.");
            return ValidationResult.Ok();
        }

        public override IReadOnlyList<string> BuildRulesSummary(GameSettings settings)
        {
            return new List<string>
            {
                "A real question appears with a real answer nobody has seen.",
                "Pass the phone round: everyone secretly writes a believable fake answer.",
                "All the answers are shuffled together with the real one.",
                "Pass the phone again and pick the answer you think is true.",
                "Score " + Scoring.CorrectGuess + " for finding the truth, " + Scoring.FakeAnswerPicked + " every time someone falls for your lie."
            };
        }

        protected override void OnRoundStarting()
        {
            _answers.Clear();
            _phase = 0;
        }

        protected override void BuildRound()
        {
            var packs = Session.Content.ResolveSelection<TriviaPackData>(
                GameModeId.Fib, Session.Settings.GetList(CommonSettingKeys.Categories));

            var pool = packs.SelectMany(p => p.Questions.Where(q => q != null && q.IsValid).Select(q => new { Pack = p, Question = q })).ToList();
            if (pool.Count == 0)
            {
                Enqueue(new MessageStep
                {
                    Title = "No questions",
                    Body = "The selected question packs are empty.",
                    Accent = StepAccent.Danger,
                    ContinueLabel = "Back"
                });
                return;
            }

            var questions = pool.Select(x => x.Question).ToList();
            _question = _questionRotation.Next(questions, Session.Random);
            var owner = pool.FirstOrDefault(x => x.Question.Id == _question.Id);
            _pack = owner != null ? owner.Pack : packs.FirstOrDefault();

            _answers.Add(new FibAnswer
            {
                Id = TruthId,
                Text = TextUtility.TidyAnswer(_question.CorrectAnswer),
                IsTruth = true
            });

            Enqueue(new MessageStep
            {
                PhaseLabel = Session.RoundLabel,
                Title = "The question",
                Feature = _question.Question,
                Body = "Pass the phone around. Everyone writes an answer that sounds true - but is not.",
                Accent = StepAccent.Primary,
                ContinueLabel = "Start writing"
            });

            var steps = new List<GameStep>();
            foreach (var player in TurnOrder())
                steps.Add(Enqueue(BuildAnswerStep(player)));
            ApplySequence(steps);
        }

        private TextInputStep BuildAnswerStep(PlayerState player)
        {
            var step = new TextInputStep
            {
                PhaseLabel = "Write a lie",
                Title = player.DisplayName,
                Body = _question.Question,
                Prompt = "Write an answer that could pass for the truth",
                Placeholder = "Your believable lie",
                ActorPlayerId = player.Id,
                Accent = StepAccent.Primary,
                MaxLength = 42,
                ContinueLabel = "Lock it in",
                Id = AnswerKeyPrefix + player.Id.ToString(CultureInfo.InvariantCulture) + "-" + _answers.Count
            };
            step.RejectedValues.AddRange(_answers.Select(a => a.Text));
            step.RejectionMessage = "That one is taken. Think of another.";
            return step;
        }

        protected override void HandleResult(GameStep step, StepResult result)
        {
            if (step == null) return;

            if (step.Id.StartsWith(AnswerKeyPrefix, System.StringComparison.Ordinal) && step.ActorPlayerId.HasValue)
            {
                HandleWrittenAnswer(step, result);
                return;
            }

            if (step.Id.StartsWith(GuessKeyPrefix, System.StringComparison.Ordinal) && step.ActorPlayerId.HasValue)
            {
                var answer = _answers.FirstOrDefault(a => a.Id == result.OptionId);
                if (answer == null) return;
                answer.PickedBy.Add(step.ActorPlayerId.Value);
                Session.PlayerOf(step.ActorPlayerId.Value)?.SetRoundData(RoundDataKeys.Guess, answer.Id);
            }
        }

        private void HandleWrittenAnswer(GameStep step, StepResult result)
        {
            var playerId = step.ActorPlayerId.Value;
            var player = Session.PlayerOf(playerId);
            var text = TextUtility.TidyAnswer(result.Text);

            if (string.IsNullOrWhiteSpace(text))
            {
                // Empty submissions are replaced with a harmless placeholder rather than
                // breaking the round.
                text = "Pass";
            }

            if (TextUtility.Matches(text, _question.CorrectAnswer))
            {
                // Guessing the real answer while writing is worth points, but they still
                // have to invent a lie, so the answer list stays the right length.
                Session.Scores.AddPoints(playerId, Scoring.WroteTheRealAnswer, "Knew the real answer");
                var retry = BuildAnswerStep(player);
                retry.Id = AnswerKeyPrefix + playerId.ToString(CultureInfo.InvariantCulture) + "-retry-" + _answers.Count;
                retry.Title = player.DisplayName + ", nice one";
                retry.Prompt = "That is the real answer. Have " + Scoring.WroteTheRealAnswer + " points - now write a lie instead";
                retry.Accent = StepAccent.Success;
                retry.RejectedValues.Add(_question.CorrectAnswer);
                InsertNext(retry);
                return;
            }

            if (_answers.Any(a => TextUtility.Matches(a.Text, text)))
            {
                var retry = BuildAnswerStep(player);
                retry.Id = AnswerKeyPrefix + playerId.ToString(CultureInfo.InvariantCulture) + "-dup-" + _answers.Count;
                retry.Prompt = "Someone already wrote that. Try a different one";
                retry.Accent = StepAccent.Warning;
                InsertNext(retry);
                return;
            }

            _answers.Add(new FibAnswer { Id = "p" + playerId, Text = text, AuthorId = playerId });
            player?.SetRoundData(RoundDataKeys.WrittenAnswer, text);
        }

        protected override void OnStepsDrained()
        {
            if (_phase == 0)
            {
                _phase = 1;
                BuildGuessingPhase();
                return;
            }

            if (_phase == 1)
            {
                _phase = 2;
                ApplyScoring();
                BuildRevealPhase();
            }
        }

        private void BuildGuessingPhase()
        {
            Shuffler.ShuffleInPlace(_answers, Session.Random);

            Enqueue(new MessageStep
            {
                PhaseLabel = "Guessing",
                Title = "One of these is true",
                Feature = _question.Question,
                Body = "Pass the phone around again. Pick the answer you believe is the real one - you will not see your own lie.",
                Accent = StepAccent.Warning,
                ContinueLabel = "Start guessing"
            });

            var steps = new List<GameStep>();
            foreach (var player in TurnOrder())
            {
                var step = new ChoiceStep
                {
                    PhaseLabel = "Guessing",
                    Title = player.DisplayName,
                    Body = _question.Question,
                    Prompt = "Which one is the truth?",
                    ActorPlayerId = player.Id,
                    Accent = StepAccent.Warning,
                    ContinueLabel = "Lock in answer",
                    Id = GuessKeyPrefix + player.Id.ToString(CultureInfo.InvariantCulture)
                };

                foreach (var answer in _answers.Where(a => a.AuthorId != player.Id))
                    step.Options.Add(new ChoiceOption(answer.Id, answer.Text));

                if (step.Options.Count == 0)
                    step.Options.Add(new ChoiceOption(TruthId, _answers.First(a => a.IsTruth).Text));

                steps.Add(Enqueue(step));
            }
            ApplySequence(steps);
        }

        private void ApplyScoring()
        {
            Session.Scores.RegisterAll(Session.Players.Select(p => p.Id));

            foreach (var answer in _answers)
            {
                if (answer.IsTruth)
                {
                    foreach (var playerId in answer.PickedBy)
                        Session.Scores.AddPoints(playerId, Scoring.CorrectGuess, "Found the real answer");
                }
                else if (answer.AuthorId >= 0)
                {
                    foreach (var _ in answer.PickedBy)
                        Session.Scores.AddPoints(answer.AuthorId, Scoring.FakeAnswerPicked, "Someone fell for the lie");
                }
            }
        }

        private void BuildRevealPhase()
        {
            var showAuthors = Session.Settings.GetBool("showAuthors", true);
            var truth = _answers.First(a => a.IsTruth);

            var reveal = new RevealStep
            {
                PhaseLabel = "Reveal",
                Title = _question.Question,
                Headline = truth.Text,
                Accent = StepAccent.Success
            };

            reveal.Entries.Add(new RevealEntry
            {
                Title = "The real answer",
                Detail = truth.Text + (truth.PickedBy.Count > 0
                    ? "  -  found by " + string.Join(", ", truth.PickedBy.Select(NameOf))
                    : "  -  nobody found it"),
                Accent = StepAccent.Success,
                Highlight = true
            });

            foreach (var answer in _answers.Where(a => !a.IsTruth).OrderByDescending(a => a.PickedBy.Count))
            {
                var author = showAuthors ? NameOf(answer.AuthorId) : "A player";
                var fooled = answer.PickedBy.Count == 0
                    ? "fooled nobody"
                    : "fooled " + string.Join(", ", answer.PickedBy.Select(NameOf));
                reveal.Entries.Add(new RevealEntry
                {
                    Title = answer.Text,
                    Detail = author + " - " + fooled,
                    Accent = answer.PickedBy.Count > 0 ? StepAccent.Danger : StepAccent.Neutral,
                    Highlight = answer.PickedBy.Count > 0
                });
            }

            if (_pack != null)
                reveal.Entries.Add(new RevealEntry { Title = "Pack", Detail = _pack.DisplayName });

            Enqueue(reveal);
            Enqueue(BuildScoreboard(Session.IsFinalRound));
        }
    }
}
