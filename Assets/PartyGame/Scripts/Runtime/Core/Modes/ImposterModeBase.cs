using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PartyGame.Core.Services;
using PartyGame.Core.Session;

namespace PartyGame.Core.Modes
{
    /// <summary>
    /// Points awarded by the hidden-role modes. Kept in one object so the numbers can be
    /// tuned (or later driven by a balance asset) without touching flow or UI code.
    /// </summary>
    public class ImposterScoring
    {
        public int CorrectAccusation { get; set; } = 100;
        public int CrewCaughtImposter { get; set; } = 150;
        public int ImposterSurvived { get; set; } = 250;
        public int ImposterCaught { get; set; }
        public int WrongAccusationPenalty { get; set; }
    }

    /// <summary>
    /// Shared behaviour for the four hidden-role games: pick imposters, run one secret
    /// ballot per player, resolve it, then reveal and score. Each mode only supplies its
    /// own secret information and reveal detail.
    /// </summary>
    public abstract class ImposterModeBase : GameModeBase
    {
        private readonly HashSet<string> _voteStepIds = new HashSet<string>();
        private bool _revealBuilt;
        private bool _revoteUsed;

        protected VotingManager Voting { get; private set; }
        protected VoteOutcome Outcome { get; private set; }
        protected ImposterScoring Scoring { get; set; } = new ImposterScoring();

        protected bool VotingEnabled => Session.Settings.GetBool(CommonSettingKeys.VotingEnabled, true);

        protected TieResolution TieBehaviour
        {
            get
            {
                switch (Session.Settings.GetString(CommonSettingKeys.TieBehaviour, "noResult"))
                {
                    case "revote": return TieResolution.Revote;
                    case "random": return TieResolution.Random;
                    default: return TieResolution.NoResult;
                }
            }
        }

        protected static IEnumerable<SettingOption> TieOptions()
        {
            return new[]
            {
                new SettingOption("noResult", "Nobody caught", "A tie means the round ends with no accusation"),
                new SettingOption("revote", "Re-vote", "Tied players go to a second ballot"),
                new SettingOption("random", "Coin flip", "One of the tied players is picked at random")
            };
        }

        protected override void OnRoundStarting()
        {
            _voteStepIds.Clear();
            _revealBuilt = false;
            _revoteUsed = false;
            Voting = null;
            Outcome = null;
        }

        /// <summary>Marks <paramref name="count"/> random active players as imposters, everyone else crew.</summary>
        protected List<PlayerState> AssignImposters(int count)
        {
            var active = Session.ActivePlayers.ToList();
            foreach (var player in active) player.RoleId = PlayerRoles.Crew;

            var maxImposters = System.Math.Max(1, (active.Count - 1) / 2);
            count = System.Math.Min(System.Math.Max(1, count), maxImposters);

            var chosen = Shuffler.PickDistinct(active, count, Session.Random);
            foreach (var player in chosen) player.RoleId = PlayerRoles.Imposter;
            return chosen;
        }

        protected List<PlayerState> Imposters => PlayersWithRole(PlayerRoles.Imposter);

        // ---------------------------------------------------------------- voting

        /// <summary>One private ballot per active player, self-votes disabled.</summary>
        protected void BuildVotingPhase(string prompt, string phaseLabel = "Voting")
        {
            if (!VotingEnabled) return;

            var voters = Session.ActivePlayerIds;
            Voting = new VotingManager(voters, voters, false, TieBehaviour);
            EnqueueBallot(Voting, prompt, phaseLabel);
        }

        private void EnqueueBallot(VotingManager voting, string prompt, string phaseLabel)
        {
            var steps = new List<GameStep>();
            foreach (var voterId in voting.Voters)
            {
                var step = new ChoiceStep
                {
                    PhaseLabel = phaseLabel,
                    Title = NameOf(voterId) + ", cast your vote",
                    Body = prompt,
                    Prompt = "Tap a player, then confirm",
                    ActorPlayerId = voterId,
                    IsPrivate = true,
                    Accent = StepAccent.Warning,
                    ContinueLabel = "Lock in vote"
                };
                foreach (var candidateId in voting.CandidatesFor(voterId))
                {
                    step.Options.Add(new ChoiceOption(
                        candidateId.ToString(CultureInfo.InvariantCulture),
                        NameOf(candidateId)));
                }
                Enqueue(step);
                _voteStepIds.Add(step.Id);
                steps.Add(step);
            }
            ApplySequence(steps);
        }

        protected override void HandleResult(GameStep step, StepResult result)
        {
            if (step != null && _voteStepIds.Contains(step.Id) && Voting != null)
            {
                if (int.TryParse(result.OptionId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var candidateId)
                    && step.ActorPlayerId.HasValue)
                {
                    Voting.CastVote(step.ActorPlayerId.Value, candidateId);
                    var voter = Session.PlayerOf(step.ActorPlayerId.Value);
                    voter?.SetRoundData(RoundDataKeys.Vote, result.OptionId);
                }
            }

            OnStepHandled(step, result);
        }

        protected virtual void OnStepHandled(GameStep step, StepResult result)
        {
        }

        protected override void OnStepsDrained()
        {
            if (_revealBuilt) return;

            if (Voting != null)
            {
                Outcome = Voting.Resolve(Session.Random);

                if (Outcome.NeedsRevote && !_revoteUsed && Outcome.TopCandidates.Count > 1)
                {
                    _revoteUsed = true;
                    var tiedNames = string.Join(", ", Outcome.TopCandidates.Select(NameOf));
                    Enqueue(new MessageStep
                    {
                        PhaseLabel = "Voting",
                        Title = "It is a tie",
                        Body = tiedNames + " are tied. Everyone votes again between them.",
                        Accent = StepAccent.Warning,
                        ContinueLabel = "Start re-vote"
                    });
                    Voting = Voting.CreateRevote(Outcome.TopCandidates);
                    EnqueueBallot(Voting, "Only the tied players can be chosen.", "Re-vote");
                    return;
                }
            }

            _revealBuilt = true;

            // Elimination is part of the rules and always runs. Scoring and the leaderboard are
            // presentation, and an unscored mode must not show either.
            OnVoteResolved();
            if (UsesScoring) ApplyScoring();
            BuildReveal();
            if (UsesScoring) Enqueue(BuildScoreboard(Session.IsFinalRound));
        }

        /// <summary>
        /// Called once the ballot has been decided, before anything is scored or shown. Modes
        /// that remove players do it here so it still happens when scoring is switched off.
        /// </summary>
        protected virtual void OnVoteResolved()
        {
        }

        /// <summary>Default hidden-role scoring. Modes may override for their own twist.</summary>
        protected virtual void ApplyScoring()
        {
            Session.Scores.RegisterAll(Session.Players.Select(p => p.Id));
            if (Voting == null || Outcome == null) return;

            var imposterIds = Imposters.Select(p => p.Id).ToHashSet();
            var caught = Outcome.HasWinner && imposterIds.Contains(Outcome.WinnerId);

            foreach (var voterId in Voting.Voters)
            {
                var target = Voting.VoteOf(voterId);
                if (target == VoteOutcome.NoWinner) continue;
                if (imposterIds.Contains(target))
                {
                    Session.Scores.AddPoints(voterId, Scoring.CorrectAccusation, "Voted for an imposter");
                }
                else if (Scoring.WrongAccusationPenalty > 0)
                {
                    Session.Scores.RemovePoints(voterId, Scoring.WrongAccusationPenalty, "Accused the wrong player");
                }
            }

            if (caught)
            {
                foreach (var player in Session.ActivePlayers.Where(p => !imposterIds.Contains(p.Id)))
                    Session.Scores.AddPoints(player.Id, Scoring.CrewCaughtImposter, "The group caught an imposter");
                if (Scoring.ImposterCaught != 0)
                    foreach (var id in imposterIds)
                        Session.Scores.AddPoints(id, Scoring.ImposterCaught, "Caught");
            }
            else
            {
                foreach (var id in imposterIds)
                    Session.Scores.AddPoints(id, Scoring.ImposterSurvived, "Survived the vote");
            }
        }

        /// <summary>Modes append their own detail, then call <see cref="AppendVoteEntries"/>.</summary>
        protected abstract void BuildReveal();

        protected void AppendVoteEntries(RevealStep reveal)
        {
            if (Voting == null || Outcome == null)
            {
                reveal.Entries.Add(new RevealEntry { Title = "No vote this round", Detail = "Voting is switched off in the settings." });
                return;
            }

            foreach (var tally in Outcome.Tallies.Where(t => t.Votes > 0))
            {
                var voters = Voting.VotersFor(tally.CandidateId).Select(NameOf);
                reveal.Entries.Add(new RevealEntry
                {
                    Title = NameOf(tally.CandidateId) + " - " + tally.Votes + (tally.Votes == 1 ? " vote" : " votes"),
                    Detail = string.Join(", ", voters),
                    Accent = Outcome.HasWinner && Outcome.WinnerId == tally.CandidateId ? StepAccent.Warning : StepAccent.Neutral,
                    Highlight = Outcome.HasWinner && Outcome.WinnerId == tally.CandidateId
                });
            }

            if (!Outcome.HasWinner)
            {
                reveal.Entries.Add(new RevealEntry
                {
                    Title = Outcome.WasTie ? "Tied vote" : "No accusation",
                    Detail = Outcome.WasTie ? "Nobody was accused this round." : "No votes were cast.",
                    Accent = StepAccent.Neutral
                });
            }
        }

        protected bool ImposterWasCaught
        {
            get
            {
                if (Outcome == null || !Outcome.HasWinner) return false;
                var player = Session.PlayerOf(Outcome.WinnerId);
                return player != null && player.RoleId == PlayerRoles.Imposter;
            }
        }
    }
}
