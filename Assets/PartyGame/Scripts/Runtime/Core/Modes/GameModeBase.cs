using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PartyGame.Core.Content;
using PartyGame.Core.Services;
using PartyGame.Core.Session;
using PartyGame.Core.Util;

namespace PartyGame.Core.Modes
{
    /// <summary>
    /// Shared plumbing for every mode: an ordered step list, result dispatch and the
    /// reveal / scoreboard builders. Concrete modes only describe their own round.
    /// </summary>
    public abstract class GameModeBase : IGameMode
    {
        private readonly List<GameStep> _steps = new List<GameStep>();
        private int _index;
        private int _stepCounter;

        protected GameSession Session { get; private set; }

        public abstract GameModeId Id { get; }

        public virtual bool UsesScoring => true;

        public GameStep Current => _index >= 0 && _index < _steps.Count ? _steps[_index] : null;

        public bool IsRoundComplete { get; private set; }

        public IReadOnlyList<GameStep> Steps => _steps;

        public int StepIndex => _index;

        public virtual IReadOnlyList<SettingDefinition> GetSettingDefinitions(ContentService content)
        {
            return new List<SettingDefinition>();
        }

        public virtual ValidationResult Validate(GameSession session)
        {
            if (session == null) return ValidationResult.Fail("No session.");
            var definition = session.Definition;
            var min = definition != null ? definition.MinPlayers : PlayerRoster.AbsoluteMinPlayers;
            var max = definition != null ? definition.MaxPlayers : PlayerRoster.AbsoluteMaxPlayers;
            return session.Roster.Validate(min, max);
        }

        public void Initialise(GameSession session)
        {
            Session = session;
            OnInitialise();
        }

        protected virtual void OnInitialise()
        {
        }

        public void StartRound()
        {
            _steps.Clear();
            _index = 0;
            _stepCounter = 0;
            IsRoundComplete = false;

            if (Session == null)
            {
                IsRoundComplete = true;
                return;
            }

            Session.Scores.BeginRound();
            Session.ClearRoundData();
            OnRoundStarting();
            BuildRound();

            if (_steps.Count == 0) IsRoundComplete = true;
        }

        protected virtual void OnRoundStarting()
        {
        }

        /// <summary>Fills the step list for this round.</summary>
        protected abstract void BuildRound();

        public void Submit(StepResult result)
        {
            var current = Current;
            if (current == null)
            {
                IsRoundComplete = true;
                return;
            }

            HandleResult(current, result ?? StepResult.Acknowledged(current));
            _index++;

            if (_index < _steps.Count) return;

            OnStepsDrained();
            if (_index >= _steps.Count) IsRoundComplete = true;
        }

        protected virtual void HandleResult(GameStep step, StepResult result)
        {
        }

        /// <summary>Last chance for a mode to append more steps (revote, extra reveal...).</summary>
        protected virtual void OnStepsDrained()
        {
        }

        public virtual RoundSummary CompleteRound()
        {
            return new RoundSummary
            {
                RoundNumber = Session != null ? Session.RoundNumber : 0,
                GameOver = Session != null && Session.IsFinalRound
            };
        }

        public virtual IReadOnlyList<string> BuildRulesSummary(GameSettings settings)
        {
            return new List<string>();
        }

        // ---------------------------------------------------------------- queue helpers

        protected T Enqueue<T>(T step) where T : GameStep
        {
            if (step == null) return null;
            if (string.IsNullOrEmpty(step.Id))
            {
                _stepCounter++;
                step.Id = Id + "-" + _stepCounter.ToString(CultureInfo.InvariantCulture);
            }
            _steps.Add(step);
            return step;
        }

        protected void EnqueueRange(IEnumerable<GameStep> steps)
        {
            if (steps == null) return;
            foreach (var step in steps) Enqueue(step);
        }

        /// <summary>Inserts directly after the step being handled. Used for follow-up prompts.</summary>
        protected T InsertNext<T>(T step) where T : GameStep
        {
            if (step == null) return null;
            if (string.IsNullOrEmpty(step.Id))
            {
                _stepCounter++;
                step.Id = Id + "-ins-" + _stepCounter.ToString(CultureInfo.InvariantCulture);
            }
            var at = System.Math.Min(_index + 1, _steps.Count);
            _steps.Insert(at, step);
            return step;
        }

        protected static void ApplySequence(IReadOnlyList<GameStep> steps)
        {
            if (steps == null) return;
            for (var i = 0; i < steps.Count; i++)
            {
                steps[i].SequenceIndex = i + 1;
                steps[i].SequenceCount = steps.Count;
            }
        }

        // ---------------------------------------------------------------- shared builders

        /// <summary>Players in seat order, starting from a rotating offset so the same person
        /// is not always first to receive the phone.</summary>
        protected List<PlayerState> TurnOrder()
        {
            var players = Session.ActivePlayers.OrderBy(p => p.SeatIndex).ToList();
            if (players.Count == 0) return players;
            var offset = (Session.RoundNumber - 1) % players.Count;
            return players.Skip(offset).Concat(players.Take(offset)).ToList();
        }

        protected DiscussionStep BuildDiscussion(string title, string body, float seconds, params string[] bullets)
        {
            var step = new DiscussionStep
            {
                PhaseLabel = "Discussion",
                Title = title,
                Body = body,
                Seconds = seconds,
                Accent = StepAccent.Primary
            };
            if (bullets != null) step.Bullets.AddRange(bullets.Where(b => !string.IsNullOrEmpty(b)));
            return step;
        }

        protected ScoreboardStep BuildScoreboard(bool isFinal)
        {
            var step = new ScoreboardStep
            {
                PhaseLabel = isFinal ? "Final scores" : "Scores",
                Title = isFinal ? "Final standings" : "Scores after " + Session.RoundLabel.ToLowerInvariant(),
                IsFinal = isFinal,
                ContinueLabel = isFinal ? "Finish" : "Next round",
                Accent = StepAccent.Success
            };

            foreach (var entry in Session.Scores.GetLeaderboard())
            {
                var player = Session.PlayerOf(entry.PlayerId);
                if (player == null) continue;
                step.Rows.Add(new ScoreRow
                {
                    PlayerId = entry.PlayerId,
                    Name = player.DisplayName,
                    Total = entry.Score,
                    Delta = Session.Scores.RoundTotalFor(entry.PlayerId),
                    Rank = entry.Rank,
                    Note = player.IsAlive ? string.Empty : "Out"
                });
            }
            return step;
        }

        protected string NameOf(int playerId) => Session.NameOf(playerId);

        protected List<PlayerState> PlayersWithRole(string roleId)
        {
            return Session.Players.Where(p => p.RoleId == roleId).ToList();
        }

        protected string JoinNames(IEnumerable<PlayerState> players)
        {
            var names = players.Select(p => p.DisplayName).ToList();
            if (names.Count == 0) return "nobody";
            if (names.Count == 1) return names[0];
            return string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];
        }
    }
}
